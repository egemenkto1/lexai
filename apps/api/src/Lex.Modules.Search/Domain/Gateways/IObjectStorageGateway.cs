using System.Threading;
using System.Threading.Tasks;

namespace Lex.Modules.Search.Domain.Gateways;

public interface IObjectStorageGateway
{
    // Dev karar metnini S3/MinIO'ya yükler
    Task UploadTextAsync(string bucketName, string objectName, string content, CancellationToken cancellationToken = default);
    
    // İleride arayüzden karar okumak istediğimizde metni geri çeker
    Task<string> DownloadTextAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
}
