using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Resources;
using SSHF.Infrastructure.Resources;

namespace SSHF.Infrastructure
{
    internal static class Resource
    {
        private const string _iconPathResource = @"Infrastructure\Resources\SSHF-S16-32-SS-36-128-Max256-512.ico";
        internal static Uri AppIcon = UriNameResourcesHelper.GetResourceUriApp(_iconPathResource);       
    }
}
