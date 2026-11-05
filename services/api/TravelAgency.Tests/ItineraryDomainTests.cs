using TravelAgency.Domain.Bookings;
using TravelAgency.Domain.Documents;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Documents;
namespace TravelAgency.Tests;

public sealed class ItineraryDomainTests
{
    [Fact]
    public void MultiLegOvernightFlightsRetainDepartureArrivalDatesAndZones()
    {
        var now = DateTimeOffset.UtcNow; var details = new FlightDetails([
            new("TEST airline","T1","NRT","LHR",new(2027,6,1,23,0,0,TimeSpan.FromHours(9)),new(2027,6,2,5,0,0,TimeSpan.FromHours(1)),"Asia/Tokyo","Europe/London"),
            new("TEST airline","T2","LHR","JFK",new(2027,6,2,8,0,0,TimeSpan.FromHours(1)),new(2027,6,2,11,0,0,TimeSpan.FromHours(-4)),"Europe/London","America/New_York")], "Economy", "TEST baggage");
        var source = ResearchDomainTests.Flight() with { Flight = details, PriceCheckedAt = now, AvailabilityCheckedAt = now }; var option = ResearchOption.Create(Guid.NewGuid(), source, now);
        var p = TravelProposal.Create(Guid.NewGuid(), 1, "en", "TEST", "test@example.test", "actor", new("NRT", "New York", false, ["London"], "fixed", new(2027, 6, 1), new(2027, 6, 7), null, null, null, 2, 0, 1), new("TEST", "SEK", 100, now), now.AddDays(14), now);
        var item = ProposalItem.Create(option.Id, option.VersionToken, option.SnapshotJson, 0); var alt = ProposalAlternative.Create(p.Id, "TEST", "TEST", false, 0, [item]); p.Edit("TEST", "TEST", "TEST", "TEST", p.ValidUntil, [alt], now); p.Send("TEST", "TEST", now); p.Respond(true, alt.Id, now); var b = TravelBooking.Create(Guid.NewGuid(), p, alt, "BKG-TEST", "actor", BookingDomainTests.Evidence(now), now); Assert.Empty(ItineraryGeneration.Generate(b));
        var r = b.Reservations.Single(); r.Update(source, SupplierPaymentResponsibility.CustomerPaysSupplier, SupplierPaymentStatus.NotPaid, "TEST direct", now); r.Start(now); r.Confirm("TEST-REF", "TEST evidence", "", "Issued", now, now); b.Recalculate(now); var entries = ItineraryGeneration.Ordered(ItineraryGeneration.Generate(b)); Assert.Equal(4, entries.Length); Assert.Equal(4, entries.Select(e => e.SourceKey).Distinct().Count()); Assert.Equal(new DateOnly(2027, 6, 1), entries[0].Date); Assert.Equal("Asia/Tokyo", entries[0].TimeZone); Assert.Equal(23, TravelTimes.Local(entries[0].At!.Value, entries[0].TimeZone!).Hour); Assert.Contains(entries, e => e.Event == "Arrival" && e.Date == new DateOnly(2027, 6, 2) && e.TimeZone == "America/New_York"); Assert.All(entries, e => Assert.True(e.Confirmed));
    }
}
