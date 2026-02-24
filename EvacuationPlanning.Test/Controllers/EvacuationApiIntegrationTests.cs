using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EvacuationPlanning.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace EvacuationPlanning.Test.Controllers;

public class EvacuationApiIntegrationTests {
    private static WebApplicationFactory<Program> CreateFactory() {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.ConfigureServices(services => {
                // Replace Redis with in-memory distributed cache for tests
                ServiceDescriptor? redisDescriptor =
                    services.SingleOrDefault(d => d.ServiceType == typeof(IDistributedCache));
                if (redisDescriptor != null) {
                    services.Remove(redisDescriptor);
                }
                services.AddDistributedMemoryCache();
            });
        });
    }

    private static EvacuationZone MakeZone(string id, int people, int urgency,
        double lat = 13.7563, double lon = 100.5018) {
        return new EvacuationZone {
            ZoneID = id,
            LocationCoordinates = new LocationCoordinates { Latitude = lat, Longitude = lon },
            NumberOfPeople = people,
            UrgencyLevel = urgency,
        };
    }

    private static Vehicle MakeVehicle(string id, int capacity, double speed = 60,
        double lat = 13.7650, double lon = 100.5381) {
        return new Vehicle {
            VehicleID = id,
            Capacity = capacity,
            Type = "bus",
            LocationCoordinates = new LocationCoordinates { Latitude = lat, Longitude = lon },
            Speed = speed,
        };
    }

    [Fact]
    public async Task AddZone_ReturnsOkWithZone() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        EvacuationZone zone = MakeZone("Z1", 100, 4);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/evacuation-zones", zone);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        EvacuationZone? returned = await response.Content.ReadFromJsonAsync<EvacuationZone>();
        Assert.NotNull(returned);
        Assert.Equal("Z1", returned.ZoneID);
        Assert.Equal(100, returned.NumberOfPeople);
    }

    [Fact]
    public async Task AddVehicle_ReturnsOkWithVehicle() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        Vehicle vehicle = MakeVehicle("V1", 40);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/vehicles", vehicle);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Vehicle? returned = await response.Content.ReadFromJsonAsync<Vehicle>();
        Assert.NotNull(returned);
        Assert.Equal("V1", returned.VehicleID);
        Assert.Equal(40, returned.Capacity);
    }

    [Fact]
    public async Task GeneratePlan_WithZonesAndVehicles_ReturnsPlanItems() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 50, 5));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V1", 40));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V2", 20));

        HttpResponseMessage response = await client.PostAsync("/api/evacuations/plan", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        EvacuationPlanItem[]? plan = await response.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(plan);
        Assert.NotEmpty(plan);
        Assert.All(plan, item => Assert.Equal("Z1", item.ZoneID));
    }

    [Fact]
    public async Task GetStatus_AfterAddingZone_ReturnsZoneStatus() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 100, 3));

        HttpResponseMessage response = await client.GetAsync("/api/evacuations/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        EvacuationStatus[]? statuses = await response.Content.ReadFromJsonAsync<EvacuationStatus[]>();
        Assert.NotNull(statuses);
        Assert.Single(statuses);
        Assert.Equal("Z1", statuses[0].ZoneID);
        Assert.Equal(0, statuses[0].TotalEvacuated);
        Assert.Equal(100, statuses[0].RemainingPeople);
    }

    [Fact]
    public async Task UpdateStatus_WithValidData_UpdatesEvacuationProgress() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 100, 4));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V1", 40));

        UpdateStatusRequest request = new() {
            ZoneID = "Z1",
            VehicleID = "V1",
            NumberOfPeopleEvacuated = 30,
        };
        HttpResponseMessage updateResponse = await client.PutAsJsonAsync("/api/evacuations/update", request);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        HttpResponseMessage statusResponse = await client.GetAsync("/api/evacuations/status");
        EvacuationStatus[]? statuses = await statusResponse.Content.ReadFromJsonAsync<EvacuationStatus[]>();
        Assert.NotNull(statuses);
        Assert.Single(statuses);
        Assert.Equal(30, statuses[0].TotalEvacuated);
        Assert.Equal(70, statuses[0].RemainingPeople);
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidZone_ReturnsNotFound() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V1", 40));

        UpdateStatusRequest request = new() {
            ZoneID = "NONEXISTENT",
            VehicleID = "V1",
            NumberOfPeopleEvacuated = 10,
        };
        HttpResponseMessage response = await client.PutAsJsonAsync("/api/evacuations/update", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Clear_RemovesAllData() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 100, 4));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V1", 40));

        HttpResponseMessage clearResponse = await client.DeleteAsync("/api/evacuations/clear");
        Assert.Equal(HttpStatusCode.OK, clearResponse.StatusCode);

        HttpResponseMessage statusResponse = await client.GetAsync("/api/evacuations/status");
        EvacuationStatus[]? statuses = await statusResponse.Content.ReadFromJsonAsync<EvacuationStatus[]>();
        Assert.NotNull(statuses);
        Assert.Empty(statuses);
    }

    [Fact]
    public async Task AddZone_SyncsStatusToCache() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 100, 4));

        IDistributedCache cache = factory.Services.GetRequiredService<IDistributedCache>();
        string? cached = await cache.GetStringAsync("evacuation:zone:Z1");
        Assert.NotNull(cached);
        EvacuationStatus? status = JsonSerializer.Deserialize<EvacuationStatus>(cached);
        Assert.NotNull(status);
        Assert.Equal("Z1", status.ZoneID);
        Assert.Equal(0, status.TotalEvacuated);
        Assert.Equal(100, status.RemainingPeople);
    }

    [Fact]
    public async Task UpdateStatus_SyncsUpdatedStatusToCache() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 100, 4));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V1", 40));

        UpdateStatusRequest request = new() {
            ZoneID = "Z1",
            VehicleID = "V1",
            NumberOfPeopleEvacuated = 25,
        };
        await client.PutAsJsonAsync("/api/evacuations/update", request);

        IDistributedCache cache = factory.Services.GetRequiredService<IDistributedCache>();
        string? cached = await cache.GetStringAsync("evacuation:zone:Z1");
        Assert.NotNull(cached);
        EvacuationStatus? status = JsonSerializer.Deserialize<EvacuationStatus>(cached);
        Assert.NotNull(status);
        Assert.Equal(25, status.TotalEvacuated);
        Assert.Equal(75, status.RemainingPeople);
    }

    [Fact]
    public async Task Clear_RemovesStatusFromCache() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 100, 4));

        IDistributedCache cache = factory.Services.GetRequiredService<IDistributedCache>();
        string? cachedBefore = await cache.GetStringAsync("evacuation:zone:Z1");
        Assert.NotNull(cachedBefore);

        await client.DeleteAsync("/api/evacuations/clear");

        string? cachedAfter = await cache.GetStringAsync("evacuation:zone:Z1");
        Assert.Null(cachedAfter);
    }

    [Fact]
    public async Task GeneratePlan_WithNoData_ReturnsEmptyPlan() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/api/evacuations/plan", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        EvacuationPlanItem[]? plan = await response.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(plan);
        Assert.Empty(plan);
    }
}