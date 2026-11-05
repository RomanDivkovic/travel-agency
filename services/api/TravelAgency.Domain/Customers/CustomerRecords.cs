namespace TravelAgency.Domain.Customers;

public enum CustomerActionPurpose { VerifyEmail, ResetPassword, ClaimRequest }
public sealed class CustomerRequestOwnership
{
    public Guid RequestId { get; set; }
    public string CustomerId { get; set; } = "";
    public DateTimeOffset LinkedAt { get; set; }
    public string Method { get; set; } = "EmailClaim";
}
public sealed class CustomerAccountAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = "";
    public Guid? RequestId { get; set; }
    public CustomerActionPurpose Purpose { get; set; }
    public string Hash { get; set; } = "";
    public string SecurityStamp { get; set; } = "";
    public string? ProtectedIdentityToken { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid VersionToken { get; set; } = Guid.NewGuid();
    public bool Available(DateTimeOffset now) => ConsumedAt is null && RevokedAt is null && ExpiresAt > now;
}
public sealed class CustomerSecurityEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
public static class CustomerValidation
{
    public static bool Text(string? value, int max, bool required = true) => value is not null && value.Length <= max && (!required || !string.IsNullOrWhiteSpace(value)) && !value.Any(char.IsControl);
    public static bool Language(string? language) => language is "sv" or "en";
    public static bool Email(string? email) => Text(email, 254) && System.Net.Mail.MailAddress.TryCreate(email!.Trim(), out var address) && address.Address == email.Trim();
}
