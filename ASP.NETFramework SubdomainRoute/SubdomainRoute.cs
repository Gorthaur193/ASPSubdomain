using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;


namespace ASPSubdomain
{
    /// <summary>
    /// Subdomain as parameter route. Use {*word} in subdomain as catchable parameter at the beginning of sequence. Inherited from Route.
    /// </summary>
    public class SubdomainRoute : Route
    {      
        private enum SubdomainType
        {
            Static,
            Dynamic
        }
        private Regex pattern = new Regex(@"\{(?<name>\w*)\}");

        private string _subdomainPattern;
        private List<(SubdomainType Type, string Value)> parsedSequence;
        private bool isCatchable = false;

        public IEnumerable<string> Hostnames { get; set; }
        public string SubdomainPattern
        {
            get => _subdomainPattern;
            set
            {
                var unparsedPatternSequence = value.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (isCatchable = pattern.IsMatch(unparsedPatternSequence[0]) && pattern.Match(unparsedPatternSequence[0]).Value.StartsWith("*"))
                    unparsedPatternSequence[0] = unparsedPatternSequence[0].Remove(1, 1);
                var parseTry = new List<(SubdomainType Type, string Value)>();
                foreach (var subdomain in unparsedPatternSequence.Reverse())
                {
                    if (pattern.IsMatch(subdomain) && pattern.Match(subdomain).Value.StartsWith("*"))
                        throw new ArgumentException("You can not catch elements inside sequence. Only at the beginning.");
                    parseTry.Add((pattern.IsMatch(subdomain) ? (SubdomainType.Dynamic, pattern.Match(subdomain).Groups["name"].Value) : (SubdomainType.Static, subdomain)));
                }

                parsedSequence = parseTry;
                //parsedSequence = unparsedPatternSequence.Reverse().Select(x => (patterner.IsMatch(x) ? (SubdomainType.Dynamic, x.Substring(1, x.Length - 2)) : (SubdomainType.Static, x)));
                _subdomainPattern = value;
            }
        }

        public SubdomainRoute(IEnumerable<string> hostnames, string domain, string url, object defaults) : this(hostnames, domain, url, defaults, new MvcRouteHandler())
        {
        }
        public SubdomainRoute(IEnumerable<string> hostnames, string domain, string url, object defaults, IRouteHandler routeHandler) : this(hostnames, domain, url, new RouteValueDictionary(defaults), routeHandler)
        {
        }
        public SubdomainRoute(IEnumerable<string> hostnames, string subdomainPattern, string url, RouteValueDictionary defaults, IRouteHandler routeHandler) : this(subdomainPattern, url, defaults, routeHandler) =>
            Hostnames = hostnames;
        public SubdomainRoute(string subdomainPattern, string url, RouteValueDictionary defaults, IRouteHandler routeHandler) : base(url, defaults, routeHandler)  =>
            SubdomainPattern = subdomainPattern;
       
        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            if (parsedSequence == null || Hostnames == null)
                throw new ArgumentNullException("SubdomainPattern or Hostnames", "One of inner parameters is missing.");

            var routeData = base.GetRouteData(httpContext);
            if (routeData == null)
                return null;

            var host = httpContext.Request.Url.Host;
            var subdomains = host.Replace(HostFromSequence(host), "").Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (subdomains[0] == "www")
                subdomains.RemoveAt(0);
            if (!isCatchable && subdomains.Count != parsedSequence.Count())
                return null;
            if (isCatchable)
            {
                string[] catchPart = new string[0];
                subdomains.CopyTo(0, catchPart, 0, subdomains.Count - parsedSequence.Count + 1);
                subdomains.RemoveRange(0, subdomains.Count - parsedSequence.Count);
                subdomains[0] = ConcatArrayToUnitedString(catchPart);
            }
            subdomains.Reverse();                          

            for (int i = 0; i < parsedSequence.Count; i++)
                switch (parsedSequence[i].Type)
                {
                    case SubdomainType.Static:
                        if (parsedSequence[i].Value != subdomains[i])
                            return null;
                        break;
                    case SubdomainType.Dynamic:
                        routeData.Values.Add(parsedSequence[i].Value, subdomains[i]);
                        break;
                    default:
                        break;
                }              
            return routeData;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {                                                                                                                                 
            /// Represents information about the route and virtual path that are the result of generating a URL with the ASP.NET routing framework.
            //var virtualPath = base.GetVirtualPath(requestContext, values);
            //TODO MY CODE
            return null;
            //return virtualPath;           
        }

        private string HostFromSequence(string domainSequence) =>
            Hostnames.FirstOrDefault(x => domainSequence.Contains(x));
        private string ConcatArrayToUnitedString(string[] array)
        {
            string line = "";
            foreach (var item in array)
                line += $"{item}.";
            return line.Remove(line.Length - 1, 1);
        }
    }
}