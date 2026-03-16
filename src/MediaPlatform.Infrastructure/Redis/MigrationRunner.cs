using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MediaPlatform.Infrastructure.Redis;

/// <summary>
/// Runs numbered Lua migration scripts against Redis.
/// Scripts are idempotent — each checks platform:schema:version before applying.
/// </summary>
public sealed partial class MigrationRunner(
    IConnectionMultiplexer redis,
    ILogger<MigrationRunner> logger)
{
    private const string VersionKey = "platform:schema:version";

    public async Task RunMigrationsAsync(string migrationsPath)
    {
        if (!Directory.Exists(migrationsPath))
        {
            logger.LogWarning("Migrations directory not found: {Path}", migrationsPath);
            return;
        }

        var db = redis.GetDatabase();
        var currentVersion = await GetCurrentVersionAsync(db);
        logger.LogInformation("Current Redis schema version: {Version}", currentVersion);

        var scripts = Directory.GetFiles(migrationsPath, "*.lua")
            .OrderBy(f => f)
            .ToList();

        if (scripts.Count == 0)
        {
            logger.LogInformation("No migration scripts found in {Path}", migrationsPath);
            return;
        }

        var applied = 0;
        foreach (var script in scripts)
        {
            var scriptVersion = ExtractVersion(script);
            if (scriptVersion <= currentVersion)
            {
                logger.LogDebug("Skipping migration {Script} (version {V} <= {Current})",
                    Path.GetFileName(script), scriptVersion, currentVersion);
                continue;
            }

            logger.LogInformation("Running migration {Script}...", Path.GetFileName(script));
            var lua = await File.ReadAllTextAsync(script);
            var result = await db.ScriptEvaluateAsync(lua);
            logger.LogInformation("Migration result: {Result}", result.ToString());
            applied++;
        }

        var finalVersion = await GetCurrentVersionAsync(db);
        logger.LogInformation("Migrations complete. Applied: {Count}, Final version: {Version}",
            applied, finalVersion);
    }

    private static async Task<int> GetCurrentVersionAsync(IDatabase db)
    {
        var version = await db.StringGetAsync(VersionKey);
        return version.HasValue ? int.Parse(version!) : 0;
    }

    private static int ExtractVersion(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var match = VersionPattern().Match(fileName);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    [GeneratedRegex(@"^(\d+)_")]
    private static partial Regex VersionPattern();
}
