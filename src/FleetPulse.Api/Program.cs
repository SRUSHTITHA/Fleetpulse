using System.Text;
using System.Text.Json.Serialization;
using FleetPulse.Api.Hubs;
using FleetPulse.Api.Services;
using FleetPulse.Application.Interfaces;
using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Infrastructure.Data;
using FleetPulse.Infrastructure.Repositories;
using FleetPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FleetPulse API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<FleetPulseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// JWT options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

// Exception engine options
builder.Services.Configure<ExceptionEngineOptions>(builder.Configuration.GetSection(ExceptionEngineOptions.SectionName));

// Google Maps options (Roads API key used for route-deviation road snapping)
builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection(GoogleMapsOptions.SectionName));

// Routing options (self-hosted OSRM base URL; empty = no public OSRM calls)
builder.Services.Configure<RoutingOptions>(builder.Configuration.GetSection(RoutingOptions.SectionName));

// Google OAuth options (Identity Services popup sign-in for admins/drivers)
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection(GoogleAuthOptions.SectionName));

// SMTP options (password reset emails — works with any free SMTP provider)
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<WebAppOptions>(builder.Configuration.GetSection(WebAppOptions.SectionName));

// CORS (allow React dev server + any origin during development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// JWT bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
        options.Events = new JwtBearerEvents
        {
            // SignalR can't set Authorization headers on WebSocket, so the token
            // travels as ?access_token= on hub connections only.
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// Repositories + unit of work
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IExceptionRepository, ExceptionRepository>();
builder.Services.AddScoped<IExceptionRuleRepository, ExceptionRuleRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application / infrastructure services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IExceptionDetectionService, ExceptionDetectionService>();
builder.Services.AddScoped<IExceptionRuleService, ExceptionRuleService>();
builder.Services.AddScoped<IExceptionService, ExceptionService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

// Google Roads API client (typed HttpClient) used for route-deviation road snapping
builder.Services.AddHttpClient<GoogleRoadsSnapService>();
builder.Services.AddScoped<IRoadSnapService, GoogleRoadsSnapService>();

builder.Services.AddHttpClient<IGeoService, GeoService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FleetPulse/1.0 (fleet tracking)");
});

// Google ID-token validator (Google Identity Services popup sign-in)
builder.Services.AddHttpClient<GoogleTokenValidator>();
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

// SMTP email delivery (password reset link emails)
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// SignalR broadcast channel (Api owns the hub context; Application only sees the interface)
builder.Services.AddSingleton<ILocationBroadcaster, LocationBroadcaster>();

// Background seed of users + default exception rules (must run before the engine starts)
builder.Services.AddHostedService<SeedDataService>();

// Exception engine: periodic scan of all in-progress trips (runs after seeding above)
builder.Services.AddHostedService<ExceptionEngineHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ClientApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LocationHub>("/hubs/location");

app.Run();

