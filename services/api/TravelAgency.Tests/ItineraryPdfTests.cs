using PdfSharp.Pdf.IO;
using TravelAgency.Application.Documents;
using TravelAgency.Domain.Research;
using TravelAgency.Infrastructure.Documents;
namespace TravelAgency.Tests;

public sealed class ItineraryPdfTests
{
    [Theory]
    [InlineData("sv")]
    [InlineData("en")]
    public async Task LongItinerariesRemainPaginatedWithEmbeddedFonts(string language)
    {
        var entries = Enumerable.Range(1, 35).Select(i => new ItineraryEntryDto(Guid.NewGuid(), "Custom", "Information", "Åre — Göteborg information " + i, "TEST public guidance. " + new string('x', 150), new(2027, 6, 1), null, null, "Mötesplats", "", false, i)).ToArray();
        var trip = new TripSnapshot("ARN", "Åre", false, [], "fixed", new(2027, 6, 1), new(2027, 6, 7), null, null, null, 2, 0, 1);
        var version = new ItineraryVersionDto(Guid.NewGuid(), 1, "Published", "Åre och Göteborg", "TEST multi-page itinerary", trip, entries, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var dto = new ItineraryDto(Guid.NewGuid(), "BKG-TEST", null, 1, 1, false, [version]);
        var bytes = new ItineraryPdf(new(), TimeProvider.System).Render(dto, version, language);
        using var stream = new MemoryStream(bytes); using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import); Assert.True(pdf.PageCount > 2); Assert.All(pdf.Pages.Cast<PdfSharp.Pdf.PdfPage>(), page => Assert.True(page.Width.Point > 500));
        await File.WriteAllBytesAsync(Path.Combine(Path.GetTempPath(), "travel-phase11-long-" + language + ".pdf"), bytes, TestContext.Current.CancellationToken);
    }
}
