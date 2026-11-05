using Microsoft.AspNetCore.DataProtection;
using TravelAgency.Infrastructure.Quotations;

namespace TravelAgency.Infrastructure.Customers;

public sealed class CustomerOperationException(int status) : Exception { public int Status { get; } = status; }
public sealed class CustomerSettings
{
    public string PublicOrigin { get; set; } = "";
    public int SessionMinutes { get; set; } = 60;
    public int ActionMinutes { get; set; } = 60;
    public void RequireDelivery() { if (PublicOrigin.Length == 0) throw new CustomerOperationException(412); }
}
public sealed class CustomerTokenCodec(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("TravelAgency.CustomerAccounts.Actions.v1");
    private readonly IDataProtector identity = provider.CreateProtector("TravelAgency.CustomerAccounts.IdentityToken.v1");
    public string ProtectIdentity(string value) => identity.Protect(value);
    public string ReadIdentity(string value) => identity.Unprotect(value);
    public static string Create() => QuotationTokenCodec.Create();
    public static string? Hash(string? raw) => QuotationTokenCodec.Hash(raw);
    public string Protect(string value) => protector.Protect(value);
    public string Read(string value) => protector.Unprotect(value);
}
