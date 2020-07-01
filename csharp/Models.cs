using Newtonsoft.Json;
using System.Collections.Generic;

namespace tickettranslator
{
    public class Models
    {
        public class DevOpsPayload
        {
            [JsonProperty("resource")]
            public DevOpsResourcePayload Resource { get; set; }

        }

        public class DevOpsResourcePayload
        {

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("workItemId")]
            public int WorkItemId { get; set; }

            [JsonProperty("fields")]
            public Dictionary<string, DevOpsField> Fields { get; set; }

        }

        public class DevOpsField
        {
            [JsonProperty("oldValue")]
            public string OldValue { get; set; }

            [JsonProperty("newValue")]
            public string NewValue { get; set; }
        }

        public class Translations
        {
            [JsonProperty("translations")]
            public List<Translation> List { get; set; }
        }

        public class Translation
        {
            [JsonProperty("text")]
            public string Text { get; set; }
        }

        public class WorkItemUpdateContainerOptions
        {
            [JsonProperty("op")]
            public string Operation { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }

        public class WorkItemUpdateContainer
        {
            public WorkItemUpdateContainerOptions Options { get; set; }

            public int Id { get; set; }

            public string PAT { get; set; }

            public string Organization { get; set; }

            public string Project { get; set; }
        }
    }
}
