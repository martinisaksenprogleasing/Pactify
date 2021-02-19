using System.Threading.Tasks;
using Newtonsoft.Json;
using Pactify.Definitions;

namespace Pactify.Retrievers
{
    internal sealed class HttpPactRetriever : IPactRetriever
    {
        private readonly PactBroker _pactBroker;
        private readonly string _version;

        public HttpPactRetriever(PactBroker pactBroker, string version)
        {
            _pactBroker = pactBroker;
            _version = version;
        }

        public async Task<PactDefinition> RetrieveAsync()
        {
            var response = await _pactBroker.GetPact(_version);
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<PactDefinition>(json);
        }
    }
}
