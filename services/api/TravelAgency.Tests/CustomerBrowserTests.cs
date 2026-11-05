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
    public async Task CustomerBrowserJourney()
    {
        Assert.EndsWith("_e2e_tests", new NpgsqlConnectionStringBuilder(Connection).Database);
        using var c = Client(); await Login(c); var email = "phase10-e2e-" + Guid.NewGuid().ToString("N") + "@example.test";
        var q = await Prepare(c, "en", contactEmail: email); await Send(c, q);
        var p = await ProposalDraft(c, "en", email); await SendProposal(c, p);
        var b = await ApprovedBooking(c, "en", email);
        foreach (var rid in b.Reservations.Select(r => r.Id).ToArray()) { b = await SaveReservation(c, b, rid); b = await BookingAction(c, b, rid, "start"); b = await BookingAction(c, b, rid, "confirm"); }
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgency.Infrastructure.Persistence.TravelAgencyDbContext>();
        var references = db.TravelRequests.Where(r => requests.Contains(r.Id)).Select(r => new { id = r.Id, reference = r.ReferenceNumber }).ToArray();
        var invoiceId = db.Invoices.Where(i => i.RequestId == p.RequestId).Select(i => i.Id).Single();
        var folder = Path.Combine(Path.GetTempPath(), "travel-customer-e2e-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        await File.WriteAllTextAsync(Path.Combine(folder, "fixture.json"), JsonSerializer.Serialize(new { email, requests = references, quotationId = q.Id, proposalId = p.Id, invoiceId, bookingId = b.Booking.Id }), Ct);
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "apps", "web"))) root = root.Parent; Assert.NotNull(root);
        var start = new ProcessStartInfo("npm") { WorkingDirectory = Path.Combine(root.FullName, "apps", "web"), RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add("run"); start.ArgumentList.Add("test:e2e"); start.Environment["TRAVEL_E2E_DIRECTORY"] = folder;
        using var process = Process.Start(start)!; var stdout = process.StandardOutput.ReadToEndAsync(Ct); var stderr = process.StandardError.ReadToEndAsync(Ct);
        try { await process.WaitForExitAsync(Ct); } finally { if (!process.HasExited) process.Kill(true); }
        var output = await stdout + await stderr;
        await File.WriteAllTextAsync(Path.Combine(folder, "browser.log"), output, Ct);
        // Browser-created identities are removed by normal fixture disposal, including failed runs.
        requests.AddRange(db.TravelRequests.Where(r => r.Contact.Email == email).Select(r => r.Id));
        customerIds.AddRange(db.Users.Where(u => u.Email == email).Select(u => u.Id));
        Assert.True(process.ExitCode == 0, "Customer browser verification failed. " + output + " Artifacts: " + folder);
    }
}
