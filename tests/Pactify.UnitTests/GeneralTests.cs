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
                .Between("Customer", "Lease")
                .WithHttpInteraction(cb => cb
                    .Given("There is a parcel with some id")
                    .UponReceiving("A GET Request to retrieve the parcel")
                    .With( request => request
                        .WithMethod(HttpMethod.Get)
                        .WithPath("api/parcels/{Id}"))
                    .WillRespondWith(response => response
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithBody<ParcelReadModel>()))
                .PublishedViaHttp("http://localhost:9292/","1.7", consumerVersionTag: "master")
                .MakeAsync();
        }

        [Fact]
        public async Task Provider_Should_Meet_Consumers_Expectations()
        {
            await PactVerifier
                .CreateFor<Startup>()
                .UseEndpointTemplate(new ParcelReadModel())
                .Between("Customer", "Lease")
                .RetrievedViaHttp("http://localhost:9292/pacts/provider/Lease/consumer/Customer/version/1.2")
                .PublishPactResultsToPactBroker("1.0.0", "DEVX-1235", "test")
                .VerifyAsync();
        }
    }
}
