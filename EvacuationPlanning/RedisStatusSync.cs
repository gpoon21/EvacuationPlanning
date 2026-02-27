using System.Text.Json;
using EvacuationPlanning.Models;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace EvacuationPlanning;

/// <summary>
/// Subscribes to Planner zone updates and syncs evacuation status to Redis.
/// </summary>
public class RedisStatusSync : IHostedService {
    private readonly Planner _planner;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<RedisStatusSync> _logger;

    public RedisStatusSync(Planner planner, IDistributedCache cache, IConfiguration config, ILogger<RedisStatusSync> logger) {
        _planner = planner;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _planner.ZoneUpdated += OnZoneUpdated;
        await ClearStaleKeysAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        _planner.ZoneUpdated -= OnZoneUpdated;
        return Task.CompletedTask;
    }

    private async Task ClearStaleKeysAsync() {
        string? connectionString = _config.GetConnectionString("Redis");
        if (string.IsNullOrEmpty(connectionString)) return;

        try {
            ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
            IServer server = redis.GetServers()[0];
            IDatabase db = redis.GetDatabase();

            await foreach (RedisKey key in server.KeysAsync(pattern: "evacuation:zone:*")) {
                await db.KeyDeleteAsync(key);
            }

            await redis.DisposeAsync();
            _logger.LogInformation("Cleared stale evacuation keys from Redis on startup");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to clear stale Redis keys on startup");
        }
    }

    private void OnZoneUpdated(EvacuationStatus status, Planner.UpdateType updateType) {
        string key = $"evacuation:zone:{status.ZoneID}";

        try {
            if (updateType == Planner.UpdateType.Removed) {
                _cache.Remove(key);
                _logger.LogInformation("Zone {ZoneID} removed from Redis", status.ZoneID);
                return;
            }

            string json = JsonSerializer.Serialize(status);
            _cache.SetString(key, json);
            _logger.LogInformation("Zone {ZoneID} synced to Redis (evacuated={TotalEvacuated}, remaining={RemainingPeople})",
                status.ZoneID, status.TotalEvacuated, status.RemainingPeople);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to sync zone {ZoneID} to Redis", status.ZoneID);
        }
    }
}