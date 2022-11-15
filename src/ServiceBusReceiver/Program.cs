// See https://aka.ms/new-console-template for more information

using System.Text;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace ServiceBusReceiver // Note: actual namespace depends on the project name.
{
    
    class Program
    {
        const string serviceBusConnectionString = "Endpoint=sb://taskservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=waHD1WV0rVFqkGBJxFXSm02Tvawmz4qVkyuS78IymCo=";
        const string queueName = "sbq";
        static IQueueClient queueClient = new QueueClient(serviceBusConnectionString, queueName);

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();                
        }

        static async Task MainAsync()
        {
            Console.WriteLine("Hello World!");
            RegisterOnMessageHandlerAndReceiveMessages();
            Console.ReadKey();
            await queueClient.CloseAsync();
        }

        static void RegisterOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };
            queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        static async Task ProcessMessagesAsync(Message message, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Message recieved: {message.Body}");
            HttpClient client = new HttpClient();

            var json = JsonConvert.SerializeObject(message.Body);
            var reqdata = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://sbtaskfileupapp.azurewebsites.net/api/FileUpload", reqdata);
            
            var responseString = await response.Content.ReadAsStringAsync();
            await queueClient.CompleteAsync(message.SystemProperties.LockToken);
            Console.WriteLine($"sent to blob: {message.Body}");
        }

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine("Some exception occured " + exceptionReceivedEventArgs.Exception);
            return Task.CompletedTask;   
        }
    }

    

}





