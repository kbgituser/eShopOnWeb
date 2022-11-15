using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ServiceBusReceiver3
{
    public class Function1
    {
        [FunctionName("Function1")]
        public void Run([ServiceBusTrigger("sbq", Connection = "SBConnection")]string myQueueItem, ILogger log)
        {
            
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            
            MainAsync().GetAwaiter().GetResult();
        }
                
        static string serviceBusConnectionString = Environment.GetEnvironmentVariable("SBConnection");
        const string queueName = "sbq";
        static IQueueClient queueClient = new QueueClient(serviceBusConnectionString, queueName);

         async Task MainAsync()
        {
            Console.WriteLine("Processing of servicebus message is started!");
            RegisterOnMessageHandlerAndReceiveMessages();
            Console.ReadKey();
            await queueClient.CloseAsync();
        }

        void RegisterOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };
            queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        async Task ProcessMessagesAsync(Message message, CancellationToken cancellationToken)
        {

            string Connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string containerName = Environment.GetEnvironmentVariable("ContainerName");
            Stream myBlob = new MemoryStream();            
            var blobClient = new BlobContainerClient(Connection, containerName);
            
            var blob = blobClient.GetBlobClient(DateTime.Now.ToLongTimeString() + ".txt");
            
            using (MemoryStream ms = new MemoryStream(message.Body))
            {
                await blob.UploadAsync(ms);
            };
        }

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine("Some exception occured " + exceptionReceivedEventArgs.Exception);
            Console.WriteLine("Email will be sent");
            var emailSenderUrl = Environment.GetEnvironmentVariable("EmailSenderLogicAppUrl");
            using (HttpClient client = new HttpClient())
            {
                client.PostAsync(emailSenderUrl, null).GetAwaiter().GetResult();
            }
            return Task.CompletedTask;
        }
    }
}
