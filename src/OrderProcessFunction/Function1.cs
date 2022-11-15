using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OrderProcessFunction
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(databaseName: "my-database", collectionName: "my-container",
                ConnectionStringSetting = "CosmosDbConnectionString"
            )] IAsyncCollector<dynamic> documentsOut,
            ILogger log            
            )
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];
            string shippingadress = req.Query["shippingadress"];
            string listofitems = req.Query["listofitems"];
            string finalprice = req.Query["finalprice"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

                // Add a JSON document to the output container.
                await documentsOut.AddAsync(new
                {
                    
                    shippingadress = shippingadress,
                    listofitems = listofitems,
                    finalprice= finalprice,
                });
            

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
