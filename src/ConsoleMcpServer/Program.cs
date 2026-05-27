using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenGoPro.Client;

class Program {
    static async Task Main(string[] args) {
        var services = new ServiceCollection();
        services.AddSingleton<IOpenGoProClient, OpenGoProClient>();
        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IOpenGoProClient>();
        Console.WriteLine("Console MCP Server started. Type commands:");
        string line;
        while ((line = Console.ReadLine()) != null) {
            var res = await client.SendMcpCommandAsync(line);
            Console.WriteLine(res);
        }
    }
}
