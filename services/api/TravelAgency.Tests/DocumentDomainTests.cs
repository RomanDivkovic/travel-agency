using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Documents;
namespace TravelAgency.Tests;

public sealed class DocumentDomainTests
{
    internal static byte[] Pdf => "%PDF-1.7\nTEST harmless fixture\n%%EOF\n"u8.ToArray();
    [Fact]
    public void OnlyCleanCurrentDraftCanPublishAndReplacementDoesNotHideExistingPublication()
    {
        var d = new TravelDocument(); var first = new TravelDocumentVersion { DocumentId = d.Id, Version = 1, Scan = DocumentScan.Quarantined }; d.Versions.Add(first);
        Assert.Throws<DocumentRuleException>(() => d.Publish(first, DateTimeOffset.UtcNow)); first.Scan = DocumentScan.Clean; d.Publish(first, DateTimeOffset.UtcNow);
        d.CurrentVersion = 2; var second = new TravelDocumentVersion { DocumentId = d.Id, Version = 2, Scan = DocumentScan.Infected }; d.Versions.Add(second);
        Assert.Equal(1, d.PublishedVersion); Assert.Throws<DocumentRuleException>(() => d.Publish(second, DateTimeOffset.UtcNow)); second.Scan = DocumentScan.Clean; d.Publish(second, DateTimeOffset.UtcNow);
        Assert.Equal(DocumentStatus.Archived, first.Status); Assert.Equal(2, d.PublishedVersion); Assert.Throws<DocumentRuleException>(() => d.Publish(first, DateTimeOffset.UtcNow));
        d.Hide(second, true, DateTimeOffset.UtcNow); Assert.Null(d.PublishedVersion); Assert.Throws<DocumentRuleException>(() => d.Publish(second, DateTimeOffset.UtcNow));
    }
    [Theory]
    [InlineData("application/pdf", "file.exe")]
    [InlineData("image/png", "file.png")]
    [InlineData("text/html", "file.pdf")]
    public void MimeExtensionAndSignatureMustAllMatch(string mime, string name) => Assert.Equal(415, Assert.Throws<DocumentOperationException>(() => DocumentFileValidation.Validate(Pdf, mime, name, 1000)).Status);
    [Fact]
    public void UploadLimitsAndFilenameNormalizationAreEnforced()
    {
        Assert.Equal("safe.pdf", DocumentFileValidation.Validate(Pdf, "application/pdf", "../../unsafe/\\safe.pdf", 1000)); Assert.Equal(413, Assert.Throws<DocumentOperationException>(() => DocumentFileValidation.Validate(Pdf, "application/pdf", "safe.pdf", 5)).Status);
    }
    [Fact]
    public void DaylightSavingGapAndAmbiguityRequireCorrectExplicitOffset()
    {
        Assert.Throws<DocumentRuleException>(() => TravelTimes.Resolve(new(2027, 3, 28, 2, 30, 0), "Europe/Stockholm", null));
        Assert.Throws<DocumentRuleException>(() => TravelTimes.Resolve(new(2027, 10, 31, 2, 30, 0), "Europe/Stockholm", null));
        var earlier = TravelTimes.Resolve(new(2027, 10, 31, 2, 30, 0), "Europe/Stockholm", "+02:00"); var later = TravelTimes.Resolve(new(2027, 10, 31, 2, 30, 0), "Europe/Stockholm", "+01:00"); Assert.Equal(TimeSpan.FromHours(1), later - earlier);
        Assert.Throws<DocumentRuleException>(() => TravelTimes.Resolve(new(2027, 6, 1, 12, 0, 0), "Europe/Lisbon", "+02:00")); Assert.Equal(TimeSpan.FromHours(-4), TravelTimes.Resolve(new(2027, 6, 1, 12, 0, 0), "America/New_York", "-04:00").Offset);
    }
    [Fact]
    public void CustomEntriesCannotClaimSupplierConfirmation()
    {
        var e = ItineraryEntry.Custom(new(null, "Airport recommendation", "TEST", new(2027, 6, 1), "2027-06-01T08:30", "Europe/Lisbon", null, "LIS", 0)); Assert.False(e.Confirmed); Assert.Null(e.ReservationId); Assert.Equal("Information", e.Event); Assert.Equal(8, e.At!.Value.Hour);
    }
}
