using FixMarketData.Backend;
using FixMarketData.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetValue<string>("UiBaseUrl") ?? "https://localhost:7069")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDbContext<MarketDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MarketDataDb")));

builder.Services.AddSingleton<BackendFixApplication>();
builder.Services.AddHostedService<BackendFixHostedService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();

app.MapGet("/", () => "FIX Market Data backend is running.");
app.MapHub<MarketDataHub>("/marketDataHub");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MarketDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();
