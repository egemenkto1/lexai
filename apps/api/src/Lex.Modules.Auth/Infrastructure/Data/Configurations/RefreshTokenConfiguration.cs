using Lex.Modules.Auth.Domain.Tokens;
using Lex.Modules.Auth.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lex.Modules.Auth.Infrastructure.Data.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", "auth");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .HasConversion(id => id.Value, value => new RefreshTokenId(value))
               .IsRequired();

        // Foreign Key ilişkisi için UserId mapping
        builder.Property(r => r.UserId)
               .HasConversion(id => id.Value, value => new UserId(value))
               .IsRequired();

        builder.Property(r => r.TokenHash)
               .HasConversion(hash => hash.Value, value => new TokenHash(value))
               .HasMaxLength(255)
               .IsRequired();

        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.RevokedAt);
        builder.Property(r => r.CreatedAt).IsRequired();
    }
}
