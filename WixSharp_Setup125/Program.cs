using System;
using System.Windows.Forms;

using WixSharp;
using WixSharp.UI.WPF;

namespace WixSharp_Setup125
{
    internal class Program
    {
        static void Main()
        {
            var project = new ManagedProject("MyProduct",
                              new Dir(@"%ProgramFiles%\My Company\My Product",
                                  new File("Program.cs")));

            project.GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b");

            // project.ManagedUI = ManagedUI.DefaultWpf; // all stock UI dialogs

            //custom set of UI WPF dialogs
            project.ManagedUI = new ManagedUI();

            project.ManagedUI.InstallDialogs.Add<WixSharp_Setup125.WelcomeDialog>()
                                            .Add<WixSharp_Setup125.LicenceDialog>()
                                            .Add<WixSharp_Setup125.FeaturesDialog>()
                                            .Add<WixSharp_Setup125.InstallDirDialog>()
                                            .Add<WixSharp_Setup125.ProgressDialog>()
                                            .Add<WixSharp_Setup125.ExitDialog>();

            project.ManagedUI.ModifyDialogs.Add<WixSharp_Setup125.MaintenanceTypeDialog>()
                                           .Add<WixSharp_Setup125.FeaturesDialog>()
                                           .Add<WixSharp_Setup125.ProgressDialog>()
                                           .Add<WixSharp_Setup125.ExitDialog>();

            //project.SourceBaseDir = "<input dir path>";
            //project.OutDir = "<output dir path>";

            project.BuildMsi();
        }
    }
}