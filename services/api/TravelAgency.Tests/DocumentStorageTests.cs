using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Documents;
namespace TravelAgency.Tests;

public sealed class DocumentStorageTests
{
    [Fact]
    public async Task PrivateLocalStorageEncryptsRoundTripsAndRejectsPathTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "travel-storage-unit-" + Guid.NewGuid().ToString("N")); var storage = new LocalDocumentStorage(new() { Provider = "LocalEncrypted", StorageRoot = root }, new EphemeralDataProtectionProvider()); var key = TravelAgency.Infrastructure.Customers.CustomerTokenCodec.Create();
        try { await storage.UploadAsync(key, DocumentDomainTests.Pdf, TestContext.Current.CancellationToken); var disk = await File.ReadAllBytesAsync(Path.Combine(root, key + ".bin"), TestContext.Current.CancellationToken); Assert.False(disk.AsSpan().StartsWith("%PDF-"u8)); Assert.Equal(DocumentDomainTests.Pdf, await storage.DownloadAsync(key, TestContext.Current.CancellationToken)); await Assert.ThrowsAsync<DocumentOperationException>(() => storage.DownloadAsync("../../etc/passwd", TestContext.Current.CancellationToken)); await storage.DeleteAsync(key, TestContext.Current.CancellationToken); await storage.DeleteAsync(key, TestContext.Current.CancellationToken); Assert.Empty(Directory.GetFiles(root)); }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Theory]
    [InlineData("stream: OK\0", DocumentScan.Clean)]
    [InlineData("stream: Eicar FOUND\0", DocumentScan.Infected)]
    [InlineData("stream: scan limit exceeded ERROR\0", DocumentScan.Quarantined)]
    [InlineData("stream: OK", DocumentScan.Quarantined)]
    public async Task ClamAvProtocolRequiresAnExplicitCompleteCleanResult(string reply, DocumentScan expected)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; var ct = TestContext.Current.CancellationToken;
        var server = Task.Run(async () => { using var socket = await listener.AcceptTcpClientAsync(ct); await using var stream = socket.GetStream(); var command = new byte[10]; await stream.ReadExactlyAsync(command, ct); Assert.Equal("zINSTREAM\0", Encoding.ASCII.GetString(command)); while (true) { var head = new byte[4]; await stream.ReadExactlyAsync(head, ct); var size = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(head); if (size == 0) break; var data = new byte[size]; await stream.ReadExactlyAsync(data, ct); } await stream.WriteAsync(Encoding.ASCII.GetBytes(reply), ct); }, ct);
        var result = await new ClamAvScanner(new() { ScannerPort = port }).ScanAsync(DocumentDomainTests.Pdf, ct); Assert.Equal(expected, result); await server;
    }
}
