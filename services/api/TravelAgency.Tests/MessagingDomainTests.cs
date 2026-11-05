using TravelAgency.Domain.Messaging;
namespace TravelAgency.Tests;

public sealed class MessagingDomainTests
{
    [Fact]
    public void MessagesUseDeterministicSequencesAndExplicitConversationStates()
    {
        var now = DateTimeOffset.UtcNow; var c = new CustomerConversation { Subject = "TEST" }; var first = c.Send("customer", MessageSender.Customer, Guid.NewGuid(), "  Hello\r\nworld  \r\n", 5000, now); Assert.Equal("Hello\nworld", first.Body); Assert.Equal(1, first.Sequence); Assert.Equal(ConversationStatus.AwaitingAgent, c.Status);
        var reply = c.Send("admin", MessageSender.Administrator, Guid.NewGuid(), "TEST reply", 5000, now); Assert.Equal(2, reply.Sequence); Assert.Equal(ConversationStatus.AwaitingCustomer, c.Status); c.SetPriority(ConversationPriority.High, now); Assert.Equal(ConversationPriority.High, c.Priority); c.SetStatus(ConversationStatus.Closed, now); Assert.NotNull(c.ClosedAt); Assert.Throws<MessagingRuleException>(() => c.Send("customer", MessageSender.Customer, Guid.NewGuid(), "TEST", 5000, now)); c.SetStatus(ConversationStatus.Open, now); Assert.Null(c.ClosedAt); Assert.Equal(3, c.Send("customer", MessageSender.Customer, Guid.NewGuid(), "TEST", 5000, now).Sequence);
    }
    [Theory]
    [InlineData("")]
    [InlineData(" \r\n\t ")]
    [InlineData("bad\0text")]
    public void EmptyAndControlMessagesAreRejected(string text) => Assert.Throws<MessagingRuleException>(() => MessageText.Normalize(text, 100));
    [Fact]
    public void LengthSubjectAndReadBoundariesAreEnforced()
    {
        Assert.Throws<MessagingRuleException>(() => MessageText.Normalize(new string('x', 101), 100)); Assert.Throws<MessagingRuleException>(() => MessageText.Subject("line\nline")); Assert.Equal("<script>TEST</script>", MessageText.Normalize("<script>TEST</script>", 100)); var read = new ConversationReadState(); read.Read(3, DateTimeOffset.UtcNow); read.Read(2, DateTimeOffset.UtcNow); Assert.Equal(3, read.LastReadSequence);
    }
}
