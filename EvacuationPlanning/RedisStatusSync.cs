using System.Text.Json;
using EvacuationPlanning.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace EvacuationPlanning;

/// <summary>
/// Subscribes to Planner zone updates and syncs evacuation status to Redis.
/// </summary>
public class RedisStatusSync : IHostedService {
    private readonly Planner _planner;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisStatusSync> _logger;

    public RedisStatusSync(Planner planner, IDistributedCache cache, ILogger<RedisStatusSync> logger) {
        _planner = planner;
        _cache = cache;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        _planner.ZoneUpdated += OnZoneUpdated;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        _planner.ZoneUpdated -= OnZoneUpdated;
        return Task.CompletedTask;
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