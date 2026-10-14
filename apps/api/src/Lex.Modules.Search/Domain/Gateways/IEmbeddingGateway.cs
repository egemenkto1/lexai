namespace Lex.Modules.Search.Domain.Gateways;

public interface IEmbeddingGateway
{
    Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken = default);
}
