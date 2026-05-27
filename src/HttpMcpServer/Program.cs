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

builder.Services.AddSingleton<IProtoTransport>(sp => 
    new OpenApiHttpTransport(openApiPath, new System.Net.Http.HttpClient { BaseAddress = new Uri(cameraBase) }, cameraBase));
builder.Services.AddSingleton<IOpenGoProClient>(sp => 
    new OpenGoProClient(sp.GetRequiredService<IProtoTransport>()));

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
