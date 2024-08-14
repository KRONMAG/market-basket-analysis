using MarketBasketAnalysis.Server.API.Extensions;
using MarketBasketAnalysis.Server.API.Services;
using MarketBasketAnalysis.Server.Application.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddGrpc(o => o.ConfigureGrpc());

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

if (app.Environment.IsDevelopment())
    app.MapGrpcReflectionService();

await app.RunAsync();