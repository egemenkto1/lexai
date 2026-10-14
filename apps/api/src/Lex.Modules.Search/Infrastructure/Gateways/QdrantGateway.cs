using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Lex.Modules.Search.Domain.Gateways;
using Lex.Modules.Search.Domain.Models;
using Lex.Modules.Search.Domain.ValueObjects;

namespace Lex.Modules.Search.Infrastructure.Gateways;

public sealed class QdrantGateway : IVectorSearchGateway
{
    private readonly QdrantClient _client;
    private const string CollectionName = "decision_chunks";

    public QdrantGateway(QdrantClient client)
    {
        _client = client;
    }

    // --- SİHİRLİ DOKUNUŞ: KOLEKSİYON YOKSA OTOMATİK OLUŞTUR ---
    public async Task InitializeCollectionAsync(CancellationToken cancellationToken = default)
    {
        var collections = await _client.ListCollectionsAsync(cancellationToken);
        
        if (!collections.Contains(CollectionName))
        {
            // BGE-M3 modeli 1024 dimension üretir. Vektör motorunu buna göre ayarlıyoruz.
            await _client.CreateCollectionAsync(
                collectionName: CollectionName,
                vectorsConfig: new VectorParams { Size = 1024, Distance = Distance.Cosine },
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task<IReadOnlyList<VectorHit>> SearchAsync(float[] queryEmbedding, SearchFilterSet filters, int topK, CancellationToken cancellationToken = default)
    {
        Filter? qdrantFilter = null;

        if (filters != null)
        {
            var conditions = new List<Condition>();

            if (filters.StartDate.HasValue || filters.EndDate.HasValue)
            {
                var qdrantRange = new Qdrant.Client.Grpc.Range();
                if (filters.StartDate.HasValue) qdrantRange.Gte = filters.StartDate.Value.Year;
                if (filters.EndDate.HasValue) qdrantRange.Lte = filters.EndDate.Value.Year;

                conditions.Add(new Condition { Field = new FieldCondition { Key = "year", Range = qdrantRange } });
            }

            if (filters.ChamberCodes != null && filters.ChamberCodes.Any())
            {
                var chamberConditions = filters.ChamberCodes.Select(code => new Condition
                {
                    Field = new FieldCondition { Key = "chamber_code", Match = new Match { Keyword = code } }
                }).ToList();

                var chamberFilter = new Filter();
                chamberFilter.Should.AddRange(chamberConditions);

                conditions.Add(new Condition { Filter = chamberFilter });
            }

            if (conditions.Any())
            {
                qdrantFilter = new Filter();
                qdrantFilter.Must.AddRange(conditions);
            }
        }

        var response = await _client.SearchAsync(
            collectionName: CollectionName,
            vector: queryEmbedding, 
            filter: qdrantFilter,
            limit: (ulong)topK,
            payloadSelector: true,  
            cancellationToken: cancellationToken
        );

        var hits = new List<VectorHit>();
        foreach (var point in response)
        {
            var decisionIdStr = point.Payload["decision_id"].StringValue;
            Guid.TryParse(decisionIdStr, out Guid decisionId);
            Guid chunkId = point.Id.HasUuid ? Guid.Parse(point.Id.Uuid) : Guid.Empty;
            double score = point.Score;

            // Vektörün içindeki o saf Türkçe Yargıtay metnini Qdrant'tan koparıyoruz!
            string text = point.Payload.TryGetValue("text", out var textVal) ? textVal.StringValue : "";

            hits.Add(new VectorHit(decisionId, chunkId, score, text));
        }

        return hits;
    }

    public async Task UpsertChunkAsync(
        string chunkId, 
        string decisionId, 
        string text, 
        float[] embedding, 
        int year, 
        int categoryCode, 
        CancellationToken cancellationToken = default)
    {
        var point = new PointStruct
        {
            Id = GenerateDeterministicGuid(chunkId), 
            Vectors = embedding  
        };

        point.Payload.Add("chunk_id", chunkId);
        point.Payload.Add("decision_id", decisionId);
        point.Payload.Add("text", text);
        point.Payload.Add("year", year);
        point.Payload.Add("category_code", categoryCode);

        await _client.UpsertAsync(
            collectionName: CollectionName, 
            points: new[] { point }, 
            cancellationToken: cancellationToken);
    }

    private static Guid GenerateDeterministicGuid(string input)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
