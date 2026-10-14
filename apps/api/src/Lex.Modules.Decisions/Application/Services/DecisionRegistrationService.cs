using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR; // <-- POSTACININ KÜTÜPHANESİ EKLENDİ
using Lex.SharedKernel;
using Lex.Modules.Decisions.Domain.Decisions;
using Lex.Modules.Decisions.Domain.Repositories;
using Lex.Modules.Decisions.Domain.ValueObjects;

namespace Lex.Modules.Decisions.Application.Services;

public sealed class DecisionRegistrationService
{
    private readonly ICourtDecisionRepository _repository;
    private readonly IDecisionUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher; // <-- POSTACIYI TANIMLADIK

    // POSTACIYI CONSTRUCTOR'A ENJEKTE ETTİK
    public DecisionRegistrationService(
        ICourtDecisionRepository repository, 
        IDecisionUnitOfWork unitOfWork,
        IPublisher publisher) 
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result<DecisionId>> RegisterDecisionAsync(
        CourtType courtType,
        string chamberCode,
        string caseNumber,
        string decisionNumber,
        DateOnly decisionDate,
        string objectStorageKey,
        string? chamberName = null, 
        CancellationToken cancellationToken = default)
    {
        var caseIdentifier = new CaseIdentifier(chamberCode, caseNumber, decisionNumber);
        var storagePath = new ObjectStoragePath(objectStorageKey);

        string finalChamberName = string.IsNullOrWhiteSpace(chamberName) ? chamberCode : chamberName;

        var decision = CourtDecision.Register(
            courtType,
            finalChamberName, 
            caseIdentifier,
            decisionDate,
            storagePath
        );

        _repository.Add(decision);
        
        // 1. Önce Veritabanına Kaydet (Hata olursa pipeline hiç tetiklenmesin)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. SİHİRLİ DOKUNUŞ: AggregateRoot'un cebinde biriken olayları (Event) fırlat!
        // SİHİRLİ DOKUNUŞ: Listeyi kopyalayarak (.ToList()) dönüyoruz ki hata vermesin!
        var eventsToPublish = decision.DomainEvents.ToList();
        decision.ClearDomainEvents(); // Yayınlamadan önce cebi boşalt
        
        foreach (var domainEvent in eventsToPublish)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        return Result<DecisionId>.Success(decision.Id);
    }
}
