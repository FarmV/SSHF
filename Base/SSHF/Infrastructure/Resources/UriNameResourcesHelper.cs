using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.Resources
{
    internal static class UriNameResourcesHelper
    {
        internal static Uri GetResourceUriApp(string resourcePath) => new Uri(string.Format("pack://application:,,,/{0};component/{1}", Assembly.GetExecutingAssembly().GetName().Name, resourcePath));
    }
}
