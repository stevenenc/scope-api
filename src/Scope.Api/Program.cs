using MongoDB.Driver;
using Scope.Application.Abstractions;
using Scope.Application.GeoFences.Handlers;
using Scope.Infrastructure.EventStore;
using Scope.Application.GeoFences.Commands;
using Scope.Contracts.GeoFences.Requests;
using Scope.Infrastructure.Projections;
using Scope.Infrastructure.ReadModels;

var builder = WebApplication.CreateBuilder(args);

var mongoSettings = builder.Configuration
    .GetSection("MongoDb")
    .Get<MongoDbSettings>() ?? new MongoDbSettings();

builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<IEventStore, MongoEventStore>();
builder.Services.AddScoped<GeoFenceCommandHandler>();
builder.Services.AddSingleton<GeoFenceProjector>();
builder.Services.AddSingleton<ProjectionRunner>();
builder.Services.AddHostedService<ProjectionWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<GeoFenceProjector>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/geofences/{id}", async (
    string id,
    MongoDbSettings settings) =>
{
    var client = new MongoClient(settings.ConnectionString);
    var database = client.GetDatabase(settings.DatabaseName);

    var collection = database.GetCollection<GeoFenceReadModel>("geofence_readmodels");

    var result = await collection.Find(x => x.Id == id).FirstOrDefaultAsync();

    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapPost("/geofences", async (
    CreateGeoFenceRequest request,
    GeoFenceCommandHandler handler,
    CancellationToken cancellationToken) =>
{
    var command = new CreateGeoFenceCommand(
        GeoFenceId: request.GeoFenceId,
        UserId: request.UserId,
        Name: request.Name,
        GeometryJson: request.GeometryJson,
        CorrelationId: Guid.NewGuid().ToString(),
        IdempotencyKey: Guid.NewGuid().ToString()
    );

    await handler.Handle(command, cancellationToken);

    return Results.Created($"/geofences/{request.GeoFenceId}", new
    {
        request.GeoFenceId,
        Message = "GeoFence created successfully."
    });
});

app.MapPut("/geofences/{id}", async (
    string id,
    UpdateGeoFenceRequest request,
    GeoFenceCommandHandler handler,
    CancellationToken cancellationToken) =>
{
    var command = new UpdateGeoFenceCommand(
        GeoFenceId: id,
        Name: request.Name,
        GeometryJson: request.GeometryJson,
        CorrelationId: Guid.NewGuid().ToString(),
        IdempotencyKey: Guid.NewGuid().ToString()
    );

    await handler.Handle(command, cancellationToken);

    return Results.Ok(new
    {
        GeoFenceId = id,
        Message = "GeoFence updated successfully."
    });
});

app.Run();