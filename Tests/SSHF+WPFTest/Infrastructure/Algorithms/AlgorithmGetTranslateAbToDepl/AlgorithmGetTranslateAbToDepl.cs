using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA2;
using FlaUI.UIA3;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using SSHF.Infrastructure.Algorithms.Base;



using Help = SSHF.Infrastructure.Algorithms.Base.ExHelp.HelpOperationForNotification;

namespace SSHF.Infrastructure.Algorithms
{

    internal class AlgorithmGetTranslateAbToDepl: BaseAlgorithm
    {

        private static AlgorithmGetTranslateAbToDepl? Instance;

        private string DeeplDirectory;

        private string ScreenshotReaderDirectory;

        private const string CloseABBYcmdQuery = "taskkill /F /IM ScreenshotReader.exe";
        protected internal override bool IsCheceked => isProcessing;
        protected internal override string Name => "AlgorithmGetTranslateAbToDepl";

        private static bool isProcessing = default;
        private AlgorithmGetTranslateAbToDepl(string deeplDirectory, string ScreenshotReaderDirectory)
        {
            DeeplDirectory = deeplDirectory;
            this.ScreenshotReaderDirectory = ScreenshotReaderDirectory;
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

            if (Instance is null) Instance = new AlgorithmGetTranslateAbToDepl(deeplDirectory, ScreenshotReaderDirectory);

            return Task.FromResult(Instance);
        }

        #region Основной алгоритм
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
            bool getText = default;

            bool? NoDeplAwaitGetTextAbby = parameter as bool?;

            if (NoDeplAwaitGetTextAbby is null || NoDeplAwaitGetTextAbby.HasValue is false) { }
            else
            {
                getText = NoDeplAwaitGetTextAbby.Value;
            }

            CancellationToken сancelToken = token ??= default;

            Lazy<KeyValuePair<GetTranslateAbtoDeplStartFail, string>[]> reasonFailsList = ExHelp.GetLazzyDictionaryFails
                (
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.OperationCanceled, ExHelp.HelerReasonFail(Help.Canecled)),                                                             //0
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.InvalidDirectoryDepl, ExHelp.HelerReasonFail(Help.InvalidDirectory)),                                                  //1
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.InvalidDirectoryReader, ExHelp.HelerReasonFail(Help.InvalidDirectory)),                                                //2
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.TheOperationWasNotCompleted, ExHelp.HelerReasonFail(Help.ThePreviousOperationWasNotCompleted)),                        //3
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.CompilationFail, ExHelp.HelerReasonFail(Help.CompilationFail)),                                                        //4
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.FailGetTransleteOrTimeout, "Не удалось получить перевод от Reader, либо сработал dispose таймаут"),                    //5
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.DeplProccesReturnNull, $"Метод {nameof(GetDeplWindow)} вернул NULL"),                                                  //6
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.MainWindowDeplIsNull, $"Метод {nameof(FlaUI.Core.Application.GetMainWindow)} вернул Null, возможно сработал таймаут"), //7
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.TimeOutPanelInpuText, $"Не была найдена панель ввода в программе deppl, и сработал таймаут"),                          //8
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.PanelInpuTextIsNotTextBox, $"Попытка приведения PanelInpuText к TexBox вурнула Null"),                                 //9
                  new KeyValuePair<GetTranslateAbtoDeplStartFail, string>(GetTranslateAbtoDeplStartFail.TypeTisNotString, $"Входной {nameof(Type)} не является {nameof(String)}")                                              //10
                );

            if (Equals(typeof(T), typeof(string)) is not true) throw new InvalidOperationException().Report(reasonFailsList.Value[10]);
            if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);
            if (isProcessing is true) throw new InvalidOperationException().Report(reasonFailsList.Value[3]);
            if (string.IsNullOrWhiteSpace(DeeplDirectory) is true) throw new NullReferenceException().Report(reasonFailsList.Value[1]);
            if (string.IsNullOrWhiteSpace(ScreenshotReaderDirectory) is true) throw new NullReferenceException().Report(reasonFailsList.Value[2]);
            isProcessing = true;
            try
            {
                if (Directory.Exists(Path.Join(Environment.CurrentDirectory, DirectoryAlgorithms, Name)) is not true) Directory.CreateDirectory(Path.Join(Environment.CurrentDirectory, DirectoryAlgorithms, Name));

                Uri[] resouceItem = new Uri[]{
                    UriHelper.GetUriApp(@"Infrastructure\Algorithms\AlgorithmGetTranslateAbToDepl\Resources\FlaUI.Core.dll"),
                    UriHelper.GetUriApp(@"Infrastructure\Algorithms\AlgorithmGetTranslateAbToDepl\Resources\FlaUI.UIA2.dll")
                };


                resouceItem.AsParallel().ForAll(async uri =>
                {
                    if (File.Exists(Path.Join(Environment.CurrentDirectory, DirectoryAlgorithms, Name, Path.GetFileName(uri.AbsolutePath))) is not true)
                    {
                        System.Windows.Resources.StreamResourceInfo res = Application.GetResourceStream(uri);
                        using Stream stream = res.Stream;
                        using FileStream stream2 = new System.IO.FileStream(Path.Join(Environment.CurrentDirectory, DirectoryAlgorithms, Name, Path.GetFileName(uri.AbsolutePath)), FileMode.Create);
                        await stream.CopyToAsync(stream2);
                        await stream.FlushAsync();
                    }
                });
                string savePathDll = Path.Join(Environment.CurrentDirectory, base.DirectoryAlgorithms, Name, Name + ".dll");
                if (File.Exists(Path.ChangeExtension(typeof(SSHF.App).Assembly.Location, "runtimeconfig.json")))
                {
                    if (File.Exists(Path.ChangeExtension(savePathDll, "runtimeconfig.json")) is not true)
                    {
                        File.Copy(Path.ChangeExtension(typeof(SSHF.App).Assembly.Location, "runtimeconfig.json"), Path.ChangeExtension(savePathDll, "runtimeconfig.json"));
                    }
                }
                else
                {
                    if (File.Exists(Path.ChangeExtension(savePathDll, "runtimeconfig.json")) is not true)
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
                        using FileStream stream = File.Create(Path.ChangeExtension(savePathDll, "runtimeconfig.json"));  // проверить
                        using StreamWriter writer = new StreamWriter(stream);
                        writer.AutoFlush = true;
                        await writer.WriteAsync(saveJson);
                    }
                }
                if (File.Exists(savePathDll) is not true)
                {
                    string parsingTextInstance = await GetStringInstance(ScreenshotReaderDirectory);

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

                    CSharpCompilation comiplation = await Compiller.GetCompiller(parsingTextInstance, Name, assemblyDependencies, сancelToken);

                    EmitResult compilationrResultToHardDrive = await Compiller.SafeDllToPath(comiplation, savePathDll); // todo запомнить ставить слеш в конце котолога. Иначе ошибка аторизации

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

                }

                var ccc = Path.GetDirectoryName(savePathDll);
                var bbb = Path.ChangeExtension(Name, ".dll");


                CmdInvoke($"cd \"{Path.GetDirectoryName(savePathDll)}\" & dotnet {Path.ChangeExtension(Name, ".dll")}");
    
                await ClipboardClear();

                if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);

                string textToDepl = await GetTextAwait(15000);

                if (textToDepl == string.Empty)
                {
                    CmdInvoke(CloseABBYcmdQuery);
                    throw new NullReferenceException().Report(reasonFailsList.Value[5]);
                }
                CmdInvoke(CloseABBYcmdQuery);

                if (textToDepl is not T returnSourceText) throw new InvalidOperationException().Report(reasonFailsList.Value[10]);
                if (getText is true) return returnSourceText;


                await StartDeplWinow(DeeplDirectory);

                if (сancelToken.IsCancellationRequested is true) throw new OperationCanceledException(сancelToken).Report(reasonFailsList.Value[0]);

                if (await GetDeplWindow() is not Process deplProc) throw new NullReferenceException().Report(reasonFailsList.Value[6]);

                FlaUI.Core.Application applicationDepl = FlaUI.Core.Application.Attach(deplProc);
                FlaUI.Core.AutomationElements.Window mainWindowDepl = applicationDepl.GetMainWindow(new UIA3Automation(), new TimeSpan(0, 0, 15));

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
            finally { isProcessing = false; CmdInvoke(CloseABBYcmdQuery); }
        }


        private static void CmdInvoke(string queriesLine)
        {
            Task.Factory.StartNew(new Action(() =>
            {
                Process.Start(
                new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c {queriesLine}",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }));
        }

        #endregion

        #region Вспомогательные методы

        private static Task<string> GetStringInstance(string ReaderDirectory)
        {
            return Task.FromResult
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
                       CmdRun(""{CloseABBYcmdQuery}"");
             
                       FlaUI.Core.Application app = FlaUI.Core.Application.Launch($@""{ReaderDirectory}"");
             
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
             
             
              
              }}"
);
        }

        private static Task ClipboardClear()
        {
            Thread STAThread = new Thread(() => Clipboard.Clear());
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            return Task.CompletedTask;
        }
        private static Task SetText(string text)
        {
            Thread STAThread = new Thread(() => Clipboard.SetText(text));
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            return Task.CompletedTask;
        }
        private static Task<string> GgetText(string text)
        {
            string ReturnValue = string.Empty;
            Thread STAThread = new Thread(() => ReturnValue = Clipboard.GetText());
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            return Task.FromResult(ReturnValue);
        }
        private static async Task<string> GetTextAwait(int TimeOut)
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
        private static Task<Process?> StartDeplWinow(string DeeplDirectory) => Task.FromResult(Process.Start(new ProcessStartInfo(DeeplDirectory)));
        private static Task<Process?> GetDeplWindow()
        {
            List<Process> proc = new List<Process>();
            Process[] poolProc = Process.GetProcessesByName("DeepL");
            poolProc.AsParallel().ForAll(oneItem => 
            {
                try
                {
                    DateTime time = oneItem.StartTime;
                    if (oneItem.Responding) proc.Add(oneItem);
                }
                catch (Exception){}
            });                                                 
            return Task.FromResult(proc.MinBy<Process, DateTime>(x => x.StartTime));      
        }
        #endregion
    }
}

