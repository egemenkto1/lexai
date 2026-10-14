namespace Lex.Modules.Decisions.Domain.ValueObjects;

// --- BÜYÜK VERİ İÇİN YENİ EKLEDİĞİMİZ KATEGORİ ---
public enum ChamberCategory
{
    Bilinmeyen = 0,
    Hukuk = 1,
    Ceza = 2,
    HukukGenelKurulu = 3,
    CezaGenelKurulu = 4
}

// --- SENİN ORİJİNAL KODUNUN İHTİYAÇ DUYDUĞU VE BİZİM YANLIŞLIKLA SİLDİĞİMİZ ENUMLAR ---
public enum CourtType
{
    Yargitay = 1,
    Danistay = 2,
    AnayasaMahkemesi = 3,
    IlkDerece = 4,
    BolgeAdliye = 5
}

public enum DecisionOutcome
{
    Unknown = 0,
    Onama = 1,
    Bozma = 2,
    DegistirerekOnama = 3,
    KismenBozma = 4,
    Red = 5
}

public enum EmbeddingStatus
{
    Pending = 0,
    Processing = 1,
    Done = 2,
    Failed = 3
}

public enum ChunkType
{
    FullText = 1,
    Summary = 2,
    Metadata = 3
}
