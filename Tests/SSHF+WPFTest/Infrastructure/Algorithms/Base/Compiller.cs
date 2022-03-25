using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;



using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CSharp;

namespace SSHF.Infrastructure.Algorithms.Base
{
    internal static class Compiller
    {
        internal static Task<CSharpCompilation> GetCompiller(string codeToCompile, string assemblyName,string [] assemblyDependencies)
        {
           
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeToCompile);

            MetadataReference[] references = assemblyDependencies.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

            CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication);

            CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, new[] { syntaxTree }, references, CompilationOptions);

            return Task.FromResult(compilation);
        }

        

        internal static Task<EmitResult> SafeDllToPath(CSharpCompilation compilation, string path) => Task.FromResult(compilation.Emit(path));

        internal static Task<IEnumerable<Diagnostic>> HelperReasonFail(EmitResult result)
        {
            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);


            return Task.FromResult(failures);
        }

        internal static async Task<Assembly> AssemlyToMemory(CSharpCompilation compilation)
        {

            using MemoryStream ms = new MemoryStream();
            EmitResult result = compilation.Emit(ms);


            if (result.Success is not true)
            {
                var failsList = await HelperReasonFail(result);

                foreach (Diagnostic diagnostic in failsList)
                {
                    Debug.WriteLine($"{Environment.NewLine}{diagnostic.Id}: {diagnostic.GetMessage()}");
                }

                throw new InvalidOperationException();
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);

                using (MemoryStream dllStream = ms)
                using (MemoryStream pdbStream = new MemoryStream())
                {
                    var emitResult = compilation.Emit(dllStream, pdbStream);
                    if (!emitResult.Success)
                    {
                        var c = emitResult.Diagnostics;
                    }
                }
                return AssemblyLoadContext.Default.LoadFromStream(ms);
            }
        }


        internal static Task test1(Assembly assembly)
        {
            throw new NotImplementedException();


            Type type = assembly.GetType("RoslynCompileSample.Writer");

            object instance = assembly.CreateInstance("RoslynCompileSample.Writer");

            var meth = type.GetMember("Main").First() as MethodInfo;

            meth.Invoke(instance, new string[1]);
          
        }


      


    }
}
