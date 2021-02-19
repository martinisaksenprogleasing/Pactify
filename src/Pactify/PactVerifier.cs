using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Pactify.Messages;
using Pactify.Retrievers;
using Pactify.Serialization;
using Pactify.Verifiers;

namespace Pactify
{
    public sealed class PactVerifier : IPactVerifier
    {
        private const string RequestHeader = "Pact-Requester";

        private readonly HttpClient _httpClient;
        private object _pathTemplateObject;
        private string _consumer;
        private string _provider;
        private IPactRetriever _retriever;
        private bool _publishResults;
        private string _providerVersion;
        private string _providerVersionTag;
        private string _buildUrl;
        private string _apiKey;
        private Uri _pactBrokerBaseUri;
        private readonly HttpClient _pactBrokerHttpClient = new HttpClient();

        private PactVerifier(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public static IPactVerifier Create(HttpClient httpClient = null)
            => new PactVerifier(httpClient ?? new HttpClient());

        public static IPactVerifier CreateFor<TStartup>() where TStartup : class
        {
            var testServer = new TestServer(new WebHostBuilder().UseStartup<TStartup>());
            var httpClient = testServer.CreateClient();
            return Create(httpClient);
        }

        public IPactVerifier Between(string consumer, string provider)
        {
            _consumer = consumer;
            _provider = provider;

            return this;
        }

        public IPactVerifier UseEndpointTemplate(object templateObject)
        {
            _pathTemplateObject = templateObject;
            return this;
        }

        public IPactVerifier RetrievedFromFile(string localPath)
        {
            if (string.IsNullOrEmpty(_consumer) || string.IsNullOrEmpty(_provider))
            {
                throw new PactifyException(ErrorMessages.ConsumerProviderMustBeDefined);
            }

            _retriever = new FilePactRetriever(_consumer, _provider, localPath);
            return this;
        }

        public IPactVerifier RetrievedViaHttp(string url, string apiKey = null)
        {
            _retriever = new HttpPactRetriever(url, apiKey);
            _pactBrokerBaseUri = new Uri(new Uri(url).GetLeftPart(UriPartial.Authority));
            return this;
        }

        public IPactVerifier PublishPactResultsToPactBroker(string providerVersion, string providerVersionTag, string buildUrl, string apiKey = null)
        {
            _publishResults = true;
            _providerVersion = providerVersion;
            _providerVersionTag = providerVersionTag;
            _buildUrl = buildUrl;
            _apiKey = apiKey;
            return this;
        }

        public void Verify()
            => VerifyAsync().GetAwaiter().GetResult();

        public async Task VerifyAsync()
        {
            var definition = await _retriever.RetrieveAsync();
            var verifier = new HttpInteractionVerifier(_httpClient);

            var resultTasks = definition.Interactions
                .Select(c => verifier.VerifyAsync(c, definition.Options, _pathTemplateObject))
                .ToList();

            await Task.WhenAll(resultTasks);

            var result = resultTasks
                .Select(t => t.Result)
                .Aggregate((c, next) => c & next);

            if (_publishResults)
            {
                await PublishVerificationResults(definition.Links.PublishVerificationResultsLink.Href, result.IsSuccessful);
                await TagProviderVersion();
            }

            if (result.IsSuccessful)
            {
                return;
            }

            throw new PactifyException(string.Join(Environment.NewLine, result.Errors));
        }

        private async Task PublishVerificationResults(string publishVerificationResultsLink, bool isSuccessful)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(publishVerificationResultsLink),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(new PactVerificationResultRequest
                    {
                        Success = isSuccessful,
                        ProviderApplicationVersion = _providerVersion,
                        BuildUrl = _buildUrl
                    }, PactifySerialization.Settings),
                    Encoding.UTF8,
                    "application/json")
            };

            if (!(_apiKey is null))
            {
                request.Headers.Add(RequestHeader, _apiKey);
            }

            await _pactBrokerHttpClient.SendAsync(request);

        }

        private async Task TagProviderVersion()
        {
            var request = new HttpRequestMessage()
            {
                //RequestUri = new Uri(_pactBrokerBaseUri, $"pacticipants/Lease/versions/{_providerVersion}/tags/{_providerVersionTag}"),
                RequestUri = new Uri(_pactBrokerBaseUri, $"pacticipants/{_provider}/versions/{_providerVersion}/tags/{_providerVersionTag}"),
                Method = HttpMethod.Put,
                Content = new StringContent("", Encoding.UTF8, "application/json")
            };

            if (!(_apiKey is null))
            {
                request.Headers.Add(RequestHeader, _apiKey);
            }

            await _pactBrokerHttpClient.SendAsync(request);

        }
    }
}
