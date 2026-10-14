using Microsoft.Extensions.DependencyInjection;
using Lex.Modules.Decisions.Application.Services;
using Lex.Modules.Decisions.Domain.Gateways;
using Lex.Modules.Decisions.Domain.Repositories;
using Lex.Modules.Decisions.Infrastructure.Gateways;
using Lex.Modules.Decisions.Infrastructure.Repositories;

namespace Lex.Modules.Decisions.Infrastructure.Extensions;

public static class DecisionsModuleExtensions
{
    public static IServiceCollection AddDecisionsModule(this IServiceCollection services)
    {
        // SİHİRLİ DOKUNUŞ: Postacıyı (MediatR) sisteme ve bu modüle tanıtıyoruz!
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DecisionsModuleExtensions).Assembly));

        // 1. Veritabanı İşçileri (Repositories & Unit of Work)
        services.AddScoped<ICourtDecisionRepository, CourtDecisionRepository>();
        services.AddScoped<IDecisionChunkRepository, DecisionChunkRepository>();
        services.AddScoped<IDecisionUnitOfWork, DecisionUnitOfWork>();

        // 2. İş Mantığı Servisleri (Application Services)
        services.AddScoped<DecisionRegistrationService>();

        // 3. Dış Dünya Bağlantıları (Gateways - Kuyruk Sistemi)
        services.AddSingleton<IMessageBusGateway, RabbitMqGateway>();
        
        // 4. Arka Plan İşçileri (Background Workers)
        services.AddHostedService<Lex.Modules.Decisions.Infrastructure.Workers.DecisionIngestionWorker>();

        return services;
    }
}
