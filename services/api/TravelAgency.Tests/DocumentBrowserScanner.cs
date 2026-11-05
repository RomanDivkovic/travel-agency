using System.Net;
using System.Net.Sockets;
using System.Text;
namespace TravelAgency.Tests;
// Protocol fixture for harmless generated PDFs only. Never registered in application services.
internal sealed class DocumentBrowserScanner : IAsyncDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource stop = new();
    private readonly Task loop;
    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public DocumentBrowserScanner() { listener.Start(); loop = RunAsync(); }
    private async Task RunAsync()
    {
        try { while (!stop.IsCancellationRequested) { using var client = await listener.AcceptTcpClientAsync(stop.Token); await using var stream = client.GetStream(); var command = new byte[10]; await stream.ReadExactlyAsync(command, stop.Token); if (Encoding.ASCII.GetString(command) != "zINSTREAM\0") continue; var total = 0; while (true) { var header = new byte[4]; await stream.ReadExactlyAsync(header, stop.Token); var size = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header); if (size == 0) break; if (size < 0 || size > 16384 || (total += size) > 10 * 1024 * 1024) throw new InvalidDataException(); await stream.ReadExactlyAsync(new byte[size], stop.Token); } await stream.WriteAsync("stream: OK\0"u8.ToArray(), stop.Token); } }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
    }
    public async ValueTask DisposeAsync() { await stop.CancelAsync(); listener.Stop(); await loop; stop.Dispose(); }
}
