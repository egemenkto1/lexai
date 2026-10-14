namespace Lex.Modules.Search.Domain.Gateways;

public interface ILlmGateway
{
    // Eski AskAsync yerine artık akışkan IAsyncEnumerable kullanıyoruz
    IAsyncEnumerable<string> AskStreamAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
