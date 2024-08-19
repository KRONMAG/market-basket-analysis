using MarketBasketAnalysis.Server.API.Extensions;
using MarketBasketAnalysis.Server.API.Services;
using MarketBasketAnalysis.Server.Application.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddGrpc(o => o.ConfigureGrpc());
builder.Services.AddGrpcHealthChecks()
    .AddCheck("Sample", () => HealthCheckResult.Healthy());

if (builder.Environment.IsDevelopment())
    services.AddGrpcReflection();

services.AddLogging();
services.AddDbContext(builder.Configuration);

services.AddScoped<IAssociationRuleSetInfoLoader, AssociationRuleSetInfoLoader>();
services.AddScoped<IAssociationRuleSetSaver, AssociationRuleSetSaver>();
services.AddScoped<IAssociationRuleSetLoader, AssociationRuleSetLoader>();
services.AddScoped<IAssociationRuleSetRemover, AssociationRuleSetRemover>();

var app = builder.Build();

ApplicationLogging.LoggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

await app.ConfigureDb();

app.MapGrpcService<AssociationRuleSetStorage>();
app.MapGrpcHealthChecksService();

if (app.Environment.IsDevelopment())
    app.MapGrpcReflectionService();

await app.RunAsync();