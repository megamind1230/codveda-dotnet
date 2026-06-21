using Azure.Storage.Blobs;

namespace FaceRank.Web.Services;

public class BlobStorageService
{
    private readonly BlobContainerClient? _container;
    private readonly IWebHostEnvironment _env;
    private readonly bool _useAzure;

    public BlobStorageService(IConfiguration config, IWebHostEnvironment env)
    {
        _env = env;
        var connString = config.GetValue<string>("Azure:BlobStorage:ConnectionString")
                         ?? config.GetValue<string>("AZURE_STORAGE_CONNECTION_STRING");

        _useAzure = _env.IsProduction() && !string.IsNullOrEmpty(connString);
        if (_useAzure)
        {
            var containerName = config.GetValue<string>("Azure:BlobStorage:ContainerName") ?? "avatars";
            _container = new BlobContainerClient(connString!, containerName);
        }
    }

    public async Task<string> UploadAsync(string fileName, Stream content)
    {
        if (_useAzure)
        {
            try
            {
                await _container!.CreateIfNotExistsAsync();
                var blob = _container.GetBlobClient(fileName);
                await blob.UploadAsync(content, overwrite: true);
                return blob.Uri.ToString();
            }
            catch
            {
                // Azure failed — fall back to local storage
            }
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var filePath = Path.Combine(uploadsDir, fileName);
        content.Position = 0;
        await using var fs = new FileStream(filePath, FileMode.Create);
        await content.CopyToAsync(fs);
        return $"/uploads/{fileName}";
    }

    public async Task DeleteAsync(string fileName)
    {
        if (_useAzure)
        {
            try
            {
                var blob = _container!.GetBlobClient(fileName);
                await blob.DeleteIfExistsAsync();
                return;
            }
            catch
            {
                // Azure failed — try local
            }
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        var filePath = Path.Combine(uploadsDir, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
