using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenGoPro.Client;
using GpSharp.Core;
using GpSharp.Network;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<MdnsDiscovery>();
builder.Services.AddSingleton<IDiscoveryService>(services => services.GetRequiredService<MdnsDiscovery>());
builder.Services.AddSingleton<OgpSdk>();
builder.Services.AddSingleton<IGpSharpAdapter, GpSharpAdapter>();
builder.Services.AddSingleton<IOpenGoProClient, OpenGoProClient>();
var app = builder.Build();
app.MapControllers();
app.Run();
