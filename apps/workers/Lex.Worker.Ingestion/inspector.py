import pandas as pd
import sys

# Dosya yolunu kendi klasörüne göre ayarla
file_path = "/home/egemen/data/train-00000-of-00009.parquet"

try:
    print(f"[{file_path}] okunuyor...")
    df = pd.read_parquet(file_path)
    
    print("\n=== KOLONLAR ===")
    print(df.columns.tolist())
    
    print("\n=== İLK SATIR ÖRNEĞİ ===")
    first_row = df.iloc[0].to_dict()
    for key, value in first_row.items():
        # Metin çok uzunsa kırpalım ki terminali boğmasın
        val_str = str(value)
        if len(val_str) > 100:
            val_str = val_str[:100] + " ... [KESİLDİ]"
        print(f"{key}: {val_str}")
        
    print(f"\nToplam Karar Sayısı (Bu Dosyada): {len(df)}")
    
except Exception as e:
    print(f"Hata oluştu: {e}")
