using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using System.Text;

namespace UploadApp
{
    public static class FileUpload
    {
        [FunctionName("FileUpload")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
                        
            string Connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string containerName = Environment.GetEnvironmentVariable("ContainerName");
            Stream myBlob = new MemoryStream();
            //var file = req.Form.Files["File"];
            //myBlob = file.OpenReadStream();            
            var blobClient = new BlobContainerClient(Connection, containerName);
            //var blob = blobClient.GetBlobClient(file.FileName);
            var blob = blobClient.GetBlobClient(DateTime.Now.ToLongTimeString()+".txt");
            //await blob.UploadAsync(myBlob);

            //string id = req.Query["id"];
            //string quantity = req.Query["quantity"];

            // read the contents of the posted data into a string
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // use Json.NET to deserialize the posted JSON into a C# dynamic object
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            //string value = data;
            string value = Convert.ToString(data);
            // data validation omitted for demo purposes


            //var ordertest = new { id = 1, quantity = 10 };
            //string order = "{'id':'"+ id+"', 'quantity':'"+  quantity+"'}";
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            {
                await blob.UploadAsync(ms);
            }
                ;                       

            return new OkObjectResult("file uploaded successfylly");

            
        }
    }
}
