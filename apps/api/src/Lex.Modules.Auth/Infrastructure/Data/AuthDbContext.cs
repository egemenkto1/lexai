using Lex.Modules.Auth.Domain.Tokens;
using Lex.Modules.Auth.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lex.Modules.Auth.Infrastructure.Data;

public sealed class AuthDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Varsayılan şemayı belirliyoruz (Mimari Kararı)
        modelBuilder.HasDefaultSchema("auth");

        // Aynı assembly'deki configuration sınıflarını otomatik bulur
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}
