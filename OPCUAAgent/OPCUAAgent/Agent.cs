using Opc.UaFx;
using Opc.UaFx.Client;
using Microsoft.Azure.Devices.Client;
using Azure.Storage.Queues;
using Newtonsoft.Json;
using System.Net.Mime;
using System.Text;

public class Program
{
    static string[] settings = File.ReadAllLines("Settings.txt");

    public static DateTime maintenanceDate = DateTime.MinValue;

    static async Task Main(string[] args)
    {
        using var deviceClient = DeviceClient.CreateFromConnectionString(settings[1], TransportType.Mqtt);
        await deviceClient.OpenAsync();

        QueueClient queueClient = new QueueClient(settings[5], "iot-project");
        await queueClient.CreateIfNotExistsAsync();

        var device = new IoTDevice(deviceClient, queueClient);

        await device.InitializeHandlers();

        OPCDevice opcDevice = new OPCDevice();

        OPCDevice.Start();


        //odczytywanie i wysylanie co sekunde
        var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await periodicTimer.WaitForNextTickAsync())
        {
            IoTDevice.SendDataToIoTHub(OPCDevice.client);
        }

        OPCDevice.End();
        Console.ReadKey(true);

    }
}