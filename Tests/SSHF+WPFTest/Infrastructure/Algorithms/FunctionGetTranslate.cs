using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
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
        /// <summary>
        /// Test
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
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

        internal string DeeplDirectory = @"C:\Users\Vikto\AppData\Local\DeepL\DeepL.exe";

        internal string ScreenshotReaderDirectory = @"D:\_MyHome\Требуется сортировка барахла\Portable ABBYY Screenshot Reader\ScreenshotReader.exe";









        Action<string> Write = Console.WriteLine;

        // static Action<string> Write = Debug.WriteLine;

        public void CreateINST()
        {
            Write("Let's compile!");

            string codeToCompile = @$"
            using System;
            using System.Threading;
            using System.Windows;

            using FlaUI.Core.AutomationElements;
            using FlaUI.Core.Conditions;
            using FlaUI.UIA2;


            namespace RoslynCompileSample
            {{
               public class Writer
               {{
                  static string myStrBuferString = ""Строка по умолчанию"";

                  public void Write(string message)
                  {{
                      FlaUI.Core.Application app = FlaUI.Core.Application.Launch($@""{ScreenshotReaderDirectory}"");
                      FlaUI.Core.AutomationElements.Window mainWindow = app.GetMainWindow(new UIA2Automation(), null);
                      ConditionFactory cf = new ConditionFactory(new UIA2PropertyLibrary());
                      mainWindow.FindFirstDescendant(cf.ByAutomationId(""Item 40001"")).AsButton().Click();
                  
                      GetCheckBuffer();
                  
                      while (myStrBuferString == ""Строка по умолчанию"" || myStrBuferString == string.Empty)
                       {{
                          Thread.Sleep(4);
                          if (myStrBuferString != ""Строка по умолчанию"" & myStrBuferString != string.Empty)
                          {{
                              Thread.Sleep(50);
                              break;
                          }}
                      }}
                  }}
                  
                  
                  String GetText()
                  {{
                      string ReturnValue = string.Empty;
                      Thread STAThread = new Thread(
                          delegate ()
                          {{
                               // Use a fully qualified name for Clipboard otherwise it
                               // will end up calling itself.
                               ReturnValue = Clipboard.GetText();
                              Clipboard.Clear();
                          }});
                      STAThread.SetApartmentState(ApartmentState.STA);
                      STAThread.Start();
                      STAThread.Join();
                  
                      return ReturnValue;
                  }}
                  
                  void GetCheckBuffer()
                  {{
                      int count = 0;
                  
                      while (myStrBuferString == ""Строка по умолчанию"" || myStrBuferString == string.Empty)
                         {{
                          Thread.Sleep(5);
                          count++;
                          Console.WriteLine($""Он пуст {{count}}"");
                          myStrBuferString = GetText();
                         }}
                  
                  
                  }}
               }}
            
            }}";

            Write("Parsing the code into the SyntaxTree");
            Debug.WriteLine("Parsing the code into the SyntaxTree");
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeToCompile);

            string assemblyName = Path.GetRandomFileName();
            var refPaths = new[]
            {
                typeof(System.Object).GetTypeInfo().Assembly.Location,
                typeof(Console).GetTypeInfo().Assembly.Location,
                typeof(UIA2Automation).GetTypeInfo().Assembly.Location,
                typeof(AutomationElement).GetTypeInfo().Assembly.Location,
                typeof(ConditionFactory).GetTypeInfo().Assembly.Location,
                typeof(ProcessStartInfo).GetTypeInfo().Assembly.Location,
                typeof(Clipboard).GetTypeInfo().Assembly.Location,


                Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")
            };

    
            MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

            Write("Adding the following references");
            Debug.WriteLine("Adding the following references");
            foreach (var r in refPaths)
                Write(r);

            Write("Compiling ...");
            Debug.WriteLine("Compiling ...");
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

            if (!result.Success)
                {
                    Write("Compilation failed!");
                    Debug.WriteLine("Compilation failed!");
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Debug.WriteLine($"{Environment.NewLine}{diagnostic.Id}: {diagnostic.GetMessage()}");
                        Console.Error.WriteLine("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    Write("Compilation successful! Now instantiating and executing the code ...");
                    Debug.WriteLine("Compilation successful! Now instantiating and executing the code ...");
                    ms.Seek(0, SeekOrigin.Begin);

                   
                    
                    
                    Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                    var type = assembly.GetType("RoslynCompileSample.Writer");
                    var instance = assembly.CreateInstance("RoslynCompileSample.Writer");
                    var meth = type.GetMember("Write").First() as MethodInfo;
                    meth.Invoke(instance, new[] { ScreenshotReaderDirectory });





                }
            }


        }









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
            Thread STAThread = new Thread(
                delegate ()
                {
                        // Use a fully qualified name for Clipboard otherwise it
                        // will end up calling itself.
                        ReturnValue = Clipboard.GetText();
                        Clipboard.Clear();
                });
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            return ReturnValue;
        }

        static void ClipboardClear()
        {
            Thread STAThread = new Thread(
                delegate ()
                {

                    Clipboard.Clear();

                });
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();
        }
        #endregion

        void StartAlgorithm(string[] args)
        {
            try
            {

                isProcessing = true;
                ShowWindow(HandleAssociatedСonsole, SW_HIDE);
                Stopwatch stopwatch = new Stopwatch();
                Console.WriteLine("Start");

                stopwatch.Start();
                //Thread thread = new Thread(delegate ()
                //{
                //});
                //thread.Start();

                //Console.WriteLine(stopwatch.ElapsedMilliseconds);
                //StartABBY();
                //Console.WriteLine(stopwatch.ElapsedMilliseconds);
                //stopwatch.Restart();


                CreateINST();

                //StatNewPocces();
                // StartABBY();
                CmdRun(CloseABBYcmdQuery);


                Console.WriteLine(stopwatch.ElapsedMilliseconds);
                FocusDepl();

                stopwatch.Restart();
                Console.WriteLine(stopwatch.ElapsedMilliseconds);
                SetDeplText();
                Console.WriteLine(stopwatch.ElapsedMilliseconds);

                // Console.ReadLine();
                Console.WriteLine("Completed algorithm");

            }
            catch (Exception ex)
            {

            }
        }

        void StatNewPocces()
        {

            var compilerParameters = new CompilerParameters();
            var netstandard = Assembly.Load("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            compilerParameters.ReferencedAssemblies.Add(netstandard.Location);

            CSharpCodeProvider? csc = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } });
            CompilerParameters? parameters = new CompilerParameters(new[] { "mscorlib.dll", "System.Core.dll" }, "foo.exe", true);
            parameters.GenerateExecutable = true;
            CompilerResults results = csc.CompileAssemblyFromSource(parameters,
            @"using System.Linq;
            class Program {
              public static void Main(string[] args) {
                var q = from i in Enumerable.Range(1,100)
                          where i % 2 == 0
                          select i;
              }
            }");
            results.Errors.Cast<CompilerError>().ToList().ForEach(error => Debug.WriteLine(error.ErrorText));
        }
        void FocusDepl()
        {



            Process? proc = Process.Start(new ProcessStartInfo(this.DeeplDirectory));

            //var res = WindowFunction.SetWindowPos(proc.Handle, 0, 50, 50, 50, 50, 0x0020 | 0x0100 | 0x0002 | 0x0400 | 0x0001 | 0x0040);





        }

        static volatile string myStrBuferString = "Строка по умолчанию";

        static void GetCheckBuffer()
        {         
            int count = 0;

            while (myStrBuferString == "Строка по умолчанию" || myStrBuferString == string.Empty)
            {
                Thread.Sleep(5);
                count++;
                Console.WriteLine($"Он пуст {count}");
                myStrBuferString = GetText();

            }
        }

        const string CloseABBYcmdQuery = "taskkill /F /IM ScreenshotReader.exe";

        static void CmdRun(string queriesLine)
        {
            Process.Start(new ProcessStartInfo { FileName = "cmd", Arguments = $"/c {queriesLine}", WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = false });
        }

        void StartABBY()
        {

            FlaUI.Core.Application app = FlaUI.Core.Application.Launch(ScreenshotReaderDirectory);

            FlaUI.Core.AutomationElements.Window mainWindow = app.GetMainWindow(new UIA2Automation(), null);

            ConditionFactory cf = new ConditionFactory(new UIA2PropertyLibrary());

            mainWindow.FindFirstDescendant(cf.ByAutomationId("Item 40001")).AsButton().Click();

            GetCheckBuffer();

            while (myStrBuferString == "Строка по умолчанию" || myStrBuferString == string.Empty)
            {
               
                Thread.Sleep(4);
                if (myStrBuferString != "Строка по умолчанию" & myStrBuferString != string.Empty)
                {
                    Thread.Sleep(50);
                   
                    break;
                    //Environment.Exit(0);
                }
            }

            CmdRun(CloseABBYcmdQuery);

        }

        static Process BigLifeTime(List<Process> proc)
        {




            // var sss = list.Where(x => x.StartTime).Max();

            IEnumerable<Process> skip = proc.SkipWhile(ss => ss.HasExited);
            List<Process> list2 = new List<Process>();
            Process sss = null;
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



            // var a = proc.Select(s => s.Key == s.Value == max);

        }
        async void SetDeplText()
        {


            Process[] poolproc = Process.GetProcessesByName("DeepL");

            Process deplProc = null;

            //Dictionary<Process, DateTime> proc = new Dictionary<Process, DateTime>();

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




            AutomationElement panel2 = mainWindowDepl.FindFirstByXPath("//Document/Group/Group[1]/Edit");



            ////panel2.AsTextBox().Text = myStrBuferString;

            //var ggg2 = mainWindowDepl.FindAllDescendants();

            //foreach (var item2 in ggg2)
            //{
            //    Thread.Sleep(300);
            //    item2.DrawHighlight();



            //}

            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

            AutomationElement[]? ggg = mainWindowDepl.FindAllDescendants(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));


            foreach (var item in ggg)
            {
                item.DrawHighlight();
                FlaUI.Core.FrameworkAutomationElementBase.IFrameworkPatterns ValuePattern = item.Patterns;

                FlaUI.Core.IAutomationPattern<FlaUI.Core.Patterns.ILegacyIAccessiblePattern> ss = ValuePattern.LegacyIAccessible;// Value STATE_SYSTEM_FOCUSED | STATE_SYSTEM_FOCUSABLE   FlaUI.Core.WindowsAPI.AccessibilityState
                if (ss.PatternOrDefault.Role == FlaUI.Core.WindowsAPI.AccessibilityRole.ROLE_SYSTEM_TEXT)
                {
                    if (ss.PatternOrDefault.State.Value == FlaUI.Core.WindowsAPI.AccessibilityState.STATE_SYSTEM_FOCUSED | ss.PatternOrDefault.State.Value == FlaUI.Core.WindowsAPI.AccessibilityState.STATE_SYSTEM_FOCUSABLE | ss.PatternOrDefault.State.Value == (FlaUI.Core.WindowsAPI.AccessibilityState.STATE_SYSTEM_FOCUSED | FlaUI.Core.WindowsAPI.AccessibilityState.STATE_SYSTEM_FOCUSABLE))
                    {
                        // item.DrawHighlight();
                        item.AsTextBox().Text = myStrBuferString;
                        break;

                    }

                }
            }



            Console.WriteLine("d");
            //panel2.AsTextBox().Text = myStrBuferString;
            //panel2.AsTextBox().Text = myStrBuferString;







            //while (myStrBuferString == "Строка по умолчанию" || myStrBuferString == string.Empty)
            //{
            //    Console.WriteLine("f");
            //    Thread.Sleep(4);
            //    if (myStrBuferString != "Строка по умолчанию" & myStrBuferString != string.Empty)
            //    {
            //        Console.WriteLine("g");
            //        panel2.AsTextBox().Text = myStrBuferString;
            //        //Environment.Exit(0);
            //    }
            //}


            //panel2.AsTextBox().Enter(myStrBuferString);

            isProcessing = false;
        }
    }
} 

