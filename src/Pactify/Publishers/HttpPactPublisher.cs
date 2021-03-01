using System.Threading.Tasks;
using Pactify.Definitions;

namespace Pactify.Publishers
{
    internal sealed class HttpPactPublisher : IPactPublisher
    {
        private readonly PactBroker _pactBroker;
        private readonly string _consumerVersion;
        private readonly string _consumerVersionTag;

        public HttpPactPublisher(PactBroker pactBroker, string consumerVersion, string consumerVersionTag)
        {
            _pactBroker = pactBroker;
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
