using TravelAgency.Application.Research;
using TravelAgency.Domain.Research;

namespace TravelAgency.Tests;

public sealed class ResearchDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);
    internal static ResearchOptionInput Extra(string currency = "SEK") => new("Extra", "TEST extra <script>x</script>", "TEST customer description", 1995, currency, "PerPerson", 3, "TEST Supplier", "https://supplier.example.test/option", Now.AddDays(-1), null, "TEST cancellation conditions", "SECRET INTERNAL NOTE", null, null, new("Transfer", "Lisbon", new DateOnly(2027, 6, 1)));
    internal static ResearchOptionInput Flight() => Extra() with { Kind = "Flight", Extra = null, Flight = new([new("TEST airline", "TEST1", "ARN", "LIS", new DateTimeOffset(2027, 6, 1, 10, 0, 0, TimeSpan.FromHours(2)), new DateTimeOffset(2027, 6, 1, 13, 0, 0, TimeSpan.FromHours(1)), "Europe/Stockholm", "Europe/Lisbon")], "Economy", "TEST baggage") };
    internal static ResearchOptionInput Stay() => Extra() with { Kind = "Accommodation", Extra = null, Accommodation = new("Lisbon", "TEST address", "Hotel", "Double", new(2027, 6, 1), new(2027, 6, 7), 2, "Breakfast", "Pool"), PriceBasis = "PerNight", Quantity = 6 };
    [Fact]
    public void ExactCostsGroupByCurrencyAndDoNotIncludePaidServiceFee()
    {
        var a = ResearchOption.Create(Guid.NewGuid(), Extra(), Now); var b = ResearchOption.Create(Guid.NewGuid(), Extra("EUR"), Now);
        Assert.Equal(5985, a.Snapshot().TotalMinorUnits); Assert.DoesNotContain("SECRET", a.SnapshotJson); Assert.Equal("SECRET INTERNAL NOTE", a.Notes);
        var costs = ProposalDtos.Costs([a.Snapshot(), b.Snapshot()]); Assert.Equal(2, costs.Length); Assert.All(costs, c => Assert.Equal(5985, c.TotalMinorUnits));
    }
    [Fact]
    public void ExplicitFlightOffsetsAndConnectionsAreValidated()
    {
        var input = Flight(); var first = input.Flight!.Legs[0]; Assert.True(ResearchValidation.Zoned(first.DepartureAt, "Europe/Stockholm"));
        Assert.False(ResearchValidation.Zoned(first.DepartureAt, "Europe/Lisbon"));
        Assert.Throws<ResearchRuleException>(() => ResearchOption.Create(Guid.NewGuid(), input with { Flight = input.Flight with { Legs = [first with { DepartureTimeZone = "Unknown/Invalid" }] } }, Now));
        Assert.Throws<ResearchRuleException>(() => ResearchOption.Create(Guid.NewGuid(), input with { Flight = input.Flight with { Legs = [first, first] } }, Now));
        var second = first with { DepartureAirport = "LIS", ArrivalAirport = "MAD", DepartureAt = first.ArrivalAt.AddHours(1), DepartureTimeZone = "Europe/Lisbon", ArrivalAt = new DateTimeOffset(2027, 6, 1, 16, 0, 0, TimeSpan.FromHours(2)), ArrivalTimeZone = "Europe/Madrid" };
        Assert.Equal(2, ResearchOption.Create(Guid.NewGuid(), input with { Flight = input.Flight with { Legs = [first, second] } }, Now).Snapshot().Flight!.Legs.Length);
    }
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://supplier.example.test")]
    [InlineData("https://user:secret@supplier.example.test")]
    [InlineData("https://localhost/private")]
    [InlineData("https://127.0.0.1/private")]
    public void UnsafeSupplierUrlsAreRejected(string url) => Assert.Throws<ResearchRuleException>(() => ResearchOption.Create(Guid.NewGuid(), Extra() with { SupplierUrl = url }, Now));
    [Fact]
    public void InvalidBasisDatesNegativePricesAndOverflowAreRejected()
    {
        Assert.Throws<ResearchRuleException>(() => ResearchOption.Create(Guid.NewGuid(), Extra() with { PriceMinorUnits = -1 }, Now));
        Assert.Throws<ResearchRuleException>(() => ResearchOption.Create(Guid.NewGuid(), Extra() with { PriceMinorUnits = 10_000_000_000, Quantity = 2 }, Now));
        Assert.Throws<ResearchRuleException>(() => ResearchOption.Create(Guid.NewGuid(), Extra() with { PriceBasis = "Total", Quantity = 2 }, Now));
        Assert.Throws<ResearchRuleException>(() => ResearchOption.Create(Guid.NewGuid(), Extra() with { PriceCheckedAt = Now.AddHours(1) }, Now));
        Assert.Throws<ResearchRuleException>(() => ResearchOption.Create(Guid.NewGuid(), Stay() with { Accommodation = Stay().Accommodation! with { CheckOut = new(2027, 6, 1) } }, Now));
    }
    private static TravelProposal Proposal()
    {
        var p = TravelProposal.Create(Guid.NewGuid(), 1, "sv", "TEST", "test@example.test", "admin", new("ARN", "Lisbon", false, [], "fixed", new(2027, 6, 1), new(2027, 6, 7), null, null, null, 2, 0, 1), new("TEST-INV", "SEK", 10000, Now), Now.AddDays(14), Now);
        var o = ResearchOption.Create(Guid.NewGuid(), Extra(), Now); p.Edit("TEST proposal", "Intro", "Summary", "TEST terms", p.ValidUntil, [ProposalAlternative.Create(p.Id, "A", "TEST", false, 0, [ProposalItem.Create(o.Id, o.VersionToken, o.SnapshotJson, 0)])], Now); return p;
    }
    [Fact]
    public void SentSnapshotsAreFrozenAndApprovalNeedsAnExplicitValidAlternative()
    {
        var p = Proposal(); p.Send("TEST-v1", "TEST acknowledgement", Now);
        Assert.Throws<ResearchRuleException>(() => p.Edit("Changed", "", "", "", p.ValidUntil, [], Now));
        Assert.Throws<ResearchRuleException>(() => p.Respond(true, null, Now)); Assert.Throws<ResearchRuleException>(() => p.Respond(true, Guid.NewGuid(), Now));
        var selected = p.Alternatives[0].Id; Assert.True(p.Respond(true, selected, Now)); Assert.False(p.Respond(true, selected, Now.AddHours(1))); Assert.Equal(Now, p.ApprovedAt); Assert.Equal(selected, p.SelectedAlternativeId);
        Assert.Throws<ResearchRuleException>(() => p.Respond(false, null, Now)); Assert.Throws<ResearchRuleException>(() => p.Revise("admin", Now.AddDays(20), Now));
    }
    [Fact]
    public void RevisionsPreserveSourceSnapshotsAndTerminalStatesCannotApprove()
    {
        var p = Proposal(); p.Send("TEST-v1", "TEST", Now); var old = p.Alternatives[0].Items[0].SnapshotJson;
        var revision = p.Revise("admin", Now.AddDays(20), Now); p.Supersede(Now); Assert.Equal(2, revision.Version); Assert.Equal(old, revision.Alternatives[0].Items[0].SnapshotJson); Assert.NotEqual(p.Alternatives[0].Id, revision.Alternatives[0].Id);
        Assert.Throws<ResearchRuleException>(() => p.Respond(true, p.Alternatives[0].Id, Now)); revision.Cancel(Now); Assert.Throws<ResearchRuleException>(() => revision.Respond(true, revision.Alternatives[0].Id, Now));
        var expired = Proposal(); expired.Send("TEST", "TEST", Now); Assert.True(expired.Expire(expired.ValidUntil)); Assert.False(expired.Expire(expired.ValidUntil)); Assert.Throws<ResearchRuleException>(() => expired.Respond(true, expired.Alternatives[0].Id, Now));
        var rejected = Proposal(); rejected.Send("TEST", "TEST", Now); Assert.True(rejected.Respond(false, null, Now)); Assert.Throws<ResearchRuleException>(() => rejected.Respond(true, rejected.Alternatives[0].Id, Now));
    }
}
