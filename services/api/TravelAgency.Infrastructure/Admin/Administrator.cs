using Microsoft.AspNetCore.Identity;

namespace TravelAgency.Infrastructure.Admin;

public sealed class Administrator : IdentityUser
{
    public string DisplayName { get; set; } = "Travel Agent";
}
