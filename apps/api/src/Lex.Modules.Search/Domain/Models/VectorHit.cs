using System;

namespace Lex.Modules.Search.Domain.Models;

public record VectorHit(
    Guid DecisionId,
    Guid ChunkId,
    double Score,
    string Text // YENİ EKLENEN KAHRAMAN ALAN: Qdrant'ın bulduğu metni buraya koyacağız!
);
