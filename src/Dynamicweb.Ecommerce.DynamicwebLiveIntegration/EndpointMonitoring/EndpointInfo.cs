using Dynamicweb.DataIntegration.EndpointManagement;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.EndpointMonitoring
{
    internal class EndpointInfo
    {
        internal string WebServiceUrl { get; private set; }
        internal Endpoint Endpoint { get; private set; }

        internal string Id { get; private set; }

        internal string GetUrl()
        {
            return Endpoint != null ? Endpoint.Url : WebServiceUrl;
        }

        internal EndpointInfo(string webServiceUrl)
        {
            WebServiceUrl = webServiceUrl;
            Id = webServiceUrl;
        }

        internal EndpointInfo(Endpoint endpoint)
        {
            Endpoint = endpoint;
            Id = endpoint.Id.ToString();
        }        
    }
}
