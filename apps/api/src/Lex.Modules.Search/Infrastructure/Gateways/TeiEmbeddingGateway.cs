using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Lex.Modules.Search.Domain.Gateways;

namespace Lex.Modules.Search.Infrastructure.Gateways;

internal sealed class TeiEmbeddingGateway : IEmbeddingGateway
{
    private readonly HttpClient _httpClient;

    public TeiEmbeddingGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var request = new { inputs = query };
        
        var response = await _httpClient.PostAsJsonAsync("/embed", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var embeddings = await response.Content.ReadFromJsonAsync<float[][]>(cancellationToken: cancellationToken);
        
        return embeddings?.FirstOrDefault() ?? Array.Empty<float>();
    }
}
