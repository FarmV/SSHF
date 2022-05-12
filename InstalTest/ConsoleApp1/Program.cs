using Caliburn.Micro;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using WixSharp;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var project =
            new Project("MyProduct",
            new Dir(@"%ProgramFiles%\My Company\My Product",
            new File(@"Files\Docs\Manual.txt"),
            new File(@"Files\Bin\MyApp.exe")));
            project.GUID = new Guid("846AE666-A54D-47ED-BA2B-9560B710A6A9");

            Compiler.BuildMsi(project);
        }
    }
}
