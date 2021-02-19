using System.Threading.Tasks;

namespace Pactify
{
    public interface IPactVerifier
    {
        IPactVerifier Between(string consumer, string provider);
        IPactVerifier UseEndpointTemplate(object templateObject);
        IPactVerifier RetrievedFromFile(string localPath);
        IPactVerifier RetrievedViaHttp(string pactBrokerUri, string consumerVersion, string apiKey = null);
        IPactVerifier PublishPactResultsToPactBroker(string providerVersion, string providerVersionTag, string buildUrl);
        Task VerifyAsync();
        void Verify();
    }
}
