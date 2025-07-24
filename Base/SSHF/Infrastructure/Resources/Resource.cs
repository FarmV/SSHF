using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;

namespace FVH.SSHF.Infrastructure
{
    internal static class Resource
    {
        private const string IconPathResource = @"Infrastructure\Resources\SSHF-S16-32-SS-36-128-Max256-512.ico";
        private const string MahAppsStylesControls = @"pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml";
        private const string MahAppsStylesFonts = @"pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml";
        private const string MahAppsSSHSBaseAccentTheme = @"pack://application:,,,/MahApps.Metro;component/Styles/Themes/Dark.Mauve.xaml";
        private const string OverridingStyles = @"pack://application:,,,/Infrastructure/Resources/OverridingStyles.xaml";

        internal static string[] StandardUriPack =
        [
            MahAppsStylesControls,
            MahAppsStylesFonts,
            MahAppsSSHSBaseAccentTheme,
            OverridingStyles
        ];

        internal static Uri AppIcon = GetResourceUriApp(IconPathResource);
        internal static Uri GetResourceUriApp(string resourcePath) => new Uri(string.Format("pack://application:,,,/{0};component/{1}", Assembly.GetExecutingAssembly().GetName().Name, resourcePath));
      
    }
}
