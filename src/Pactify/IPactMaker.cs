using System;
using System.Threading.Tasks;

namespace Pactify
{
    public interface IPactMaker
    {
        IPactMaker Between(string consumer, string provider);
        IPactMaker WithHttpInteraction(Action<IHttpInteractionBuilder> buildCoupling);
        IPactMaker PublishedAsFile(string localPath);
        IPactMaker PublishedViaHttp(string pactBrokerUri, string consumerVersion, string apiKey = null, string consumerVersionTag = null);
        void Make();
        Task MakeAsync();
    }
}
