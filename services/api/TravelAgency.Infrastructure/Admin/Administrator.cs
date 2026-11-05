using Microsoft.AspNetCore.Identity;

namespace TravelAgency.Infrastructure.Admin;

public sealed class Administrator : IdentityUser
{
    // Existing Identity storage is shared; customer policy and cookie remain separate from administrator access.
    public bool IsCustomer { get; set; }
    public bool CustomerDisabled { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string PreferredLanguage { get; set; } = "sv";
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public string DisplayName { get; set; } = "Travel Agent";
}
