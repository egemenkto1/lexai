using Lex.Modules.Auth.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lex.Modules.Auth.Infrastructure.Data.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Tablo ve şema adı
        builder.ToTable("users", "auth");

        builder.HasKey(u => u.Id);

        // Güçlü tipli ID'yi Guid'e çevirip DB'ye yazma
        builder.Property(u => u.Id)
               .HasConversion(id => id.Value, value => new UserId(value))
               .IsRequired();

        // Email Value Object çevirisi
        builder.Property(u => u.Email)
               .HasConversion(email => email.Value, value => Email.Create(value).Value)
               .HasMaxLength(255)
               .IsRequired();
               
        builder.HasIndex(u => u.Email).IsUnique();

        // PasswordHash Value Object çevirisi
        builder.Property(u => u.PasswordHash)
               .HasConversion(hash => hash.Value, value => new PasswordHash(value))
               .HasMaxLength(255)
               .IsRequired();

        builder.Property(u => u.FullName).HasMaxLength(255);
        
        builder.Property(u => u.Role)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();
    }
}
