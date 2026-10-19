using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.ArchiveExporter;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure;
using WorldCupTyper.Infrastructure.Persistence;

if (args.Length != 1) throw new ArgumentException("Usage: ArchiveExporter <external-output-directory>. Supply ConnectionStrings__DefaultConnection for a LOCAL backup restore.");
var output = Path.GetFullPath(args[0]);
ArchiveOutputGuard.Validate(output, ArchiveOutputGuard.FindRepositories(Environment.CurrentDirectory, AppContext.BaseDirectory, output));
if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()) throw new InvalidOperationException("Output must be a new or empty directory.");
var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
var connection = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing local restore connection.");
var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
if (settings.Host is not ("localhost" or "127.0.0.1" or "::1")) throw new InvalidOperationException("Export is restricted to a local database restore.");
var services = new ServiceCollection();
services.AddLogging();
services.AddWorldCupTyperInfrastructure(configuration);
// A plain ServiceProvider intentionally does not start any registered hosted workers.
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<WorldCupTyperDbContext>();
var users = await db.Users.AsNoTracking().Where(user => user.IsActive).OrderBy(user => user.Id).ToListAsync();
if (users.Any(user => user.RequiresPasswordChange)) throw new InvalidOperationException("Resolve active accounts requiring a password change before exporting an immutable archive.");
var matchIds = await db.Matches.AsNoTracking().OrderBy(match => match.MatchNumber).Select(match => match.Id).ToListAsync();
var matches = scope.ServiceProvider.GetRequiredService<IMatchService>();
var predictions = scope.ServiceProvider.GetRequiredService<IPredictionService>();
var ranking = scope.ServiceProvider.GetRequiredService<IRankingService>();
var summary = scope.ServiceProvider.GetRequiredService<IFinalSummaryService>();
var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
var teams = scope.ServiceProvider.GetRequiredService<ITeamService>();
var players = scope.ServiceProvider.GetRequiredService<IPlayerService>();
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
json.Converters.Add(new JsonStringEnumConverter());
var availability = await summary.GetFinalSummaryAvailabilityAsync();
var publicResponses = new Dictionary<string, object>
{
    ["/ranking/top"] = await ranking.GetTopAsync(),
    ["/summary/final/availability"] = availability,
};
if (availability.IsReady) publicResponses["/summary/final"] = await summary.GetFinalSummaryAsync();
var aliases = new Dictionary<string, string>();
var accounts = new Dictionary<string, object>();
Directory.CreateDirectory(Path.Combine(output, "accounts"));
foreach (var user in users)
{
    var isAdmin = user.Role == UserRole.Admin;
    var currentUser = await auth.GetCurrentUserAsync(user.Id);
    var responses = new Dictionary<string, object>(publicResponses)
    {
        ["/auth/me"] = currentUser,
        ["/matches"] = await matches.GetMatchesAsync(user.Id),
        ["/matches/today"] = await matches.GetTodayMatchesAsync(user.Id),
        ["/matches/upcoming"] = await matches.GetUpcomingMatchesAsync(user.Id),
        ["/predictions/my"] = await predictions.GetMyPredictionsAsync(user.Id),
        ["/ranking"] = await ranking.GetRankingAsync(user.Id),
        ["/ranking/top"] = await ranking.GetTopAsync(5, user.Id),
        ["/ranking/me"] = await ranking.GetUserRankingAsync(user.Id),
        ["/ranking/progress"] = await ranking.GetProgressAsync(user.Id),
        ["/ranking/progress/all"] = await ranking.GetProgressForRankingAsync(user.Id),
        ["/teams"] = await teams.GetTeamsAsync(),
        ["/notifications/settings"] = new { morningDigestEnabled = false, missingPrediction2hEnabled = false, missingPrediction30mEnabled = false, rankingUpdatedEnabled = false, hasActiveSubscription = false },
        ["/notifications/vapid-public-key"] = new { publicKey = "" },
    };
    foreach (var matchId in matchIds)
    {
        responses[$"/matches/{matchId}"] = await matches.GetMatchDetailsAsync(matchId, user.Id, isAdmin);
        responses[$"/matches/{matchId}/predictions"] = await predictions.GetPredictionsForMatchAsync(user.Id, isAdmin, matchId);
    }
    if (availability.IsReady)
    {
        responses["/summary/final"] = await summary.GetFinalSummaryAsync(user.Id);
        responses["/summary/final/me"] = await summary.GetPersonalFinalSummaryAsync(user.Id);
    }
    if (isAdmin)
    {
        responses["/admin/players"] = await players.GetPlayersAsync();
        responses["/admin/matches"] = await matches.GetAdminMatchesAsync();
        responses["/admin/teams"] = await teams.GetTeamsAsync();
    }
    var envelope = ArchiveEncryption.Encrypt(user.PasswordHash, JsonSerializer.SerializeToUtf8Bytes(responses, json));
    var id = user.Id.ToString();
    await File.WriteAllTextAsync(Path.Combine(output, "accounts", id + ".json"), JsonSerializer.Serialize(envelope, json));
    accounts[id] = envelope.Kdf;
    foreach (var alias in new[] { user.Email, user.DisplayName })
    {
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(alias.Trim().ToLowerInvariant()))).ToLowerInvariant();
        if (aliases.TryGetValue(digest, out var previous) && previous != id) throw new InvalidOperationException("Ambiguous login alias.");
        aliases[digest] = id;
    }
}
await File.WriteAllTextAsync(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(new { version = 1, exportedAtUtc = DateTime.UtcNow, publicResponses, aliases, accounts }, json));
Console.WriteLine($"Exported {users.Count} encrypted accounts and {matchIds.Count} matches. No password hashes or session keys were published.");
