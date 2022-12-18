using Opc.UaFx;
using Opc.UaFx.Client;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class OPCDevice
{
    public static OpcClient client;

   
    public static void Start()
    {
        client = new OpcClient(File.ReadAllLines("Settings.txt")[3]);
        client.Connect();
    }

    public static void End()
    {
        client.Disconnect();
    }

    public static async Task EmergencyStop(string deviceId)
    {
        Console.WriteLine($"Device shuts down device {deviceId} ...\n");
        client.CallMethod($"ns=2;s=Device {deviceId}", $"ns=2;s=Device {deviceId}/EmergencyStop");
        client.WriteNode($"ns=2;s=Device {deviceId}/ProductionRate", OpcAttribute.Value, 0);
        await Task.Delay(1000);
    }
    public static async Task ResetErrorStatus(string deviceId)
    {
        client.CallMethod($"ns=2;s=Device {deviceId}", $"ns=2;s=Device {deviceId}/ResetErrorStatus");
        await Task.Delay(1000);
    }
    public static async Task Maintenance()
    {
        Program.maintenanceDate = DateTime.Now;
        Console.WriteLine($"Device Last Maintenace Date set to: {Program.maintenanceDate} ...\n");
        await IoTDevice.UpdateTwinValueAsync("Last Maintenance Date", Program.maintenanceDate);
    }

}

