using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using TravelAgency.Application.Documents;
using TravelAgency.Domain.Documents;
namespace TravelAgency.Infrastructure.Documents;

public sealed class DocumentOperationException(int status) : Exception { public int Status { get; } = status; }
public sealed class DocumentSettings
{
    public string Provider { get; set; } = "Disabled";
    public string StorageRoot { get; set; } = "";
    public long MaxBytes { get; set; } = 10 * 1024 * 1024;
    public int ArchivedRetentionDays { get; set; } = 90;
    public int DraftRetentionDays { get; set; } = 30;
    public string ScannerHost { get; set; } = "127.0.0.1";
    public int ScannerPort { get; set; } = 3310;
    public int ScanTimeoutSeconds { get; set; } = 5;
    public void RequireStorage() { if (Provider != "LocalEncrypted") throw new DocumentOperationException(412); }
}
public sealed partial class LocalDocumentStorage(DocumentSettings settings, IDataProtectionProvider protection) : ITravelDocumentStorage
{
    private readonly IDataProtector protector = protection.CreateProtector("TravelAgency.PrivateTravelDocuments.v1");
    [GeneratedRegex("^[A-Za-z0-9_-]{43}$")] private static partial Regex KeyPattern();
    private string PathFor(string key)
    {
        settings.RequireStorage(); if (!KeyPattern().IsMatch(key)) throw new DocumentOperationException(400);
        Directory.CreateDirectory(settings.StorageRoot);
        if (File.GetAttributes(settings.StorageRoot).HasFlag(FileAttributes.ReparsePoint)) throw new DocumentOperationException(503);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(settings.StorageRoot, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var path = Path.Combine(settings.StorageRoot, key + ".bin");
        if (File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint)) throw new DocumentOperationException(503);
        return path;
    }
    public async Task UploadAsync(string key, ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        if (content.Length > settings.MaxBytes) throw new DocumentOperationException(413);
        var destination = PathFor(key); var temp = destination + ".tmp";
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        try { await using (var file = new FileStream(temp, options)) await file.WriteAsync(protector.Protect(content.ToArray()), ct); File.Move(temp, destination, false); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public async Task<byte[]> DownloadAsync(string key, CancellationToken ct)
    {
        var path = PathFor(key); if (!File.Exists(path)) throw new DocumentOperationException(404);
        var info = new FileInfo(path); if (info.Length > settings.MaxBytes + 4096) throw new DocumentOperationException(503);
        return protector.Unprotect(await File.ReadAllBytesAsync(path, ct));
    }
    public Task DeleteAsync(string key, CancellationToken ct) { ct.ThrowIfCancellationRequested(); var path = PathFor(key); File.Delete(path); File.Delete(path + ".tmp"); return Task.CompletedTask; }
}
public sealed class ClamAvScanner(DocumentSettings settings) : IDocumentScanner
{
    public async Task<DocumentScan> ScanAsync(ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(settings.ScanTimeoutSeconds));
        try
        {
            using var client = new TcpClient(); await client.ConnectAsync(settings.ScannerHost, settings.ScannerPort, timeout.Token); await using var stream = client.GetStream();
            await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), timeout.Token);
            for (var offset = 0; offset < content.Length; offset += 16384)
            {
                var length = Math.Min(16384, content.Length - offset); var header = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(header, length); await stream.WriteAsync(header, timeout.Token); await stream.WriteAsync(content.Slice(offset, length), timeout.Token);
            }
            await stream.WriteAsync(new byte[4], timeout.Token); var reply = new byte[1024]; var used = 0;
            while (used < reply.Length) { var read = await stream.ReadAsync(reply.AsMemory(used), timeout.Token); if (read == 0) break; used += read; if (reply.AsSpan(0, used).Contains((byte)0)) break; }
            if (!reply.AsSpan(0, used).Contains((byte)0)) return DocumentScan.Quarantined;
            var result = Encoding.UTF8.GetString(reply, 0, used).TrimEnd('\0', '\r', '\n');
            return result == "stream: OK" ? DocumentScan.Clean : result.StartsWith("stream: ", StringComparison.Ordinal) && result.EndsWith(" FOUND", StringComparison.Ordinal) ? DocumentScan.Infected : DocumentScan.Quarantined;
        }
        catch (Exception e) when (e is IOException or SocketException || e is OperationCanceledException && !ct.IsCancellationRequested) { return DocumentScan.Quarantined; }
    }
}
public static class DocumentFileValidation
{
    public static string Validate(byte[] bytes, string mime, string original, long limit)
    {
        if (bytes.Length == 0) throw new DocumentOperationException(400); if (bytes.LongLength > limit) throw new DocumentOperationException(413);
        var name = original.Replace('\\', '/').Split('/').Last(); name = new string(name.Where(c => char.IsLetterOrDigit(c) || " ._-".Contains(c)).Take(120).ToArray()).Trim(' ', '.');
        var ext = Path.GetExtension(name).ToLowerInvariant(); var content = bytes.AsSpan();
        var pdf = mime == "application/pdf" && ext == ".pdf" && content.StartsWith("%PDF-"u8) && content[^Math.Min(1024, bytes.Length)..].IndexOf("%%EOF"u8) >= 0;
        var png = mime == "image/png" && ext == ".png" && content.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && bytes.Length >= 20 && content[^12..^8].SequenceEqual(new byte[4]) && content[^8..^4].SequenceEqual("IEND"u8);
        var jpg = mime == "image/jpeg" && ext is ".jpg" or ".jpeg" && content.StartsWith(new byte[] { 255, 216, 255 }) && content.EndsWith(new byte[] { 255, 217 });
        if (!pdf && !png && !jpg) throw new DocumentOperationException(415);
        return name;
    }
}
