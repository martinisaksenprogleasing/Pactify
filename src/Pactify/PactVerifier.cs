using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Pactify.Messages;
using Pactify.Retrievers;
using Pactify.Verifiers;

namespace Pactify
{
    public sealed class PactVerifier : IPactVerifier
    {
        private readonly HttpClient _httpClient;
        private object _pathTemplateObject;
        private string _consumer;
        private string _provider;
        private IPactRetriever _retriever;
        private bool _publishResults;
        private PactBroker _pactBroker;
        private string _buildUrl;
        private string _providerVersionTag;
        private string _providerVersion;

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

        public IPactVerifier ConfigurePactBroker(string pactBrokerUri, string apiKey = null)
        {
            _pactBroker = new PactBroker(pactBrokerUri, apiKey) { Provider = _provider, Consumer = _consumer };

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

        public IPactVerifier RetrievedViaHttp(string consumerVersion)
        {
            if (_pactBroker == null) throw new PactifyException($"You must call {nameof(ConfigurePactBroker)} first before using {nameof(RetrievedViaHttp)}.");
            _retriever = new HttpPactRetriever(_pactBroker, consumerVersion);
            return this;
        }

        public IPactVerifier PublishPactResults(bool isCi, string providerVersion, string providerVersionTag, string buildUrl)
        {
            if (_retriever == null && _pactBroker == null) throw new PactifyException($"You must call {nameof(RetrievedViaHttp)} first before using {nameof(PublishPactResults)}.");

            _publishResults = isCi;
            _providerVersion = providerVersion;
            _providerVersionTag = providerVersionTag;
            _buildUrl = buildUrl;

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
                await _pactBroker.PublishVerificationResults(definition.Links.PublishVerificationResultsLink.Href, result.IsSuccessful, _providerVersion, _buildUrl);
                await _pactBroker.TagProviderVersion(_providerVersion, _providerVersionTag);
            }

            if (result.IsSuccessful)
            {
                return;
            }

            throw new PactifyException(string.Join(Environment.NewLine, result.Errors));
        }
    }
}
