using System.Collections.Generic;
using Newtonsoft.Json;
using Pactify.Definitions.Http;

namespace Pactify.Definitions
{
    internal sealed class PactDefinition
    {
        public ConsumerDefinition Consumer { get; set; }
        public ProviderDefinition Provider { get; set; }
        public List<HttpInteractionDefinition> Interactions { get; set; } = new List<HttpInteractionDefinition>();
        public PactDefinitionOptions Options { get; set; }

        [JsonProperty(PropertyName = "_links")]
        public LinkDefinition Links { get; set; }
    }
}
