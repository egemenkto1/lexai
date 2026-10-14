using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Lex.Modules.Search.Domain.Gateways;

namespace Lex.Modules.Search.Infrastructure.Gateways;

internal sealed class OllamaGateway : ILlmGateway
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;

    public OllamaGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _modelName = "qwen2:1.5b"; 
    }

    public async IAsyncEnumerable<string> AskStreamAsync(
        string systemPrompt, 
        string userPrompt, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _modelName,
            stream = true,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        // --- DEĞİŞEN KISIM BURASI ---
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(requestBody)
        };

        // Sihirli Kod: ResponseHeadersRead (Tüm cevabın bitmesini bekleme, akışı anında başlat!)
        using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        // ------------------------------

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(line)) continue;

            using var jsonDoc = JsonDocument.Parse(line);
            if (jsonDoc.RootElement.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                var token = contentElement.GetString();
                if (!string.IsNullOrEmpty(token))
                {
                    yield return token;
                }
            }
        }
    }
}
