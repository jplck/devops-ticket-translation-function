using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;

namespace tickettranslator
{
    public static class WebHook
    {
        [FunctionName("webhook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhook")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Hook has been called.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<Models.DevOpsPayload>(requestBody);

            var descriptionNewValue = data.Resource.Fields["System.Description"].NewValue;
            var descriptionOldValue = data.Resource.Fields["System.Description"].OldValue;

            if (descriptionNewValue != descriptionOldValue)
            {
                var translatedContent = await CallTranslationServiceAsync(descriptionNewValue, log);

                var options = new Models.WorkItemUpdateContainerOptions()
                {
                    Operation = "replace",
                    Path = Environment.GetEnvironmentVariable("FIELD"),
                    Value = translatedContent,
                };

                var workItemContainer = new Models.WorkItemUpdateContainer()
                {
                    Id = data.Resource.WorkItemId,
                    Options = options,
                    PAT = Environment.GetEnvironmentVariable("PAT"),
                    Organization = Environment.GetEnvironmentVariable("ORGANIZATION"),
                    Project = Environment.GetEnvironmentVariable("PROJECT")
                };

                await UpdateWorkItemAsync(workItemContainer);
            }

            return new OkObjectResult(requestBody);
        }

        public static Task<string> CallTranslationServiceAsync(string contentToTranslate,
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

            var payloadArray = new List<Dictionary<string, string>>() { new Dictionary<string, string>() { { "text", contentToTranslate } } };
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
                    var translations = JsonConvert.DeserializeObject<List<Models.Translations>>(response);
                    return Task.FromResult(translations[0].List[0].Text);
                }
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
            }

            return Task.FromResult(string.Empty);
        }

        private static async Task<bool> UpdateWorkItemAsync(Models.WorkItemUpdateContainer container)
        {
            var wid = container.Id;
            var client = PrepareRequest(container);
            var containerSerialized = JsonConvert.SerializeObject(new List<Models.WorkItemUpdateContainerOptions>() { container.Options });

            HttpContent content = new StringContent(containerSerialized, Encoding.UTF8, "application/json-patch+json");
            using (HttpResponseMessage response = 
                await client.PatchAsync($"https://dev.azure.com/{container.Organization}/{container.Project}/_apis/wit/workitems/{wid}?api-version=5.1", content))
                {
                    return response.IsSuccessStatusCode;
                }
        }

        public static HttpClient PrepareRequest(Models.WorkItemUpdateContainer container)
        {
            HttpClient client = new HttpClient();
            var token = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", container.PAT)));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);

            return client;

            //https://docs.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-5.1&viewFallbackFrom=azure-devops
        }
    }
}
