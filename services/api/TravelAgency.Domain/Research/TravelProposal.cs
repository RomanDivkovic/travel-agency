using System.Text.Json;
using TravelAgency.Domain.Quotations;

namespace TravelAgency.Domain.Research;

public enum ProposalStatus { Draft, Sent, Approved, Rejected, Expired, Superseded, Cancelled }
public sealed class ProposalSeries
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public string Number { get; set; } = "";
    public int CurrentVersion { get; set; } = 1;
}
public sealed record TripSnapshot(string? Origin, string? Destination, bool FlexibleDestination, string[] AdditionalStops, string DateMode, DateOnly? Departure, DateOnly? Return, DateOnly? Earliest, DateOnly? Latest, int? DurationDays, int Adults, int Children, int? Rooms);
public sealed record PaidServiceSnapshot(string InvoiceNumber, string Currency, long AmountMinorUnits, DateTimeOffset PaidAt);
public sealed class TravelProposal
{
    private TravelProposal() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SeriesId { get; private set; }
    public int Version { get; private set; }
    public Guid VersionToken { get; private set; } = Guid.NewGuid();
    public ProposalStatus Status { get; private set; }
    public string Language { get; private set; } = "sv";
    public string CustomerName { get; private set; } = "";
    public string CustomerEmail { get; private set; } = "";
    public string CreatedByAdministratorId { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string Introduction { get; private set; } = "";
    public string Summary { get; private set; } = "";
    public string TermsSnapshot { get; private set; } = "";
    public string TermsVersion { get; private set; } = "";
    public string AcceptanceText { get; private set; } = "";
    public string TripJson { get; private set; } = "";
    public string PaidServiceJson { get; private set; } = "";
    public DateTimeOffset ValidUntil { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public Guid? SelectedAlternativeId { get; private set; }
    private readonly List<ProposalAlternative> alternatives = [];
    public IReadOnlyList<ProposalAlternative> Alternatives => alternatives;
    public bool CanRevise => Status is ProposalStatus.Sent or ProposalStatus.Rejected or ProposalStatus.Expired or ProposalStatus.Cancelled;
    public static TravelProposal Create(Guid seriesId, int version, string language, string name, string email, string actor, TripSnapshot trip, PaidServiceSnapshot service, DateTimeOffset until, DateTimeOffset now)
    {
        if (language is not ("sv" or "en") || !ResearchValidation.Text(name, 100) || !ResearchValidation.Text(email, 254) || string.IsNullOrWhiteSpace(actor) || version < 1) throw new ResearchRuleException("Invalid proposal identity.");
        var p = new TravelProposal
        {
            SeriesId = seriesId,
            Version = version,
            Language = language,
            CustomerName = name,
            CustomerEmail = email,
            CreatedByAdministratorId = actor,
            TripJson = JsonSerializer.Serialize(trip),
            PaidServiceJson = JsonSerializer.Serialize(service),
            CreatedAt = Quotation.Utc(now)
        };
        p.Edit(language == "sv" ? "Ditt personliga reseförslag" : "Your personalized travel proposal", "", "", "", until, [], now); return p;
    }
    public void Edit(string title, string intro, string summary, string terms, DateTimeOffset until, ProposalAlternative[] choices, DateTimeOffset now)
    {
        if (Status != ProposalStatus.Draft || !ResearchValidation.Text(title, 200) || !ResearchValidation.Text(intro, 3000, false) || !ResearchValidation.Text(summary, 3000, false) || !ResearchValidation.Text(terms, 6000, false)
            || until.Offset != TimeSpan.Zero || until <= now || until > now.AddDays(366) || choices.Length > 6 || choices.Count(a => a.Recommended) > 1 || choices.Select(a => a.Id).Distinct().Count() != choices.Length) throw new ResearchRuleException("Invalid proposal draft.");
        Title = title.Trim(); Introduction = intro.Trim(); Summary = summary.Trim(); TermsSnapshot = terms.Trim(); ValidUntil = Quotation.Utc(until);
        alternatives.Clear(); alternatives.AddRange(choices); Touch(now);
    }
    public void Send(string termsVersion, string acknowledgement, DateTimeOffset now)
    {
        if (Status != ProposalStatus.Draft || ValidUntil <= now || alternatives.Count == 0 || alternatives.Any(a => a.Items.Count == 0)
            || !ResearchValidation.Text(Introduction, 3000) || !ResearchValidation.Text(Summary, 3000) || !ResearchValidation.Text(TermsSnapshot, 6000)
            || !ResearchValidation.Text(termsVersion, 100) || !ResearchValidation.Text(acknowledgement, 2000)) throw new ResearchRuleException("Complete proposal and reviewed terms required.");
        TermsVersion = termsVersion; AcceptanceText = acknowledgement; Status = ProposalStatus.Sent; SentAt = Quotation.Utc(now); Touch(now);
    }
    public bool Expire(DateTimeOffset now) { if (Status != ProposalStatus.Sent || ValidUntil > now) return false; Status = ProposalStatus.Expired; Touch(now); return true; }
    public bool Respond(bool approve, Guid? choice, DateTimeOffset now)
    {
        var target = approve ? ProposalStatus.Approved : ProposalStatus.Rejected;
        if (Status == target && (!approve || SelectedAlternativeId == choice)) return false;
        if (Status != ProposalStatus.Sent || ValidUntil <= now || (approve && (choice is null || !alternatives.Any(a => a.Id == choice)))) throw new ResearchRuleException("Proposal cannot be answered with this selection.");
        Status = target; if (approve) { ApprovedAt = Quotation.Utc(now); SelectedAlternativeId = choice; } else RejectedAt = Quotation.Utc(now); Touch(now); return true;
    }
    public void Cancel(DateTimeOffset now) { if (Status is not (ProposalStatus.Draft or ProposalStatus.Sent)) throw new ResearchRuleException("Cannot cancel this proposal."); Status = ProposalStatus.Cancelled; Touch(now); }
    public void Supersede(DateTimeOffset now) { if (Status != ProposalStatus.Sent) throw new ResearchRuleException("Only sent proposals are superseded."); Status = ProposalStatus.Superseded; Touch(now); }
    public TravelProposal Revise(string actor, DateTimeOffset until, DateTimeOffset now)
    {
        if (!CanRevise) throw new ResearchRuleException("Approved proposals are final.");
        var p = Create(SeriesId, Version + 1, Language, CustomerName, CustomerEmail, actor, Trip(), ServiceFee(), until, now);
        p.Edit(Title, Introduction, Summary, TermsSnapshot, until, alternatives.OrderBy(a => a.SortOrder).Select(a => a.Clone(p.Id)).ToArray(), now); return p;
    }
    public void RecordAccessRevocation(DateTimeOffset now)
    {
        if (SentAt is null) throw new ResearchRuleException("Only issued proposal access can be revoked."); Touch(now);
    }
    public TripSnapshot Trip() => JsonSerializer.Deserialize<TripSnapshot>(TripJson)!;
    public PaidServiceSnapshot ServiceFee() => JsonSerializer.Deserialize<PaidServiceSnapshot>(PaidServiceJson)!;
    private void Touch(DateTimeOffset now) { UpdatedAt = Quotation.Utc(now); VersionToken = Guid.NewGuid(); }
}
public sealed class ProposalAlternative
{
    private ProposalAlternative() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProposalId { get; private set; }
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public bool Recommended { get; private set; }
    public int SortOrder { get; private set; }
    private readonly List<ProposalItem> items = [];
    public IReadOnlyList<ProposalItem> Items => items;
    public static ProposalAlternative Create(Guid proposalId, string title, string description, bool recommended, int order, IEnumerable<ProposalItem> items)
    {
        var snapshot = items.ToArray();
        if (!ResearchValidation.Text(title, 200) || !ResearchValidation.Text(description, 2000) || snapshot.Length is < 1 or > 20 || snapshot.Select(i => i.ResearchOptionId).Distinct().Count() != snapshot.Length)
            throw new ResearchRuleException("Complete alternative details and distinct items required.");
        var a = new ProposalAlternative { ProposalId = proposalId, Title = title.Trim(), Description = description.Trim(), Recommended = recommended, SortOrder = order };
        a.items.AddRange(snapshot); return a;
    }
    public ProposalAlternative Clone(Guid proposal) => Create(proposal, Title, Description, Recommended, SortOrder,
        Items.OrderBy(i => i.SortOrder).Select(i => ProposalItem.Create(i.ResearchOptionId, i.SourceVersion, i.SnapshotJson, i.SortOrder)));
}
public sealed class ProposalItem
{
    private ProposalItem() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AlternativeId { get; private set; }
    public Guid ResearchOptionId { get; private set; }
    public Guid SourceVersion { get; private set; }
    public int SortOrder { get; private set; }
    public string SnapshotJson { get; private set; } = "";
    public static ProposalItem Create(Guid source, Guid version, string snapshot, int order)
    {
        if (source == Guid.Empty || version == Guid.Empty || string.IsNullOrWhiteSpace(snapshot)) throw new ResearchRuleException("A saved source snapshot is required.");
        return new ProposalItem { ResearchOptionId = source, SourceVersion = version, SnapshotJson = snapshot, SortOrder = order };
    }
    public ResearchSnapshot Snapshot() => JsonSerializer.Deserialize<ResearchSnapshot>(SnapshotJson)!;
}
public sealed class ProposalAccessToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProposalId { get; set; }
    public string Hash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
public sealed class ProposalDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProposalId { get; set; }
    public Guid? AlternativeId { get; set; }
    public bool Approved { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
    public string TermsVersion { get; set; } = "";
    public string Acknowledgement { get; set; } = "";
    public string Feedback { get; set; } = "";
}
