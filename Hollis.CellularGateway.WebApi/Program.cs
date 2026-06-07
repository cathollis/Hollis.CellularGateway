using Hollis.CellularGateway.AtClient;
using Hollis.CellularGateway.ServiceDefaults;
using Hollis.CellularGateway.WebApi;
using Hollis.CellularGateway.WebApi.Conventions;
using Hollis.CellularGateway.WebApi.Messaging;
using Hollis.CellularGateway.WebApi.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new ChildResourceConvention());
});
builder.Services.AddOpenApi();

// ── EF Core (MariaDB) ─────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("MySql");
builder.Services.AddDbContextFactory<DatabaseContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ── Services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IShortMessageService, ShortMessageService>();
builder.Services.AddScoped<ISimCardService, SimCardService>();

// ── AtClient (singleton — one modem connection for the app lifetime) ───
var modemPort = builder.Configuration.GetValue<string>("Modem:PortName") ?? "COM8";
var modemBaudRate = builder.Configuration.GetValue<int?>("Modem:BaudRate") ?? 115200;
builder.Services.AddSingleton(new AtClient(modemPort, modemBaudRate));
builder.Services.AddHostedService<ModemBackgroundService>();

// ── MassTransit (in-memory bus) ────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SendSmsConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
