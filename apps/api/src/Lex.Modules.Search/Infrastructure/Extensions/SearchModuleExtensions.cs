using System;
using System.Net.Http;
using Lex.Modules.Search.Application.Services;
using Lex.Modules.Search.Domain.Gateways;
using Lex.Modules.Search.Domain.Services;
using Lex.Modules.Search.Infrastructure.Gateways;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSearch.Client;
using Qdrant.Client;
using Polly;
using Polly.Extensions.Http;

namespace Lex.Modules.Search.Infrastructure.Extensions;

public static class SearchModuleExtensions
{
    public static IServiceCollection AddSearchModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. OpenSearch Bağlantısı
        var openSearchUrl = configuration["Search:OpenSearchUrl"] ?? "http://localhost:9200";
        var osSettings = new ConnectionSettings(new Uri(openSearchUrl)).DefaultIndex("decisions");
        services.AddSingleton<IOpenSearchClient>(new OpenSearchClient(osSettings));

        // 2. Qdrant Bağlantısı (gRPC üzerinden)
        var qdrantHost = configuration["Search:QdrantHost"] ?? "localhost";
        var qdrantPort = int.Parse(configuration["Search:QdrantPort"] ?? "6334");
        services.AddSingleton(new QdrantClient(qdrantHost, qdrantPort));

        // 3. TEI Vektörleştirici Bağlantısı (HTTP) + POLLY ZIRHI
        var teiUrl = configuration["Search:TeiUrl"] ?? "http://localhost:8080";
        services.AddHttpClient<IEmbeddingGateway, TeiEmbeddingGateway>(client => 
        {
            client.BaseAddress = new Uri(teiUrl);
        })
        .AddPolicyHandler(GetRetryPolicy())          // <-- Hata alırsan bekle ve tekrar dene
        .AddPolicyHandler(GetCircuitBreakerPolicy());// <-- Çok hata alırsan şalteri indir

        // 4. Ollama Bağlantısı (HTTP) + POLLY ZIRHI
        var ollamaUrl = configuration["Search:OllamaUrl"] ?? "http://localhost:11434";
        services.AddHttpClient<ILlmGateway, OllamaGateway>(client => 
        {
            client.BaseAddress = new Uri(ollamaUrl);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // 5. Gateways & Services
        services.AddScoped<IFullTextSearchGateway, OpenSearchGateway>();
        services.AddScoped<IVectorSearchGateway, QdrantGateway>();
        
        services.AddSingleton<RrfFusionService>();
        services.AddSingleton<IObjectStorageGateway, MinioGateway>();
        services.AddScoped<HybridSearchService>();
        services.AddScoped<LegalAssistantService>();

        return services;
    }

    // --- SİHİRLİ DOKUNUŞLAR: POLLY POLİTİKALARI ---

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        // Hata (5xx veya 408) alırsan 3 kez daha dene. Bekleme süresi: 2s, 4s, 8s
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        // Peş peşe 5 hata alırsan 30 saniye servisi rahat bırak (Timeout/Circuit Breaker)
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
