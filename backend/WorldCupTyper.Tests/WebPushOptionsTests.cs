using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorldCupTyper.Infrastructure;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Tests;

public sealed class WebPushOptionsTests
{
    [Fact]
    public void Validate_WithEmptyOptions_ShouldSucceed()
    {
        var result = new WebPushOptionsValidator().Validate(null, new WebPushOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("mailto:powiadomienia@marekwozniak.me")]
    [InlineData("https://typer.marekwozniak.me")]
    public void Validate_WithSupportedSubject_ShouldSucceed(string subject)
    {
        var result = new WebPushOptionsValidator().Validate(null, CreateConfiguredOptions(subject));

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("powiadomienia@marekwozniak.me")]
    [InlineData("mailto:ImieNazwisko")]
    [InlineData("http://typer.marekwozniak.me")]
    [InlineData("Test")]
    public void Validate_WithUnsupportedSubject_ShouldFail(string subject)
    {
        var result = new WebPushOptionsValidator().Validate(null, CreateConfiguredOptions(subject));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("WebPush:Subject");
        result.FailureMessage.Should().Contain("mailto:");
        result.FailureMessage.Should().Contain("https://");
    }

    [Fact]
    public void Validate_WithPartialConfiguration_ShouldFail()
    {
        var result = new WebPushOptionsValidator().Validate(
            null,
            new WebPushOptions
            {
                Subject = "mailto:powiadomienia@marekwozniak.me",
                PublicKey = "public-key",
            });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Subject, PublicKey, and PrivateKey");
    }

    [Fact]
    public void AddWorldCupTyperInfrastructure_WithInvalidWebPushSubject_ShouldRegisterOptionsValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=world_cup_typer_tests",
                ["WebPush:Subject"] = "ImieNazwisko",
                ["WebPush:PublicKey"] = "public-key",
                ["WebPush:PrivateKey"] = "private-key",
            })
            .Build();
        var services = new ServiceCollection();

        services.AddWorldCupTyperInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var act = () => provider.GetRequiredService<IOptions<WebPushOptions>>().Value;

        act.Should()
            .Throw<OptionsValidationException>()
            .WithMessage("*WebPush:Subject*mailto:*https://*");
    }

    private static WebPushOptions CreateConfiguredOptions(string subject) =>
        new()
        {
            Subject = subject,
            PublicKey = "public-key",
            PrivateKey = "private-key",
        };
}
