using System.IO;
using System.Text;
using Minio;
using Minio.DataModel.Args;
using Lex.Modules.Search.Domain.Gateways;

namespace Lex.Modules.Search.Infrastructure.Gateways;

internal sealed class MinioGateway : IObjectStorageGateway
{
    private readonly IMinioClient _minioClient;

    public MinioGateway()
    {
        // docker-compose dosyasındaki bilgilerle bağlantı kuruyoruz
        _minioClient = new MinioClient()
            .WithEndpoint("localhost:9000")
            .WithCredentials("lexai_admin", "lexai_secret_123")
            .Build();
    }

    public async Task UploadTextAsync(string bucketName, string objectName, string content, CancellationToken cancellationToken = default)
    {
        // 1. Kova (Bucket) var mı kontrol et, yoksa oluştur
        var bktExistArgs = new BucketExistsArgs().WithBucket(bucketName);
        bool found = await _minioClient.BucketExistsAsync(bktExistArgs, cancellationToken);
        if (!found)
        {
            var mkBktArgs = new MakeBucketArgs().WithBucket(bucketName);
            await _minioClient.MakeBucketAsync(mkBktArgs, cancellationToken);
        }

        // 2. Metni byte dizisine çevir ve MinIO'ya fırlat
        var contentBytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(contentBytes);

        var putObjArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType("text/plain");

        await _minioClient.PutObjectAsync(putObjArgs, cancellationToken);
    }

    public async Task<string> DownloadTextAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        
        var getObjArgs = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithCallbackStream((stream) =>
            {
                stream.CopyTo(memoryStream);
            });

        await _minioClient.GetObjectAsync(getObjArgs, cancellationToken);
        
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}
