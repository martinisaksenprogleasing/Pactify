using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Pactify.UnitTests
{
    public class GeneralTests
    {
        [Fact]
        public async Task Consumer_Should_Create_APact()
        {
            var options = new PactDefinitionOptions
            {
                IgnoreContractValues = true,
                IgnoreCasing = true
            };

            await PactMaker
                .Create(options)
                .Between("ServiceA", "ServiceB")
                .WithHttpInteraction(cb => cb
                    .Given("There is a parcel with some id")
                    .UponReceiving("A GET Request to retrieve the parcel")
                    .With( request => request
                        .WithMethod(HttpMethod.Get)
                        .WithPath("api/parcels/{Id}"))
                    .WillRespondWith(response => response
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithBody<ParcelReadModel>()))
                .WithHttpInteraction(cb => cb
                    .Given("There is not a parcel with some id")
                    .UponReceiving("A GET Request to retrieve the parcel")
                    .With(request => request
                        .WithMethod(HttpMethod.Get)
                        .WithPath("api/parcels/{Id}"))
                    .WillRespondWith(response => response
                        .WithStatusCode(HttpStatusCode.NotFound)))
                .PublishedViaHttp("http://localhost:9292/","1.7", consumerVersionTag: "SA-100")
                .MakeAsync();
        }

        [Fact]
        public async Task Provider_Should_Meet_Consumers_Expectations()
        {
            await PactVerifier
                .CreateFor<Startup>()
                .ConfigurePactBroker("http://localhost:9292/")
                .UseEndpointTemplate(new ParcelReadModel())
                .Between("ServiceA", "ServiceB")
                .RetrievedViaHttp("1.7")
                .PublishPactResults(true, "1.0.0", "SB-100", "test")
                .VerifyAsync();
        }
    }
}
