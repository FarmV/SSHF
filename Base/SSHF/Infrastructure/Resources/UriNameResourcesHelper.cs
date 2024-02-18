using System;
using System.Reflection;

namespace FVH.SSHF.Infrastructure.Resources
{
    internal static class UriNameResourcesHelper
    {
        internal static Uri GetResourceUriApp(string resourcePath) => new Uri(string.Format("pack://application:,,,/{0};component/{1}", Assembly.GetExecutingAssembly().GetName().Name, resourcePath));
    }
}
