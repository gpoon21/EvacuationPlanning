using System.Text;
using System.Text.Json;
using EvacuationPlanning.Models;
using StackExchange.Redis;

namespace EvacuationPlanning;

/// <summary>
/// Registers a minimal endpoint at "/" that reads evacuation status from Redis and renders an HTML dashboard.
/// </summary>
public static class DashboardEndpoint {
    public static void MapDashboard(this WebApplication app) {
        app.MapGet("/", async (IConfiguration config) => {
            string? connectionString = config.GetConnectionString("Redis");
            if (string.IsNullOrEmpty(connectionString)) {
                return Results.Content(BuildHtml("Redis not configured", []), "text/html");
            }

            List<EvacuationStatus> statuses = [];
            try {
                ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
                IServer server = redis.GetServers()[0];
                IDatabase db = redis.GetDatabase();

                await foreach (RedisKey key in server.KeysAsync(pattern: "evacuation:zone:*")) {
                    RedisValue raw = await db.HashGetAsync(key, "data");
                    if (raw.IsNullOrEmpty) continue;
                    EvacuationStatus? status = JsonSerializer.Deserialize<EvacuationStatus>(raw.ToString());
                    if (status != null) statuses.Add(status);
                }

                await redis.DisposeAsync();
            }
            catch (Exception ex) {
                return Results.Content(BuildHtml($"Redis connection failed: {ex.Message}", []), "text/html");
            }

            return Results.Content(BuildHtml(null, statuses), "text/html");
        });
    }

    private static string BuildHtml(string? error, List<EvacuationStatus> statuses) {
        StringBuilder sb = new();
        sb.Append("""
            <!DOCTYPE html>
            <html>
            <head>
                <title>Evacuation Dashboard</title>
                <meta charset="utf-8">
                <meta http-equiv="refresh" content="5">
                <style>
                    body { font-family: system-ui, sans-serif; max-width: 800px; margin: 40px auto; padding: 0 20px; background: #f5f5f5; }
                    h1 { color: #333; }
                    .badge { display: inline-block; padding: 2px 8px; border-radius: 10px; font-size: 12px; font-weight: bold; }
                    .badge-ok { background: #d4edda; color: #155724; }
                    .badge-err { background: #f8d7da; color: #721c24; }
                    table { width: 100%; border-collapse: collapse; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
                    th { background: #2c3e50; color: white; text-align: left; padding: 12px; }
                    td { padding: 12px; border-top: 1px solid #eee; }
                    tr:hover { background: #f8f9fa; }
                    .progress { background: #e9ecef; border-radius: 4px; height: 20px; overflow: hidden; }
                    .progress-bar { background: #28a745; height: 100%; transition: width 0.3s; }
                    .empty { text-align: center; padding: 40px; color: #888; }
                    .info { color: #666; font-size: 14px; margin-bottom: 20px; }
                    a { color: #2c3e50; }
                </style>
            </head>
            <body>
                <h1>Evacuation Dashboard</h1>
            """);

        if (error != null) {
            sb.Append($"""<p><span class="badge badge-err">ERROR</span> {error}</p>""");
        } else {
            sb.Append("""<p><span class="badge badge-ok">CONNECTED</span> Reading from Redis</p>""");
        }

        sb.Append("""<p class="info">Auto-refreshes every 5 seconds &middot; <a href="/swagger">Swagger UI</a></p>""");

        if (statuses.Count == 0) {
            sb.Append("""<div class="empty">No evacuation zones in Redis. Use the API to add zones and generate a plan.</div>""");
        } else {
            sb.Append("""
                <table>
                    <tr><th>Zone ID</th><th>Evacuated</th><th>Remaining</th><th>Progress</th><th>Last Vehicle</th></tr>
                """);

            foreach (EvacuationStatus status in statuses) {
                int total = status.TotalEvacuated + status.RemainingPeople;
                int percent = total > 0 ? (int)(100.0 * status.TotalEvacuated / total) : 0;
                string vehicle = status.LastVehicleUsed ?? "-";

                sb.Append($"""
                    <tr>
                        <td><strong>{status.ZoneID}</strong></td>
                        <td>{status.TotalEvacuated}</td>
                        <td>{status.RemainingPeople}</td>
                        <td><div class="progress"><div class="progress-bar" style="width:{percent}%"></div></div> {percent}%</td>
                        <td>{vehicle}</td>
                    </tr>
                    """);
            }

            sb.Append("</table>");
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }
}