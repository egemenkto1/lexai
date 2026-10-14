using System;
using Lex.SharedKernel;
using Lex.Modules.Decisions.Domain.ValueObjects;
using Lex.Modules.Decisions.Domain.Decisions.Events;

namespace Lex.Modules.Decisions.Domain.Decisions;

public sealed class CourtDecision : AggregateRoot<DecisionId>
{
    // --- BÜYÜK VERİ HİYERARŞİSİ (YENİ EKLENENLER) ---
    public int DecisionYear { get; private set; }
    public ChamberCategory Category { get; private set; }
    public string ChamberName { get; private set; } = string.Empty;

    // --- TEMEL BİLGİLER ---
    public CourtType CourtType { get; private set; }
    public CaseIdentifier CaseIdentifier { get; private set; }
    public DateOnly DecisionDate { get; private set; }
    public ObjectStoragePath RawTextLocation { get; private set; }
    
    // --- AI & DURUM TAKİBİ ---
    public string? Summary { get; private set; }
    public DecisionOutcome? Outcome { get; private set; }
    public EmbeddingStatus EmbeddingStatus { get; private set; }
    public DateTimeOffset? IndexedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // EF Core Concurrency Token (PostgreSQL xmin için)
    public uint Version { get; private set; }

    private CourtDecision() { } 

    // Constructor'a ChamberName'i ekledik
    private CourtDecision(DecisionId id, CourtType courtType, string chamberName, CaseIdentifier caseIdentifier, DateOnly decisionDate, ObjectStoragePath rawTextLocation)
        : base(id)
    {
        CourtType = courtType;
        CaseIdentifier = caseIdentifier;
        ChamberName = chamberName;
        DecisionDate = decisionDate;
        RawTextLocation = rawTextLocation;
        
        // Hiyerarşi zekası: Yıl ve Kategori otomatik hesaplanıyor!
        DecisionYear = decisionDate.Year;
        Category = DetermineCategory(chamberName);

        EmbeddingStatus = EmbeddingStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Factory metoduna ChamberName parametresini ekledik
    public static CourtDecision Register(CourtType courtType, string chamberName, CaseIdentifier caseIdentifier, DateOnly decisionDate, ObjectStoragePath rawTextLocation)
    {
        var decision = new CourtDecision(new DecisionId(Guid.NewGuid()), courtType, chamberName, caseIdentifier, decisionDate, rawTextLocation);
        
        
	// Orijinal Create yapısına geri döndük
        decision.AddDomainEvent(CourtDecisionRegistered.Create(decision.Id));
        
        return decision;
    }

    // "12. Ceza Dairesi" metninden kategoriyi anlayan zeka
    private static ChamberCategory DetermineCategory(string chamber)
    {
        if (string.IsNullOrWhiteSpace(chamber)) return ChamberCategory.Bilinmeyen;
        
        var lower = chamber.ToLowerInvariant();
        if (lower.Contains("ceza genel")) return ChamberCategory.CezaGenelKurulu;
        if (lower.Contains("hukuk genel")) return ChamberCategory.HukukGenelKurulu;
        if (lower.Contains("ceza")) return ChamberCategory.Ceza;
        if (lower.Contains("hukuk")) return ChamberCategory.Hukuk;
        
        return ChamberCategory.Bilinmeyen;
    }

    // --- SIKI YÖNETİM: STATE MACHINE GUARD'LARI ---

    public Result MarkEmbeddingProcessing()
    {
        if (EmbeddingStatus != EmbeddingStatus.Pending && EmbeddingStatus != EmbeddingStatus.Failed)
            return Result.Failure(Error.Conflict("Decision.InvalidState", "Only Pending or Failed decisions can be processed."));

        EmbeddingStatus = EmbeddingStatus.Processing;
        FailureReason = null;
        Touch();
        
        return Result.Success();
    }

    public Result MarkEmbeddingCompleted(DateTimeOffset indexedAt)
    {
        if (EmbeddingStatus != EmbeddingStatus.Processing)
            return Result.Failure(Error.Conflict("Decision.InvalidState", "Only Processing decisions can be marked as completed."));

        EmbeddingStatus = EmbeddingStatus.Done;
        IndexedAt = indexedAt;
        FailureReason = null;
        Touch();
        
        // Senin orijinal Event yapına göre Create kullanımı
        AddDomainEvent(CourtDecisionEmbeddingCompleted.Create(Id));
        return Result.Success();
    }

    public Result MarkEmbeddingFailed(string reason)
    {
        if (EmbeddingStatus != EmbeddingStatus.Processing)
            return Result.Failure(Error.Conflict("Decision.InvalidState", "Only Processing decisions can be marked as failed."));

        EmbeddingStatus = EmbeddingStatus.Failed;
        FailureReason = reason;
        Touch();

        // Senin orijinal Event yapına göre Create kullanımı
        AddDomainEvent(CourtDecisionEmbeddingFailed.Create(Id, reason));
        return Result.Success();
    }

    public void AttachSummary(string summary)
    {
        Summary = summary;
        Touch();
    }

    public void SetOutcome(DecisionOutcome outcome)
    {
        Outcome = outcome;
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
