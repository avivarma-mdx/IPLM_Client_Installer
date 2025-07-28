using System;
using WindowsIplmClientInstaller.Dialogs;
using WixSharp;
using WixSharp.CommonTasks;

namespace WindowsIplmClientInstaller
{
    public class Program
    {
        static void Main()
        {
            var localClientDir = @"C:\dev\PiWindows\iplm-prod-test\*.*";
            var DEFAULT_INSTALL_DIR = @"%ProgramFiles%\perforce\IPLM Client\";

            var project = new ManagedProject("IPLM Client",
                             new Dir(DEFAULT_INSTALL_DIR,
                                 new Files(localClientDir)));

            project.GUID = new Guid("b93ce6c8-c702-4e94-ab5e-7d645efff2fa");

            //custom set of standard UI dialogs
            project.ManagedUI = new ManagedUI();

            project.ManagedUI.InstallDialogs.Add<WelcomeDialog>()
                                            .Add<LicenceDialog>()
                                            .Add<SetupTypeDialog>()
                                            .Add<FeaturesDialog>()
                                            .Add<InstallDirDialog>()
                                            .Add<ProgressDialog>()
                                            .Add<ExitDialog>();

            project.ManagedUI.ModifyDialogs.Add<MaintenanceTypeDialog>()
                                           .Add<FeaturesDialog>()
                                           .Add<ProgressDialog>()
                                           .Add<ExitDialog>();

            //project.SourceBaseDir = "<input dir path>";
            //project.OutDir = "<output dir path>";

            ValidateAssemblyCompatibility();

            project.BuildMsi();
        }

        static void ValidateAssemblyCompatibility()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            if (!assembly.ImageRuntimeVersion.StartsWith("v2."))
            {
                Console.WriteLine("Warning: assembly '{0}' is compiled for {1} runtime, which may not be compatible with the CLR version hosted by MSI. " +
                                  "The incompatibility is particularly possible for the EmbeddedUI scenarios. " +
                                   "The safest way to solve the problem is to compile the assembly for v3.5 Target Framework.",
                                   assembly.GetName().Name, assembly.ImageRuntimeVersion);
            }
        }
    }
}