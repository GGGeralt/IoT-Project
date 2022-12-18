using Opc.UaFx;
using Opc.UaFx.Client;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Net.Mime;
using System.Text;

public class Program
{
    static string[] settings = File.ReadAllLines("Settings.txt");
    static string deviceConnectionString = settings[1];
    static string OpcClientAddresString = settings[3];

    public static DateTime maintenanceDate = DateTime.MinValue;

    static async Task Main(string[] args)
    {
        using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
        await deviceClient.OpenAsync();
        var device = new IoTDevice(deviceClient);

        await device.InitializeHandlers();

        OPCDevice opcDevice = new OPCDevice();

        OPCDevice.Start();

        Console.WriteLine("Connected");

        //odczytywanie i wysylanie co sekunde
        var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await periodicTimer.WaitForNextTickAsync())
        {
            //await device.SendDataToIoTHub(OPCDevice.client);
        }

        OPCDevice.End();
        Console.ReadKey(true);

    }
}