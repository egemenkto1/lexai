import json
import pika

def main():
    print("🚀 Test Füzesi Hazırlanıyor...")
    credentials = pika.PlainCredentials('lexai_admin', 'lexai_secret_123')
    connection = pika.BlockingConnection(pika.ConnectionParameters(
        host='localhost', 
        port=5672, 
        credentials=credentials
    ))
    channel = connection.channel()

    # Tam C#'ın beklediği, hatasız JSON
    payload = {
        "ChamberCode": "Yapay_Zeka_Test_Dairesi",
        "CaseNumber": "2037/999",
        "DecisionNumber": "2037/888",
        "DecisionDate": "2026-06-24",
        "FullText": "T.C. YARGITAY YAPAY ZEKA TEST DAİRESİ. Bu metin, sistemin uçtan uca yapay zeka (AI) entegrasyonunu, bge-m3 embedding modelini ve Qdrant vektör veritabanı bağlantılarını test etmek amacıyla enjekte edilmiştir. Altyapının kusursuz çalıştığı anlaşıldığından KABULÜNE karar verilmiştir.",
        "Summary": None
    }

    channel.basic_publish(
        exchange='',
        routing_key='RawDecisionsQueue',
        body=json.dumps(payload),
        properties=pika.BasicProperties(delivery_mode=2)
    )
    print("✅ Füze RabbitMQ'ya Başarıyla Fırlatıldı!")
    connection.close()

if __name__ == "__main__":
    main()
