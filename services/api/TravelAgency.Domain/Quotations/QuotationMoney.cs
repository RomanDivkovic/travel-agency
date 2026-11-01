namespace TravelAgency.Domain.Quotations;

public enum TaxTreatment { Unconfirmed, VatExclusive, VatIncluded, NoVat }
public sealed class QuotationRuleException(string message) : Exception(message);
public sealed record LineSpecification(string Description, int Quantity, long UnitPriceMinorUnits, int TaxRateBasisPoints);
public sealed record QuotationDraft(string Description, string IncludedServices, string ExcludedServices, string TermsSnapshot,
    string CustomerNotes, string Currency, TaxTreatment TaxTreatment, DateTimeOffset ValidUntil, LineSpecification[] Lines);

public sealed class QuotationLineItem
{
    private QuotationLineItem() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid QuotationId { get; private set; }
    public string Description { get; private set; } = "";
    public int Quantity { get; private set; }
    public long UnitPriceMinorUnits { get; private set; }
    public int TaxRateBasisPoints { get; private set; }
    public long SubtotalMinorUnits { get; private set; }
    public long TaxMinorUnits { get; private set; }
    public long TotalMinorUnits { get; private set; }
    public int SortOrder { get; private set; }
    public static QuotationLineItem Calculate(Guid quotationId, LineSpecification line, TaxTreatment treatment, int order)
    {
        if (string.IsNullOrWhiteSpace(line.Description) || line.Description.Length > 200 || line.Quantity is < 1 or > 100
            || line.UnitPriceMinorUnits is < 0 or > Quotation.MaxTotal || line.TaxRateBasisPoints is < 0 or > 10000
            || !Enum.IsDefined(treatment) || (treatment is TaxTreatment.Unconfirmed or TaxTreatment.NoVat && line.TaxRateBasisPoints != 0))
            throw new QuotationRuleException("Invalid line item.");
        var amount = checked(line.Quantity * line.UnitPriceMinorUnits);
        var tax = treatment switch
        {
            TaxTreatment.VatExclusive => (long)decimal.Round(amount * (decimal)line.TaxRateBasisPoints / 10000m, 0, MidpointRounding.AwayFromZero),
            TaxTreatment.VatIncluded => (long)decimal.Round(amount * (decimal)line.TaxRateBasisPoints / (10000m + line.TaxRateBasisPoints), 0, MidpointRounding.AwayFromZero),
            _ => 0
        };
        var subtotal = treatment == TaxTreatment.VatIncluded ? amount - tax : amount;
        var total = treatment == TaxTreatment.VatIncluded ? amount : checked(amount + tax);
        if (total > Quotation.MaxTotal) throw new QuotationRuleException("Amount exceeds the supported limit.");
        return new()
        {
            QuotationId = quotationId,
            Description = line.Description.Trim(),
            Quantity = line.Quantity,
            UnitPriceMinorUnits = line.UnitPriceMinorUnits,
            TaxRateBasisPoints = line.TaxRateBasisPoints,
            SubtotalMinorUnits = subtotal,
            TaxMinorUnits = tax,
            TotalMinorUnits = total,
            SortOrder = order
        };
    }
}
