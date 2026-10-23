using MongoDB.Bson;
using MongoDB.Driver;
using Risk.Api.DesignPatterns;
using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.DesignPatterns.Repository;
using Risk.Api.Services;
using Risk.Api.Services.Interfaces;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
// swagger is only enabled in development mode, so that the API is not exposed to the public
   if (app.Environment.IsDevelopment())
   {
       app.MapOpenApi();
       app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Risk API v1"));
   }

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// check to see if mongo is connected
app.MapGet("/health/db", async (IMongoClient client) =>
{
    await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
    return Results.Ok("Mongo connected");
});

app.Run();
