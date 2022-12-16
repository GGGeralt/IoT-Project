using Opc.UaFx;
using Opc.UaFx.Client;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Net.Mime;
using System.Text;
using DeviceSdkDemo.Device;

class Program
{
    static string deviceConnectionString = "HostName=EasyTestHub.azure-devices.net;DeviceId=device-2022-10-13;SharedAccessKey=/8+AHgWly8gBebwW17hUAfaiObRTyljKsgSMGbyiE48=";

    static async Task Main(string[] args)
    {
        using (var client = new OpcClient("opc.tcp://localhost:4840/"))
        {
            client.Connect();

            var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await periodicTimer.WaitForNextTickAsync())
            {
                await SendTelymetryDataToIoTHub(client);
            }

            client.Disconnect();
            Console.ReadKey(true);
        }
    }

    static async Task SendTelymetryDataToIoTHub(OpcClient opcClient)
    {
        var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);

        using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
        await deviceClient.OpenAsync();
        var device = new VirtualDevice(deviceClient);

        await device.UpdateTwinAsync();


        if (node.Children().Count() > 1)
        {
            int deviceCount = node.Children().Count() - 1;

            for (int i = 1; i <= deviceCount; i++)
            {
                Console.WriteLine($"Device {i}");

                #region telemetryValues
                int productionStatus = (int)opcClient.ReadNode($"ns=2;s=Device {i}/ProductionStatus").Value;
                string workorderId = (string)opcClient.ReadNode($"ns=2;s=Device {i}/WorkorderId").Value;
                long goodCount = (long)opcClient.ReadNode($"ns=2;s=Device {i}/GoodCount").Value;
                long badCount = (long)opcClient.ReadNode($"ns=2;s=Device {i}/BadCount").Value;
                double temperature = (double)opcClient.ReadNode($"ns=2;s=Device {i}/Temperature").Value;

                await VirtualDevice.SendTelemetryData(deviceClient, productionStatus, workorderId, goodCount, badCount, temperature);

                Console.WriteLine("ProductionStatus: " + productionStatus);
                Console.WriteLine("workorderId: " + workorderId);
                Console.WriteLine("goodCount: " + goodCount);
                Console.WriteLine("badCount: " + badCount);
                Console.WriteLine("temperature: " + temperature);

                #endregion telemetryValues

                //int productionRate = (int)opcClient.ReadNode($"ns=2;s=Device {i}/ProductionRate").Value;

                //Console.WriteLine("productionRate: " + productionRate);

                //OpcReadNode[] commands = new OpcReadNode[]
                //{
                //new OpcReadNode($"ns=2;s=Device {i}/ProductionStatus"),
                //new OpcReadNode($"ns=2;s=Device {i}/WorkorderId"),
                //new OpcReadNode($"ns=2;s=Device {i}/ProductionRate"),
                //new OpcReadNode($"ns=2;s=Device {i}/GoodCount"),
                //new OpcReadNode($"ns=2;s=Device {i}/BadCount"),
                //new OpcReadNode($"ns=2;s=Device {i}/Temperature"),
                //};

                //IEnumerable<OpcValue> job = opcClient.ReadNodes(commands);

                //int productionStatus = (OpcValue)commands[0];


                //foreach (var attribute in job)
                //{
                //    Console.WriteLine(attribute.Value);
                //}

                Console.WriteLine();
            }
            Console.WriteLine("-----------------------------------------------------------------");

            //int count = 0;
            //foreach (var childNode in node.Children())
            //{
            //    node.Child($"");
            //    var displayName = childNode.Attribute(OpcAttribute.DisplayName);


            //    Console.WriteLine(displayName.Value);
            //    foreach (var attribute in childNode.Children())
            //    {
            //        Console.WriteLine(
            //            "{0} === {1}",
            //            attribute.AttributeValue(OpcAttribute.DisplayName),
            //            attribute.AttributeValue(OpcAttribute.Value));
            //    }

            //    Console.WriteLine();
            //}
        }

        await Task.Delay(1000);
    }
    #region Sending Messages

    public async Task SendMessages()
    {
        var rnd = new Random();

        var data = new
        {
            temperature = rnd.Next(20, 35),
            humidity = rnd.Next(60, 80),
        };

        var dataString = JsonConvert.SerializeObject(data);

        Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
        eventMessage.ContentType = MediaTypeNames.Application.Json;
        eventMessage.ContentEncoding = "utf-8";
        eventMessage.Properties.Add("temperatureAlert", (data.temperature > 30) ? "true" : "false");
        Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message Data: [{dataString}]");

        //await opcClient.SendEventAsync(eventMessage);

        await Task.Delay(1000);

        Console.WriteLine();
    }

    #endregion Sending Messages
}