using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Opc.UaFx.Client;
using Opc.UaFx;
using System.Net.Mime;
using System.Text;


public class IoTDevice
{
    public static DeviceClient client;

    public IoTDevice(DeviceClient deviceClient)
    {
        client = deviceClient;
    }

    #region Sending Messages
    public static async Task SendDataToIoTHub(OpcClient opcClient)
    {
        var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);

        if (node.Children().Count() > 1)
        {
            foreach (var childNode in node.Children())
            {
                if (!childNode.DisplayName.Value.Contains("Server"))
                {
                    #region telemetryValues

                    int deviceId = Convert.ToInt32(childNode.DisplayName.Value.Split(" ")[1]);

                    int productionStatus = (int)opcClient.ReadNode($"ns=2;s=Device {deviceId}/ProductionStatus").Value;
                    string workorderId = (string)opcClient.ReadNode($"ns=2;s=Device {deviceId}/WorkorderId").Value;
                    long goodCount = (long)opcClient.ReadNode($"ns=2;s=Device {deviceId}/GoodCount").Value;
                    long badCount = (long)opcClient.ReadNode($"ns=2;s=Device {deviceId}/BadCount").Value;
                    double temperature = (double)opcClient.ReadNode($"ns=2;s=Device {deviceId}/Temperature").Value;

                    var temeletryData = new
                    {
                        deviceId = deviceId,
                        productionStatus = productionStatus,
                        workorderId = workorderId,
                        goodCount = goodCount,
                        badCount = badCount,
                        temperature = temperature
                    };

                    await SendTelemetryData(client, temeletryData);

                    #endregion telemetryValues

                    int deviceErrors = (int)opcClient.ReadNode($"ns=2;s=Device {deviceId}/DeviceError").Value;
                    int productionRate = (int)opcClient.ReadNode($"ns=2;s=Device {deviceId}/ProductionRate").Value;

                    await UpdateTwinAsync(deviceErrors, productionRate);

                }

                //Console.WriteLine();
            }
            //Console.WriteLine("-----------------------------------------------------------------");
        }
        await Task.Delay(1000);
    }
    public static async Task SendTelemetryData(DeviceClient client, dynamic machineData)
    {
        Console.WriteLine($"Device sending ||TELEMETRY DATA|| messages to IoTHub...\n");

        var dataString = JsonConvert.SerializeObject(machineData);

        Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
        eventMessage.ContentType = MediaTypeNames.Application.Json;
        eventMessage.ContentEncoding = "utf-8";
        //eventMessage.Properties.Add("temperatureAlert", (temeletryData.temperature > 30) ? "true" : "false");
        Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message Telemetry Data: [{dataString}]");

        await client.SendEventAsync(eventMessage);
    }

    #endregion Sending Messages

    #region Receiving Messages

    private static async Task OnC2dMessageReceivedAsync(Message receivedMessage, object _)
    {
        Console.WriteLine($"\t{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
        PrintMessage(receivedMessage);

        await client.CompleteAsync(receivedMessage);
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
        await UpdateTwinValueAsync("deviceErrors", deviceErrors);
        await UpdateTwinValueAsync("productionRate", productionRate);
    }

    public static async Task UpdateTwinValueAsync(string valueName, int value)
    {
        var twin = await client.GetTwinAsync();

        var reportedProperties = new TwinCollection();
        reportedProperties[valueName] = value;

        await client.UpdateReportedPropertiesAsync(reportedProperties);
    }

    public static async Task UpdateTwinValueAsync(string valueName, DateTime value)
    {
        var twin = await client.GetTwinAsync();

        var reportedProperties = new TwinCollection();
        reportedProperties[valueName] = value;

        await client.UpdateReportedPropertiesAsync(reportedProperties);
    }

    private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
    {
        Console.WriteLine($"\tDesired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
        Console.WriteLine("\tSending current time as reported property");
        TwinCollection reportedProperties = new TwinCollection();
        reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;

        await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
    }

    #endregion Device Twin

    #region Direct Methods

    async Task EmergencyStop(string deviceId)
    {
        OPCDevice.EmergencyStop(deviceId);
        await (Task.Delay(1000));
    }
    private async Task<MethodResponse> EmergencyStopHandler(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");

        var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { machineId = default(string) });

        await EmergencyStop(payload.machineId);

        return new MethodResponse(0);
    }

    async Task ResetErrorStatus(string deviceId)
    {
        OPCDevice.ResetErrorStatus(deviceId);
        await (Task.Delay(1000));
    }
    private async Task<MethodResponse> ResetErrorStatusHandler(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");

        var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { machineId = default(string) });

        await ResetErrorStatus(payload.machineId);

        return new MethodResponse(0);
    }

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

    private static async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");

        await Task.Delay(1000);

        return new MethodResponse(0);
    }

    #endregion Direct Methods

    public async Task InitializeHandlers()
    {
        await client.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, client);

        await client.SetMethodHandlerAsync("EmergencyStop", EmergencyStopHandler, client);
        await client.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatusHandler, client);
        await client.SetMethodHandlerAsync("Maintenance", MaintenanceHandler, client);

        await client.SetMethodDefaultHandlerAsync(DefaultServiceHandler, client);

        await client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, client);
    }
}

