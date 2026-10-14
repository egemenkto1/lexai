using Lex.Modules.Decisions.Domain.Chunks;
using Lex.Modules.Decisions.Domain.ValueObjects;

namespace Lex.Modules.Decisions.Domain.Repositories;

public interface IDecisionChunkRepository
{
    Task<IReadOnlyList<DecisionChunk>> GetByDecisionIdAsync(DecisionId decisionId, CancellationToken cancellationToken = default);
    void AddRange(IEnumerable<DecisionChunk> chunks);
    void Update(DecisionChunk chunk);
}
