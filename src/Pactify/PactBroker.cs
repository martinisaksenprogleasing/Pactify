using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pactify.Definitions;
using Pactify.Serialization;

namespace Pactify
{
    internal class PactBroker
    {
        private const string RequestHeader = "Pact-Requester";
        private const string JsonContentType = "application/json";

        private static readonly HttpClient PactBrokerHttpClient = new HttpClient();

        public PactBroker(string baseUri, string apiKey = null)
        {
            if (!Uri.TryCreate(baseUri, UriKind.Absolute, out var validBaseUri))
            {
                throw new UriFormatException($"{baseUri} is not a valid Uri");
            }

            PactBrokerHttpClient.BaseAddress = validBaseUri;

            if (!(apiKey is null))
            {
                PactBrokerHttpClient.DefaultRequestHeaders.Add(RequestHeader, apiKey);
            }
        }

        internal async Task PublishPact(PactDefinition definition, string version)
        {
            var json = JsonConvert.SerializeObject(definition, PactifySerialization.Settings);

            var content = new StringContent(json, Encoding.UTF8, JsonContentType);

            await PactBrokerHttpClient.PutAsync(
                $"pacts/provider/{definition.Provider.Name}/consumer/{definition.Consumer.Name}/version/{version}",
                content);
        }

        internal async Task TagPacticipantVersion(string pacticipant, string version, string tag)
        {
            var content = new StringContent("", Encoding.UTF8, JsonContentType);

            await PactBrokerHttpClient.PutAsync($"pacticipants/{pacticipant}/versions/{version}/tags/{tag}", content);
        }
    }
}
