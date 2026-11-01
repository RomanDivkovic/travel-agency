using System.Text.Json;
using TravelAgency.Domain.Quotations;

namespace TravelAgency.Domain.Payments;

public enum InvoiceStatus { Draft, Issued, PaymentPending, Paid, Overdue, Cancelled }
public sealed class InvoiceRuleException(string message) : Exception(message);
public sealed record InvoiceLine(string Description, int Quantity, long UnitPriceMinorUnits, int TaxRateBasisPoints, long SubtotalMinorUnits, long TaxMinorUnits, long TotalMinorUnits);
public sealed record InvoiceDetails(string CustomerAddress, string ServicePeriod, DateTimeOffset DueAt);
public sealed class Invoice
{
    private Invoice() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RequestId { get; private set; }
    public Guid QuotationId { get; private set; }
    public int QuotationVersion { get; private set; }
    public string QuotationNumber { get; private set; } = "";
    public string? Number { get; private set; }
    public Guid VersionToken { get; private set; } = Guid.NewGuid();
    public InvoiceStatus Status { get; private set; }
    public bool IsSimulated { get; private set; }
    public string Language { get; private set; } = "sv";
    public string CustomerName { get; private set; } = "";
    public string CustomerEmail { get; private set; } = "";
    public string CustomerAddress { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string ServicePeriod { get; private set; } = "";
    public string Currency { get; private set; } = "SEK";
    public string TaxTreatment { get; private set; } = "";
    public long SubtotalMinorUnits { get; private set; }
    public long TaxMinorUnits { get; private set; }
    public long TotalMinorUnits { get; private set; }
    public string LinesJson { get; private set; } = "[]";
    public string SellerName { get; private set; } = "";
    public string SellerAddress { get; private set; } = "";
    public string RegistrationNumber { get; private set; } = "";
    public string TaxInformation { get; private set; } = "";
    public string Terms { get; private set; } = "";
    public string TermsVersion { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset DueAt { get; private set; }
    public DateTimeOffset? IssuedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public static Invoice Create(Quotation q, Guid requestId, string quotationNumber, bool simulated, DateTimeOffset now)
    {
        if (q.Status != QuotationStatus.Accepted || q.AcceptedAt is null || q.TotalMinorUnits <= 0 || q.TaxTreatment == TravelAgency.Domain.Quotations.TaxTreatment.Unconfirmed)
            throw new InvoiceRuleException("An accepted priced quotation is required.");
        return new Invoice
        {
            RequestId = requestId,
            QuotationId = q.Id,
            QuotationVersion = q.Version,
            QuotationNumber = quotationNumber,
            IsSimulated = simulated,
            Language = q.Language,
            CustomerName = q.CustomerName,
            CustomerEmail = q.CustomerEmail,
            Description = q.Description,
            Currency = q.Currency,
            TaxTreatment = q.TaxTreatment.ToString(),
            SubtotalMinorUnits = q.SubtotalMinorUnits,
            TaxMinorUnits = q.TaxMinorUnits,
            TotalMinorUnits = q.TotalMinorUnits,
            LinesJson = JsonSerializer.Serialize(q.Lines.OrderBy(l => l.SortOrder).Select(l => new InvoiceLine(l.Description, l.Quantity, l.UnitPriceMinorUnits, l.TaxRateBasisPoints, l.SubtotalMinorUnits, l.TaxMinorUnits, l.TotalMinorUnits))),
            CreatedAt = Quotation.Utc(now),
            UpdatedAt = Quotation.Utc(now),
            DueAt = Quotation.Utc(now.AddDays(14))
        };
    }
    public InvoiceLine[] Lines() => JsonSerializer.Deserialize<InvoiceLine[]>(LinesJson)!;
    public void Edit(InvoiceDetails details, DateTimeOffset now)
    {
        if (Status != InvoiceStatus.Draft || string.IsNullOrWhiteSpace(details.CustomerAddress) || details.CustomerAddress.Length > 1000
            || string.IsNullOrWhiteSpace(details.ServicePeriod) || details.ServicePeriod.Length > 300 || details.DueAt.Offset != TimeSpan.Zero || details.DueAt <= now || details.DueAt > now.AddDays(366))
            throw new InvoiceRuleException("Check draft billing details and future due date.");
        CustomerAddress = details.CustomerAddress.Trim(); ServicePeriod = details.ServicePeriod.Trim(); DueAt = Quotation.Utc(details.DueAt); Touch(now);
    }
    public void Issue(string number, string seller, string address, string registration, string taxInformation, string terms, string termsVersion, DateTimeOffset now)
    {
        if (Status != InvoiceStatus.Draft || DueAt <= now || new[] { number, seller, address, registration, taxInformation, terms, termsVersion, CustomerAddress, ServicePeriod }.Any(string.IsNullOrWhiteSpace))
            throw new InvoiceRuleException("Approved complete invoice details required.");
        Number = number; SellerName = seller; SellerAddress = address; RegistrationNumber = registration; TaxInformation = taxInformation;
        Terms = terms; TermsVersion = termsVersion; Status = InvoiceStatus.Issued; IssuedAt = Quotation.Utc(now); Touch(now);
    }
    public void BeginPayment(DateTimeOffset now)
    {
        if (Status is not (InvoiceStatus.Issued or InvoiceStatus.PaymentPending or InvoiceStatus.Overdue)) throw new InvoiceRuleException("Invoice is not payable.");
        Status = DueAt <= now ? InvoiceStatus.Overdue : InvoiceStatus.PaymentPending; Touch(now);
    }
    public bool MarkPaid(long amount, string currency, DateTimeOffset now)
    {
        if (amount != TotalMinorUnits || currency != Currency || Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled) throw new InvoiceRuleException("Verified payment does not match this invoice.");
        if (Status == InvoiceStatus.Paid) return false;
        Status = InvoiceStatus.Paid; PaidAt = Quotation.Utc(now); Touch(now); return true;
    }
    public bool MarkOverdue(DateTimeOffset now)
    {
        if (DueAt > now || Status is not (InvoiceStatus.Issued or InvoiceStatus.PaymentPending)) return false;
        Status = InvoiceStatus.Overdue; Touch(now); return true;
    }
    public void CancelDraft(DateTimeOffset now)
    {
        if (Status != InvoiceStatus.Draft) throw new InvoiceRuleException("Issued invoices require an approved correction workflow.");
        Status = InvoiceStatus.Cancelled; CancelledAt = Quotation.Utc(now); Touch(now);
    }
    public void RecordAccessRevocation(DateTimeOffset now)
    {
        if (IssuedAt is null) throw new InvoiceRuleException("Only issued invoice links can be revoked."); Touch(now);
    }
    private void Touch(DateTimeOffset now) { UpdatedAt = Quotation.Utc(now); VersionToken = Guid.NewGuid(); }
}
