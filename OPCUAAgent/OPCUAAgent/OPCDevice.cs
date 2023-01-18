﻿using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

public class OPCDevice
{
    public static OpcClient client = new OpcClient(File.ReadAllLines($"../../../../../Settings.txt")[1]);

    public static void Start()
    {
        client.Connect();
        Console.WriteLine("Connected to Opc");

        CheckDevices();

    }
    public static void End()
    {
        client.Disconnect();
    }

    public static void CheckDevices()
    {
        var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);

        Console.WriteLine("----------------");
        Console.WriteLine(" Actual devices:");

        if (node.Children().Count() > 1)
        {
            foreach (var childNode in node.Children())
            {
                if (!childNode.DisplayName.Value.Contains("Server"))
                {
                    int deviceId = Convert.ToInt32(childNode.DisplayName.Value.Split(" ")[1]);
                    Console.WriteLine($"\tDevice {deviceId}");
                }
            }
        }
        Console.WriteLine("----------------");
    }

    public static async Task EmergencyStop()
    {
        Console.WriteLine($"Device shuts down device {Program.deviceID} ...\n");
        client.CallMethod($"ns=2;s=Device {Program.deviceID}", $"ns=2;s=Device {Program.deviceID}/EmergencyStop");
        client.WriteNode($"ns=2;s=Device {Program.deviceID}/ProductionRate", OpcAttribute.Value, 0);
        await Task.Delay(1000);
    }
    public static async Task ResetErrorStatus()
    {
        client.CallMethod($"ns=2;s=Device {Program.deviceID}", $"ns=2;s=Device {Program.deviceID}/ResetErrorStatus");
        await Task.Delay(1000);
    }
    public static async Task Maintenance()
    {
        Program.maintenanceDate = DateTime.Now;
        Console.WriteLine($"Device Last Maintenace Date set to: {Program.maintenanceDate} ...\n");
        await IoTDevice.UpdateTwinValueAsync("LastMaintenanceDate", Program.maintenanceDate);
    }
    public static async Task ReduceProductionRate()
    {
        int value = (int)client.ReadNode($"ns=2;s=Device {Program.deviceID}/ProductionRate").Value;
        client.WriteNode($"ns=2;s=Device {Program.deviceID}/ProductionRate", OpcAttribute.Value, value - 10);
        await Task.Delay(1000);
    }
    public static async Task SetProductionRate(int value)
    {
        client.WriteNode($"ns=2;s=Device {Program.deviceID}/ProductionRate", OpcAttribute.Value, value);
        await Task.Delay(1000);
    }
}

