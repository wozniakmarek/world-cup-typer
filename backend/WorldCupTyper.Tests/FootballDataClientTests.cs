using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using WorldCupTyper.Infrastructure.FootballData;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Tests;

public sealed class FootballDataClientTests
{
    [Fact]
    public async Task GetCompetitionMatchesAsync_ShouldCallCompetitionEndpointWithAuthTokenAndMapMatches()
    {
        var handler = new CapturingHttpMessageHandler("""
            {
              "matches": [
                {
                  "id": 1001,
                  "matchday": 4,
                  "stage": "GROUP_STAGE",
                  "group": "Group A",
                  "utcDate": "2026-06-18T18:00:00Z",
                  "status": "FINISHED",
                  "homeTeam": { "id": 794, "name": "Poland", "shortName": "Poland", "tla": "POL" },
                  "awayTeam": { "id": 759, "name": "Germany", "shortName": "Germany", "tla": "GER" },
                  "score": {
                    "duration": "REGULAR",
                    "fullTime": { "home": 2, "away": 0 },
                    "regularTime": { "home": null, "away": null }
                  },
                  "venue": "MetLife Stadium"
                }
              ]
            }
            """);

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.football-data.org/v4/"),
        };
        var options = Options.Create(new FootballDataOptions
        {
            ApiToken = "secret-token",
            CompetitionCode = "WC",
        });
        var client = new FootballDataClient(httpClient, options);

        var matches = await client.GetCompetitionMatchesAsync();

        handler.RequestUri.Should().Be(new Uri("https://api.football-data.org/v4/competitions/WC/matches"));
        handler.AuthToken.Should().Be("secret-token");
        matches.Should().ContainSingle();
        matches.Single().ExternalId.Should().Be("football-data:1001");
        matches.Single().HomeScore90.Should().Be(2);
        matches.Single().AwayScore90.Should().Be(0);
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _json;

        public CapturingHttpMessageHandler(string json)
        {
            _json = json;
        }

        public Uri? RequestUri { get; private set; }
        public string? AuthToken { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            AuthToken = request.Headers.TryGetValues("X-Auth-Token", out var values)
                ? values.SingleOrDefault()
                : null;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json),
            };
            return Task.FromResult(response);
        }
    }
}
