using Microsoft.Azure.Devices.Common.Exceptions;
using ServiceSdkDemo.Lib;

namespace ServiceSdkDemo.Console
{
    internal static class FeatureSelector
    {

        public static void PrintMenu()
        {
            System.Console.WriteLine(@"
    1 - Send message from Cloud to Device
    2 - Invoke method on Device
    3 - Update Device Twin
    0 - Exit");
        }

        public static async Task Execute(int feature, Lib.IoTHubManager manager)
        {
            switch (feature)
            {
                case 1:
                    {
                        System.Console.WriteLine("\nType your message (confirm with enter):");
                        string messageText = System.Console.ReadLine() ?? string.Empty;

                        System.Console.WriteLine("Type your device ID (confirm with enter):");
                        string deviceId = System.Console.ReadLine() ?? string.Empty;

                        await manager.SendMessage(messageText, deviceId);

                        System.Console.WriteLine("Message sent!");
                    }
                    break;
                case 2:
                    {
                        Method method = 0;

                        System.Console.WriteLine(@"
    Choose Method (confirm with enter):
        1 - Emergency Stop
        2 - ResetErrorStatus
        3 - Maintenance");

                        string machineId = "";

                        switch (Convert.ToInt32(System.Console.ReadLine()))
                        {
                            case 1:
                                method = Method.EmergencyStop;
                                System.Console.WriteLine("\nType your machine ID (confirm with enter):");
                                machineId = System.Console.ReadLine() ?? string.Empty;
                                break;
                            case 2:
                                method = Method.ResetErrorStatus;
                                System.Console.WriteLine("\nType your machine ID (confirm with enter):");
                                machineId = System.Console.ReadLine() ?? string.Empty;
                                break;
                            case 3:
                                method = Method.Maintenance;
                                break;
                            default:
                                break;
                        }
                        System.Console.WriteLine("\nType your device ID (confirm with enter):");
                        string deviceId = System.Console.ReadLine() ?? string.Empty;

                        try
                        {
                            var result = await manager.ExecuteDeviceMethod(method, deviceId, machineId);
                            System.Console.WriteLine($"Method executed with status {result}");
                        }
                        catch (DeviceNotFoundException)
                        {
                            System.Console.WriteLine("Device not connected!");
                        }
                    }
                    break;
                case 3:
                    {
                        System.Console.WriteLine("\nType property name (confirm with enter):");
                        string propertyName = System.Console.ReadLine() ?? string.Empty;

                        System.Console.WriteLine("\nType your device ID (confirm with enter):");
                        string deviceId = System.Console.ReadLine() ?? string.Empty;

                        var random = new Random();
                        await manager.UpdateDesiredTwin(deviceId, propertyName, random.Next());
                    }
                    break;
                default:
                    break;
            }
        }

        internal static int ReadInput()
        {
            var keyPressed = System.Console.ReadKey();
            var isParsed = int.TryParse(keyPressed.KeyChar.ToString(), out int result);
            return isParsed ? result : -1;
        }
    }
}
