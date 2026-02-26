using System.Net;
using System.Net.Http.Json;
using EvacuationPlanning.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace EvacuationPlanning.Test.Controllers;

public class StatefulEvacuationTests {
    private static WebApplicationFactory<Program> CreateFactory() {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.ConfigureServices(services => {
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
    public async Task Plan_DispatchesVehicles_SecondPlanExcludesThem() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 100, 5));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V1", 40));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V2", 30));

        HttpResponseMessage firstPlanResponse = await client.PostAsync("/api/evacuations/plan", null);
        EvacuationPlanItem[]? firstPlan = await firstPlanResponse.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(firstPlan);
        Assert.NotEmpty(firstPlan);
        string[] assignedVehicleIds = firstPlan.Select(p => p.VehicleID).ToArray();

        HttpResponseMessage secondPlanResponse = await client.PostAsync("/api/evacuations/plan", null);
        EvacuationPlanItem[]? secondPlan = await secondPlanResponse.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(secondPlan);

        foreach (EvacuationPlanItem item in secondPlan) {
            Assert.DoesNotContain(item.VehicleID, assignedVehicleIds);
        }
    }

    [Fact]
    public async Task Update_ReleasesVehicle_AvailableForNextPlan() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 80, 5));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V1", 40));

        await client.PostAsync("/api/evacuations/plan", null);

        // V1 is dispatched — second plan should have no vehicles
        HttpResponseMessage emptyPlanResponse = await client.PostAsync("/api/evacuations/plan", null);
        EvacuationPlanItem[]? emptyPlan = await emptyPlanResponse.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(emptyPlan);
        Assert.Empty(emptyPlan);

        // Confirm evacuation — V1 is released
        UpdateStatusRequest update = new() { ZoneID = "Z1", VehicleID = "V1", NumberOfPeopleEvacuated = 40 };
        await client.PutAsJsonAsync("/api/evacuations/update", update);

        // V1 is available again, 40 people remaining
        HttpResponseMessage thirdPlanResponse = await client.PostAsync("/api/evacuations/plan", null);
        EvacuationPlanItem[]? thirdPlan = await thirdPlanResponse.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(thirdPlan);
        Assert.NotEmpty(thirdPlan);
        Assert.Contains(thirdPlan, item => item.VehicleID == "V1");
    }

    [Fact]
    public async Task Plan_UsesRemainingPeople_NotOriginalCount() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 50, 5));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V1", 30));

        await client.PostAsync("/api/evacuations/plan", null);

        UpdateStatusRequest update = new() { ZoneID = "Z1", VehicleID = "V1", NumberOfPeopleEvacuated = 30 };
        await client.PutAsJsonAsync("/api/evacuations/update", update);

        HttpResponseMessage secondPlanResponse = await client.PostAsync("/api/evacuations/plan", null);
        EvacuationPlanItem[]? secondPlan = await secondPlanResponse.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(secondPlan);
        Assert.NotEmpty(secondPlan);

        int totalPeople = secondPlan.Sum(p => p.NumberOfPeople);
        Assert.Equal(20, totalPeople);
    }

    [Fact]
    public async Task FullEvacuationFlow_MultipleRounds() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 60, 5));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V1", 25));

        // Round 1: plan and evacuate 25
        HttpResponseMessage round1Response = await client.PostAsync("/api/evacuations/plan", null);
        EvacuationPlanItem[]? round1 = await round1Response.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(round1);
        Assert.Equal(25, round1.Sum(p => p.NumberOfPeople));

        UpdateStatusRequest update1 = new() { ZoneID = "Z1", VehicleID = "V1", NumberOfPeopleEvacuated = 25 };
        await client.PutAsJsonAsync("/api/evacuations/update", update1);

        // Round 2: plan and evacuate 25
        HttpResponseMessage round2Response = await client.PostAsync("/api/evacuations/plan", null);
        EvacuationPlanItem[]? round2 = await round2Response.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(round2);
        Assert.Equal(25, round2.Sum(p => p.NumberOfPeople));

        UpdateStatusRequest update2 = new() { ZoneID = "Z1", VehicleID = "V1", NumberOfPeopleEvacuated = 25 };
        await client.PutAsJsonAsync("/api/evacuations/update", update2);

        // Round 3: plan for remaining 10
        HttpResponseMessage round3Response = await client.PostAsync("/api/evacuations/plan", null);
        EvacuationPlanItem[]? round3 = await round3Response.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(round3);
        Assert.Equal(10, round3.Sum(p => p.NumberOfPeople));

        UpdateStatusRequest update3 = new() { ZoneID = "Z1", VehicleID = "V1", NumberOfPeopleEvacuated = 10 };
        await client.PutAsJsonAsync("/api/evacuations/update", update3);

        // Zone fully evacuated — no more assignments
        HttpResponseMessage finalPlanResponse = await client.PostAsync("/api/evacuations/plan", null);
        EvacuationPlanItem[]? finalPlan = await finalPlanResponse.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(finalPlan);
        Assert.Empty(finalPlan);

        // Verify final status
        HttpResponseMessage statusResponse = await client.GetAsync("/api/evacuations/status");
        EvacuationStatus[]? statuses = await statusResponse.Content.ReadFromJsonAsync<EvacuationStatus[]>();
        Assert.NotNull(statuses);
        Assert.Single(statuses);
        Assert.Equal(60, statuses[0].TotalEvacuated);
        Assert.Equal(0, statuses[0].RemainingPeople);
    }

    [Fact]
    public async Task Update_ExceedingRemaining_ReturnsBadRequest() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 20, 3));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V1", 40));

        UpdateStatusRequest update = new() { ZoneID = "Z1", VehicleID = "V1", NumberOfPeopleEvacuated = 25 };
        HttpResponseMessage response = await client.PutAsJsonAsync("/api/evacuations/update", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExceedingRemaining_VehicleStaysDispatched() {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/evacuation-zones", MakeZone("Z1", 20, 5));
        await client.PostAsJsonAsync("/api/vehicles", MakeVehicle("V1", 40));

        // Dispatch V1
        await client.PostAsync("/api/evacuations/plan", null);

        // Invalid update — V1 should stay dispatched
        UpdateStatusRequest badUpdate = new() { ZoneID = "Z1", VehicleID = "V1", NumberOfPeopleEvacuated = 25 };
        HttpResponseMessage badResponse = await client.PutAsJsonAsync("/api/evacuations/update", badUpdate);
        Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);

        // V1 still dispatched — plan should have no vehicles
        HttpResponseMessage planResponse = await client.PostAsync("/api/evacuations/plan", null);
        EvacuationPlanItem[]? plan = await planResponse.Content.ReadFromJsonAsync<EvacuationPlanItem[]>();
        Assert.NotNull(plan);
        Assert.Empty(plan);

        // Valid update — releases V1
        UpdateStatusRequest goodUpdate = new() { ZoneID = "Z1", VehicleID = "V1", NumberOfPeopleEvacuated = 20 };
        HttpResponseMessage goodResponse = await client.PutAsJsonAsync("/api/evacuations/update", goodUpdate);
        Assert.Equal(HttpStatusCode.OK, goodResponse.StatusCode);
    }
}