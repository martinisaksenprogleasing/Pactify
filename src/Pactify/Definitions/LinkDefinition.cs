using Newtonsoft.Json;
using Pactify.Definitions.LinkDataDefinitions;

namespace Pactify.Definitions
{
    internal sealed class LinkDefinition
    {
        [JsonProperty(PropertyName = "pb:tag-version")]
        public TagVersion TagVersion { get; set; }

        [JsonProperty(PropertyName = "pb:publish-verification-results")]
        public PublishVerificationResults PublishVerificationResultsLink { get; set; }

        [JsonProperty(PropertyName = "pb:pact-version")]
        public PactVersion PactVersion { get; set; }
    }
}
