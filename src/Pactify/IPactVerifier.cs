using System.Threading.Tasks;

namespace Pactify
{
    public interface IPactVerifier
    {
        IPactVerifier Between(string consumer, string provider);
        IPactVerifier ConfigurePactBroker(string pactBrokerUri, string apiKey = null);
        IPactVerifier UseEndpointTemplate(object templateObject);
        IPactVerifier RetrievedFromFile(string localPath);
        IPactVerifier RetrievedViaHttp(string consumerVersion);
        IPactVerifier PublishPactResults(bool isCi, string providerVersion, string providerVersionTag, string buildUrl);
        Task VerifyAsync();
        void Verify();
    }
}
