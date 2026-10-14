import os
import json
import pika
import re
import pandas as pd
import glob
import uuid # Bunu en üste importların arasına eklemeyi unutma!
import string

# RabbitMQ Ayarları
RABBITMQ_HOST = 'localhost'
QUEUE_NAME = 'RawDecisionsQueue'
DATA_DIR = '/home/egemen/data' # Senin Parquet dosyalarının olduğu klasör

def sanitize_text(text):
    if not isinstance(text, str):
        return ""
    # Sadece okunabilir harfleri, boşlukları ve satır atlamalarını tutar.
    # Görünmez/bozuk/terminal çökerten (NULL vb.) karakterleri yok eder!
    return "".join(ch for ch in text if ch.isprintable() or ch in ['\n', '\r', '\t'])

def parse_decision_text(content):
    chamber = "Bilinmeyen_Daire"
    
    # 1. Daire Bulma (Biraz daha esnek)
    chamber_match = re.search(r'(\d+\.\s*[a-zA-ZçğıöşüÇĞİÖŞÜ\s]+\s*Dairesi)', content, re.IGNORECASE)
    if chamber_match: 
        chamber = chamber_match.group(1).strip().replace(" ", "_")

    # 2. Esas Bulma (Çok yönlü tarama)
    # Yakaladıkları: "Esas No: 2023/1", "E: 2023/1", "E. 2023 / 1"
    case_match = re.search(r'(?:Esas|E\.)\s*(?:No:?)?\s*[:\.]?\s*(\d{4}\s*/\s*\d+)', content, re.IGNORECASE)
    if not case_match:
        # Yakaladıkları (Ters format): "2023/1 E.", "2023/1 Esas"
        case_match = re.search(r'(\d{4}\s*/\s*\d+)\s*(?:Esas|E\.)', content, re.IGNORECASE)
        
    if case_match: 
        case_no = case_match.group(1).replace(" ", "").replace("/", "_")
    else:
        case_no = f"EsasBulunamadi_{uuid.uuid4().hex[:6]}"

    # 3. Karar Bulma (Çok yönlü tarama)
    # Yakaladıkları: "Karar No: 2023/1", "K: 2023/1", "K. 2023 / 1"
    decision_match = re.search(r'(?:Karar|K\.)\s*(?:No:?)?\s*[:\.]?\s*(\d{4}\s*/\s*\d+)', content, re.IGNORECASE)
    if not decision_match:
        # Yakaladıkları (Ters format): "2023/1 K.", "2023/1 Karar"
        decision_match = re.search(r'(\d{4}\s*/\s*\d+)\s*(?:Karar|K\.)', content, re.IGNORECASE)
        
    if decision_match: 
        decision_no = decision_match.group(1).replace(" ", "").replace("/", "_")
    else:
        decision_no = f"KararBulunamadi_{uuid.uuid4().hex[:6]}"
    
    date_str = "2000-01-01" 
    
    return chamber, case_no, decision_no, date_str

def main():
    print("🐰 RabbitMQ'ya bağlanılıyor...")
    credentials = pika.PlainCredentials('lexai_admin', 'lexai_secret_123')
    
    # İŞTE BURASI: credentials'ı parametrelere ekliyoruz!
    connection = pika.BlockingConnection(
        pika.ConnectionParameters(
            host='localhost',
            port=5672,
            virtual_host='/',
            credentials=credentials, # <--- BU SATIRI EKLE
            heartbeat=600,
            blocked_connection_timeout=300
        )
    )
    channel = connection.channel()
    
    # Kuyruğu garantile
    channel.queue_declare(queue=QUEUE_NAME, durable=True)
    print(f"✅ '{QUEUE_NAME}' kuyruğu hazır. Parquet dosyaları taranıyor...\n")

    # Klasördeki tüm .parquet dosyalarını bul
    parquet_files = glob.glob(os.path.join(DATA_DIR, '*.parquet'))
    
    if not parquet_files:
        print("❌ HATA: Belirtilen klasörde hiç .parquet dosyası bulunamadı!")
        return

    count = 0

    for filepath in parquet_files:
        print(f"📦 Dosya açılıyor: {os.path.basename(filepath)}")
        
        # Parquet dosyasını Pandas ile RAM'e al
        df = pd.read_parquet(filepath)
        
        # Karar metninin hangi kolonda olduğunu tahmin et
        text_col = None
        for col in ['text', 'icerik', 'karar', 'content', 'metin']:
            if col in df.columns:
                text_col = col
                break
        
        if not text_col:
            # Tahmin edemezsek ilk kolonu metin kabul et
            text_col = df.columns[0]
            
        print(f"🔍 Karar metinleri '{text_col}' kolonundan okunuyor...")

        # Satır satır oku ve RabbitMQ'ya fırlat
        for index, row in df.iterrows():
            raw_content = str(row[text_col])
            content = sanitize_text(raw_content)
            
            # Eğer satır boşsa atla
            if not content or content.strip() == "" or content == "nan": 
                continue
                
            chamber, case_no, decision_no, date_str = parse_decision_text(content)

            payload = {
                "ChamberCode": chamber,
                "CaseNumber": case_no,
                "DecisionNumber": decision_no,
                "DecisionDate": date_str,
                "FullText": content,
                "Summary": None
            }

            # JSON'a çevir ve kuyruğa fırlat!
            channel.basic_publish(
                exchange='',
                routing_key=QUEUE_NAME,
                body=json.dumps(payload),
                properties=pika.BasicProperties(
                    delivery_mode=2 # pika.DeliveryMode.Persistent (Mesajlar diskte tutulsun)
                )
            )
            count += 1
            
            # Hızdan dolayı terminal çökmesin diye her 1000'de bir bilgi ver
            if count % 1000 == 0:
                print(f"🚀 {count} karar RabbitMQ'ya fırlatıldı...")

    connection.close()
    print(f"\n🎉 Toplam {count} karar RabbitMQ kuyruğuna ateşlendi!")

if __name__ == "__main__":
    main()
