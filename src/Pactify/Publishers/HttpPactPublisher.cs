using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pactify.Definitions;
using Pactify.Serialization;

namespace Pactify.Publishers
{
    internal sealed class HttpPactPublisher : IPactPublisher
    {
        private readonly PactBroker _pactBroker;
        private readonly string _consumerVersion;
        private readonly string _consumerVersionTag;

        public HttpPactPublisher(string pactBrokerUri, string consumerVersion, string apiKey, string consumerVersionTag)
        {
            _pactBroker = new PactBroker(pactBrokerUri, apiKey);
            _consumerVersion = consumerVersion;
            _consumerVersionTag = consumerVersionTag;
        }

        public async Task PublishAsync(PactDefinition definition)
        {
            await _pactBroker.PublishPact(definition, _consumerVersion);

            if (_consumerVersionTag != null)
            {
                await _pactBroker.TagPacticipantVersion(definition.Consumer.Name, _consumerVersion, _consumerVersionTag);
            }
        }
    }
}
