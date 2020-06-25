using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;

namespace tickettranslator
{
    public static class WebHook
    {
        public class DevOpsPayload
        {

            [JsonProperty("message")]
            public DevOpsMessagePayload Message { get; set; }

        }

        public class DevOpsMessagePayload
        {

            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("html")]
            public string HTML { get; set; }

            [JsonProperty("markdown")]
            public string Markdown { get; set; }

        }

        [FunctionName("webhook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhook")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log)
        {
            log.LogInformation("Hook has been called.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<DevOpsPayload>(requestBody);

            await client.StartNewAsync("ModelOrchestrator", data);

            return new OkObjectResult(requestBody);
        }

        [FunctionName("ModelOrchestrator")]
        public static async Task ModelOrchestratorAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var input = context.GetInput<DevOpsPayload>();

            var validationResult = await context.CallActivityAsync<string>("CallTranslationServiceFunction", input);
            await context.CallActivityAsync<bool>("UpdateDevOpsTicketFunction", validationResult);
        }

        [FunctionName("CallTranslationServiceFunction")]
        public static Task<string> CallTranslationServiceFunction([ActivityTrigger] DevOpsPayload payload,
            ILogger log)
        {
            var apimKey = Environment.GetEnvironmentVariable("ENDPOINT_SECRET");
            var region = Environment.GetEnvironmentVariable("ENDPOINT_REGION");
            var endpoint = Environment.GetEnvironmentVariable("TRANSLATION_ENDPOINT");
            var apiVersion = "3.0";
            var targetLang = "de";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{endpoint}/translate?api-version={apiVersion}&to={targetLang}");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add($"Ocp-Apim-Subscription-Key:{apimKey}");
            request.Headers.Add($"Ocp-Apim-Subscription-Region:{region}");
            request.Headers.Add("charset: UTF-8");

            var payloadArray = new List<DevOpsMessagePayload>();
            payloadArray.Add(payload.Message);
            var content = JsonConvert.SerializeObject(payloadArray);

            using (Stream webStream = request.GetRequestStream())
            using (StreamWriter requestWriter = new StreamWriter(webStream, System.Text.Encoding.ASCII))
            {
                requestWriter.Write(content);
            }

            try
            {
                WebResponse webResponse = request.GetResponse();
                using (Stream webStream = webResponse.GetResponseStream() ?? Stream.Null)
                using (StreamReader responseReader = new StreamReader(webStream))
                {
                    string response = responseReader.ReadToEnd();
                    return Task.FromResult(response);
                }
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
            }

            return Task.FromResult(String.Empty);
        }

        [FunctionName("UpdateDevOpsTicketFunction")]
        public static Task<bool> UpdateDevOpsTicketFunction([ActivityTrigger] string translatedContent,
            ILogger log)
        {
            //https://docs.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-5.1&viewFallbackFrom=azure-devops
            return Task.FromResult(true);
        }

    }
}
