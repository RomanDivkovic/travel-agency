using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Documents;
using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Documents;
using TravelAgency.Infrastructure.Email;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    [Theory(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    [InlineData("sv")]
    [InlineData("en")]
    public async Task DocumentRetentionDeletesPrivateFilesAndSuppressesArchivedNotifications(string language)
    {
        await using var clean = WithScanner(DocumentScan.Clean); using var c = Client(clean); await Login(c); var b = await ApprovedBooking(c, language); using var uploaded = await UploadDocument(c, b.Booking.Id); Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode); var d = (await Documents(c, b.Booking.Id)).Items.Single();
        using var publish = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/documents/{d.Id}/publish", new DocumentVersionInput(d.VersionToken!.Value, 1)); Assert.Equal(HttpStatusCode.NoContent, publish.StatusCode);
        using var scope = clean.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var job = await db.NotificationOutbox.SingleAsync(n => n.BookingId == b.Booking.Id && n.Kind.StartsWith("document-published-"), Ct); var message = await DocumentNotifications.MessageAsync(job, db, new(), Ct); Assert.NotNull(message); Assert.Contains(language == "sv" ? "Dina resedokument" : "Your travel documents", message.Subject); Assert.DoesNotContain("StorageRoot", message.Text); Assert.Contains("/account/bookings/", message.Text); Assert.DoesNotContain("<script>", message.Html);
        d = (await Documents(c, b.Booking.Id)).Items.Single(); using var archived = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/documents/{d.Id}/archive", new DocumentVersionInput(d.VersionToken!.Value, 1)); Assert.Equal(HttpStatusCode.NoContent, archived.StatusCode); Assert.Null(await DocumentNotifications.MessageAsync(job, db, new(), Ct));
        var blob = await db.DocumentBlobs.SingleAsync(x => x.BookingId == b.Booking.Id, Ct); var settings = scope.ServiceProvider.GetRequiredService<DocumentSettings>(); var path = Path.Combine(settings.StorageRoot, blob.Key + ".bin"); Assert.True(File.Exists(path));
        await db.DocumentVersions.Where(v => v.DocumentId == d.Id).ExecuteUpdateAsync(s => s.SetProperty(v => v.UpdatedAt, DateTimeOffset.UtcNow.AddDays(-settings.ArchivedRetentionDays - 1)), Ct); db.ChangeTracker.Clear(); await scope.ServiceProvider.GetRequiredService<DocumentMaintenance>().RunAsync(Ct); Assert.False(File.Exists(path)); db.ChangeTracker.Clear(); Assert.Equal("", (await db.DocumentVersions.SingleAsync(v => v.DocumentId == d.Id, Ct)).OriginalFileName); Assert.Equal(DocumentStatus.Deleted, (await db.DocumentVersions.SingleAsync(v => v.DocumentId == d.Id, Ct)).Status);
    }
}
