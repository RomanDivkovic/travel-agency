namespace TravelAgency.Domain.Quotations;

public enum QuotationStatus { Draft, Sent, Accepted, Declined, Expired, Superseded, Cancelled }
public sealed class QuotationSeries
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public string Number { get; set; } = "";
    public int CurrentVersion { get; set; } = 1;
}
public sealed class Quotation
{
    public const long MaxTotal = 10_000_000_000;
    private Quotation() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SeriesId { get; private set; }
    public int Version { get; private set; }
    public Guid VersionToken { get; private set; } = Guid.NewGuid();
    public QuotationStatus Status { get; private set; }
    public string Language { get; private set; } = "sv";
    public string CustomerName { get; private set; } = "";
    public string CustomerEmail { get; private set; } = "";
    public string CreatedByAdministratorId { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string IncludedServices { get; private set; } = "";
    public string ExcludedServices { get; private set; } = "";
    public string TermsSnapshot { get; private set; } = "";
    public string TermsVersion { get; private set; } = "";
    public string AcceptanceTextSnapshot { get; private set; } = "";
    public string? AcceptedTermsVersion { get; private set; }
    public string CustomerNotes { get; private set; } = "";
    public string Currency { get; private set; } = "SEK";
    public TaxTreatment TaxTreatment { get; private set; }
    public long SubtotalMinorUnits { get; private set; }
    public long TaxMinorUnits { get; private set; }
    public long TotalMinorUnits { get; private set; }
    public DateTimeOffset ValidUntil { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? DeclinedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    private readonly List<QuotationLineItem> lines = [];
    public IReadOnlyList<QuotationLineItem> Lines => lines;
    public bool CanRevise => Status is QuotationStatus.Sent or QuotationStatus.Declined or QuotationStatus.Expired or QuotationStatus.Cancelled;
    public static DateTimeOffset Utc(DateTimeOffset now)
    {
        if (now.Offset != TimeSpan.Zero) throw new QuotationRuleException("UTC timestamp required.");
        return now.AddTicks(-(now.Ticks % 10));
    }
    public static Quotation Create(Guid series, int version, string language, string customerName, string customerEmail, string actor, QuotationDraft draft, DateTimeOffset now)
    {
        if (series == Guid.Empty || version < 1 || language is not ("sv" or "en") || string.IsNullOrWhiteSpace(customerName) || customerName.Length > 100
            || string.IsNullOrWhiteSpace(customerEmail) || customerEmail.Length > 254 || string.IsNullOrWhiteSpace(actor)) throw new QuotationRuleException("Invalid quotation identity.");
        var quote = new Quotation { SeriesId = series, Version = version, Language = language, CustomerName = customerName, CustomerEmail = customerEmail, CreatedByAdministratorId = actor, CreatedAt = Utc(now) };
        quote.UpdateDraft(draft, now); return quote;
    }
    public void UpdateDraft(QuotationDraft draft, DateTimeOffset now)
    {
        if (Status != QuotationStatus.Draft) throw new QuotationRuleException("Issued quotations cannot be edited.");
        now = Utc(now);
        if (string.IsNullOrWhiteSpace(draft.Description) || draft.Description.Length > 1000 || draft.IncludedServices.Length > 2000 || draft.ExcludedServices.Length > 2000
            || draft.TermsSnapshot.Length > 6000 || draft.CustomerNotes.Length > 2000 || draft.Currency is not ("SEK" or "EUR" or "USD")
            || !Enum.IsDefined(draft.TaxTreatment) || draft.ValidUntil.Offset != TimeSpan.Zero || draft.ValidUntil <= now || draft.ValidUntil > now.AddDays(366)
            || draft.Lines.Length is < 1 or > 20) throw new QuotationRuleException("Invalid draft fields.");
        var lines = draft.Lines.Select((l, i) => QuotationLineItem.Calculate(Id, l, draft.TaxTreatment, i)).ToArray();
        var total = lines.Sum(l => l.TotalMinorUnits); if (total > MaxTotal) throw new QuotationRuleException("Amount exceeds the supported limit.");
        Description = draft.Description.Trim(); IncludedServices = draft.IncludedServices.Trim(); ExcludedServices = draft.ExcludedServices.Trim();
        TermsSnapshot = draft.TermsSnapshot.Trim(); CustomerNotes = draft.CustomerNotes.Trim(); Currency = draft.Currency; TaxTreatment = draft.TaxTreatment;
        ValidUntil = Utc(draft.ValidUntil); SubtotalMinorUnits = lines.Sum(l => l.SubtotalMinorUnits); TaxMinorUnits = lines.Sum(l => l.TaxMinorUnits); TotalMinorUnits = total;
        this.lines.Clear(); this.lines.AddRange(lines); Touch(now);
    }
    public void Issue(string termsVersion, string acceptanceText, DateTimeOffset now)
    {
        if (Status != QuotationStatus.Draft || ValidUntil <= now || TotalMinorUnits <= 0 || TaxTreatment == TaxTreatment.Unconfirmed
            || string.IsNullOrWhiteSpace(IncludedServices) || string.IsNullOrWhiteSpace(ExcludedServices) || string.IsNullOrWhiteSpace(TermsSnapshot)
            || string.IsNullOrWhiteSpace(termsVersion) || termsVersion.Length > 100 || string.IsNullOrWhiteSpace(acceptanceText) || acceptanceText.Length > 2000)
            throw new QuotationRuleException("The quotation is not ready to issue.");
        TermsVersion = termsVersion; AcceptanceTextSnapshot = acceptanceText; Status = QuotationStatus.Sent; SentAt = Utc(now); Touch(now);
    }
    public bool Expire(DateTimeOffset now)
    {
        if (Status != QuotationStatus.Sent || ValidUntil > now) return false;
        Status = QuotationStatus.Expired; Touch(now); return true;
    }
    public bool Respond(bool accept, DateTimeOffset now)
    {
        var target = accept ? QuotationStatus.Accepted : QuotationStatus.Declined;
        if (Status == target) return false; // Replay of the same explicit decision is idempotent.
        if (Status != QuotationStatus.Sent || ValidUntil <= now) throw new QuotationRuleException("The quotation can no longer be answered.");
        Status = target;
        if (accept) { AcceptedAt = Utc(now); AcceptedTermsVersion = TermsVersion; } else DeclinedAt = Utc(now);
        Touch(now); return true;
    }
    public void Cancel(DateTimeOffset now)
    {
        if (Status is not (QuotationStatus.Draft or QuotationStatus.Sent)) throw new QuotationRuleException("The quotation cannot be cancelled.");
        Status = QuotationStatus.Cancelled; Touch(now);
    }
    public void Supersede(DateTimeOffset now)
    {
        if (Status != QuotationStatus.Sent) throw new QuotationRuleException("Only an issued offer can be superseded.");
        Status = QuotationStatus.Superseded; Touch(now);
    }
    public Quotation Revise(string actor, DateTimeOffset until, string terms, DateTimeOffset now)
    {
        if (!CanRevise) throw new QuotationRuleException("This version cannot be revised.");
        return Create(SeriesId, Version + 1, Language, CustomerName, CustomerEmail, actor, CopyDraft(until, terms), now);
    }
    private QuotationDraft CopyDraft(DateTimeOffset until, string terms) => new(Description, IncludedServices, ExcludedServices, terms, CustomerNotes, Currency, TaxTreatment, until,
        Lines.OrderBy(l => l.SortOrder).Select(l => new LineSpecification(l.Description, l.Quantity, l.UnitPriceMinorUnits, l.TaxRateBasisPoints)).ToArray());
    private void Touch(DateTimeOffset now) { UpdatedAt = Utc(now); VersionToken = Guid.NewGuid(); }
}
