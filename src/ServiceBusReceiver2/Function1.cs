using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ServiceBusReceiver2
{
    public class Function1
    {
        [FunctionName("Function1")]
        public void Run([ServiceBusTrigger("sbq", Connection = "Endpoint=sb://taskservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=waHD1WV0rVFqkGBJxFXSm02Tvawmz4qVkyuS78IymCo=")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }
    }
}
