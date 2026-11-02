using TravelAgency.Application.Admin;
using TravelAgency.Domain.Research;

namespace TravelAgency.Application.Research;

public sealed record ResearchOptionDto(Guid Id, Guid VersionToken, ResearchSnapshot Option, string Notes, DateTimeOffset UpdatedAt);
public sealed record ResearchWorkspaceDto(bool Eligible, string Status, Guid? VersionToken, string Notes, ResearchOptionDto[] Options, PageResult<ProposalListItem> Proposals);
public sealed record AlternativeInput(string Title, string Description, bool Recommended, Guid[] ItemIds);
public sealed record ProposalDraftInput(Guid VersionToken, string Title, string Introduction, string Summary, string TermsSnapshot, DateTimeOffset ValidUntil, AlternativeInput[] Alternatives);
public sealed record EstimatedCost(string Currency, long FlightsMinorUnits, long AccommodationMinorUnits, long ExtrasMinorUnits, long TotalMinorUnits);
public sealed record AlternativeDto(Guid Id, string Title, string Description, bool Recommended, ResearchSnapshot[] Items, EstimatedCost[] EstimatedCosts);
public sealed record CustomerProposalDto(string Number, int Version, string Status, string Language, string Title, string Introduction, string Summary, string TermsSnapshot, string TermsVersion,
    string AcceptanceText, TripSnapshot Trip, PaidServiceSnapshot PaidServiceFee, DateTimeOffset ValidUntil, DateTimeOffset? ApprovedAt, DateTimeOffset? RejectedAt, Guid? SelectedAlternativeId, bool CanRespond, AlternativeDto[] Alternatives);
public sealed record AdminProposalDto(Guid Id, Guid RequestId, Guid VersionToken, string CustomerEmail, bool IsCurrentVersion, bool CanRevise, CustomerProposalDto Proposal, Guid[][] ItemIds, string? CustomerFeedback, bool CustomerLinkActive);
public sealed record ProposalListItem(Guid Id, string Number, int Version, Guid RequestId, string Title, string Status, DateTimeOffset ValidUntil, DateTimeOffset UpdatedAt, Guid? SelectedAlternativeId, string? SelectedAlternativeTitle = null);
public sealed record ProposalDecisionInput(int Version, bool Confirmed, Guid? AlternativeId, string? TermsVersion, string? Feedback);
public static class ProposalDtos
{
    public static EstimatedCost[] Costs(IEnumerable<ResearchSnapshot> items) => items.GroupBy(i => i.Currency).OrderBy(g => g.Key).Select(g => new EstimatedCost(g.Key,
        g.Where(i => i.Kind == "Flight").Sum(i => i.TotalMinorUnits), g.Where(i => i.Kind == "Accommodation").Sum(i => i.TotalMinorUnits), g.Where(i => i.Kind == "Extra").Sum(i => i.TotalMinorUnits), g.Sum(i => i.TotalMinorUnits))).ToArray();
    public static CustomerProposalDto Customer(TravelProposal p, string number, bool eligible) => new(number, p.Version, p.Status.ToString(), p.Language, p.Title, p.Introduction, p.Summary, p.TermsSnapshot, p.TermsVersion, p.AcceptanceText,
        p.Trip(), p.ServiceFee(), p.ValidUntil, p.ApprovedAt, p.RejectedAt, p.SelectedAlternativeId, eligible && p.Status == ProposalStatus.Sent,
        p.Alternatives.OrderBy(a => a.SortOrder).Select(a => new AlternativeDto(a.Id, a.Title, a.Description, a.Recommended, a.Items.OrderBy(i => i.SortOrder).Select(i => i.Snapshot()).ToArray(), Costs(a.Items.Select(i => i.Snapshot())))).ToArray());
    public static ProposalListItem Item(TravelProposal p, ProposalSeries s, string? selectedTitle = null) => new(p.Id, s.Number, p.Version, s.RequestId, p.Title, p.Status.ToString(), p.ValidUntil, p.UpdatedAt, p.SelectedAlternativeId, selectedTitle);
}
