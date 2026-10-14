using Lex.Modules.Decisions.Domain.Chunks;
using Lex.Modules.Decisions.Domain.Decisions;
using Microsoft.EntityFrameworkCore;

namespace Lex.Modules.Decisions.Infrastructure.Data;

public sealed class DecisionsDbContext : DbContext
{
    public DbSet<CourtDecision> CourtDecisions { get; set; } = null!;
    public DbSet<DecisionChunk> DecisionChunks { get; set; } = null!;

    public DecisionsDbContext(DbContextOptions<DecisionsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("decisions");
        
        // Ayarları doğrudan Configuration dosyalarından çeker.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DecisionsDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}
