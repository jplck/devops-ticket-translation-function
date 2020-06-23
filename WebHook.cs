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
            public double Text { get; set; }

            [JsonProperty("html")]
            public double HTML { get; set; }

            [JsonProperty("markdown")]
            public double Markdown { get; set; }

        }

        [FunctionName("webhook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhook")] HttpRequest req,
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
            return Task.FromResult("translated content");
        }

        [FunctionName("UpdateDevOpsTicketFunction")]
        public static Task<bool> UpdateDevOpsTicketFunction([ActivityTrigger] string translatedContent,
            ILogger log)
        {
            return Task.FromResult(true);
        }

    }
}
