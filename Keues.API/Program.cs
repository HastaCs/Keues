using System.Text;
using Keues.API.Common;
using Keues.API.Hubs;
using Keues.Application.Common;
using Keues.Application.DeviceRegistry;
using Keues.Application.Features.Counters;
using Keues.Application.Features.Dashboard;
using Keues.Application.Features.Devices;
using Keues.Application.Features.Flows;
using Keues.Application.Features.Locations;
using Keues.Application.Features.Locations.CreateLocation;
using Keues.Application.Features.Locations.DeleteLocation;
using Keues.Application.Features.Locations.GetAllLocations;
using Keues.Application.Features.Locations.GetLocation;
using Keues.Application.Features.Locations.UpdateLocation;

using Keues.Application.Features.Queues;
using Keues.Application.Features.Tickets;
using Keues.Application.Features.Users.CreateAdmin;
using Keues.Application.Features.Users.HasAdmin;
using Keues.Application.Features.Users.Login;
using Keues.Application.Features.Users.Me;
using Keues.Application.Features.Users.ForgotPassword;
using Keues.Application.Features.Users.ResetPassword;
using Keues.Infrastructure.Authorization;
using Keues.Infrastructure.Email;

using Keues.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);

var configPath = Path.Combine(dataDir, "config.json");
var runtimeConfig = RuntimeConfigStore.LoadOrCreate(configPath);

runtimeConfig.ApplyEnvironmentOverrides();
runtimeConfig.BindToConfiguration(builder.Configuration);

builder.Services.Configure<PasswordResetOptions>(
  options => options.FrontendUrl = runtimeConfig.DashboardUrl);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
  options.AddDocumentTransformer((document, context, cancellationToken) =>
  {
    document.Info.Title = "Keues API";
    document.Info.Description = "Queue and ticket management API: locations, flows, queues, counters, tickets and devices. Authentication is done via the HttpOnly \"access_token\" cookie (JWT) set by the login and create-admin endpoints.";
    document.Info.Version = "1.0.0";
    document.Info.Contact = new OpenApiContact
    {
      Name = "Keues",
      Url = new Uri("https://www.keues.dev")
    };
    document.Servers =
    [
      new OpenApiServer { Url = "http://localhost:5125", Description = "Local development server" }
    ];
    document.AddComponent("access_token", new OpenApiSecurityScheme
    {
      Type = SecuritySchemeType.ApiKey,
      Name = "access_token",
      In = ParameterLocation.Cookie,
      Description = "HttpOnly cookie with the JWT. It is obtained by calling POST /api/users/login or POST /api/users/create-admin."
    });
    return Task.CompletedTask;
  });

  options.AddOperationTransformer((operation, context, cancellationToken) =>
  {
    var hasAuthorize = context.Description.ActionDescriptor.EndpointMetadata.Any(m => m is IAuthorizeData);
    var allowAnonymous = context.Description.ActionDescriptor.EndpointMetadata.Any(m => m is IAllowAnonymous);
    if (!hasAuthorize || allowAnonymous)
      return Task.CompletedTask;

    operation.Security =
    [
      new OpenApiSecurityRequirement
      {
        [new OpenApiSecuritySchemeReference("access_token", context.Document, null)] = []
      }
    ];
    return Task.CompletedTask;
  });
});


builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseSqlite($"Data Source={Path.Combine(dataDir, "keues.db")}"));

builder.Services.AddScoped<IApplicationDbContext>(sp =>
  sp.GetRequiredService<AppDbContext>());

#region useCases
builder.Services.AddTicketTypesUseCases();
builder.Services.AddCountersUseCases();
builder.Services.AddLocationUseCases();
builder.Services.AddTicketsUseCases();
builder.Services.AddDashboardUseCases();
builder.Services.AddFlowUseCases();
builder.Services.AddScoped<CreateAdminHandle>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<HasAdminHandler>();
builder.Services.AddScoped<GetCurrentUserHandler>();
builder.Services.AddScoped<ForgotPasswordHandler>();
builder.Services.AddScoped<ResetPasswordHandler>();
builder.Services.AddDeviceUseCases();
#endregion


builder.Services.AddSingleton<ConnectedDeviceRegistry>();
builder.Services.AddSignalR();


builder.Services.AddAuthorizationServices(builder.Configuration);
builder.Services.AddEmailServices(builder.Configuration);

var jwt = builder.Configuration
  .GetSection("Jwt")
  .Get<JwtOptions>()!;

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,

      ValidIssuer = jwt.Issuer,
      ValidAudience = jwt.Audience,

      IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(jwt.Key))
    };
    options.Events = new JwtBearerEvents
    {
      OnMessageReceived = context =>
      {
        context.Token = context.Request.Cookies["access_token"];
        return Task.CompletedTask;
      }
    };
  });

builder.Services.AddAuthorization();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  db.Database.Migrate();
}



app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.UseHttpsRedirection();
}
else
{
  app.UseDefaultFiles();
  app.UseStaticFiles();
}

app.UseAuthorization();
app.MapControllers();
app.MapHub<DeviceHub>("/devices");

if (!app.Environment.IsDevelopment())
{
  app.MapWhen(
    context => !context.Request.Path.StartsWithSegments("/api")
      && !context.Request.Path.StartsWithSegments("/devices"),
    appBuilder => appBuilder.Run(async context =>
    {
      context.Response.ContentType = "text/html";
      await context.Response.SendFileAsync(
        Path.Combine(builder.Environment.WebRootPath, "index.html"));
    }));
}

app.Run();