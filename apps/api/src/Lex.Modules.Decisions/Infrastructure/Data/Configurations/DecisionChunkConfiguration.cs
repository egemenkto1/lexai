using Lex.Modules.Decisions.Domain.Chunks;
using Lex.Modules.Decisions.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lex.Modules.Decisions.Infrastructure.Data.Configurations;

internal sealed class DecisionChunkConfiguration : IEntityTypeConfiguration<DecisionChunk>
{
    public void Configure(EntityTypeBuilder<DecisionChunk> builder)
    {
        builder.ToTable("decision_chunks", "decisions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasConversion(id => id.Value, value => new ChunkId(value))
               .IsRequired();

        builder.Property(c => c.DecisionId)
               .HasConversion(id => id.Value, value => new DecisionId(value))
               .IsRequired();

        builder.Property(c => c.ChunkIndex).IsRequired();
        builder.Property(c => c.ChunkText).HasColumnType("text").IsRequired();
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.TokenCount).IsRequired();

        builder.OwnsOne(c => c.EmbeddingRef, er =>
        {
            er.Property(p => p.QdrantPointId).HasColumnName("QdrantPointId");
            er.Property(p => p.Model).HasColumnName("EmbeddingModel").HasMaxLength(100);
            er.Property(p => p.Version).HasColumnName("EmbeddingVersion");
        });

        builder.Property(c => c.CreatedAt).IsRequired();

        // İndeksler
        builder.HasIndex(c => c.DecisionId);
        // Aynı kararın aynı indeksli chunk'ı iki kez olamaz
        builder.HasIndex(c => new { c.DecisionId, c.ChunkIndex }).IsUnique();
    }
}
