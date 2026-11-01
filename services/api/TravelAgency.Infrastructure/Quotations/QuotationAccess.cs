using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace TravelAgency.Infrastructure.Quotations;

public sealed class QuotationAccessToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuotationId { get; set; }
    public string Hash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
public sealed partial class QuotationTokenCodec(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("TravelAgency.Quotations.DeliveryLink.v1");
    public static string Create() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    public static string? Hash(string? token) => token is { Length: 43 } && TokenPattern().IsMatch(token) ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))) : null;
    public string ProtectLink(string link) => protector.Protect(link);
    public string ReadLink(string protectedLink) => protector.Unprotect(protectedLink);
    [GeneratedRegex("^[A-Za-z0-9_-]{43}$")]
    private static partial Regex TokenPattern();
}
