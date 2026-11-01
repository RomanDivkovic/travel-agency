using System.Globalization;
using System.Net;
using TravelAgency.Domain.Payments;

namespace TravelAgency.Infrastructure.Payments;

public static class InvoiceDocument
{
    public static string Html(Invoice i)
    {
        var sv = i.Language == "sv"; string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        string Money(long n) => (n / 100m).ToString("F2", CultureInfo.InvariantCulture) + " " + E(i.Currency);
        var rows = string.Join("", i.Lines().Select(l => $"<tr><td>{E(l.Description)}</td><td>{l.Quantity}</td><td>{Money(l.UnitPriceMinorUnits)}</td><td>{l.TaxRateBasisPoints / 100m}%</td><td>{Money(l.TotalMinorUnits)}</td></tr>"));
        return $"""
            <!doctype html><html lang="{i.Language}"><meta charset="utf-8"><meta name="referrer" content="no-referrer"><meta name="viewport" content="width=device-width"><title>{(sv ? "Faktura" : "Invoice")} {E(i.Number)}</title>
            <body><main><h1>{(sv ? "Faktura" : "Invoice")} {E(i.Number)}</h1><p>{(i.IsSimulated ? "TEST / SIMULATED — no money collected; not an accounting invoice." : "")}</p>
            <h2>{E(i.SellerName)}</h2><p>{E(i.SellerAddress)}</p><p>{E(i.RegistrationNumber)}</p><p>{E(i.TaxInformation)}</p>
            <h2>{E(i.CustomerName)}</h2><p>{E(i.CustomerAddress)}</p><p>{E(i.Description)}</p>
            <p>{(sv ? "Offert" : "Quotation")}: {E(i.QuotationNumber)} v{i.QuotationVersion}</p><p>{(sv ? "Utfärdad" : "Issued")}: {i.IssuedAt:yyyy-MM-dd} UTC</p><p>{(sv ? "Förfallodatum" : "Due date")}: {i.DueAt:yyyy-MM-dd} UTC</p><p>{(sv ? "Tjänsteperiod" : "Service period")}: {E(i.ServicePeriod)}</p>
            <table><caption>{(sv ? "Serviceavgift" : "Service fee")}</caption><thead><tr><th scope="col">{(sv ? "Beskrivning" : "Description")}</th><th scope="col">{(sv ? "Antal" : "Quantity")}</th><th scope="col">{(sv ? "Styckpris" : "Unit price")}</th><th scope="col">{(sv ? "Momssats" : "Tax rate")}</th><th scope="col">{(sv ? "Belopp" : "Amount")}</th></tr></thead><tbody>{rows}</tbody></table>
            <p>{(sv ? "Delsumma" : "Subtotal")}: {Money(i.SubtotalMinorUnits)}</p><p>{(sv ? "Moms" : "Tax")}: {Money(i.TaxMinorUnits)}</p><p><strong>{(sv ? "Totalt" : "Total")}: {Money(i.TotalMinorUnits)}</strong></p><p>{E(i.TaxTreatment)}</p>
            <p>{E(i.Terms)}</p><p>{E(i.TermsVersion)}</p><p>{(sv ? "Serviceavgiften bekräftar inte flyg, hotell eller andra bokningar." : "The service fee does not confirm flights, hotels or other bookings.")}</p></main></body></html>
            """;
    }
}
