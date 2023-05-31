using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure
{
    internal static class UriHelper
    {
        public static Uri GetResourceUriApp(string resourcePath) => new Uri(string.Format("pack://application:,,,/{0};component/{1}", Assembly.GetExecutingAssembly().GetName().Name, resourcePath));
    }
}
