﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

            App.KeyBoardHandler.RegisterAFunction("Translate", NameButton, new Action(() => { StartFunction(); }) , true);
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
        public Tuple<bool,string> CheckAndRegistrationFunction(object? parameter = null)
        {
            if (string.IsNullOrEmpty(DeeplDirectory) is true) return Tuple.Create(false,$"Не установлена директория программы Deepl");
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
                if(_status is false)
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

                StatNewPocces();
              //      StartABBY();


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
           


            Process? proc =  Process.Start(new ProcessStartInfo(this.DeeplDirectory));

            var res =   WindowFunction.SetWindowPos(proc.Handle, 0, 50, 50, 50, 50, 0x0020 | 0x0100 | 0x0002 | 0x0400 | 0x0001 | 0x0040);





        }

        static volatile string myStrBuferString = "Строка по умолчанию";

        static void GetCheckBuffer()
        {
            ClipboardClear();

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
            Process.Start(new ProcessStartInfo { FileName = "cmd", Arguments = $"/c {queriesLine}", WindowStyle = ProcessWindowStyle.Normal, CreateNoWindow = false });
        }

        void StartABBY()
        {

            FlaUI.Core.Application app = FlaUI.Core.Application.Launch(ScreenshotReaderDirectory);

            FlaUI.Core.AutomationElements.Window mainWindow = app.GetMainWindow(new UIA2Automation(), null);

            ConditionFactory cf = new ConditionFactory(new UIA2PropertyLibrary());

            mainWindow.FindFirstDescendant(cf.ByAutomationId("Item 40001")).AsButton().Click();

            //Task taskCheck = new Task(GetCheckBuffer);
            //taskCheck.Start();

            //taskCheck.Wait();

            GetCheckBuffer();

            while (myStrBuferString == "Строка по умолчанию" || myStrBuferString == string.Empty)
            {
                Console.WriteLine("f");
                Thread.Sleep(4);
                if (myStrBuferString != "Строка по умолчанию" & myStrBuferString != string.Empty)
                {
                    Thread.Sleep(50);
                    Console.WriteLine("g");
                    break;
                    //Environment.Exit(0);
                }
            }

            CmdRun(CloseABBYcmdQuery);

            //while (!app.HasExited)
            //{
            //    Thread.Sleep(4);
            //}





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

            FlaUI.Core.AutomationElements.Window mainWindowDepl = appDepl.GetMainWindow(new UIA3Automation(),new TimeSpan(0,0,5));




                AutomationElement panel2 = mainWindowDepl.FindFirstByXPath("//Document/Edit[1]"); 



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
