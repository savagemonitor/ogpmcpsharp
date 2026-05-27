using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenGoPro.Client;

var builder = WebApplication.CreateBuilder(args);

// Register OpenGoPro transports and client
var openApiPath = System.Environment.GetEnvironmentVariable("OGP_OPENAPI_PATH") 
    ?? throw new InvalidOperationException("Environment variable 'OGP_OPENAPI_PATH' is required");
var cameraBase = System.Environment.GetEnvironmentVariable("OGP_CAMERA_BASE") 
    ?? throw new InvalidOperationException("Environment variable 'OGP_CAMERA_BASE' is required");

// Register typed HTTP client generated from OpenAPI
builder.Services.AddSingleton<OpenGoPro.Client.Generated.IOpenGoProHttpClient>(sp =>
    new OpenGoPro.Client.Generated.OpenGoProHttpClient(openApiPath, new System.Net.Http.HttpClient { BaseAddress = new Uri(cameraBase) }));

// Optionally register proto transport if available (keeps previous abstraction)
builder.Services.AddSingleton<IProtoTransport>(sp =>
    new OpenApiHttpTransport(openApiPath, new System.Net.Http.HttpClient { BaseAddress = new Uri(cameraBase) }, cameraBase));

// Register high-level client that prefers proto transport when present and falls back to HTTP client
builder.Services.AddSingleton<IOpenGoProClient>(sp =>
{
    var proto = sp.GetService<IProtoTransport>();
    var http = sp.GetRequiredService<OpenGoPro.Client.Generated.IOpenGoProHttpClient>();
    return new OpenGoProClient(proto, http);
});

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
