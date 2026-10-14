using Lex.Modules.Auth.Api;
using Lex.Modules.Auth.Infrastructure.Data;
using Lex.Modules.Auth.Infrastructure.Extensions;
using Lex.Modules.Decisions.Infrastructure.Data; 
using Lex.Modules.Decisions.Api; 
using Lex.Modules.Decisions.Infrastructure.Extensions; 
using Microsoft.EntityFrameworkCore;
using Lex.Modules.Search.Api; 
using Lex.Modules.Search.Infrastructure.Extensions; 
using Lex.Modules.Search.Domain.Gateways; // SİHİRLİ DOKUNUŞ İÇİN EKLENDİ

var builder = WebApplication.CreateBuilder(args);

// Veritabanı Bağlamları
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString));

// DECISIONS VERİTABANI BAĞLANTISINI EKLİYORUZ
builder.Services.AddDbContext<DecisionsDbContext>(options =>
    options.UseNpgsql(connectionString));

// Modülleri Sisteme Dahil Et
Lex.Modules.Auth.Infrastructure.Extensions.AuthModuleExtensions.AddAuthModule(builder.Services);
builder.Services.AddDecisionsModule(); 

// SEARCH MODÜLÜNÜ SİSTEME DAHİL ET (Configuration parametresi ile)
builder.Services.AddSearchModule(builder.Configuration);

var app = builder.Build();

// --- SİHİRLİ DOKUNUŞ: MOTOR KONTAĞI AÇILIRKEN İNDEKSİ VE SÖZLÜĞÜ KUR ---
using (var scope = app.Services.CreateScope())
{
    var searchGateway = scope.ServiceProvider.GetRequiredService<IFullTextSearchGateway>();
    // API her ayağa kalktığında kontrol eder, indeks yoksa o devasa sözlükle birlikte yaratır!
    await searchGateway.InitializeIndexAsync();
}
// -----------------------------------------------------------------------

// Endpoints
app.MapAuthEndpoints();
app.MapDecisionsEndpoints(); 
app.MapSearchEndpoints();

app.Run();
