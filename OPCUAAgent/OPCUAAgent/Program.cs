using Opc.UaFx;
using Opc.UaFx.Client;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

class Program
{
    static async Task Main(string[] args)
    {
        using (var client = new OpcClient("opc.tcp://localhost:4840/"))
        {
            client.Connect();

            var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await periodicTimer.WaitForNextTickAsync())
            {
                var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
                await Browse(node);
            }

            client.Disconnect();
            Console.ReadKey(true);
        }
    }

    static async Task Browse(OpcNodeInfo node)
    {
        foreach (var childNode in node.Children())
        {
            var displayName = childNode.Attribute(OpcAttribute.DisplayName);

            if (!displayName.Value.ToString().Contains("Server"))
            {
                Console.WriteLine(displayName.Value);
                foreach (var attribute in childNode.Children())
                {
                    Console.WriteLine(
                        "{0} === {1}",
                        attribute.AttributeValue(OpcAttribute.DisplayName),
                        attribute.AttributeValue(OpcAttribute.Value));
                }
            }
            Console.WriteLine();
        }
        await Task.Delay(1000);
    }
}