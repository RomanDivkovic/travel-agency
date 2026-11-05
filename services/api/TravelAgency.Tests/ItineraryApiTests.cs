using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Documents;
using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Documents;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    private static async Task<ItineraryDto> Itinerary(HttpClient c, Guid booking, bool admin = true) => (await c.GetFromJsonAsync<ItineraryDto>($"/api/{(admin ? "admin" : "customer")}/bookings/{booking}/itinerary", Ct))!;
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ItineraryUsesConfirmedSourcesPreservesCustomEntriesAndPublishesImmutableVersions()
    {
        using var admin = Client(); await Login(admin); var email = CustomerEmail(); var b = await ApprovedBooking(admin, contactEmail: email); var flight = b.Reservations.Single(r => r.Current.Kind == "Flight").Id; b = await SaveReservation(admin, b, flight); b = await BookingAction(admin, b, flight, "start"); b = await BookingAction(admin, b, flight, "confirm");
        using var customer = Client(); var u = await RegisterCustomer(customer, email); await CustomerLogin(customer, email); await ClaimCustomerRequest(customer, u.Id, b.Booking.RequestId);
        using var create = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/itinerary"); Assert.Equal(HttpStatusCode.NoContent, create.StatusCode); var d = await Itinerary(admin, b.Booking.Id);
        using var generate = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/itinerary/generate", new ItineraryVersionInput(d.VersionToken!.Value)); Assert.Equal(HttpStatusCode.NoContent, generate.StatusCode); d = await Itinerary(admin, b.Booking.Id); Assert.Equal(2, d.Versions[0].Entries.Length); Assert.All(d.Versions[0].Entries, e => Assert.Equal("Flight", e.Kind)); Assert.Contains(d.Versions[0].Entries, e => e.Event == "Departure" && e.TimeZone == "Europe/Stockholm"); Assert.Contains(d.Versions[0].Entries, e => e.Event == "Arrival" && e.TimeZone == "Europe/Lisbon");
        var custom = new CustomEntryInput(Guid.NewGuid(), "TEST airport guidance", "TEST public note", new(2027, 6, 1), "2027-06-01T08:30", "Europe/Stockholm", null, "ARN", 0);
        using var save = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/itinerary", new ItinerarySave(d.VersionToken!.Value, "TEST itinerary <script>", "TEST overview", [custom]), HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, save.StatusCode); d = await Itinerary(admin, b.Booking.Id);
        using var again = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/itinerary/generate", new ItineraryVersionInput(d.VersionToken!.Value)); Assert.Equal(HttpStatusCode.NoContent, again.StatusCode); d = await Itinerary(admin, b.Booking.Id); Assert.Equal(3, d.Versions[0].Entries.Length); Assert.Single(d.Versions[0].Entries, e => !e.Confirmed); Assert.Equal(custom.Id, d.Versions[0].Entries[0].Id);
        using var hidden = await customer.GetAsync($"/api/customer/bookings/{b.Booking.Id}/itinerary", Ct); Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        using var publish = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/itinerary/publish", new ItineraryVersionInput(d.VersionToken!.Value)); Assert.Equal(HttpStatusCode.NoContent, publish.StatusCode); d = await Itinerary(admin, b.Booking.Id); var snapshot = System.Text.Json.JsonSerializer.Serialize(d.Versions[0].Entries);
        var own = await Itinerary(customer, b.Booking.Id, false); Assert.Single(own.Versions); Assert.Equal("Published", own.Versions[0].Status); var json = await customer.GetStringAsync($"/api/customer/bookings/{b.Booking.Id}/itinerary", Ct); foreach (var secret in new[] { "sourceKey", "arrangementVersion", "versionToken", "SECRET" }) Assert.DoesNotContain(secret, json);
        using var edit = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/itinerary", new ItinerarySave(d.VersionToken!.Value, "changed", "", []), HttpMethod.Patch); Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        using var revise = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/itinerary/revise", new ItineraryVersionInput(d.VersionToken!.Value)); Assert.Equal(HttpStatusCode.NoContent, revise.StatusCode); d = await Itinerary(admin, b.Booking.Id); Assert.Equal(2, d.CurrentVersion); Assert.Equal(1, (await Itinerary(customer, b.Booking.Id, false)).CurrentVersion); Assert.Equal(snapshot, System.Text.Json.JsonSerializer.Serialize(d.Versions.Single(v => v.Version == 1).Entries));
        using var publish2 = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/itinerary/publish", new ItineraryVersionInput(d.VersionToken!.Value)); Assert.Equal(HttpStatusCode.NoContent, publish2.StatusCode); Assert.Equal(2, (await Itinerary(customer, b.Booking.Id, false)).CurrentVersion);
        using var pdf = await customer.GetAsync($"/api/customer/bookings/{b.Booking.Id}/itinerary/pdf?language=sv", Ct); Assert.Equal(HttpStatusCode.OK, pdf.StatusCode); Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType); var bytes = await pdf.Content.ReadAsByteArrayAsync(Ct); Assert.True(bytes.AsSpan().StartsWith("%PDF-"u8)); Assert.True(bytes.Length > 1000); await File.WriteAllBytesAsync("/private/tmp/travel-phase11-itinerary-sv.pdf", bytes, Ct);
        using var english = await customer.GetAsync($"/api/customer/bookings/{b.Booking.Id}/itinerary/pdf?language=en", Ct); Assert.Equal(HttpStatusCode.OK, english.StatusCode); await File.WriteAllBytesAsync("/private/tmp/travel-phase11-itinerary-en.pdf", await english.Content.ReadAsByteArrayAsync(Ct), Ct);
        using var other = Client(); var second = await RegisterCustomer(other); await CustomerLogin(other, second.Email); foreach (var resource in new[] { "itinerary", "itinerary/pdf", "documents" }) { using var denied = await other.GetAsync($"/api/customer/bookings/{b.Booking.Id}/{resource}", Ct); Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode); }
        b = await BookingDetail(admin, b.Booking.Id); using var changed = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}", new TravelAgency.Application.Bookings.BookingNotesInput(b.VersionToken, "SECRET new internal note"), HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode); own = await Itinerary(customer, b.Booking.Id, false); Assert.True(own.SourceChanged); Assert.Equal(snapshot, System.Text.Json.JsonSerializer.Serialize(own.Versions[0].Entries));
        d = await Itinerary(admin, b.Booking.Id); using var revision3 = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/itinerary/revise", new ItineraryVersionInput(d.VersionToken!.Value)); Assert.Equal(HttpStatusCode.NoContent, revision3.StatusCode); d = await Itinerary(admin, b.Booking.Id);
        using var withdraw = await Mutate(admin, $"/api/admin/bookings/{b.Booking.Id}/itinerary/archive", new ItineraryVersionInput(d.VersionToken!.Value)); Assert.Equal(HttpStatusCode.NoContent, withdraw.StatusCode); using var withdrawn = await customer.GetAsync($"/api/customer/bookings/{b.Booking.Id}/itinerary", Ct); Assert.Equal(HttpStatusCode.NotFound, withdrawn.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task ItineraryRejectsUnconfirmedGenerationStaleWritesAndInvalidCustomTimes()
    {
        using var c = Client(); await Login(c); var b = await ApprovedBooking(c); using var create = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/itinerary"); Assert.Equal(HttpStatusCode.NoContent, create.StatusCode); var d = await Itinerary(c, b.Booking.Id);
        using var empty = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/itinerary/generate", new ItineraryVersionInput(d.VersionToken!.Value)); Assert.Equal(HttpStatusCode.Conflict, empty.StatusCode);
        using var stale = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/itinerary", new ItinerarySave(Guid.NewGuid(), "TEST", "", []), HttpMethod.Patch); Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var invalid = new CustomEntryInput(null, "TEST", "", new(2027, 3, 28), "2027-03-28T02:30", "Europe/Stockholm", null, "", 0); using var gap = await Mutate(c, $"/api/admin/bookings/{b.Booking.Id}/itinerary", new ItinerarySave(d.VersionToken!.Value, "TEST", "", [invalid]), HttpMethod.Patch); Assert.Equal(HttpStatusCode.BadRequest, gap.StatusCode);
    }
}
