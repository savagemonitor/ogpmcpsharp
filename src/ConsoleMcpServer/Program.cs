using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenGoPro.Client;
using GpSharp.Core;
using GpSharp.Network;

class Program {
    static async Task Main(string[] args) {
        var services = new ServiceCollection();
        services.AddSingleton<MdnsDiscovery>();
        services.AddSingleton<IDiscoveryService>(sp => sp.GetRequiredService<MdnsDiscovery>());
        services.AddSingleton<OgpSdk>();
        services.AddSingleton<IGpSharpAdapter, GpSharpAdapter>();
        services.AddSingleton<IOpenGoProClient, OpenGoProClient>();
        using var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IOpenGoProClient>();
        Console.WriteLine("Console MCP server started. Enter JSON commands (for example: {\"name\":\"list_cameras\"}):");
        string? line;
        while ((line = Console.ReadLine()) != null) {
            try {
                var res = await client.SendMcpCommandAsync(line);
                Console.WriteLine(res);
            }
            catch (Exception error) {
                Console.Error.WriteLine($"Command failed: {error.Message}");
            }
        }
    }
}
