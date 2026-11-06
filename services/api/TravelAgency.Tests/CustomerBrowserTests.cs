using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    public static bool RunCustomerBrowser => HasDatabase && Environment.GetEnvironmentVariable("TRAVEL_CUSTOMER_E2E") == "1";
    // No production seed endpoint or eligibility override. Existing fixtures live only for this process.
    [Fact(Skip = "Opt in with TRAVEL_CUSTOMER_E2E=1 and an isolated *_e2e_tests database.", SkipUnless = nameof(RunCustomerBrowser))]
    public Task CustomerBrowserJourney() => RunBrowserAsync(false);
    public static bool RunDocumentBrowser => HasDatabase && Environment.GetEnvironmentVariable("TRAVEL_DOCUMENT_E2E") == "1";
    [Fact(Skip = "Opt in with TRAVEL_DOCUMENT_E2E=1 and an isolated *_e2e_tests database.", SkipUnless = nameof(RunDocumentBrowser))]
    public Task DocumentBrowserJourney() => RunBrowserAsync(true);
    public static bool RunMessagingBrowser => HasDatabase && Environment.GetEnvironmentVariable("TRAVEL_MESSAGING_E2E") == "1";
    [Fact(Skip = "Opt in with TRAVEL_MESSAGING_E2E=1 and an isolated *_e2e_tests database.", SkipUnless = nameof(RunMessagingBrowser))]
    public Task MessagingBrowserJourney() => RunBrowserAsync(true, true);
    public static bool RunCmsBrowser => HasDatabase && Environment.GetEnvironmentVariable("TRAVEL_CMS_E2E") == "1";
    [Fact(Skip = "Opt in with TRAVEL_CMS_E2E=1 and an isolated *_e2e_tests database.", SkipUnless = nameof(RunCmsBrowser))]
    public Task CmsBrowserJourney() => RunBrowserAsync(true, true, true);
    private async Task RunBrowserAsync(bool documents, bool messaging = false, bool cms = false)
    {
        Assert.EndsWith("_e2e_tests", new NpgsqlConnectionStringBuilder(Connection).Database);
        using var c = Client(); await Login(c); var email = "phase10-e2e-" + Guid.NewGuid().ToString("N") + "@example.test";
        var q = await Prepare(c, "en", contactEmail: email); await Send(c, q);
        var p = await ProposalDraft(c, "en", email); await SendProposal(c, p);
        var b = await ApprovedBooking(c, "en", email);
        foreach (var rid in b.Reservations.Select(r => r.Id).ToArray()) { b = await SaveReservation(c, b, rid); b = await BookingAction(c, b, rid, "start"); b = await BookingAction(c, b, rid, "confirm"); }
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgency.Infrastructure.Persistence.TravelAgencyDbContext>();
        var references = db.TravelRequests.Where(r => requests.Contains(r.Id)).Select(r => new { id = r.Id, reference = r.ReferenceNumber }).ToArray();
        var documentEmail = "phase11-e2e-" + Guid.NewGuid().ToString("N") + "@example.test";
        TravelAgency.Application.Bookings.BookingDto? documentBooking = null;
        if (documents)
        {
            documentBooking = await ApprovedBooking(c, "en", documentEmail);
            foreach (var rid in documentBooking.Reservations.Select(r => r.Id).ToArray()) { documentBooking = await SaveReservation(c, documentBooking, rid); documentBooking = await BookingAction(c, documentBooking, rid, "start"); documentBooking = await BookingAction(c, documentBooking, rid, "confirm"); }
        }
        var messagingEmail = "phase12-e2e-" + Guid.NewGuid().ToString("N") + "@example.test";
        TravelAgency.Application.Bookings.BookingDto? messagingBooking = null;
        Guid? otherConversationId = null;
        if (messaging)
        {
            messagingBooking = await ApprovedBooking(c, "en", messagingEmail);
            using var customer = Client(); var owner = await RegisterCustomer(customer, messagingEmail); await CustomerLogin(customer, owner.Email); await ClaimCustomerRequest(customer, owner.Id, messagingBooking.Booking.RequestId);
            using var other = Client(); var outsider = await RegisterCustomer(other); await CustomerLogin(other, outsider.Email); otherConversationId = await StartConversation(other, "TEST private other customer");
        }
        var invoiceId = db.Invoices.Where(i => i.RequestId == p.RequestId).Select(i => i.Id).Single();
        var folder = Path.Combine(Path.GetTempPath(), "travel-customer-e2e-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        await File.WriteAllTextAsync(Path.Combine(folder, "fixture.json"), JsonSerializer.Serialize(new { messagingEmail, messagingBookingId = messagingBooking?.Booking.Id, messagingRequestId = messagingBooking?.Booking.RequestId, otherConversationId, email, requests = references, quotationId = q.Id, proposalId = p.Id, invoiceId, bookingId = b.Booking.Id, documentEmail, documentBookingId = documentBooking?.Booking.Id, documentReference = documentBooking?.Booking.Reference, documentRequestReference = documentBooking is null ? null : db.TravelRequests.Where(r => r.Id == documentBooking.Booking.RequestId).Select(r => r.ReferenceNumber).Single() }), Ct);
        if (documents)
        {
            var pdf = new TravelAgency.Infrastructure.Documents.ItineraryPdf(new(), TimeProvider.System);
            var version = new TravelAgency.Application.Documents.ItineraryVersionDto(Guid.NewGuid(), 1, "Draft", "TEST harmless browser fixture", "No supplier booking is executed.", documentBooking!.Proposal.Trip, [], null, DateTimeOffset.UtcNow);
            var data = new TravelAgency.Application.Documents.ItineraryDto(documentBooking.Booking.Id, documentBooking.Booking.Reference, null, 1, null, false, [version]);
            await File.WriteAllBytesAsync(Path.Combine(folder, "fixture.pdf"), pdf.Render(data, version, "en"), Ct);
        }
        await using var scanner = new DocumentBrowserScanner();
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "apps", "web"))) root = root.Parent; Assert.NotNull(root);
        var start = new ProcessStartInfo("npm") { WorkingDirectory = Path.Combine(root.FullName, "apps", "web"), RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add("run"); start.ArgumentList.Add("test:e2e");
        if (messaging && Environment.GetEnvironmentVariable("TRAVEL_E2E_SPEC") == "messaging") { start.ArgumentList.Add("--"); start.ArgumentList.Add("e2e/messaging.spec.ts"); }
        if (cms && Environment.GetEnvironmentVariable("TRAVEL_E2E_SPEC") == "cms") { start.ArgumentList.Add("--"); start.ArgumentList.Add("e2e/website-management.spec.ts"); }
        start.Environment["TRAVEL_CMS_E2E"] = cms ? "1" : "0";
        start.Environment["TRAVEL_E2E_DIRECTORY"] = folder;
        start.Environment["TRAVEL_CUSTOMER_E2E"] = "1";
        start.Environment["TRAVEL_MESSAGING_E2E"] = messaging ? "1" : "0";
        start.Environment["TRAVEL_E2E_MESSAGE_PASSWORD"] = CustomerPassword;
        start.Environment["TRAVEL_DOCUMENT_E2E"] = documents ? "1" : "0";
        start.Environment["TRAVEL_E2E_SCANNER_PORT"] = scanner.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        start.Environment["TRAVEL_E2E_STORAGE_ROOT"] = scope.ServiceProvider.GetRequiredService<TravelAgency.Infrastructure.Documents.DocumentSettings>().StorageRoot;
        start.Environment["TRAVEL_E2E_ADMIN_EMAIL"] = this.email;
        start.Environment["TRAVEL_E2E_ADMIN_PASSWORD"] = password;
        using var process = Process.Start(start)!; var stdout = process.StandardOutput.ReadToEndAsync(Ct); var stderr = process.StandardError.ReadToEndAsync(Ct);
        try { await process.WaitForExitAsync(Ct); } finally { if (!process.HasExited) process.Kill(true); }
        var output = await stdout + await stderr;
        await File.WriteAllTextAsync(Path.Combine(folder, "browser.log"), output, Ct);
        // Browser-created identities are removed by normal fixture disposal, including failed runs.
        requests.AddRange(db.TravelRequests.Where(r => r.Contact.Email == email || r.Contact.Email == documentEmail).Select(r => r.Id));
        customerIds.AddRange(db.Users.Where(u => u.Email == email || u.Email == documentEmail).Select(u => u.Id));
        Assert.True(process.ExitCode == 0, "Customer browser verification failed. " + output + " Artifacts: " + folder);
    }
}
