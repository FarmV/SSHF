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

using SSHF.Infrastructure.Algorithms.Base;
using SSHF.Infrastructure.Interfaces;
using SSHF.Infrastructure.SharedFunctions;


using Help = SSHF.Infrastructure.Algorithms.Base.ExHelp.HelpOperationForNotification;

namespace SSHF.Infrastructure.Algorithms
{

    internal class AlgorithmGetTranslateAbToDepl: BaseAlgorithm
    {

        private static readonly AlgorithmGetTranslateAbToDepl Instance = new AlgorithmGetTranslateAbToDepl();

        internal static string? DeeplDirectory1;

        internal static string? ScreenshotReaderDirectory1;

        const string CloseABBYcmdQuery1 = "taskkill /F /IM ScreenshotReader.exe";
        protected internal override bool IsCheceked => isProcessing;
        protected internal override string Name => "FunctionGetTranslateAbtoDepl";

        private static bool isProcessing1 = default;
            
        #region Регистрация функции
        private AlgorithmGetTranslateAbToDepl()
        {
        }

        enum GetInstanceFail
        {
            None,
            ArgumentIsNullOrWhiteSpace
        }

        internal static Task<AlgorithmGetTranslateAbToDepl> GetInstance(string deeplDirectory, string ScreenshotReaderDirectory)
        {
            var getInstanceFails = ExHelp.GetLazzyDictionaryFails<GetInstanceFail, string>
                (
                  new KeyValuePair<GetInstanceFail, string>(GetInstanceFail.ArgumentIsNullOrWhiteSpace, "Некорректная директория") //0
                );


            if (string.IsNullOrWhiteSpace(deeplDirectory) || string.IsNullOrWhiteSpace(deeplDirectory)) throw new InvalidOperationException().Report(getInstanceFails.Value[0]);

            DeeplDirectory1 = deeplDirectory;
            ScreenshotReaderDirectory1 = ScreenshotReaderDirectory;

            return Task.FromResult(Instance);
        }




        enum GetTranslateAbtoDeplStartFail
        {
            None,
            OperationCanceled,
            InvalidDirectoryDepl,
            InvalidDirectoryReader,
            TheOperationWasNotCompleted,
            CompilationFail,
            FailGetTransleteOrTimeout,
            DeplProccesReturnNull,
            MainWindowDeplIsNull,
            TimeOutPanelInpuText,
            PanelInpuTextIsNotTextBox,
            TypeTisNotString

        }
        /// <summary>
        /// T string
        /// T2 (NoDeplAwaitGetTextAbby? bool = null)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="parameter"></param>
        /// <param name="token"></param>
        /// <returns> Успешность операции</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected internal override async Task<T> Start<T, T2>(T2 parameter, CancellationToken? token = null)
        {
            

            isProcessing1 = true;

            bool getText = default;

            bool? NoDeplAwaitGetTextAbby = parameter as bool?;
            
            if(NoDeplAwaitGetTextAbby is null || NoDeplAwaitGetTextAbby.HasValue is false){}
            else
            {
                getText = NoDeplAwaitGetTextAbby.Value;
            }

            CancellationToken сancelToken = token ??= default;
         
            var reasonFailsList = ExHelp.GetLazzyDictionaryFails
                (
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.OperationCanceled, ExHelp.HelerReasonFail(Help.Canecled)),                                                             //0
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.InvalidDirectoryDepl, ExHelp.HelerReasonFail(Help.InvalidDirectory)),                                                  //1
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.InvalidDirectoryReader, ExHelp.HelerReasonFail(Help.InvalidDirectory)),                                                //2
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.TheOperationWasNotCompleted, ExHelp.HelerReasonFail(Help.ThePreviousOperationWasNotCompleted)),                        //3
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.CompilationFail, ExHelp.HelerReasonFail(Help.CompilationFail)),                                                        //4
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.FailGetTransleteOrTimeout, "Не удалось получить перевод от Depl, либо сработал dispose таймаут"),                      //5
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.DeplProccesReturnNull, $"Метод {nameof(GetDeplWindow)} вернул NULL"),                                                  //6
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.MainWindowDeplIsNull, $"Метод {nameof(FlaUI.Core.Application.GetMainWindow)} вернул Null, возможно сработал таймаут"), //7
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.TimeOutPanelInpuText, $"Не была найдена панель ввода в программе deppl, и сработал таймаут"),                          //8
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.PanelInpuTextIsNotTextBox, $"Попытка приведения PanelInpuText к TexBox вурнула Null"),                                 //9
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.TypeTisNotString, $"Входной {nameof(Type)} не является {nameof(String)}")                                              //10
                );

            if (Equals(typeof(T), typeof(string)) is not true) throw new InvalidOperationException().Report(reasonFailsList.Value[10]);
            if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);
            if (isProcessing is true) throw new InvalidOperationException().Report(reasonFailsList.Value[3]);
            if (string.IsNullOrWhiteSpace(DeeplDirectory1) is true) throw new NullReferenceException().Report(reasonFailsList.Value[1]);
            if (string.IsNullOrWhiteSpace(ScreenshotReaderDirectory1) is true) throw new NullReferenceException().Report(reasonFailsList.Value[2]);
            try
            {
                
                string parsingTextInstance = await GetStringInstance();


                // string assemblyName = $"{Path.GetRandomFileName()}";
                if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);
                string[] assemblyDependencies = new[]
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

                CSharpCompilation comiplation = await Compiller.GetCompiller(parsingTextInstance, "AlgorithmGetTranslate", assemblyDependencies, сancelToken);
                string savePath = Path.Join(Environment.CurrentDirectory, "Extension", "AlgorithmGetTranslateAbToDepl");
                EmitResult compilationrResultToHardDrive = await Compiller.SafeDllToPath(comiplation, savePath);

                if (compilationrResultToHardDrive.Success is not true)
                {
                    IEnumerable<Diagnostic> resultCompilation = await Compiller.HelperReasonFail(compilationrResultToHardDrive);
                    Debug.WriteLine("Compilation failed!");

                    foreach (Diagnostic diagnostic in resultCompilation)
                    {
                        Debug.WriteLine($"{Environment.NewLine}{diagnostic.Id}: {diagnostic.GetMessage()}");
                    }
                    throw new InvalidOperationException().Report(reasonFailsList.Value[4]);
                }

                if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);

                if (File.Exists(Path.ChangeExtension(typeof(SSHF.App).Assembly.Location, "runtimeconfig.json")))
                {
                    File.Copy(Path.ChangeExtension(typeof(SSHF.App).Assembly.Location, "runtimeconfig.json"), savePath);
                }
                else
                {
                    string saveJson = @"
                    {
                       ""runtimeOptions"": {
                       ""tfm"": ""net6.0"",
                       ""frameworks"": [
                       {
                         ""name"": ""Microsoft.NETCore.App"",
                         ""version"": ""6.0.0""
                       },
                       {
                         ""name"": ""Microsoft.WindowsDesktop.App"",
                         ""version"": ""6.0.0""
                       }
                      ]
                     }
                    }
                    ";
                    if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);
                    using FileStream stream = File.Create(Path.Join(Environment.CurrentDirectory, "Extension"));  // проверить
                    using StreamWriter writer = new StreamWriter(stream);
                    writer.AutoFlush = true;
                    await writer.WriteAsync(saveJson);
                }


                //  var outputFilePath = @$"C:\Users\Vikto\Desktop\g\ggg\{nameOutput}" + ".dll";

                // var outputRuntimeConfigPath = Path.ChangeExtension(outputFilePath, "runtimeconfig.json");
                // var currentRuntimeConfigPath = Path.ChangeExtension(typeof(SSHF.App).Assembly.Location, "runtimeconfig.json");

                //  File.Copy(currentRuntimeConfigPath, outputRuntimeConfigPath, true);

                await CmdRun1(@$"cd {savePath} & dotnet AlgorithmGetTranslate.dll");

                await ClipboardClear1();

                if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);

                string textToDepl = await GetTextAwait1(15000);

                if (textToDepl == string.Empty)
                {
                    CmdRun1(CloseABBYcmdQuery1).Start();
                    throw new NullReferenceException().Report(reasonFailsList.Value[5]);
                }

                if (textToDepl is not T returnSourceText) throw new InvalidOperationException().Report(reasonFailsList.Value[10]);
                if (getText is true) return returnSourceText;


                await StartDeplWinow();

                if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);

                if (await GetDeplWindow() is not Process deplProc) throw new NullReferenceException().Report(reasonFailsList.Value[6]);

                FlaUI.Core.Application applicationDepl = FlaUI.Core.Application.Attach(deplProc);
                FlaUI.Core.AutomationElements.Window mainWindowDepl = applicationDepl.GetMainWindow(new UIA3Automation(), new TimeSpan(0, 0, 5));

                if (mainWindowDepl is not FlaUI.Core.AutomationElements.Window Depl) throw new NullReferenceException().Report(reasonFailsList.Value[7]);


                if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);

                bool TimeOut = default;
                using Timer breakTimer = new Timer(new TimerCallback((arg) => TimeOut = true), null, 10000, Timeout.Infinite);

                AutomationElement panelInpuText;
                while (true)
                {

                    if (TimeOut is true) throw new NullReferenceException().Report(reasonFailsList.Value[8]);
                    panelInpuText = Depl.FindFirstByXPath("//Document/Group/Group[1]/Edit");
                    if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);
                    if (panelInpuText is null) continue;
                    if (panelInpuText is not null) break;
                }

                if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);

                if (panelInpuText.AsTextBox() is not FlaUI.Core.AutomationElements.TextBox inputBox) throw new NullReferenceException().Report(reasonFailsList.Value[9]);

                inputBox.Text = string.Empty;
                inputBox.Text = textToDepl;

                return returnSourceText;
            }
            catch (Exception) { throw; }
            finally { isProcessing = false; CmdRun1(CloseABBYcmdQuery1).Start(); }
        }

        static bool isProcessing = false;


        #endregion
        #region Методы 1
        /// <summary>
        /// Получить сроку строику для парсинга - компиляции
        /// </summary>
        /// <returns></returns>
        private static Task<string> GetStringInstance() => Task.FromResult
            (
              @$"
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
                       CmdRun(""{CloseABBYcmdQuery1}"");
             
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
                               CmdRun(""{CloseABBYcmdQuery1}"");
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
             
             
              
              }}"
            );

        private static Task ClipboardClear1()
        {
            Thread STAThread = new Thread(() => Clipboard.Clear());
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            return Task.CompletedTask;
        }
        private static Task SetText1(string text)
        {
            Thread STAThread = new Thread(() => Clipboard.SetText(text));
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            return Task.CompletedTask;
        }
        private static Task<string> GgetText1(string text)
        {
            string ReturnValue = string.Empty;
            Thread STAThread = new Thread(() => ReturnValue = Clipboard.GetText());
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            return Task.FromResult(ReturnValue);
        }
        private static async Task<string> GetTextAwait1(int TimeOut)
        {
            string ReturnValue = string.Empty;

            bool cancelTheOperation = default;

            await Task.Run(() =>
            {
                Thread STAThread = new Thread(() =>
                {
                    using Timer breakTimer = new Timer(new TimerCallback((arg) => cancelTheOperation = true), null, TimeOut, Timeout.Infinite);
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


        private static Task<Process?> StartDeplWinow() => Task.FromResult(Process.Start(new ProcessStartInfo(DeeplDirectory1)));

        private static Task CmdRun1(string queriesLine) => Task.FromResult(Process.Start(new ProcessStartInfo { FileName = "cmd", Arguments = $"/c {queriesLine}", WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true }));

        private static Task<Process> GetBigLifeTimeDeplProcess(List<Process> proc)
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
            return Task.FromResult(sss);
        }

        private static async Task<Process?> GetDeplWindow()
        {
            Process[] poolproc = Process.GetProcessesByName("DeepL");

            List<Process> proc = new List<Process>();
            foreach (var item in poolproc)
            {
                try
                {
                    DateTime time = item.StartTime;
                    if (item.Responding) proc.Add(item);
                }
                catch (Exception)
                {
                }
            }
            return await GetBigLifeTimeDeplProcess(proc);
        }



        #endregion



        //#region Основной алгоритм
        //async void StartAlgorithm()
        //{

        //    try
        //    {
        //        isProcessing = true;
        //        CreateINST();
        //        ClipboardClear();
        //        string? check = GetTextAwait(15000).Result;

        //        if (check is not null)
        //        {
        //            myStrBuferString = check;
        //        }
        //        if (check is null || check == string.Empty)
        //        {
        //            CmdRun(CloseABBYcmdQuery);
        //            throw new NullReferenceException("Вероятно сработал таймаут");
        //        }

        //        CmdRun(CloseABBYcmdQuery);

        //        FocusDepl();

        //        SetDeplText();

        //        isProcessing = false;

        //        Сompleted?.Invoke(true);
        //    }
        //    catch (Exception)
        //    {

        //        Сompleted?.Invoke(false);
        //    }
        //    finally
        //    {
        //        isProcessing = false;
        //    }
        //}

        //#endregion

        //#region Вспомогательные методы

        //internal string DeeplDirectory = @"C:\Users\Vikto\AppData\Local\DeepL\DeepL.exe";

        //internal string ScreenshotReaderDirectory = @"D:\_MyHome\Требуется сортировка барахла\Portable ABBYY Screenshot Reader\ScreenshotReader.exe";

        //#region WinAPI initializing

        ////[DllImport("user32.dll")]
        ////static extern bool SetWindowPos(IntPtr handle, int handle2, int x, int y, int cx, int cy, int flag);

        ////[DllImport("user32.dll")]
        ////static extern IntPtr FindWindowA(string a, string b);

        //[DllImport("kernel32.dll")]
        //static extern IntPtr GetConsoleWindow();

        //[DllImport("user32.dll")]
        //static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);             //показать скырть приложение

        //const int SW_HIDE = 0;
        //const int SW_SHOW = 5;

        //static IntPtr HandleAssociatedСonsole = GetConsoleWindow();
        ////// Hide
        //////ShowWindow(HandleAssociatedСonsole, SW_HIDE);

        ////// Show
        //////ShowWindow(handle, SW_SHOW);

        //#endregion

        //#region Clipboard
        //static void SetText(string p_Text)
        //{
        //    Thread STAThread = new Thread(
        //        delegate ()
        //        {
        //            // Use a fully qualified name for Clipboard otherwise it
        //            // will end up calling itself.
        //            Clipboard.SetText(p_Text);
        //        });
        //    STAThread.SetApartmentState(ApartmentState.STA);
        //    STAThread.Start();
        //    STAThread.Join();
        //}
        //static string GetText()
        //{
        //    string ReturnValue = string.Empty;
        //    Thread STAThread = new Thread(() => { ReturnValue = Clipboard.GetText(); });
        //    STAThread.SetApartmentState(ApartmentState.STA);
        //    STAThread.Start();
        //    STAThread.Join();

        //    return ReturnValue;
        //}


        //static async Task<string?> GetTextAwait(int TimeOut)
        //{

        //    string? ReturnValue = null;

        //    bool cancelTheOperation = default;

        //    await Task.Run(() =>
        //    {
        //        Thread STAThread = new Thread(() =>
        //        {

        //            Timer breakTimer = new Timer(new TimerCallback((arg) => { cancelTheOperation = true; }), null, TimeOut, Timeout.Infinite);
        //            while (true)
        //            {
        //                if (cancelTheOperation is true)
        //                {
        //                    breakTimer.Dispose();
        //                    break;
        //                }

        //                if (Clipboard.ContainsText() is false) continue;

        //                ReturnValue = Clipboard.GetText();

        //                if (string.IsNullOrWhiteSpace(ReturnValue) is true) continue;
        //                breakTimer.Dispose();
        //                break;
        //            }
        //        });
        //        STAThread.SetApartmentState(ApartmentState.STA);
        //        STAThread.Start();
        //        STAThread.Join();
        //    });


        //    return ReturnValue;
        //}




        //static void ClipboardClear()
        //{
        //    Thread STAThread = new Thread(() => { Clipboard.Clear(); });

        //    STAThread.SetApartmentState(ApartmentState.STA);
        //    STAThread.Start();
        //    STAThread.Join();
        //}
        //#endregion

        //void FocusDepl()
        //{
        //    Process? proc = Process.Start(new ProcessStartInfo(this.DeeplDirectory));
        //    //var res = WindowFunction.SetWindowPos(proc.Handle, 0, 50, 50, 50, 50, 0x0020 | 0x0100 | 0x0002 | 0x0400 | 0x0001 | 0x0040);
        //}

        //volatile static string myStrBuferString = "Строка по умолчанию";

        //const string CloseABBYcmdQuery = "taskkill /F /IM ScreenshotReader.exe";

        //static void CmdRun(string queriesLine)
        //{
        //    Process.Start(new ProcessStartInfo { FileName = "cmd", Arguments = $"/c {queriesLine}", WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true });
        //}

        //static Process BigLifeTime(List<Process> proc)
        //{

        //    IEnumerable<Process> skip = proc.SkipWhile(ss => ss.HasExited);
        //    List<Process> list2 = new List<Process>();
        //    Process? sss = null;
        //    foreach (var item in skip)
        //    {
        //        try
        //        {
        //            DateTime asc = item.StartTime;
        //            list2.Add(item);
        //        }
        //        catch (Exception)
        //        {


        //        }

        //    }
        //    if (list2.Count > 1)
        //    {

        //        sss = list2.Aggregate((x, y) => x?.StartTime < y?.StartTime ? x : y);
        //    }
        //    else
        //    {
        //        var bbb = list2.ToArray();
        //        sss = bbb[0];
        //    }

        //    return sss;
        //}

        //void SetDeplText()
        //{
        //    Process[] poolproc = Process.GetProcessesByName("DeepL");

        //    Process? deplProc = null;

        //    List<Process> proc = new List<Process>();
        //    foreach (var item in poolproc)
        //    {
        //        try
        //        {
        //            DateTime time = item.StartTime;
        //            if (item.Responding)
        //                proc.Add(item);
        //        }
        //        catch (Exception)
        //        {
        //        }
        //    }

        //    deplProc = BigLifeTime(proc);

        //    FlaUI.Core.Application appDepl = FlaUI.Core.Application.Attach(deplProc);

        //    FlaUI.Core.AutomationElements.Window mainWindowDepl = appDepl.GetMainWindow(new UIA3Automation(), new TimeSpan(0, 0, 5));

        //    AutomationElement panel2;
        //    while (true)
        //    {
        //        panel2 = mainWindowDepl.FindFirstByXPath("//Document/Group/Group[1]/Edit");
        //        if (panel2 is null) continue;
        //        if (panel2 is not null) break;
        //    }

        //    if (panel2.AsTextBox() is not FlaUI.Core.AutomationElements.TextBox inputBox) throw new NullReferenceException("//Document/Group/Group[1]/Edit стало NULL");

        //    inputBox.Text = string.Empty;
        //    inputBox.Text = myStrBuferString;

        //    isProcessing = false;
        //}

        //public void CreateINST()
        //{
        //    string codeToCompile = @$"
        //    using System;
        //    using System.Threading;
        //    using System.Windows;
        //    using System.Diagnostics;

        //    using FlaUI.Core.AutomationElements;
        //    using FlaUI.Core.Conditions;
        //    using FlaUI.UIA2;


        //    namespace RoslynCompileSample
        //    {{
        //       public class Writer
        //       {{
                  

        //          public static void Main(string[] args)
        //          {{
        //             CmdRun(""{CloseABBYcmdQuery}"");

        //             FlaUI.Core.Application app = FlaUI.Core.Application.Launch($@""D:\_MyHome\Требуется сортировка барахла\Portable ABBYY Screenshot Reader\ScreenshotReader.exe"");

        //             FlaUI.Core.AutomationElements.Window? mainWindow = null;

        //              try
        //              {{
        //                 ConditionFactory cf = new ConditionFactory(new UIA2PropertyLibrary());
        //                 mainWindow = app.GetMainWindow(new UIA2Automation(), null);
        //                 var myButton = mainWindow.FindFirstDescendant(cf.ByAutomationId(""Item 40001""));
        //                 if (myButton.Name is not ""Снимок"")
        //                 {{
        //                     Environment.Exit(1352);
        //                 }}
                  
        //                 myButton.Click();
                  
        //                 string? result = null;
                  
        //               System.Threading.Tasks.Task<string?> result2 = GetTextAwait(15000);
        //                 result2.Wait();
        //                 result = result2.Result;
        //                 if (result is null || result == string.Empty )
        //                 {{
        //                     CmdRun(""{CloseABBYcmdQuery}"");
        //                     Environment.Exit(1352);
        //                 }}
        //              }}
        //              catch (Exception)
        //              {{
                  
        //                 Environment.Exit(1352);
        //              }}
        //          }} 

        //          static async System.Threading.Tasks.Task<string?> GetTextAwait(int TimeOut)
        //          {{
        //              string myStrBuferString = ""Строка по умолчанию"";

        //              string ? ReturnValue = null;
                  
        //              bool cancelTheOperation = default;
                  
        //              await System.Threading.Tasks.Task.Run(() =>
        //              {{
        //                  Thread STAThread = new Thread(() =>
        //                  {{
        //                      Timer breakTimer = new Timer(new TimerCallback((arg) => {{ cancelTheOperation = true; }}), null, TimeOut, Timeout.Infinite);
        //                      Clipboard.Clear();
        //                      while (true)
        //                      {{
        //                          if (cancelTheOperation is true)
        //                          {{
        //                              breakTimer.Dispose();
        //                              ReturnValue = null;
        //                              break;
        //                          }}
                  
        //                           if (Clipboard.ContainsText() is false) continue;

        //                           ReturnValue = Clipboard.GetText();
                                  
        //                           Clipboard.SetText(ReturnValue);
        //                           Clipboard.Flush();
                                  
        //                           if (string.IsNullOrWhiteSpace(ReturnValue) is true) continue;
        //                           breakTimer.Dispose();
        //                           break;
        //                      }}
        //                 }});
        //                  STAThread.SetApartmentState(ApartmentState.STA);
        //                  STAThread.Start();
        //                  STAThread.Join();
        //              }});
                  
        //              return ReturnValue;
        //         }}



        //         static void CmdRun(string queriesLine)
        //         {{
        //             Process.Start(new ProcessStartInfo {{ FileName = ""cmd"", Arguments = $""/c {{ queriesLine }}"", WindowStyle = ProcessWindowStyle.Maximized, CreateNoWindow = false }}).WaitForExit();
                 
                 
        //         }}





        //       }}


            
        //    }}";


        //    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeToCompile);

        //    string assemblyName = $"{Path.GetRandomFileName()}";
        //    string[] refPaths = new[]
        //    {
        //        typeof(object).GetTypeInfo().Assembly.Location,
        //        typeof(Console).GetTypeInfo().Assembly.Location,
        //        typeof(FlaUI.Core.Application).Assembly.Location,
        //        typeof(UIA2Automation).GetTypeInfo().Assembly.Location,
        //        typeof(AutomationElement).GetTypeInfo().Assembly.Location,
        //        typeof(ConditionFactory).GetTypeInfo().Assembly.Location,
        //        typeof(ProcessStartInfo).GetTypeInfo().Assembly.Location,
        //        typeof(Clipboard).GetTypeInfo().Assembly.Location,
        //        typeof(Task<>).GetTypeInfo().Assembly.Location,
        //        typeof(System.ComponentModel.Component).GetTypeInfo().Assembly.Location,

        //        Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")
        //    };

        //    MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

        //    CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication);

        //    CSharpCompilation compilation = CSharpCompilation.Create(
        //        assemblyName,
        //        syntaxTrees: new[] { syntaxTree },
        //        references: references,
        //        options: CompilationOptions);

        //    string nameOutput = "my";

        //    EmitResult result25 = compilation.Emit(@$"C:\Users\Vikto\Desktop\g\ggg\{nameOutput}" + ".dll");
        //    if (!result25.Success)
        //    {
        //        Debug.WriteLine("Compilation failed!");
        //        IEnumerable<Diagnostic> failures = result25.Diagnostics.Where(diagnostic =>
        //            diagnostic.IsWarningAsError ||
        //            diagnostic.Severity == DiagnosticSeverity.Error);

        //        foreach (Diagnostic diagnostic in failures)
        //        {
        //            Debug.WriteLine($"{Environment.NewLine}{diagnostic.Id}: {diagnostic.GetMessage()}");
        //        }
        //    }
        //    else
        //    {
        //        var outputFilePath = @$"C:\Users\Vikto\Desktop\g\ggg\{nameOutput}" + ".dll";

        //        var outputRuntimeConfigPath = Path.ChangeExtension(outputFilePath, "runtimeconfig.json");
        //        var currentRuntimeConfigPath = Path.ChangeExtension(typeof(SSHF.App).Assembly.Location, "runtimeconfig.json");

        //        File.Copy(currentRuntimeConfigPath, outputRuntimeConfigPath, true);

        //        CmdRun(@$"cd C:\Users\Vikto\Desktop\g\ggg & dotnet {nameOutput}.dll");
        //    } //todo доделать  функцию         
        //}
        //#endregion

    }
}

