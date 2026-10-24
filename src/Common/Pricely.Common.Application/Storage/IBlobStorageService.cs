namespace Pricely.Common.Application.Storage;
/// <summary>
/// Defines methods for interacting with a blob storage service.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a stream to the specified blob storage location.
    /// </summary>
    /// <param name="folderPath">The folder path in the storage.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="data">The data stream to upload.</param>
    /// <param name="contentType">The MIME type of the content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UploadAsync(string folderPath, string blobName, Stream data, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permet d'uploader le fichier d'acquittement sur le storage qui indique que l'export est terminé et que SAP peut récupérer le fichier
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="blobName"></param>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task UploadAcquitmentFileAsync(
        string folderPath,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a blob as a stream from the specified location.
    /// </summary>
    /// <param name="folderPath">The folder path in the storage.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The blob data as a stream.</returns>
    Task<Stream> DownloadAsync(string folderPath, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob if it exists at the specified location.
    /// </summary>
    /// <param name="folderPath">The folder path in the storage.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if the blob was deleted; otherwise, false.</returns>
    Task<bool> DeleteIfExistsAsync(string folderPath, string blobName, CancellationToken ct = default);

    /// <summary>
    /// Checks if a blob exists at the specified location.
    /// </summary>
    /// <param name="folderPath">The folder path in the storage.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if the blob exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string folderPath, string blobName, CancellationToken ct = default);

    /// <summary>
    /// Lists the names of blobs in the specified folder, optionally filtered by a prefix.
    /// </summary>
    /// <param name="folderPath">The folder path in the storage.</param>
    /// <param name="prefix">The optional prefix to filter blob names.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A read-only list of blob names.</returns>
    Task<IReadOnlyList<string>> ListBlobsAsync(string folderPath, string? prefix = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the URI of a blob at the specified location.
    /// </summary>
    /// <param name="folderPath">The folder path in the storage.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <returns>The URI of the blob.</returns>
    Uri GetUri(string folderPath, string blobName);

    /// <summary>
    /// Downloads all blobs matching a regular expression pattern in the specified folder.
    /// </summary>
    /// <param name="folderPath">The folder path in the storage.</param>
    /// <param name="regexPattern">The regular expression pattern to match blob names.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A read-only dictionary mapping blob names to their data streams.</returns>
    Task<IReadOnlyDictionary<string, Stream>> DownloadBlobsMatchingAsync(string folderPath, string regexPattern, int maxItems = 1000, CancellationToken ct = default);

    /// <summary>
    /// Copies a blob to a backup folder and deletes the original blob.
    /// </summary>
    /// <param name="sourceFolderPath">The source folder path in the storage.</param>
    /// <param name="blobName">The name of the blob to move.</param>
    /// <param name="backupFolderPath">The backup folder path in the storage.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if the original file was successfully backed up and removed; otherwise, false.</returns>
    Task<bool> BackupAndDeleteFileAsync(string sourceFolderPath, string blobName, string backupFolderPath, CancellationToken ct = default);

}
