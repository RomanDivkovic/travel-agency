using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TravelAgency.Application.Documents;
using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Documents;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    private sealed class TestDocumentScanner(DocumentScan result) : IDocumentScanner { public Task<DocumentScan> ScanAsync(ReadOnlyMemory<byte> content, CancellationToken ct) => Task.FromResult(result); }
    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> WithScanner(DocumentScan scan) => factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => { s.RemoveAll<IDocumentScanner>(); s.AddScoped<IDocumentScanner>(_ => new TestDocumentScanner(scan)); }));
    private static async Task<HttpResponseMessage> UploadDocument(HttpClient c, Guid booking, byte[]? bytes = null, string mime = "application/pdf", Guid? document = null, string? version = null, Guid? reservation = null)
    {
        var csrf = (await c.GetFromJsonAsync<JsonElement>("/api/admin/auth/csrf", Ct)).GetProperty("token").GetString(); using var data = new MultipartFormDataContent(); data.Add(new StringContent(JsonSerializer.Serialize(new DocumentMetadata("TEST ticket <script>", "TEST customer description", "FlightTicket", reservation, true), new JsonSerializerOptions(JsonSerializerDefaults.Web))), "metadata"); if (version is not null) data.Add(new StringContent(version), "versionToken");
        var file = new ByteArrayContent(bytes ?? DocumentDomainTests.Pdf); file.Headers.ContentType = new(mime); data.Add(file, "file", "TEST.pdf"); using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/bookings/{booking}/documents" + (document is null ? "" : $"/{document}/replace")) { Content = data }; request.Headers.Add("X-CSRF-TOKEN", csrf); return await c.SendAsync(request, Ct);
    }
    private static async Task<DocumentLibrary> Documents(HttpClient c, Guid booking, bool admin = true) => (await c.GetFromJsonAsync<DocumentLibrary>($"/api/{(admin ? "admin" : "customer")}/bookings/{booking}/documents", Ct))!;
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task DocumentPublicationVersioningDownloadsAndOwnershipAreEnforced()
    {
        await using var clean = WithScanner(DocumentScan.Clean); using var admin = Client(clean); await Login(admin); var email = CustomerEmail(); var b = await ApprovedBooking(admin, contactEmail: email);
        foreach (var rid in b.Reservations.Select(r => r.Id).ToArray()) { b = await SaveReservation(admin, b, rid); b = await BookingAction(admin, b, rid, "start"); b = await BookingAction(admin, b, rid, "confirm"); }
        using var customer = Client(clean); var u = await RegisterCustomer(customer, email); await CustomerLogin(customer, email); await ClaimCustomerRequest(customer, u.Id, b.Booking.RequestId);
        using var upload = await UploadDocument(admin, b.Booking.Id, reservation: b.Reservations[0].Id); Assert.Equal(HttpStatusCode.Created, upload.StatusCode); var d = (await Documents(admin, b.Booking.Id)).Items.Single(); Assert.Equal("Draft", d.Versions[0].Status); Assert.Empty((await Documents(customer, b.Booking.Id, false)).Items);
        using var draftDownload = await customer.GetAsync($"/api/customer/bookings/{b.Booking.Id}/documents/{d.Id}/download", Ct); Assert.Equal(HttpStatusCode.NotFound, draftDownload.StatusCode);
        using var publish = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/documents/{d.Id}/publish", new DocumentVersionInput(d.VersionToken!.Value, 1)); Assert.Equal(HttpStatusCode.NoContent, publish.StatusCode);
        using var download = await customer.GetAsync($"/api/customer/bookings/{b.Booking.Id}/documents/{d.Id}/download", Ct); Assert.Equal(HttpStatusCode.OK, download.StatusCode); Assert.Equal(DocumentDomainTests.Pdf, await download.Content.ReadAsByteArrayAsync(Ct)); Assert.Contains("attachment", download.Content.Headers.ContentDisposition!.ToString()); Assert.Contains("no-store", download.Headers.CacheControl!.ToString());
        var publicJson = await customer.GetStringAsync($"/api/customer/bookings/{b.Booking.Id}/documents", Ct); Assert.DoesNotContain("storageKey", publicJson); Assert.DoesNotContain("blobId", publicJson); Assert.DoesNotContain("versionToken", publicJson);
        d = (await Documents(admin, b.Booking.Id)).Items.Single(); using var replace = await UploadDocument(admin, b.Booking.Id, document: d.Id, version: d.VersionToken.ToString(), reservation: d.ReservationId); Assert.Equal(HttpStatusCode.Created, replace.StatusCode); d = (await Documents(admin, b.Booking.Id)).Items.Single(); Assert.Equal(1, (await Documents(customer, b.Booking.Id, false)).Items.Single().CurrentVersion);
        using var send = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/documents/{d.Id}/publish", new DocumentVersionInput(d.VersionToken!.Value, 2)); Assert.Equal(HttpStatusCode.NoContent, send.StatusCode); Assert.Equal(2, (await Documents(customer, b.Booking.Id, false)).Items.Single().CurrentVersion);
        using var other = Client(clean); var second = await RegisterCustomer(other); await CustomerLogin(other, second.Email); using var forbidden = await other.GetAsync($"/api/customer/bookings/{b.Booking.Id}/documents/{d.Id}/download", Ct); Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
        using var cross = await customer.GetAsync($"/api/customer/bookings/{Guid.NewGuid()}/documents/{d.Id}/download", Ct); Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);
        d = (await Documents(admin, b.Booking.Id)).Items.Single(); using var archive = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/documents/{d.Id}/archive", new DocumentVersionInput(d.VersionToken!.Value, 2)); Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode); using var hidden = await customer.GetAsync($"/api/customer/bookings/{b.Booking.Id}/documents/{d.Id}/download", Ct); Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }
    [Theory(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    [InlineData(DocumentScan.Quarantined)]
    [InlineData(DocumentScan.Infected)]
    public async Task UnscannedAndRejectedFilesCannotPublishOrDownload(DocumentScan result)
    {
        await using var blocked = WithScanner(result); using var admin = Client(blocked); await Login(admin); var b = await ApprovedBooking(admin); using var upload = await UploadDocument(admin, b.Booking.Id); Assert.Equal(HttpStatusCode.Created, upload.StatusCode); var d = (await Documents(admin, b.Booking.Id)).Items.Single();
        using var publish = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/documents/{d.Id}/publish", new DocumentVersionInput(d.VersionToken!.Value, 1)); Assert.Equal(HttpStatusCode.Conflict, publish.StatusCode); using var preview = await admin.GetAsync($"/api/admin/bookings/{b.Booking.Id}/documents/{d.Id}/download", Ct); Assert.Equal(HttpStatusCode.NotFound, preview.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task DocumentsRejectInvalidUploadsCrossBookingReservationsCsrfAndUnauthorizedAccess()
    {
        using var c = Client(); var id = Guid.NewGuid(); using var anon = await c.GetAsync($"/api/customer/bookings/{id}/documents", Ct); Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode); using var adminDenied = await c.PostAsync($"/api/admin/bookings/{id}/documents", null, Ct); Assert.Equal(HttpStatusCode.Unauthorized, adminDenied.StatusCode);
        await Login(c); var b = await ApprovedBooking(c); using var bad = await UploadDocument(c, b.Booking.Id, "<script>bad</script>"u8.ToArray()); Assert.Equal(HttpStatusCode.UnsupportedMediaType, bad.StatusCode); using var cross = await UploadDocument(c, b.Booking.Id, reservation: Guid.NewGuid()); Assert.Equal(HttpStatusCode.BadRequest, cross.StatusCode);
        using var noCsrf = await c.PostAsync($"/api/admin/bookings/{b.Booking.Id}/documents", null, Ct); Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using var oversized = await UploadDocument(c, b.Booking.Id, new byte[10 * 1024 * 1024 + 1]); Assert.Equal(HttpStatusCode.RequestEntityTooLarge, oversized.StatusCode); Assert.Empty((await Documents(c, b.Booking.Id)).Items);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task DocumentConcurrentPublicationAndFailedReplacementPreserveOneCurrentVersion()
    {
        await using var clean = WithScanner(DocumentScan.Clean); using var c = Client(clean); await Login(c); var b = await ApprovedBooking(c);
        // Larger than the global JSON body limit: multipart has its own bounded endpoint limit.
        var bytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\n" + new string('x', 40000) + "\n%%EOF\n");
        using var upload = await UploadDocument(c, b.Booking.Id, bytes); Assert.Equal(HttpStatusCode.Created, upload.StatusCode); var d = (await Documents(c, b.Booking.Id)).Items.Single();
        var input = new DocumentVersionInput(d.VersionToken!.Value, 1); var results = await Task.WhenAll(Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/documents/{d.Id}/publish", input), Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/documents/{d.Id}/publish", input));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.NoContent); Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict); foreach (var response in results) response.Dispose();
        using var failed = await UploadDocument(c, b.Booking.Id, document: d.Id, version: input.VersionToken.ToString()); Assert.Equal(HttpStatusCode.Conflict, failed.StatusCode); d = (await Documents(c, b.Booking.Id)).Items.Single(); Assert.Equal(1, d.CurrentVersion); Assert.Single(d.Versions);
        using var scope = clean.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var leases = await db.DocumentBlobs.Where(x => x.BookingId == b.Booking.Id && x.DeleteAfter != null).ToArrayAsync(Ct); var lease = Assert.Single(leases); var root = scope.ServiceProvider.GetRequiredService<DocumentSettings>().StorageRoot; Assert.True(File.Exists(Path.Combine(root, lease.Key + ".bin")));
        await db.DocumentBlobs.Where(x => x.Id == lease.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.DeleteAfter, DateTimeOffset.UtcNow.AddMinutes(-1)), Ct); db.ChangeTracker.Clear(); await scope.ServiceProvider.GetRequiredService<DocumentMaintenance>().RunAsync(Ct); Assert.False(File.Exists(Path.Combine(root, lease.Key + ".bin"))); Assert.Single((await Documents(c, b.Booking.Id)).Items.Single().Versions);
    }

}
