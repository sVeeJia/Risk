using System.Net;
using System.Text.Json;
using Risk.Api.Commentary;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;

namespace Risk.Tests.Commentary;

public class CommentaryGeneratorTests
{
    private static string ChatJson(string content) => JsonSerializer.Serialize(new { choices = new[] { new { message = new { content } } } });

    private static CommentaryGenerator Create(StubHttpHandler handler, string? key = "test-key", int timeoutSeconds = 3) =>
        new(new StubHttpClientFactory(handler), key, timeoutSeconds: timeoutSeconds);

    [Fact]
    public async Task ReturnsTheModelsSentence_MarkedAsLlm()
    {
        var handler = StubHttpHandler.Json(ChatJson("Peru storms Jordan in a thrilling clash!"));

        var result = await Create(handler).GenerateAsync(RoundSamples.Sample());

        Assert.Equal("Peru storms Jordan in a thrilling clash!", result.Text);
        Assert.Equal("llm", result.Source);
    }

    [Fact]
    public async Task SendsTheKeyTheModelAndTheFacts()
    {
        var handler = StubHttpHandler.Json(ChatJson("A fine roll."));

        await Create(handler).GenerateAsync(RoundSamples.Sample());

        Assert.Equal("Bearer test-key", handler.Authorization);
        Assert.EndsWith("/v1/chat/completions", handler.Url);
        Assert.Contains("\"model\":\"mistral-small-latest\"", handler.Body);
        Assert.Contains("Attacker: Peru", handler.Body);
        Assert.Contains("Defender: Jordan", handler.Body);
        Assert.Contains("Attacker dice: 6,2", handler.Body);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ABadStatus_UsesAReadyMadeSentence(HttpStatusCode status)
    {
        var handler = StubHttpHandler.Json("{}", status);

        var result = await Create(handler).GenerateAsync(RoundSamples.Sample());

        Assert.Equal("template", result.Source);
        Assert.Contains("Peru", result.Text);
    }

    [Fact]
    public async Task ATimeout_UsesAReadyMadeSentence()
    {
        var handler = new StubHttpHandler(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await Create(handler, timeoutSeconds: 1).GenerateAsync(RoundSamples.Sample());

        Assert.Equal("template", result.Source);
    }

    [Fact]
    public async Task AReplyThatIsNotJson_UsesAReadyMadeSentence()
    {
        var handler = StubHttpHandler.Json("this is not json");

        var result = await Create(handler).GenerateAsync(RoundSamples.Sample());

        Assert.Equal("template", result.Source);
    }

    [Fact]
    public async Task WithoutAKey_DoesNotCallMistral()
    {
        var handler = StubHttpHandler.Json(ChatJson("Never used."));

        var result = await Create(handler, key: "").GenerateAsync(RoundSamples.Sample());

        Assert.Equal("template", result.Source);
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(RoundResult.Conquered, 0, 2)]
    [InlineData(RoundResult.AttackerExhausted, 1, 0)]
    [InlineData(RoundResult.Continues, 0, 2)]
    [InlineData(RoundResult.Continues, 2, 0)]
    [InlineData(RoundResult.Continues, 1, 1)]
    [InlineData(RoundResult.Continues, 0, 0)]
    public void Describe_MentionsBothCountries_ForEveryKindOfRoll(RoundResult result, int attackerLosses, int defenderLosses)
    {
        foreach (var sequence in new[] { 0, 1 })
        {
            var text = CommentaryGenerator.Describe(RoundSamples.Sample(result, attackerLosses, defenderLosses, sequence));

            Assert.Contains("Peru", text);
            Assert.Contains("Jordan", text);
        }
    }

    [Fact]
    public void Describe_SaysHowManySoldiersWereLost_WithTheRightPlural()
    {
        var one = CommentaryGenerator.Describe(RoundSamples.Sample(RoundResult.Continues, 0, 1, sequence: 0));
        var two = CommentaryGenerator.Describe(RoundSamples.Sample(RoundResult.Continues, 0, 2, sequence: 0));

        Assert.Contains("1 soldier", one);
        Assert.DoesNotContain("1 soldiers", one);
        Assert.Contains("2 soldiers", two);
    }

    [Fact]
    public void Tidy_RemovesNewlinesQuotesAndMarkdown()
    {
        var text = CommentaryGenerator.Tidy("  \"**Peru** wins!\"\n\n# Jordan falls.  ");

        Assert.Equal("Peru wins! Jordan falls.", text);
    }

    [Fact]
    public void Tidy_CapsTheLength_AndRejectsEmptyText()
    {
        var text = CommentaryGenerator.Tidy(new string('a', 1000));

        Assert.NotNull(text);
        Assert.True(text!.Length <= CommentaryGenerator.MaxLength);
        Assert.Null(CommentaryGenerator.Tidy("   "));
    }
}