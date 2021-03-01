using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pactify.Definitions;
using Pactify.Serialization;

namespace Pactify
{
    internal class PactBroker
    {
        private const string RequestHeader = "Authorization";
        private const string JsonContentType = "application/json";

        private readonly HttpClient _pactBrokerHttpClient = new HttpClient();
        private readonly ILogger _logger;

        public string Provider { get; set; }
        public string Consumer { get; set; }

        public PactBroker(string baseUri, string apiKey = null, ILogger logger = null)
        {
            if (!Uri.TryCreate(baseUri, UriKind.Absolute, out var validBaseUri))
            {
                throw new UriFormatException($"{baseUri} is not a valid Uri");
            }

            _pactBrokerHttpClient.BaseAddress = validBaseUri;

            if (!(apiKey is null))
            {
                _pactBrokerHttpClient.DefaultRequestHeaders.Add(RequestHeader, $"Bearer {apiKey}");
            }

            _logger = logger;
        }

        internal async Task PublishPact(PactDefinition definition, string version)
        {
            var json = JsonConvert.SerializeObject(definition, PactifySerialization.Settings);

            var content = new StringContent(json, Encoding.UTF8, JsonContentType);
            var requestUri = $"pacts/provider/{Provider}/consumer/{Consumer}/version/{version}";

            var response = await _pactBrokerHttpClient.PutAsync(requestUri, content);

            _logger?.Log(LogLevel.Debug, $"Request to {requestUri} returned the following status code: {response.StatusCode}");
        }

        internal async Task TagPacticipantVersion(string pacticipant, string version, string tag)
        {
            var content = new StringContent("", Encoding.UTF8, JsonContentType);
            var requestUri = $"pacticipants/{pacticipant}/versions/{version}/tags/{tag}";

            var response = await _pactBrokerHttpClient.PutAsync(requestUri, content);

            _logger?.Log(LogLevel.Debug, $"Request to {requestUri} returned the following status code: {response.StatusCode}");
        }

        internal async Task<HttpResponseMessage> GetPact(string desiredVersion)
        {
            var version = desiredVersion == "latest" ? desiredVersion : $"versions/{desiredVersion}";

            var requestUri = $"pacts/provider/{Provider}/consumer/{Consumer}/{version}";

            var response = await _pactBrokerHttpClient.GetAsync(requestUri);

            _logger?.Log(LogLevel.Debug, $"Request to {requestUri} returned the following status code: {response.StatusCode}");

            return response;
        }

        internal async Task PublishVerificationResults(string publishVerificationResultsLink, bool isSuccessful, string providerVersion, string buildUrl)
        {
            var requestUri = publishVerificationResultsLink;

            var content = new StringContent(JsonConvert.SerializeObject(new PactVerificationResultRequest
                    {
                        Success = isSuccessful,
                        ProviderApplicationVersion = providerVersion,
                        BuildUrl = buildUrl
                    }, PactifySerialization.Settings),
                    Encoding.UTF8,
                    JsonContentType);

            var response = await _pactBrokerHttpClient.PostAsync(publishVerificationResultsLink, content);

            _logger?.Log(LogLevel.Debug, $"Request to {requestUri} returned the following status code: {response.StatusCode}");
        }

        internal async Task TagProviderVersion(string providerVersion, string providerVersionTag)
        {
            var requestUri = $"pacticipants/{Provider}/versions/{providerVersion}/tags/{providerVersionTag}";

            var response = await _pactBrokerHttpClient.PutAsync(requestUri, new StringContent("", Encoding.UTF8, JsonContentType));

            _logger?.Log(LogLevel.Debug, $"Request to {requestUri} returned the following status code: {response.StatusCode}");
        }
    }
}
