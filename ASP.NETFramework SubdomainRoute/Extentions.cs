using System.Collections.Generic;
using System.Web.Routing;

namespace ASPSubdomain
{
    public  static class Extentions
    {
        /// <summary>
        /// Maps subdomain rote with specified url and subdomain pattern and default route values
        /// </summary>
        public static Route MapSubdomainRoute(this RouteCollection ts, string name, IEnumerable<string> hostnames, string domain, string url, object defaults)
        {
            var temp = new SubdomainRoute(hostnames, domain, url, defaults);
            ts.Add(name, temp);
            return temp;
        }      
    }
}