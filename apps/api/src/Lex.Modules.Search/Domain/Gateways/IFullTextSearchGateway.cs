using Lex.Modules.Search.Domain.Models;
using Lex.Modules.Search.Domain.ValueObjects;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Lex.Modules.Search.Domain.Gateways;

public interface IFullTextSearchGateway
{
    // Mevcut Okuma Metodu
    Task<IReadOnlyList<FullTextHit>> SearchAsync(string query, SearchFilterSet filters, int topK, CancellationToken cancellationToken = default);

    // Mevcut Yazma Metodu
    Task IndexDecisionAsync(string decisionId, string text, int year, int categoryCode, CancellationToken cancellationToken = default);

    // YENİ EKLENEN KURULUM METODU (İndeksi ve Sözlüğü Yaratmak İçin)
    Task InitializeIndexAsync(CancellationToken cancellationToken = default);
}
