using System.Globalization;
using System.Text.RegularExpressions;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using TravelAgency.Application.Documents;
using TravelAgency.Domain.Documents;
using TravelAgency.Infrastructure.Email;
namespace TravelAgency.Infrastructure.Documents;

public sealed class ItineraryPdf(EmailSettings brand, TimeProvider clock)
{
    private static readonly object FontLock = new();
    public byte[] Render(ItineraryDto itinerary, ItineraryVersionDto version, string language)
    {
        lock (FontLock) { GlobalFontSettings.FontResolver ??= new NotoResolver(); }
        var sv = language == "sv"; var culture = CultureInfo.GetCultureInfo(sv ? "sv-SE" : "en-GB"); var doc = new Document(); doc.Info.Title = version.Title; doc.Info.Author = brand.BusinessName;
        var normal = doc.Styles[StyleNames.Normal]!; normal.Font.Name = "Noto Sans"; normal.Font.Size = 10; normal.Font.Color = Colors.DarkSlateGray; normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(7);
        foreach (var (style, size) in new[] { (StyleNames.Heading1, 22), (StyleNames.Heading2, 14), (StyleNames.Heading3, 11) }) { doc.Styles[style]!.Font.Name = "Noto Sans"; doc.Styles[style]!.Font.Size = size; doc.Styles[style]!.Font.Color = new Color(24, 59, 86); doc.Styles[style]!.ParagraphFormat.KeepWithNext = true; }
        var section = doc.AddSection(); section.PageSetup.PageFormat = PageFormat.A4; section.PageSetup.TopMargin = Unit.FromCentimeter(2); section.PageSetup.BottomMargin = Unit.FromCentimeter(2); section.PageSetup.LeftMargin = Unit.FromCentimeter(2); section.PageSetup.RightMargin = Unit.FromCentimeter(2);
        var footer = section.Footers.Primary.AddParagraph(); footer.Format.Font.Size = 8; footer.AddText(brand.BusinessName + " · " + itinerary.Reference + " · "); footer.AddPageField(); footer.AddText(" / "); footer.AddNumPagesField();
        void Text(string value, string? style = null) { var p = style is null ? section.AddParagraph(Wrap(value)) : section.AddParagraph(Wrap(value), style); p.Format.KeepTogether = false; }
        Text(brand.BusinessName); Text(sv ? "Din resplan" : "Your travel itinerary", StyleNames.Heading1); Text(version.Title, StyleNames.Heading2);
        Text((sv ? "Bokning" : "Booking") + ": " + itinerary.Reference + " · v" + version.Version);
        if (version.Status != "Published") Text(sv ? "UTKAST / ADMINISTRATÖRSFÖRHANDSVISNING" : "DRAFT / ADMINISTRATOR PREVIEW");
        if (itinerary.SourceChanged) Text(sv ? "Bokningen har ändrats sedan denna version publicerades. Kontrollera aktuell bokningsstatus och kontakta reseagenten." : "The booking has changed since this version was prepared. Check current booking status and contact your travel agent.");
        Text((version.Trip.Destination ?? "") + " · " + (version.Trip.Departure ?? version.Trip.Earliest)?.ToString("d", culture) + " - " + (version.Trip.Return ?? version.Trip.Latest)?.ToString("d", culture));
        Text((sv ? "Resenärer: " : "Travelers: ") + (version.Trip.Adults + version.Trip.Children));
        if (version.Description.Length > 0) Text(version.Description);
        var labels = sv ? new Dictionary<string, string> { ["Departure"] = "Avgång", ["Arrival"] = "Ankomst", ["CheckIn"] = "Incheckning - tid ej angiven", ["CheckOut"] = "Utcheckning - tid ej angiven", ["Service"] = "Leverantörstjänst - tid ej angiven", ["Information"] = "Information / rekommendation" } : new Dictionary<string, string> { ["Departure"] = "Departure", ["Arrival"] = "Arrival", ["CheckIn"] = "Check-in - time not specified", ["CheckOut"] = "Check-out - time not specified", ["Service"] = "Supplier service - time not specified", ["Information"] = "Information / recommendation" };
        foreach (var day in version.Entries.GroupBy(e => e.Date).OrderBy(g => g.Key ?? DateOnly.MaxValue))
        {
            Text(day.Key?.ToString("dddd d MMMM yyyy", culture) ?? (sv ? "Datum ej angivet" : "Date not specified"), StyleNames.Heading2);
            foreach (var entry in day)
            {
                var local = entry.At is { } at ? TravelTimes.Local(at, entry.TimeZone!) : (DateTimeOffset?)null;
                Text((local?.ToString("HH:mm zzz", culture) is { } time ? time + " (" + entry.TimeZone + ") · " : "") + labels.GetValueOrDefault(entry.Event, entry.Event) + " · " + entry.Title, StyleNames.Heading3);
                Text(entry.Confirmed ? sv ? "Leverantörsbekräftad uppgift" : "Supplier-confirmed information" : sv ? "Endast information - ingen bekräftad bokning" : "Information only - not a confirmed booking");
                if (entry.Location.Length > 0) Text(entry.Location); if (entry.Description.Length > 0) Text(entry.Description);
                if (entry.SupplierReference.Length > 0) Text((sv ? "Leverantörsreferens: " : "Supplier reference: ") + entry.SupplierReference);
            }
        }
        Text((sv ? "Skapad (UTC): " : "Generated (UTC): ") + clock.GetUtcNow().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        var renderer = new PdfDocumentRenderer { Document = doc }; renderer.RenderDocument(); using var stream = new MemoryStream(); renderer.PdfDocument.Save(stream, false); return stream.ToArray();
    }
    private static string Wrap(string value) => Regex.Replace(value.Replace("→", "to"), @"[^\s]{55,}", m => string.Join("\n", m.Value.Chunk(50).Select(c => new string(c))));
    private sealed class NotoResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new(isBold ? "NotoSans-Bold" : "NotoSans-Regular", false, isItalic);
        public byte[] GetFont(string faceName) { using var stream = typeof(ItineraryPdf).Assembly.GetManifestResourceStream("TravelAgency.Infrastructure.Documents.Fonts." + faceName + ".ttf")!; using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray(); }
    }
}
