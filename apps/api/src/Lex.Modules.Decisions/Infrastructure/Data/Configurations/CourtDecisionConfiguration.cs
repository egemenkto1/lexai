using Lex.Modules.Decisions.Domain.Decisions;
using Lex.Modules.Decisions.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lex.Modules.Decisions.Infrastructure.Data.Configurations;

internal sealed class CourtDecisionConfiguration : IEntityTypeConfiguration<CourtDecision>
{
    public void Configure(EntityTypeBuilder<CourtDecision> builder)
    {
        builder.ToTable("court_decisions", "decisions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasConversion(id => id.Value, value => new DecisionId(value))
               .IsRequired();

        builder.Property(c => c.CourtType).HasConversion<string>().HasMaxLength(50).IsRequired();

        // --- BÜYÜK VERİ HİYERARŞİSİ (YENİ EKLENENLER) ---
        builder.Property(c => c.DecisionYear).IsRequired();
        builder.Property(c => c.Category).HasConversion<int>().IsRequired(); // Enum'u sayı (int) olarak tutar ki sorgular jet gibi çalışsın
        builder.Property(c => c.ChamberName).HasMaxLength(200).IsRequired();
        
        // Value Object Dönüşümleri ve İndeksi
        builder.OwnsOne(c => c.CaseIdentifier, ci => 
        {
            ci.Property(p => p.ChamberCode).HasColumnName("ChamberCode").HasMaxLength(50).IsRequired(); // 20'den 50'ye çıkardık, garanti olsun
            ci.Property(p => p.CaseNumber).HasColumnName("CaseNumber").HasMaxLength(50).IsRequired();
            ci.Property(p => p.DecisionNumber).HasColumnName("DecisionNumber").HasMaxLength(50).IsRequired();

            // İndeksi ait olduğu yere, yani bloğun içine taşıdık!
            ci.HasIndex(p => new { p.ChamberCode, p.CaseNumber, p.DecisionNumber }).IsUnique();
        });

        builder.Property(c => c.RawTextLocation)
               .HasConversion(path => path.Key, value => new ObjectStoragePath(value))
               .HasMaxLength(500).IsRequired();

        builder.Property(c => c.DecisionDate).IsRequired();
        builder.Property(c => c.Summary).HasColumnType("text");
        builder.Property(c => c.Outcome).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.EmbeddingStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.FailureReason).HasMaxLength(1000);
        builder.Property(c => c.IndexedAt);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        // Concurrency Token (Warning'i çözen yeni EF Core standardı)
        builder.Property(c => c.Version).IsRowVersion();

        // İndeksler
        builder.HasIndex(c => c.DecisionDate);
        builder.HasIndex(c => c.EmbeddingStatus);
        
        // VİZYON: Hiyerarşik Arama İndeksi (PostgreSQL bu indeksle uçacak)
        builder.HasIndex(c => new { c.DecisionYear, c.Category, c.ChamberName });
    }
}
