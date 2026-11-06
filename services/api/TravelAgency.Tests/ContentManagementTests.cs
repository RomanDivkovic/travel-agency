using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.BusinessManagement;
using TravelAgency.Infrastructure.BusinessManagement;
using TravelAgency.Infrastructure.Persistence;
namespace TravelAgency.Tests;

public sealed partial class QuotationApiTests
{
    private static string CmsPath(string key) => "/api/admin/cms/documents/" + Uri.EscapeDataString(key);
    private static async Task<ContentDetail> CmsDetail(HttpClient c, string key) => (await c.GetFromJsonAsync<ContentDetail>(CmsPath(key), Ct))!;
    private static async Task<PublicContent> Website(HttpClient c) => (await c.GetFromJsonAsync<PublicContent>("/api/public/website", Ct))!;
    private static async Task<ContentDetail> CmsSave(HttpClient c, string key, object value)
    {
        var d = await CmsDetail(c, key); using var r = await Mutate(c, CmsPath(key) + "/draft", new ContentDraftInput(d.VersionToken, ContentSchema.Element(value))); Assert.Equal(HttpStatusCode.NoContent, r.StatusCode); return await CmsDetail(c, key);
    }
    private static async Task CmsPublish(HttpClient c, ContentDetail d) { using var r = await Mutate(c, CmsPath(d.Key) + "/publish", new ContentAction(d.VersionToken, true)); Assert.Equal(HttpStatusCode.NoContent, r.StatusCode); }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CmsDraftPreviewPublicationConcurrencyAndHistoryStaySeparate()
    {
        using var c = Client(); await Login(c); using var anon = Client(); var d = await CmsDetail(c, "page:about"); var page = ContentSchema.Read<PageData>(d.Draft); var original = page.Sv["public.about.title"]; page.Sv["public.about.title"] = "TEST CMS <script>not HTML</script>";
        using var before = await anon.GetAsync("/api/public/website", Ct); var etag = before.Headers.ETag!.Tag;
        var saved = await CmsSave(c, d.Key, page); Assert.Equal(0, saved.PublishedVersion); Assert.Empty(saved.Versions);
        Assert.Equal(original, ContentSchema.Read<PageData>((await Website(anon)).Documents[d.Key]).Sv["public.about.title"]);
        using var previewDenied = await anon.GetAsync("/api/admin/cms/preview/page:about", Ct); Assert.Equal(HttpStatusCode.Unauthorized, previewDenied.StatusCode);
        var preview = (await c.GetFromJsonAsync<PublicContent>("/api/admin/cms/preview/page:about", Ct))!; Assert.Equal(page.Sv["public.about.title"], ContentSchema.Read<PageData>(preview.Documents[d.Key]).Sv["public.about.title"]);
        using var cachedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/public/website"); cachedRequest.Headers.TryAddWithoutValidation("If-None-Match", etag); using var unchanged = await anon.SendAsync(cachedRequest, Ct); Assert.Equal(HttpStatusCode.NotModified, unchanged.StatusCode);
        using var csrf = await c.PostAsJsonAsync(CmsPath(d.Key) + "/publish", new ContentAction(saved.VersionToken, true), Ct); Assert.Equal(HttpStatusCode.BadRequest, csrf.StatusCode);
        using var uncheckedPublish = await Mutate(c, CmsPath(d.Key) + "/publish", new ContentAction(saved.VersionToken)); Assert.Equal(HttpStatusCode.BadRequest, uncheckedPublish.StatusCode);
        await CmsPublish(c, saved); var issued = await CmsDetail(c, d.Key); Assert.Single(issued.Versions); Assert.False((await Website(anon)).Documents.ContainsKey("preferences")); using var changed = await anon.GetAsync("/api/public/website", Ct); Assert.NotEqual(etag, changed.Headers.ETag!.Tag); var json = await changed.Content.ReadAsStringAsync(Ct); foreach (var secret in new[] { "versionToken", "publishedBy", "draftJson" }) Assert.DoesNotContain(secret, json);
        using var stale = await Mutate(c, CmsPath(d.Key) + "/draft", new ContentDraftInput(saved.VersionToken, ContentSchema.Element(page))); Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        page.En["public.about.title"] = "TEST English revision"; var next = await CmsSave(c, d.Key, page); await CmsPublish(c, next); var second = await CmsDetail(c, d.Key); Assert.Equal(2, second.PublishedVersion); Assert.Equal(issued.Versions[0].Data.GetRawText(), second.Versions[1].Data.GetRawText());
        using var restore = await Mutate(c, CmsPath(d.Key) + "/restore", new ContentAction(second.VersionToken, RestoreVersion: 1)); Assert.Equal(HttpStatusCode.NoContent, restore.StatusCode); var restored = await CmsDetail(c, d.Key); Assert.Equal(2, restored.PublishedVersion); Assert.Equal(issued.Versions[0].Data.GetRawText(), restored.Draft.GetRawText());
        var concurrent = await Task.WhenAll(Mutate(c, CmsPath(d.Key) + "/draft", new ContentDraftInput(restored.VersionToken, restored.Draft)), Mutate(c, CmsPath(d.Key) + "/draft", new ContentDraftInput(restored.VersionToken, restored.Draft))); Assert.Single(concurrent, r => r.StatusCode == HttpStatusCode.NoContent); Assert.Single(concurrent, r => r.StatusCode == HttpStatusCode.Conflict); foreach (var r in concurrent) r.Dispose();
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CmsBusinessValidationHoursVisibilityAndCustomerAuthorization()
    {
        using var c = Client(); using var denied = await c.GetAsync("/api/admin/cms/documents", Ct); Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode); await Login(c); var b = ContentSchema.Read<BusinessData>((await CmsDetail(c, "business")).Draft);
        foreach (var bad in new[] { b with { ContactEmail = "bad email" }, b with { WebsiteUrl = "javascript:alert(1)" }, b with { OrganizationNumber = "made-up" }, b with { ContactPhone = "<script>" }, b with { TimeZone = "Mars/Unknown" }, b with { OpeningHours = [new(1, "17:00", "09:00")] }, b with { OpeningHours = [new(1, "09:00", "12:00"), new(1, "11:00", "13:00")] } })
        { var d = await CmsDetail(c, "business"); using var r = await Mutate(c, CmsPath("business") + "/draft", new ContentDraftInput(d.VersionToken, ContentSchema.Element(bad))); Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode); }
        b = b with { ContactEmail = "TEST@example.test", ContactPhone = "+46 1234567", OpeningHours = [new(1, "09:00", "12:00"), new(1, "13:00", "17:00")], Exceptions = [new(new(2027, 1, 1), true, "", "")] }; await CmsPublish(c, await CmsSave(c, "business", b)); Assert.Equal(b.ContactEmail, ContentSchema.Read<BusinessData>((await Website(c)).Documents["business"]).ContactEmail);
        await CmsPublish(c, await CmsSave(c, "business", b with { PublicContactVisible = false })); Assert.Equal("", ContentSchema.Read<BusinessData>((await Website(c)).Documents["business"]).ContactEmail);
        using var customer = Client(); var u = await RegisterCustomer(customer); await CustomerLogin(customer, u.Email); using var forbidden = await customer.GetAsync("/api/admin/cms/documents", Ct); Assert.Equal(HttpStatusCode.Unauthorized, forbidden.StatusCode); using var mutation = await customer.PostAsJsonAsync(CmsPath("business") + "/draft", new { }, Ct); Assert.Equal(HttpStatusCode.Unauthorized, mutation.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CmsPricingChangesPreserveAcceptedQuoteAndIssuedInvoiceSnapshots()
    {
        using var c = Client(); await Login(c); var invoice = (await IssueInvoice(c)).Invoice; var before = JsonSerializer.Serialize(invoice.Invoice); var quote = await Detail(c, invoice.QuotationId); var quoteBefore = JsonSerializer.Serialize(quote.Quotation);
        var data = ContentSchema.Read<ServiceData>((await CmsDetail(c, "service:CompleteTrip")).Draft); var price = data.Pricing with { Type = "FixedFee", AmountMinor = 50000 }; await CmsPublish(c, await CmsSave(c, "service:CompleteTrip", data with { Pricing = price })); await CmsPublish(c, await CmsSave(c, "service:CompleteTrip", data with { Pricing = price with { AmountMinor = 70000 }, Enabled = false }));
        Assert.Equal(before, JsonSerializer.Serialize((await InvoiceDetail(c, invoice.Id)).Invoice)); Assert.Equal(quoteBefore, JsonSerializer.Serialize((await Detail(c, invoice.QuotationId)).Quotation));
        using var disabled = new HttpRequestMessage(HttpMethod.Post, "/api/travel-requests") { Content = JsonContent.Create(RequestExamples.Input("complete", "sv")) }; disabled.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); using var response = await c.SendAsync(disabled, Ct); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(ContentSchema.Read<ServiceData>((await Website(c)).Documents["service:CompleteTrip"]).Enabled);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CmsFaqTranslationsPoliciesEffectiveDatesAndUnknownSchemas()
    {
        using var c = Client(); await Login(c); var key = "faq:" + Guid.NewGuid(); var faq = new FaqData(true, 2, new("TEST Fråga", "TEST Svar"), new("TEST Question", "TEST Answer")); var d = await CmsSave(c, key, faq); Assert.False((await Website(c)).Documents.ContainsKey(key)); await CmsPublish(c, d); Assert.Equal(faq, ContentSchema.Read<FaqData>((await Website(c)).Documents[key])); await CmsPublish(c, await CmsSave(c, key, faq with { Enabled = false, DisplayOrder = 1 })); Assert.False(ContentSchema.Read<FaqData>((await Website(c)).Documents[key]).Enabled);
        using var bad = await Mutate(c, CmsPath(key) + "/draft", new { versionToken = (await CmsDetail(c, key)).VersionToken, data = new { faq.Enabled, faq.DisplayOrder, faq.Sv, faq.En, script = "evil" } }); Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var policy = new PolicyData(new("TEST integritet — inte juridisk text", "TEST sv"), new("TEST privacy — not legal text", "TEST en"), DateOnly.FromDateTime(DateTime.UtcNow), false, "", null); var draft = await CmsSave(c, "policy:Privacy", policy); using var unreviewed = await Mutate(c, CmsPath("policy:Privacy") + "/publish", new ContentAction(draft.VersionToken, true)); Assert.Equal(HttpStatusCode.BadRequest, unreviewed.StatusCode); Assert.False((await Website(c)).Documents.ContainsKey("policy:Privacy"));
        policy = policy with { LegallyReviewed = true, ReviewedBy = "TEST reviewer (not actual legal approval)", ReviewedOn = policy.EffectiveFrom }; await CmsPublish(c, await CmsSave(c, "policy:Privacy", policy)); await CmsPublish(c, await CmsSave(c, "policy:Privacy", policy with { EffectiveFrom = policy.EffectiveFrom.AddDays(10), En = new("TEST future", "TEST future") })); Assert.DoesNotContain("reviewedBy", JsonSerializer.Serialize((await Website(c)).Documents)); Assert.Equal(policy.En.Title, ContentSchema.Read<PublicPolicyData>((await Website(c)).Documents["policy:Privacy"]).En.Title);
        using var unknown = await c.GetAsync(CmsPath("page:arbitrary-route"), Ct); Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CmsNotificationPreferencesSuppressDeliveryWithoutChangingReadState()
    {
        using var customer = Client(); var owner = await RegisterCustomer(customer); await CustomerLogin(customer, owner.Email); var id = await StartConversation(customer); using var admin = Client(); await Login(admin);
        await CmsPublish(admin, await CmsSave(admin, "preferences", new BusinessPreferences(false, false)));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>(); var job = await db.NotificationOutbox.SingleAsync(n => n.ConversationId == id, Ct);
        var suppressed = await TravelAgency.Infrastructure.Messaging.MessagingNotifications.PrepareAsync(job, db, new(), new(), DateTimeOffset.UtcNow.AddMinutes(1), Ct); Assert.Null(suppressed.Message); Assert.Empty(await db.ConversationReads.Where(r => r.ConversationId == id).ToArrayAsync(Ct));
        await CmsPublish(admin, await CmsSave(admin, "preferences", new BusinessPreferences(true, true))); var delivered = await TravelAgency.Infrastructure.Messaging.MessagingNotifications.PrepareAsync(job, db, new(), new(), DateTimeOffset.UtcNow.AddMinutes(1), Ct); Assert.NotNull(delivered.Message);
    }
    private static readonly byte[] WebsitePng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aGHsAAAAASUVORK5CYII=");
    private static async Task<HttpResponseMessage> MediaUpload(HttpClient c, byte[] bytes, string mime = "image/png", string name = "../../test.png", bool csrf = true)
    {
        using var form = new MultipartFormDataContent(); var file = new ByteArrayContent(bytes); file.Headers.ContentType = new(mime); form.Add(file, "file", name); form.Add(new StringContent("TEST bild"), "altSv"); form.Add(new StringContent("TEST image"), "altEn"); using var r = new HttpRequestMessage(HttpMethod.Post, "/api/admin/cms/media") { Content = form }; if (csrf) r.Headers.Add("X-CSRF-TOKEN", (await c.GetFromJsonAsync<JsonElement>("/api/admin/auth/csrf", Ct)).GetProperty("token").GetString()); return await c.SendAsync(r, Ct);
    }
    [Fact(Skip = "Requires isolated PostgreSQL", SkipUnless = nameof(HasDatabase))]
    public async Task CmsMediaSignaturesVisibilitySnapshotsAndArchivingAreEnforced()
    {
        using var c = Client(); using var anonymous = await MediaUpload(c, WebsitePng, csrf: false); Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode); await Login(c);
        using var noCsrf = await MediaUpload(c, WebsitePng, csrf: false); Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode); using var fake = await MediaUpload(c, "<svg onload='evil'/>"u8.ToArray()); Assert.Equal(HttpStatusCode.UnsupportedMediaType, fake.StatusCode); using var oversized = await MediaUpload(c, new byte[WebsiteImageValidation.MaxBytes + 1]); Assert.Equal(HttpStatusCode.RequestEntityTooLarge, oversized.StatusCode);
        using var uploaded = await MediaUpload(c, WebsitePng); Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode); var m = (await uploaded.Content.ReadFromJsonAsync<MediaDto>(Ct))!; Assert.Equal("test.png", m.FileName); using var anon = Client(); using var privateFile = await anon.GetAsync("/api/public/website/media/" + m.Id, Ct); Assert.Equal(HttpStatusCode.NotFound, privateFile.StatusCode);
        var page = ContentSchema.Read<PageData>((await CmsDetail(c, "page:about")).Draft) with { Image = new(m.Id, m.AltSv, m.AltEn, false) }; await CmsPublish(c, await CmsSave(c, "page:about", page)); Assert.Equal(WebsitePng, await anon.GetByteArrayAsync("/api/public/website/media/" + m.Id, Ct));
        using var inUse = await Mutate(c, "/api/admin/cms/media/" + m.Id, new MediaChange(m.VersionToken, m.AltSv, m.AltEn, false, true), HttpMethod.Patch); Assert.Equal(HttpStatusCode.Conflict, inUse.StatusCode);
        await CmsPublish(c, await CmsSave(c, "page:about", page with { Image = null })); using var archive = await Mutate(c, "/api/admin/cms/media/" + m.Id, new MediaChange(m.VersionToken, m.AltSv, m.AltEn, false, true), HttpMethod.Patch); Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode); using var gone = await anon.GetAsync("/api/public/website/media/" + m.Id, Ct); Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }
}
public sealed class ContentSchemaTests
{
    [Theory]
    [InlineData("/9j/4AAQSkZJRgABAQAAAQABAAD/4gHYSUNDX1BST0ZJTEUAAQEAAAHIAAAAAAQwAABtbnRyUkdCIFhZWiAH4AABAAEAAAAAAABhY3NwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAA9tYAAQAAAADTLQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAlkZXNjAAAA8AAAACRyWFlaAAABFAAAABRnWFlaAAABKAAAABRiWFlaAAABPAAAABR3dHB0AAABUAAAABRyVFJDAAABZAAAAChnVFJDAAABZAAAAChiVFJDAAABZAAAAChjcHJ0AAABjAAAADxtbHVjAAAAAAAAAAEAAAAMZW5VUwAAAAgAAAAcAHMAUgBHAEJYWVogAAAAAAAAb6IAADj1AAADkFhZWiAAAAAAAABimQAAt4UAABjaWFlaIAAAAAAAACSgAAAPhAAAts9YWVogAAAAAAAA9tYAAQAAAADTLXBhcmEAAAAAAAQAAAACZmYAAPKnAAANWQAAE9AAAApbAAAAAAAAAABtbHVjAAAAAAAAAAEAAAAMZW5VUwAAACAAAAAcAEcAbwBvAGcAbABlACAASQBuAGMALgAgADIAMAAxADb/2wBDAAMCAgICAgMCAgIDAwMDBAYEBAQEBAgGBgUGCQgKCgkICQkKDA8MCgsOCwkJDRENDg8QEBEQCgwSExIQEw8QEBD/2wBDAQMDAwQDBAgEBAgQCwkLEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBD/wAARCAACAAIDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAj/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFAEBAAAAAAAAAAAAAAAAAAAABv/EABQRAQAAAAAAAAAAAAAAAAAAAAD/2gAMAwEAAhEDEQA/AIyALQx//9k=", "image/jpeg", "test.jpg")]
    [InlineData("UklGRhQCAABXRUJQVlA4WAoAAAAgAAAAAQAAAQAASUNDUMgBAAAAAAHIAAAAAAQwAABtbnRyUkdCIFhZWiAH4AABAAEAAAAAAABhY3NwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAA9tYAAQAAAADTLQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAlkZXNjAAAA8AAAACRyWFlaAAABFAAAABRnWFlaAAABKAAAABRiWFlaAAABPAAAABR3dHB0AAABUAAAABRyVFJDAAABZAAAAChnVFJDAAABZAAAAChiVFJDAAABZAAAAChjcHJ0AAABjAAAADxtbHVjAAAAAAAAAAEAAAAMZW5VUwAAAAgAAAAcAHMAUgBHAEJYWVogAAAAAAAAb6IAADj1AAADkFhZWiAAAAAAAABimQAAt4UAABjaWFlaIAAAAAAAACSgAAAPhAAAts9YWVogAAAAAAAA9tYAAQAAAADTLXBhcmEAAAAAAAQAAAACZmYAAPKnAAANWQAAE9AAAApbAAAAAAAAAABtbHVjAAAAAAAAAAEAAAAMZW5VUwAAACAAAAAcAEcAbwBvAGcAbABlACAASQBuAGMALgAgADIAMAAxADZWUDggJgAAAJABAJ0BKgIAAgABQCYlAE6XQABzAAD++Op/t2Lt5DfoUbKJfMAA", "image/webp", "test.webp")]
    public void ValidRasterFixturesAreAcceptedAndMimeSpoofingIsRejected(string encoded, string mime, string name)
    {
        var bytes = Convert.FromBase64String(encoded); Assert.Equal(name, WebsiteImageValidation.Validate(bytes, mime, name)); Assert.Throws<ContentOperationException>(() => WebsiteImageValidation.Validate(bytes, "image/png", "test.png"));
    }
    [Fact] public void BaselineIsBilingualAndEveryStructuredSchemaValidates() { foreach (var (key, value) in ContentSchema.Baseline) Assert.NotEmpty(ContentSchema.Validate(key, value, true)); }
    [Fact] public void PageFieldsCannotChangeRoutesAndIncompleteTranslationsAreRejected() { var p = ContentSchema.Read<PageData>(ContentSchema.Default("page:home")); p.En.Remove(p.En.Keys.First()); Assert.Throws<ContentOperationException>(() => ContentSchema.Validate("page:home", ContentSchema.Element(p), true)); p.En["shared.routing"] = "javascript:evil"; Assert.Throws<ContentOperationException>(() => ContentSchema.Validate("page:home", ContentSchema.Element(p), true)); }
    [Fact] public void InvalidMoneyCurrencyAndImageDimensionsAreRejected() { var s = ContentSchema.Read<ServiceData>(ContentSchema.Default("service:FlightsOnly")); foreach (var p in new[] { s.Pricing with { Type = "FixedFee", AmountMinor = -1 }, s.Pricing with { Currency = "XXX" }, s.Pricing with { Type = "FixedFee", AmountMinor = 10, EffectiveFrom = DateTimeOffset.UtcNow, EffectiveUntil = DateTimeOffset.UtcNow.AddDays(-1) } }) Assert.Throws<ContentOperationException>(() => ContentSchema.Validate("service:FlightsOnly", ContentSchema.Element(s with { Pricing = p }), true)); Assert.Throws<ContentOperationException>(() => WebsiteImageValidation.Validate("<svg/>"u8.ToArray(), "image/svg+xml", "x.svg")); }
}
