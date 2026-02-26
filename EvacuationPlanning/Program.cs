using EvacuationPlanning;
using EvacuationPlanning.Strategies;
using EvacuationPlanning.Strategies.Genetic;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IFitnessProvider, ThroughputFitnessProvider>();
builder.Services.AddSingleton<IStrategy, GeneticStrategy>();
builder.Services.AddSingleton<Planner>();
builder.Services.AddHostedService<RedisStatusSync>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapDashboard();

app.Run();

// Required for WebApplicationFactory to access the entry point in integration tests
public partial class Program;