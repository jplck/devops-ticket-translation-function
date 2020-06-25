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
using System.Net.Http.Headers;
using System.Text;

namespace tickettranslator
{
    public static class WebHook
    {
        public class DevOpsPayload
        {

            [JsonProperty("message")]
            public DevOpsMessagePayload Message { get; set; }

            [JsonProperty("resource")]
            public DevOpsResourcePayload Resource { get; set; }

        }

        public class DevOpsResourcePayload
        {

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("workItemId")]
            public string WorkItemId { get; set; }

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

        public class WorkItemUpdateContainer
        {
            public WorkItemUpdateContainer(string operation, string path, string value)
            {
                Operation = operation;
                Path = path;
                Value = value;
            }

            [JsonProperty("op")]
            public string Operation { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
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
            await context.CallActivityAsync<bool>("UpdateDevOpsTicketFunction", (payload: input, translatedContent: validationResult));
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
            using (StreamWriter requestWriter = new StreamWriter(webStream, Encoding.ASCII))
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

            return Task.FromResult(string.Empty);
        }

        [FunctionName("UpdateDevOpsTicketFunction")]
        public static async Task<bool> UpdateDevOpsTicketFunction([ActivityTrigger] Tuple<DevOpsPayload, string> contentTuple,
            ILogger log)
        {
            var pat = Environment.GetEnvironmentVariable("PAT");
            var org = Environment.GetEnvironmentVariable("ORGANIZATION");
            var project = Environment.GetEnvironmentVariable("PROJECT");
            var field = Environment.GetEnvironmentVariable("FIELD");
            var wid = contentTuple.Item1.Resource.Id;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Content-Type", "application/json-patch+json");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat))));

                var container = new WorkItemUpdateContainer("replace", field, contentTuple.Item2);
                var containerSerialized = JsonConvert.SerializeObject(container);

                HttpContent content = new StringContent(containerSerialized, Encoding.UTF8, "application/json-patch+json");

                using (HttpResponseMessage response = await client.PatchAsync($"https://dev.azure.com/{org}/{project}/_apis/wit/workitems/{wid}?api-version=5.1", content))
                {
                    return response.IsSuccessStatusCode;
                }
            }
            //https://docs.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-5.1&viewFallbackFrom=azure-devops
        }

    }
}
