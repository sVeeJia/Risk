using MongoDB.Bson;
using MongoDB.Driver;
using Risk.Api.DesignPatterns;
using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.DesignPatterns.Repository;
using Risk.Api.DesignPatterns.Factory.Interfaces;
using Risk.Api.DesignPatterns.Factory;
using Risk.Api.Services;
using Risk.Api.Services.Interfaces;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Risk.Api.Data;
using Risk.Api.Data.Import;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add MongoDB client as a singleton service
var mongoConnection = builder.Configuration["Mongo:ConnectionString"]?? throw new InvalidOperationException("Mongo:ConnectionString is not set.");
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnection));

// Added services
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<ICountrySource>(_ => new ExcelCountrySource(Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "CountriesPopulationForRisk.xlsx")));
builder.Services.AddSingleton<ICountryRepository, CountryRepository>();
builder.Services.AddSingleton<CountrySeeder>();
builder.Services.AddSingleton<ICountryService, CountryService>();
builder.Services.AddSingleton<IGameRepository, GameRepository>();
builder.Services.AddSingleton<IGameFactory, GameFactory>();
builder.Services.AddSingleton<IGameService, GameService>();

// Configure JWT authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not set.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep "sub", "name", "email" exactly as written in the token
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name"
        };

        // The token travels in the HttpOnly cookie, not in an Authorization header.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["risk_auth"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy => policy
        .WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Content-Type")
        .AllowCredentials());
});

// Configure rate limiting for authentication endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Max 10 login/register attempts per minute for each IP address.
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// swagger is only enabled in development mode, so that the API is not exposed to the public
   if (app.Environment.IsDevelopment())
   {
       app.MapOpenApi();
       app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Risk API v1"));
   }

app.UseHttpsRedirection();

app.UseCors("web");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// check to see if mongo is connected
app.MapGet("/health/db", async (IMongoClient client) =>
{
    await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
    return Results.Ok("Mongo connected");
});

await app.Services.GetRequiredService<CountrySeeder>().SeedAsync();

app.Run();
