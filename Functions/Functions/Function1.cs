using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Resources;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Devices;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using System.Text.RegularExpressions;
using Properties;
using Newtonsoft.Json;

namespace Functions
{
    public class Function1
    {
        [FunctionName("Function1")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            // get info from storage

            string[] strings = File.ReadAllLines($"../../../connectionStringClient.txt");

            string connectionString = strings[0];
            Console.WriteLine(connectionString);


            string connectionStringClient = strings[1];

            QueueClient queueClient = new QueueClient(connectionString, "iot-project");
            await queueClient.CreateIfNotExistsAsync();

            Azure.Storage.Queues.Models.QueueMessage[] messages = await queueClient.ReceiveMessagesAsync(32);

            foreach (var message in messages)
            {
                Console.WriteLine(message.Body);
                string text = message.Body.ToString();
                string deviceId = getParameter(text, "DeviceId");
                int goodCount = Int32.Parse(getParameter(text, "GoodCount"));
                int badCount = Int32.Parse(getParameter(text, "BadCount"));
                double percentage = (goodCount / (goodCount + badCount)) * 100;

                if (percentage < 90)
                {
                    int id = Int32.Parse(Regex.Match(deviceId, @"\d+").Value);

                    var serviceClient = ServiceClient.CreateFromConnectionString(connectionStringClient);
                    var cloudToDeviceMethod = new CloudToDeviceMethod("ReduceProductionRate");
                    cloudToDeviceMethod.SetPayloadJson("{deviceId: " + id + "}");

                    await serviceClient.InvokeDeviceMethodAsync("newDevice", cloudToDeviceMethod);
                }

                await queueClient.DeleteMessageAsync(deviceId, message.PopReceipt);
                await Task.Delay(1000);
            }
        }

        private string getParameter(string text, string parameter)
        {
            int from = text.IndexOf($"{parameter}: ");
            int to = text.IndexOf(",", from);
            from += $"{parameter}: ".Length;

            string result = text.Substring(from, to - from);
            return result;
        }
    }
}

