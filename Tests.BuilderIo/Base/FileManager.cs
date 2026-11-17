using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Tests.BuilderIo.Base;

public class FileManager : IFileManagementClient
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "BuilderIoTests", Guid.NewGuid().ToString("N"));

    public Task<Stream> DownloadAsync(FileReference reference)
        => Task.FromResult<Stream>(File.OpenRead(Path.Combine(directory, Path.GetFileName(reference.Name))));

    public async Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName)
    {
        Directory.CreateDirectory(directory);
        var name = Path.GetFileName(fileName);
        using var output = File.Create(Path.Combine(directory, name));
        await stream.CopyToAsync(output);
        return new FileReference { Name = name, ContentType = contentType };
    }
}
