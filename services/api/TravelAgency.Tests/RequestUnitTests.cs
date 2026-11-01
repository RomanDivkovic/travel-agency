using System.Net;
using System.Text.Json;
using TravelAgency.Application.Requests;
using TravelAgency.Domain.Requests;
using TravelAgency.Infrastructure.Email;

namespace TravelAgency.Tests;

public sealed class RequestUnitTests
{
    [Theory]
    [InlineData("flights")]
    [InlineData("accommodation")]
    [InlineData("flight-accommodation")]
    [InlineData("complete")]
    public void AllServicesValidateIndependently(string service) => Assert.Empty(RequestValidator.Validate(RequestExamples.Input(service), DateOnly.FromDateTime(DateTime.UtcNow)));
    [Fact]
    public void DomainCreatesOnlySubmittedWithUtcImmutableAuditFields()
    {
        var r = RequestExamples.Inquiry();
        Assert.Equal(RequestStatus.Submitted, r.Status); Assert.Equal(r.CreatedAt, r.UpdatedAt); Assert.Equal(TimeSpan.Zero, r.CreatedAt.Offset);
        Assert.NotEqual(Guid.Empty, r.Id); Assert.Equal("TRV-2026-000001", r.ReferenceNumber);
        Assert.Throws<ArgumentException>(() => TravelInquiry.Create("", r.Service, "sv", r.Contact, r.Plan, r.CreatedAt));
        Assert.Throws<ArgumentException>(() => TravelInquiry.Create(r.ReferenceNumber, r.Service, "de", r.Contact, r.Plan, r.CreatedAt));
        Assert.Throws<ArgumentException>(() => TravelInquiry.Create(r.ReferenceNumber, r.Service, "sv", r.Contact, r.Plan with { BudgetAmount = 0 }, r.CreatedAt));
        Assert.Throws<ArgumentException>(() => TravelInquiry.Create(r.ReferenceNumber, r.Service, "sv", r.Contact, r.Plan, r.CreatedAt.ToOffset(TimeSpan.FromHours(1))));
    }
    [Fact]
    public void ValidatorRejectsMissingInvalidAndConditionalFields()
    {
        var r = RequestExamples.Input(); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.Contains("service", RequestValidator.Validate(r with { Service = "unknown" }, today));
        Assert.Contains("place", RequestValidator.Validate(r with { Destination = null }, today));
        Assert.Contains("origin", RequestValidator.Validate(r with { Destination = r.Destination! with { Origin = " " } }, today));
        Assert.Contains("return", RequestValidator.Validate(r with { Dates = r.Dates! with { Return = r.Dates.Departure } }, today));
        Assert.Contains("departure", RequestValidator.Validate(r with { Dates = r.Dates! with { Departure = today.AddDays(-1) } }, today));
        Assert.Contains("children", RequestValidator.Validate(r with { Travelers = new(20, [7], 1) }, today));
        Assert.Contains("age-0", RequestValidator.Validate(r with { Travelers = new(1, [18], 1) }, today));
        Assert.Contains("amount", RequestValidator.Validate(r with { Budget = new(0, "SEK", "total") }, today));
        Assert.Contains("amount", RequestValidator.Validate(r with { Budget = new(1.001m, "SEK", "total") }, today));
        Assert.Contains("email", RequestValidator.Validate(r with { Contact = r.Contact! with { Email = "bad\r\nBcc:bad" } }, today));
        Assert.Contains("phone", RequestValidator.Validate(r with { Contact = r.Contact! with { PreferredMethod = "phone" } }, today));
        Assert.Contains("priority", RequestValidator.Validate(r with { Preferences = r.Preferences! with { Flight = null } }, today));
        Assert.Contains("request", RequestValidator.Validate(r with { Website = "spam" }, today));
        var single = RequestExamples.Input("flights");
        Assert.Empty(RequestValidator.Validate(single with { Dates = single.Dates! with { Return = null } }, today));
        Assert.Contains("rooms", RequestValidator.Validate(single with { Travelers = single.Travelers! with { Rooms = 1 } }, today));
        var flexible = r with { Destination = r.Destination! with { Place = null, Flexible = true }, Dates = new("flexible", null, null, today.AddDays(10), today.AddDays(20), 7) };
        Assert.Empty(RequestValidator.Validate(flexible, today));
        Assert.Contains("duration", RequestValidator.Validate(flexible with { Dates = flexible.Dates! with { DurationDays = 90 } }, today));
    }
    [Theory]
    [InlineData("sv", "Vi har tagit emot")]
    [InlineData("en", "We’ve received")]
    public void CustomerEmailsHaveBothAlternativesAndEscapeUserContent(string language, string subject)
    {
        var r = RequestExamples.Inquiry(language, "<script>alert(1)</script> Person");
        var email = NotificationTemplates.Create(Guid.NewGuid(), "customer", r, new EmailSettings());
        Assert.Contains(subject, email.Subject); Assert.Contains(r.ReferenceNumber, email.Text);
        Assert.DoesNotContain("<script>", email.Html); Assert.Contains("&lt;script&gt;", email.Html);
        Assert.Contains(language == "sv" ? "inte innebär" : "does not create", email.Text);
        Assert.Contains("viewport", email.Html); Assert.DoesNotContain("<script>", email.Subject);
        var agent = NotificationTemplates.Create(Guid.NewGuid(), "agent", r, new EmailSettings());
        Assert.Contains(r.ReferenceNumber, agent.Subject); Assert.Contains("25000.00 SEK", agent.Text);
        Assert.Contains(WebUtility.HtmlEncode(r.Contact.Name), agent.Html);
        Assert.DoesNotContain("preferencesJson", agent.Text, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void RetryDelaysAreBounded() { Assert.Equal(TimeSpan.FromSeconds(30), OutboxProcessor.RetryDelay(1)); Assert.Equal(TimeSpan.FromHours(1), OutboxProcessor.RetryDelay(100)); }
    [Fact]
    public async Task ProviderFailureIsClassifiedAndUsesStableIdempotencyWithoutRealDelivery()
    {
        var id = Guid.NewGuid(); var handler = new ProviderHandler(); using var client = new HttpClient(handler);
        var sender = new EmailSender(client, new EmailSettings { Provider = "Resend", ApiKey = "test-only" });
        var message = NotificationTemplates.Create(id, "customer", RequestExamples.Inquiry(), new());
        var failure = await Assert.ThrowsAsync<EmailDeliveryException>(() => sender.SendAsync(message, TestContext.Current.CancellationToken));
        Assert.True(failure.Retryable); Assert.Equal($"notification-{id:D}", handler.Key);
    }
    private sealed class ProviderHandler : HttpMessageHandler
    {
        public string? Key { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Key = request.Headers.GetValues("Idempotency-Key").Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }
    }
}
