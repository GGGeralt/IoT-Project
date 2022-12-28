using Microsoft.Azure.Devices;
using ServiceSdkDemo.Console;
using ServiceSdkDemo.Lib;

string serviceConnectionString = "HostName=EasyTestHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=k0MGf4s9tgYhrzOOz41Pu5fzGnbX7EIsxIQHaROjmdY=";

using var serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString);
using var registryManager = RegistryManager.CreateFromConnectionString(serviceConnectionString);

var manager = new IoTHubManager(serviceClient, registryManager);

int input = 0;
do
{
    FeatureSelector.PrintMenu();
    input = FeatureSelector.ReadInput();
    await FeatureSelector.Execute(input, manager);
} while (input != 0);