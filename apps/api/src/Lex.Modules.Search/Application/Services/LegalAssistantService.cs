using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Lex.Modules.Search.Domain.Gateways;
using Lex.Modules.Search.Domain.ValueObjects;

namespace Lex.Modules.Search.Application.Services;

public sealed class LegalAssistantService
{
    private readonly HybridSearchService _searchService;
    private readonly ILlmGateway _llmGateway;

    public LegalAssistantService(HybridSearchService searchService, ILlmGateway llmGateway)
    {
        _searchService = searchService;
        _llmGateway = llmGateway;
    }

    public async IAsyncEnumerable<string> AnswerLegalQuestionStreamAsync(
        string question, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 1. LLM'e yeterli bağlam sunmak için en iyi 5 kararı çekiyoruz
        var pagination = new Pagination(1, 5); 
        var emptyFilters = new SearchFilterSet();
        var searchResults = await _searchService.SearchAsync(question, emptyFilters, pagination, cancellationToken);

        // 2. RAG Bağlamını (Context) Etiketleyerek (Tagging) İnşa Et
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("AŞAĞIDA YARGITAY KARARLARINDAN BULUNAN İLGİLİ REFERANS METİNLER YER ALMAKTADIR:\n");

        int sourceIndex = 1;
        foreach (var result in searchResults)
        {
            // Eğer Snippet boşsa LLM'in kafasını karıştırmamak için uyarı koyuyoruz
            var content = string.IsNullOrWhiteSpace(result.Snippet) 
                ? "Bu kararın özet metni bulunamadı, anlamsal eşleşme yapıldı." 
                : result.Snippet;
            
            contextBuilder.AppendLine($"[Kaynak {sourceIndex}]");
            contextBuilder.AppendLine($"Daire: {result.ChamberCode}");
            contextBuilder.AppendLine($"Esas: {result.CaseNumber} | Karar: {result.DecisionNumber}");
            contextBuilder.AppendLine($"Metin: {content}");
            contextBuilder.AppendLine("-------------------");
            
            sourceIndex++;
        }

        // 3. EFSANEVİ PROMPT ENGINEERING (İstem Mühendisliği)
        var systemPrompt = @"Sen 'LexAI' adında, sadece Türk Hukuku üzerine uzmanlaşmış profesyonel bir yapay zeka avukatısın. 
Sana sağlanan Yargıtay kararı referanslarını (Kaynakları) kullanarak kullanıcının hukuki sorusunu yanıtla.

KESİN KURALLAR:
1. HALÜSİNASYON YASAK: Sadece sana verilen [Kaynak X] metinlerindeki bilgileri kullan. Kendi başına kanun, madde veya Yargıtay kararı uydurma.
2. ATIF ZORUNLULUĞU (CITATION): Kurduğun her hukuki cümlenin veya iddianın sonuna, o bilgiyi hangi kaynaktan aldığını köşeli parantez içinde belirt. 
   Örnek kullanım: 'İşçinin kıdem tazminatı alabilmesi için haklı fesih yapması gerekir [Kaynak 1]. Ayrıca yan haklar da ücrete tabidir [Kaynak 2].'
3. BİLMİYORSAN SÖYLE: Eğer verilen kaynaklarda sorunun net bir cevabı yoksa, ASLA tahmin yürütme. Sadece 'Verilen Yargıtay kararları arasında bu sorunun net bir cevabı bulunmamaktadır.' de.
4. DİL: Hitabetin resmi, saygılı, net ve Yargıtay diline hakim bir avukat profesyonelliğinde olmalıdır.";

        var finalPrompt = $"{contextBuilder}\n\nKULLANICI SORUSU: {question}\n\nLütfen yukarıdaki kurallara kesinlikle uyarak cevap ver:";

        // 4. Ollama / GPT-4'ten Gelen Akışı (Stream) Yukarı Fırlat
        await foreach (var token in _llmGateway.AskStreamAsync(systemPrompt, finalPrompt, cancellationToken))
        {
            yield return token;
        }
    }
}
