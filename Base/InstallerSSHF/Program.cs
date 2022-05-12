using System;
using System.Windows.Forms;

using WixSharp;
using WixSharp.UI.WPF;

namespace WixSharp_Setup1
{
    internal class Program
    {
        static void Main()
        {
            var project = new ManagedProject("SSHF Installer", new Dir(@"%ProgramFiles%\My Company\My Product", Files.FromBuildDir("")));

            project.GUID = new Guid("524EE9BC-83E6-4F0D-917E-6A4C69A4DDD4");

            project.ManagedUI = ManagedUI.DefaultWpf; // all stock UI dialogs

            //custom set of UI WPF dialogs
            project.ManagedUI = new ManagedUI();

            project.ManagedUI.InstallDialogs.Add<WixSharp_Setup1.WelcomeDialog>()
                                            .Add<WixSharp_Setup1.LicenceDialog>()
                                            .Add<WixSharp_Setup1.FeaturesDialog>()
                                            .Add<WixSharp_Setup1.InstallDirDialog>()
                                            .Add<WixSharp_Setup1.ProgressDialog>()
                                            .Add<WixSharp_Setup1.ExitDialog>();

            project.ManagedUI.ModifyDialogs.Add<WixSharp_Setup1.MaintenanceTypeDialog>()
                                           .Add<WixSharp_Setup1.FeaturesDialog>()
                                           .Add<WixSharp_Setup1.ProgressDialog>()
                                           .Add<WixSharp_Setup1.ExitDialog>();

            project.SourceBaseDir = @"C:\Work\Studio\SSHF\Base\SSHF\bin\Release\net6.0-windows10.0.19041.0\publish\win-x64";
            project.OutDir = @"D:\Новая папка\";

            project.BuildMsi();
        }
    }
}