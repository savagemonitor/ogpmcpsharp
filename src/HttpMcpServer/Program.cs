using OpenGoPro.Client;
using GpSharp.Core;
using GpSharp.Network;
using HttpMcpServer.Tools;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<MdnsDiscovery>();
builder.Services.AddSingleton<IDiscoveryService>(services => services.GetRequiredService<MdnsDiscovery>());
builder.Services.AddSingleton<OgpSdk>();
builder.Services.AddSingleton<IGpSharpAdapter, GpSharpAdapter>();
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<CameraTools>();
var app = builder.Build();
app.MapMcp("/mcp");
app.Run();

public partial class Program;
