using Lex.Modules.Decisions.Domain.Chunks;
using Lex.Modules.Decisions.Domain.Repositories;
using Lex.Modules.Decisions.Domain.ValueObjects;
using Lex.Modules.Decisions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Modules.Decisions.Infrastructure.Repositories;

internal sealed class DecisionChunkRepository : IDecisionChunkRepository
{
    private readonly DecisionsDbContext _dbContext;

    public DecisionChunkRepository(DecisionsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DecisionChunk>> GetByDecisionIdAsync(DecisionId decisionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DecisionChunks
            .Where(x => x.DecisionId == decisionId)
            .OrderBy(x => x.ChunkIndex)
            .ToListAsync(cancellationToken);
    }

    public void AddRange(IEnumerable<DecisionChunk> chunks)
    {
        _dbContext.DecisionChunks.AddRange(chunks);
    }

    public void Update(DecisionChunk chunk)
    {
        _dbContext.DecisionChunks.Update(chunk);
    }
}
