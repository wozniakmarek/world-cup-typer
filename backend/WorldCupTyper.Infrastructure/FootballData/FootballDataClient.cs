using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Infrastructure.FootballData;

public sealed class FootballDataClient : IFootballDataClient
{
    private readonly HttpClient _httpClient;
    private readonly FootballDataOptions _options;

    public FootballDataClient(HttpClient httpClient, IOptions<FootballDataOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyCollection<FootballDataMatchSyncModel>> GetCompetitionMatchesAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            throw new BusinessRuleException("Brak tokena API football-data.org.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"competitions/{_options.CompetitionCode}/matches");
        request.Headers.Add("X-Auth-Token", _options.ApiToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<FootballDataMatchesResponseDto>(cancellationToken);
        return (payload?.Matches ?? [])
            .Select(FootballDataMatchMapper.Map)
            .Where(match => match is not null)
            .Select(match => match!)
            .ToList();
    }
}
