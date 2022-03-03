﻿using System.Reflection;
using System.Runtime.Loader;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

public class Program
{
    static Action<string> Write = Console.WriteLine;


    public enum PhoneService
    {
        None = 0,
        LandLine = 1,
        Cell = 2,
        Fax = 4,
        Internet = 8,
        Other = 16
    }


    static Task task()
    {
        Thread.Sleep(4000);
        return Task.CompletedTask;

    }
    public static void Main(string[] args)
    {

        Task? cc = task().WaitAsync(new TimeSpan(0,0,0,0,2));
        TaskStatus bb = cc.Status;
        

        Console.WriteLine("hello");



        //var household1 = PhoneService.LandLine | PhoneService.Cell | PhoneService.Internet;
        //var household2 = PhoneService.None;
        //var household3 = PhoneService.Cell | PhoneService.Internet;

        //PhoneService[] households = { household1, household2, household3 };


        //// Which households have cell phone service?
        //for (int ctr = 0;ctr < households.Length;ctr++)
        //    Console.WriteLine("Household {0} has cell phone service: {1}",
        //                      ctr + 1,
        //                      (households[ctr] & PhoneService.Cell) == PhoneService.Cell ?
        //                         "Yes" : "No");







        //Write("Let's compile!");

        //string codeToCompile = @"
        //    using System;
        //    namespace RoslynCompileSample
        //    {
        //        public class Writer
        //        {
        //            public void Write(string message)
        //            {
        //                Console.WriteLine($""you said '{message}!'"");
        //            }
        //        }
        //    }";

        //Write("Parsing the code into the SyntaxTree");
        //SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeToCompile);

        //string assemblyName = Path.GetRandomFileName();
        //var refPaths = new[] {
        //        typeof(System.Object).GetTypeInfo().Assembly.Location,
        //        typeof(Console).GetTypeInfo().Assembly.Location,
        //        Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")
        //    };
        //MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

        //Write("Adding the following references");
        //foreach (var r in refPaths)
        //    Write(r);

        //Write("Compiling ...");
        //CSharpCompilation compilation = CSharpCompilation.Create(
        //    assemblyName,
        //    syntaxTrees: new[] { syntaxTree },
        //    references: references,
        //    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        //using (var ms = new MemoryStream())
        //{
        //    EmitResult result = compilation.Emit(ms);

        //    if (!result.Success)
        //    {
        //        Write("Compilation failed!");
        //        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
        //            diagnostic.IsWarningAsError ||
        //            diagnostic.Severity == DiagnosticSeverity.Error);

        //        foreach (Diagnostic diagnostic in failures)
        //        {
        //            Console.Error.WriteLine("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
        //        }
        //    }
        //    else
        //    {
        //        Write("Compilation successful! Now instantiating and executing the code ...");
        //        ms.Seek(0, SeekOrigin.Begin);

        //        Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
        //        var type = assembly.GetType("RoslynCompileSample.Writer");
        //        var instance = assembly.CreateInstance("RoslynCompileSample.Writer");
        //        var meth = type.GetMember("Write").First() as MethodInfo;
        //        meth.Invoke(instance, new[] { "joel" });
        //    }
        //}






    }
}