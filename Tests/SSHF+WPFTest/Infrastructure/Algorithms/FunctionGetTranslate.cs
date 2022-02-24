using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA2;
using FlaUI.UIA3;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CSharp;

using SSHF.Infrastructure.Interfaces;
using SSHF.Infrastructure.SharedFunctions;

namespace SSHF.Infrastructure.Algorithms
{

    internal class FunctionGetTranslate: Freezable, IActionFunction
    {
        #region Регистрация функции
        public FunctionGetTranslate()
        {
        }

        void Registration(string NameButton)
        {
            if (App.KeyBoardHandler is null) throw new NullReferenceException("App.KeyBoarHandle is NULL");

            App.KeyBoardHandler.RegisterAFunction("Translate", NameButton, new Action(() => { StartFunction(); }), true);
        }

        public string Name => "Translate";

        private bool _status = default;
        public bool Enable
        {
            get => _status;
            private set => _status = value;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new FunctionScreenShot();
        }

        public Tuple<bool, string> CheckAndRegistrationFunction(object? parameter = null)
        {
            if (string.IsNullOrEmpty(DeeplDirectory) is true) return Tuple.Create(false, $"Не установлена директория программы Deepl");
            if (string.IsNullOrEmpty(DeeplDirectory) is true) return Tuple.Create(false, "Не установлена директория программы ScreenshotReader");

            if (parameter is not string nameButton)
            {
                _status = true;
                return Tuple.Create(true, "Функция может быть выполнена, но клавиши глобального вызова небыли зарегистрированы");
            }

            try
            {
                if (_status is true)
                {
                    if (App.KeyBoardHandler is null) throw new NullReferenceException("App.KeyBoarHandle is NULL");

                    if (App.KeyBoardHandler.ReplaceRegisteredFunction("Translate", nameButton, new Action(() => { StartFunction(); })) is false)
                    {
                        throw new NullReferenceException("KeyBoardHandler.ReplaceRegisteredFunction вернул false");
                    }
                }
                if (_status is false)
                {
                    Registration(nameButton);
                    _status = true;
                }
            }
            catch (Exception)
            {
                _status = false;
                Tuple.Create(false, "Произошла ошибка в класе регистрации регистрации клавиш");
            }
            return Tuple.Create(true, "Функция и клавиши успешно зарегистрированны");
        }

        bool isProcessing = false;

        public bool StartFunction(object? parameter = null)
        {
            if (_status is false) return false;
            if (isProcessing is true) return false;
            StartAlgorithm(new string[1]);
            return true;
        }

        public bool СancelFunction(object? parameter = null)
        {
            return false;
        }

        #endregion

        #region Основной алгоритм
        void StartAlgorithm(string[] args)
        {
            try
            {
                isProcessing = true;              
                CreateINST();
                ClipboardClear();
                string? check = GetTextAwait(15000).Result;

                if (check is not null)
                {
                    myStrBuferString = check;
                }
                if (check is null || check == string.Empty)
                {
                    CmdRun(CloseABBYcmdQuery);
                    throw new NullReferenceException("Вероятно сработал таймаут");
                }

                CmdRun(CloseABBYcmdQuery);

                FocusDepl();

                SetDeplText();

                isProcessing = false;
            }
            catch (Exception)
            {
                isProcessing = false;               
            }
        }

        #endregion

        #region Вспомогательные методы

        internal string DeeplDirectory = @"C:\Users\Vikto\AppData\Local\DeepL\DeepL.exe";

        internal string ScreenshotReaderDirectory = @"D:\_MyHome\Требуется сортировка барахла\Portable ABBYY Screenshot Reader\ScreenshotReader.exe";

        #region WinAPI initializing

        //[DllImport("user32.dll")]
        //static extern bool SetWindowPos(IntPtr handle, int handle2, int x, int y, int cx, int cy, int flag);

        //[DllImport("user32.dll")]
        //static extern IntPtr FindWindowA(string a, string b);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);             //показать скырть приложение

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static IntPtr HandleAssociatedСonsole = GetConsoleWindow();
        //// Hide
        ////ShowWindow(HandleAssociatedСonsole, SW_HIDE);

        //// Show
        ////ShowWindow(handle, SW_SHOW);

        #endregion

        #region Clipboard
        static void SetText(string p_Text)
        {
            Thread STAThread = new Thread(
                delegate ()
                {
                    // Use a fully qualified name for Clipboard otherwise it
                    // will end up calling itself.
                    Clipboard.SetText(p_Text);
                });
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();
        }
        static string GetText()
        {
            string ReturnValue = string.Empty;
            Thread STAThread = new Thread(() => { ReturnValue = Clipboard.GetText(); });
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            return ReturnValue;
        }


        static async Task<string?> GetTextAwait(int TimeOut)
        {

            string? ReturnValue = null;

            bool cancelTheOperation = default;

            await Task.Run(() =>
            {
                Thread STAThread = new Thread(() =>
                {

                    Timer breakTimer = new Timer(new TimerCallback((arg) => { cancelTheOperation = true; }), null, TimeOut, Timeout.Infinite);
                    while (true)
                    {
                        if (cancelTheOperation is true)
                        {
                            breakTimer.Dispose();
                            break;
                        }

                        if (Clipboard.ContainsText() is false) continue;

                        ReturnValue = Clipboard.GetText();

                        if (string.IsNullOrWhiteSpace(ReturnValue) is true) continue;
                        breakTimer.Dispose();
                        break;
                    }
                });
                STAThread.SetApartmentState(ApartmentState.STA);
                STAThread.Start();
                STAThread.Join();
            });


            return ReturnValue;
        }




        static void ClipboardClear()
        {
            Thread STAThread = new Thread(() => { Clipboard.Clear(); });

            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();
        }
        #endregion

        void FocusDepl()
        {
            Process? proc = Process.Start(new ProcessStartInfo(this.DeeplDirectory));
            //var res = WindowFunction.SetWindowPos(proc.Handle, 0, 50, 50, 50, 50, 0x0020 | 0x0100 | 0x0002 | 0x0400 | 0x0001 | 0x0040);
        }

        volatile string myStrBuferString = "Строка по умолчанию";

        const string CloseABBYcmdQuery = "taskkill /F /IM ScreenshotReader.exe";

        static void CmdRun(string queriesLine)
        {
            Process.Start(new ProcessStartInfo { FileName = "cmd", Arguments = $"/c {queriesLine}", WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true });
        }
    
        static Process BigLifeTime(List<Process> proc)
        {

            IEnumerable<Process> skip = proc.SkipWhile(ss => ss.HasExited);
            List<Process> list2 = new List<Process>();
            Process? sss = null;
            foreach (var item in skip)
            {
                try
                {
                    DateTime asc = item.StartTime;
                    list2.Add(item);
                }
                catch (Exception)
                {


                }

            }
            if (list2.Count > 1)
            {

                sss = list2.Aggregate((x, y) => x?.StartTime < y?.StartTime ? x : y);
            }
            else
            {
                var bbb = list2.ToArray();
                sss = bbb[0];
            }

            return sss;
        }

        void SetDeplText()
        {
            Process[] poolproc = Process.GetProcessesByName("DeepL");

            Process? deplProc = null;

            List<Process> proc = new List<Process>();
            foreach (var item in poolproc)
            {
                try
                {
                    DateTime time = item.StartTime;
                    if (item.Responding)
                        proc.Add(item);
                }
                catch (Exception)
                {
                }
            }

            deplProc = BigLifeTime(proc);

            FlaUI.Core.Application appDepl = FlaUI.Core.Application.Attach(deplProc);

            FlaUI.Core.AutomationElements.Window mainWindowDepl = appDepl.GetMainWindow(new UIA3Automation(), new TimeSpan(0, 0, 5));

            AutomationElement panel2;
            while (true)
            {
                panel2 = mainWindowDepl.FindFirstByXPath("//Document/Group/Group[1]/Edit");
                if (panel2 is null) continue;
                if (panel2 is not null) break;
            }

            if (panel2.AsTextBox() is not FlaUI.Core.AutomationElements.TextBox inputBox) throw new NullReferenceException("//Document/Group/Group[1]/Edit стало NULL");

            inputBox.Text = string.Empty;
            inputBox.Text = myStrBuferString;

            isProcessing = false;
        }

        public void CreateINST()
        {
            string codeToCompile = @$"
            using System;
            using System.Threading;
            using System.Windows;
            using System.Diagnostics;

            using FlaUI.Core.AutomationElements;
            using FlaUI.Core.Conditions;
            using FlaUI.UIA2;


            namespace RoslynCompileSample
            {{
               public class Writer
               {{
                  

                  public static void Main(string[] args)
                  {{
                     CmdRun(""{CloseABBYcmdQuery}"");

                     FlaUI.Core.Application app = FlaUI.Core.Application.Launch($@""D:\_MyHome\Требуется сортировка барахла\Portable ABBYY Screenshot Reader\ScreenshotReader.exe"");

                     FlaUI.Core.AutomationElements.Window? mainWindow = null;

                      try
                      {{
                         ConditionFactory cf = new ConditionFactory(new UIA2PropertyLibrary());
                         mainWindow = app.GetMainWindow(new UIA2Automation(), null);
                         var myButton = mainWindow.FindFirstDescendant(cf.ByAutomationId(""Item 40001""));
                         if (myButton.Name is not ""Снимок"")
                         {{
                             Environment.Exit(1352);
                         }}
                  
                         myButton.Click();
                  
                         string? result = null;
                  
                       System.Threading.Tasks.Task<string?> result2 = GetTextAwait(15000);
                         result2.Wait();
                         result = result2.Result;
                         if (result is null || result == string.Empty )
                         {{
                             CmdRun(""{CloseABBYcmdQuery}"");
                             Environment.Exit(1352);
                         }}
                      }}
                      catch (Exception)
                      {{
                  
                         Environment.Exit(1352);
                      }}
                  }} 

                  static async System.Threading.Tasks.Task<string?> GetTextAwait(int TimeOut)
                  {{
                      string myStrBuferString = ""Строка по умолчанию"";

                      string ? ReturnValue = null;
                  
                      bool cancelTheOperation = default;
                  
                      await System.Threading.Tasks.Task.Run(() =>
                      {{
                          Thread STAThread = new Thread(() =>
                          {{
                              Timer breakTimer = new Timer(new TimerCallback((arg) => {{ cancelTheOperation = true; }}), null, TimeOut, Timeout.Infinite);
                              Clipboard.Clear();
                              while (true)
                              {{
                                  if (cancelTheOperation is true)
                                  {{
                                      breakTimer.Dispose();
                                      ReturnValue = null;
                                      break;
                                  }}
                  
                                   if (Clipboard.ContainsText() is false) continue;

                                   ReturnValue = Clipboard.GetText();
                                  
                                   Clipboard.SetText(ReturnValue);
                                   Clipboard.Flush();
                                  
                                   if (string.IsNullOrWhiteSpace(ReturnValue) is true) continue;
                                   breakTimer.Dispose();
                                   break;
                              }}
                         }});
                          STAThread.SetApartmentState(ApartmentState.STA);
                          STAThread.Start();
                          STAThread.Join();
                      }});
                  
                      return ReturnValue;
                 }}



                 static void CmdRun(string queriesLine)
                 {{
                     Process.Start(new ProcessStartInfo {{ FileName = ""cmd"", Arguments = $""/c {{ queriesLine }}"", WindowStyle = ProcessWindowStyle.Maximized, CreateNoWindow = false }}).WaitForExit();
                 
                 
                 }}





               }}


            
            }}";


            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeToCompile);

            string assemblyName = $"{Path.GetRandomFileName()}";
            var refPaths = new[]
            {
                typeof(object).GetTypeInfo().Assembly.Location,
                typeof(Console).GetTypeInfo().Assembly.Location,
                typeof(FlaUI.Core.Application).Assembly.Location,
                typeof(UIA2Automation).GetTypeInfo().Assembly.Location,
                typeof(AutomationElement).GetTypeInfo().Assembly.Location,
                typeof(ConditionFactory).GetTypeInfo().Assembly.Location,
                typeof(ProcessStartInfo).GetTypeInfo().Assembly.Location,
                typeof(Clipboard).GetTypeInfo().Assembly.Location,
                typeof(Task<>).GetTypeInfo().Assembly.Location,
                typeof(System.ComponentModel.Component).GetTypeInfo().Assembly.Location,

                Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")
            };

            MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

            CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication);

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: CompilationOptions);

            string nameOutput = "my";

            EmitResult result25 = compilation.Emit(@$"C:\Users\Vikto\Desktop\g\ggg\{nameOutput}" + ".dll");
            if (!result25.Success)
            {
                Debug.WriteLine("Compilation failed!");
                IEnumerable<Diagnostic> failures = result25.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    Debug.WriteLine($"{Environment.NewLine}{diagnostic.Id}: {diagnostic.GetMessage()}");
                }
            }
            else
            {
                var outputFilePath = @$"C:\Users\Vikto\Desktop\g\ggg\{nameOutput}" + ".dll";

                var outputRuntimeConfigPath = Path.ChangeExtension(outputFilePath, "runtimeconfig.json");
                var currentRuntimeConfigPath = Path.ChangeExtension(typeof(SSHF.App).Assembly.Location, "runtimeconfig.json");

                File.Copy(currentRuntimeConfigPath, outputRuntimeConfigPath, true);

                CmdRun(@$"cd C:\Users\Vikto\Desktop\g\ggg & dotnet {nameOutput}.dll");
            }

            //using (var ms = new MemoryStream())
            //{
            //    EmitResult result = compilation.Emit(ms);


            //    if (!result.Success)
            //    {
            //        Debug.WriteLine("Compilation failed!");
            //        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
            //            diagnostic.IsWarningAsError ||
            //            diagnostic.Severity == DiagnosticSeverity.Error);

            //        foreach (Diagnostic diagnostic in failures)
            //        {
            //            Debug.WriteLine($"{Environment.NewLine}{diagnostic.Id}: {diagnostic.GetMessage()}");
            //        }
            //    }
            //    else
            //    {
            //        ms.Seek(0, SeekOrigin.Begin);

            //        using (var dllStream = ms)
            //        using (var pdbStream = new MemoryStream())
            //        {
            //            var emitResult = compilation.Emit(dllStream, pdbStream);
            //            if (!emitResult.Success)
            //            {
            //                var c = emitResult.Diagnostics;
            //            }
            //        }

            //        Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
            //        var type = assembly.GetType("RoslynCompileSample.Writer");
            //        var instance = assembly.CreateInstance("RoslynCompileSample.Writer");
            //        var meth = type.GetMember("Main").First() as MethodInfo;
            //        meth.Invoke(instance, new string[1]);
            //    }
            //}

        }
        #endregion
    }
}

