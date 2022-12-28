using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System.Text;

namespace ServiceSdkDemo.Lib
{
    public enum Method
    {
        EmergencyStop = 0,
        ResetErrorStatus,
        Maintenance,
    }
    public class IoTHubManager
    {
        private readonly ServiceClient client;
        private readonly RegistryManager registry;

        public IoTHubManager(ServiceClient client, RegistryManager registry)
        {
            this.client = client;
            this.registry = registry;
        }

        public async Task SendMessage(string messageText, string deviceId)
        {
            var messageBody = new { text = messageText };
            var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageBody)));
            message.MessageId = Guid.NewGuid().ToString();
            await client.SendAsync(deviceId, message);
        }

        public async Task<int> ExecuteDeviceMethod(Method method, string deviceId, string machineId = default(string))
        {
            string methodName = "";
            switch (method)
            {
                case Method.EmergencyStop:
                    methodName = "EmergencyStop";
                    break;
                case Method.ResetErrorStatus:
                    methodName = "ResetErrorStatus";
                    break;
                case Method.Maintenance:
                    methodName = "Maintenance";
                    break;
                default:
                    break;
            }
            var C2DMethod = new CloudToDeviceMethod(methodName);

            var methodBody = new { machineId = machineId };
            C2DMethod.SetPayloadJson(JsonConvert.SerializeObject(methodBody));

            var result = await client.InvokeDeviceMethodAsync(deviceId, C2DMethod);

            return result.Status;
        }

        public async Task UpdateDesiredTwin(string deviceId, string propertyName, dynamic propertyValue)
        {
            var twin = await registry.GetTwinAsync(deviceId);
            twin.Properties.Desired[propertyName] = propertyValue;
            await registry.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);
        }
    }
}
