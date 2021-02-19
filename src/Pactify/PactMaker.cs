using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pactify.Builders;
using Pactify.Builders.Http;
using Pactify.Definitions;
using Pactify.Definitions.Http;
using Pactify.Messages;
using Pactify.Publishers;

namespace Pactify
{
    public class PactMaker : IPactMaker
    {
        private readonly PactDefinition _pactDefinition;
        private readonly ILogger _logger;
        private IPactPublisher _publisher;
        private string _provider;
        private string _consumer;
        private PactBroker _pactBroker;

        private PactMaker(PactDefinitionOptions options, ILogger logger = null)
        {
            _pactDefinition = new PactDefinition
            {
                Options = options
            };
            _logger = logger;
        }

        public static IPactMaker Create(PactDefinitionOptions options = null, ILogger logger = null)
            => new PactMaker(options ?? new PactDefinitionOptions(), logger);

        public IPactMaker Between(string consumer, string provider)
        {
            if (string.IsNullOrEmpty(consumer) || string.IsNullOrEmpty(provider))
            {
                throw new PactifyException(ErrorMessages.ConsumerProviderMustBeDefined);
            }

            _pactDefinition.Consumer = new ConsumerDefinition { Name = consumer };
            _pactDefinition.Provider = new ProviderDefinition { Name = provider };

            _provider = provider;
            _consumer = consumer;

            return this;
        }

        public IPactMaker WithHttpInteraction(Action<IHttpInteractionBuilder> buildCoupling)
        {
            if (buildCoupling is null)
            {
                throw new PactifyException(ErrorMessages.InteractionMustBeDefined);
            }

            var builder = new HttpInteractionBuilder();
            buildCoupling(builder);

            var accessor = (IBuildingAccessor<HttpInteractionDefinition>)builder;
            var definition = accessor.Build();
            _pactDefinition.Interactions.Add(definition);

            return this;
        }

        public IPactMaker PublishedAsFile(string localPath)
        {
            _publisher = new FilePactPublisher(localPath);
            return this;
        }

        public IPactMaker PublishedViaHttp(string pactBrokerUri, string consumerVersion, string apiKey = null, string consumerVersionTag = null)
        {
            _pactBroker = new PactBroker(pactBrokerUri, apiKey, _logger)
            {
                Consumer = _consumer,
                Provider = _provider
            };
            _publisher = new HttpPactPublisher(_pactBroker, consumerVersion, consumerVersionTag);
            return this;
        }

        public void Make()
            => MakeAsync().GetAwaiter().GetResult();

        public async Task MakeAsync()
        {
            if (_publisher is null)
            {
                throw new PactifyException(ErrorMessages.PublisherNotSetUp);
            }
            await _publisher.PublishAsync(_pactDefinition);
        }
    }
}
