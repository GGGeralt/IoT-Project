using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Opc.UaFx.Client;
using Opc.UaFx;
using System.Net.Mime;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Azure.Amqp.Framing;
using System.Net.Sockets;

public class IoTDevice
{
    public static DeviceClient deviceClient;

    public IoTDevice(DeviceClient deviceClient)
    {
        IoTDevice.deviceClient = deviceClient;
        Console.WriteLine("Connected to IoT");
    }

    #region Sending Messages
    public static async Task OneDeviceMagic(OpcClient opcClient, int deviceId)
    {
        var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);

        #region telemetryValues

        int productionStatus = (int)opcClient.ReadNode($"ns=2;s=Device {deviceId}/ProductionStatus").Value;
        string workorderId = (string)opcClient.ReadNode($"ns=2;s=Device {deviceId}/WorkorderId").Value;
        long goodCount = (long)opcClient.ReadNode($"ns=2;s=Device {deviceId}/GoodCount").Value;
        long badCount = (long)opcClient.ReadNode($"ns=2;s=Device {deviceId}/BadCount").Value;
        double temperature = (double)opcClient.ReadNode($"ns=2;s=Device {deviceId}/Temperature").Value;

        dynamic telemetryData = new
        {
            productionStatus = productionStatus,
            workorderId = workorderId,
            goodCount = goodCount,
            badCount = badCount,
            temperature = temperature
        };

        Console.WriteLine(telemetryData);

        await SendTelemetryData(deviceClient, telemetryData);

        #endregion telemetryValues

        int deviceErrors = (int)opcClient.ReadNode($"ns=2;s=Device {deviceId}/DeviceError").Value;
        int productionRate = (int)opcClient.ReadNode($"ns=2;s=Device {deviceId}/ProductionRate").Value;
        //HERE
        await UpdateTwinAsync(deviceErrors, productionRate);

        //Console.WriteLine("-----------------------------------------------------------------");

    }

    //public static async Task SendDataToIoTHub(OpcClient opcClient)
    //{
    //    var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);

    //    if (node.Children().Count() > 1)
    //    {
    //        foreach (var childNode in node.Children())
    //        {
    //            if (!childNode.DisplayName.Value.Contains("Server"))
    //            {
    //                #region telemetryValues

    //                int deviceId = Convert.ToInt32(childNode.DisplayName.Value.Split(" ")[1]);

    //                int productionStatus = (int)opcClient.ReadNode($"ns=2;s=Device {deviceId}/ProductionStatus").Value;
    //                string workorderId = (string)opcClient.ReadNode($"ns=2;s=Device {deviceId}/WorkorderId").Value;
    //                long goodCount = (long)opcClient.ReadNode($"ns=2;s=Device {deviceId}/GoodCount").Value;
    //                long badCount = (long)opcClient.ReadNode($"ns=2;s=Device {deviceId}/BadCount").Value;
    //                double temperature = (double)opcClient.ReadNode($"ns=2;s=Device {deviceId}/Temperature").Value;

    //                dynamic telemetryData = new
    //                {
    //                    productionStatus = productionStatus,
    //                    workorderId = workorderId,
    //                    goodCount = goodCount,
    //                    badCount = badCount,
    //                    temperature = temperature
    //                };


    //                await SendTelemetryData(deviceClient, deviceId, telemetryData);

    //                #endregion telemetryValues

    //                int deviceErrors = (int)opcClient.ReadNode($"ns=2;s=Device {deviceId}/DeviceError").Value;
    //                int productionRate = (int)opcClient.ReadNode($"ns=2;s=Device {deviceId}/ProductionRate").Value;

    //                await UpdateTwinAsync(deviceId, deviceErrors, productionRate);
    //            }

    //            //Console.WriteLine();
    //        }
    //        //Console.WriteLine("-----------------------------------------------------------------");
    //    }
    //}
    public static async Task SendTelemetryData(DeviceClient client, dynamic machineData)
    {
        Console.WriteLine($"Device sending ||TELEMETRY DATA|| messages to IoTHub...\n");

        var dataString = JsonConvert.SerializeObject(machineData);

        Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
        eventMessage.ContentType = MediaTypeNames.Application.Json;
        eventMessage.ContentEncoding = "utf-8";
        //Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message Telemetry Data: [{dataString}]");

        await client.SendEventAsync(eventMessage);
    }

    #endregion Sending Messages

    #region Receiving Messages

    private static async Task OnC2dMessageReceivedAsync(Message receivedMessage, object _)
    {
        Console.WriteLine($"\t{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
        PrintMessage(receivedMessage);

        await deviceClient.CompleteAsync(receivedMessage);
        Console.WriteLine($"\t{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");

        receivedMessage.Dispose();
    }

    private static void PrintMessage(Message receivedMessage)
    {
        string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
        Console.WriteLine($"\t\tReceived message: {messageData}");

        int propCount = 0;
        foreach (var prop in receivedMessage.Properties)
        {
            Console.WriteLine($"\t\tProperty[{propCount++}> Key={prop.Key} : Value={prop.Value}");
        }
    }

    #endregion Receiving Messages

    #region Device Twin
    public static async Task UpdateTwinAsync(int deviceErrors, int productionRate)
    {
        //updatowanie device twina tylko gdy jakas wartosc sie zmieni
        //jezeli odczytana wartosc jest inna niz ta co chcemy wsadzic, wtedy wysylamy
        await UpdateTwinValueAsync("deviceErrors", deviceErrors);
        await UpdateTwinValueAsync("productionRate", productionRate);
    }

    public static async Task UpdateTwinValueAsync(string valueName, int value)
    {
        var twin = await deviceClient.GetTwinAsync();
        var reportedProperties = new TwinCollection();

        reportedProperties = new TwinCollection();
        reportedProperties[valueName] = value;
        await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    }

    public static async Task UpdateTwinValueAsync(string valueName, DateTime value, int deviceId = 0)
    {
        var twin = await deviceClient.GetTwinAsync();
        var reportedProperties = new TwinCollection();
        reportedProperties[valueName] = value;

        await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    }

    private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
    {
        Console.WriteLine($"\tDesired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
        TwinCollection reportedProperties = new TwinCollection();
        reportedProperties["productionRate"] = userContext;

        await deviceClient.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
    }

    #endregion Device Twin

    #region Direct Methods
    #region EmergencyStop
    async Task EmergencyStop()
    {
        OPCDevice.EmergencyStop();
        await (Task.Delay(1000));
    }
    private async Task<MethodResponse> EmergencyStopHandler(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
        await EmergencyStop();
        return new MethodResponse(0);
    }
    #endregion EmergencyStop
    #region ResetErrorStatus
    async Task ResetErrorStatus()
    {
        OPCDevice.ResetErrorStatus();
        await (Task.Delay(1000));
    }
    private async Task<MethodResponse> ResetErrorStatusHandler(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
        await ResetErrorStatus();

        return new MethodResponse(0);
    }
    #endregion ResetErrorStatus
    #region Maintenance
    async Task Maintenance()
    {
        OPCDevice.Maintenance();
        await (Task.Delay(1000));

    }
    private async Task<MethodResponse> MaintenanceHandler(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
        await Maintenance();

        return new MethodResponse(0);
    }
    #endregion Maintenance
    #region ReduceProductionRate

    private async Task ReduceProductionRate()
    {
        OPCDevice.ReduceProductionRate();
        await Task.Delay(1000);
    }
    private async Task<MethodResponse> ReduceProductionRateHandler(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
        await ReduceProductionRate();

        return new MethodResponse(0);
    }

    #endregion ReduceProductionRate
    private static async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
        await Task.Delay(1000);

        return new MethodResponse(0);
    }

    #endregion Direct Methods

    public async Task InitializeHandlers()
    {
        await deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, deviceClient);

        await deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStopHandler, deviceClient);
        await deviceClient.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatusHandler, deviceClient);
        await deviceClient.SetMethodHandlerAsync("Maintenance", MaintenanceHandler, deviceClient);
        await deviceClient.SetMethodHandlerAsync("ReduceProductionRate", ReduceProductionRateHandler, deviceClient);

        await deviceClient.SetMethodDefaultHandlerAsync(DefaultServiceHandler, deviceClient);

        await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, deviceClient);
    }
}

