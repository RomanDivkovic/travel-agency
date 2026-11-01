using Microsoft.AspNetCore.DataProtection;
using TravelAgency.Infrastructure.Quotations;

namespace TravelAgency.Infrastructure.Payments;

public sealed class InvoiceTokenCodec(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("TravelAgency.Invoices.DeliveryLink.v1");
    public static string Create() => QuotationTokenCodec.Create();
    public static string? Hash(string? token) => QuotationTokenCodec.Hash(token);
    public string Protect(string link) => protector.Protect(link);
    public string Read(string envelope) => protector.Unprotect(envelope);
}
