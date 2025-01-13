using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using System.Reactive.Disposables;

using FVH.Background.Input;
using FVH.Background.Input.Infrastructure.Interfaces;

using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.TrayIconManagement;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Xml.Linq;
using WinRT;
using FVH.SSHF.Infrastructure.Input;
using FVH.SSHF.Infrastructure.Win32;

namespace FVH.SSHF
{
    internal partial class App
    {
        private class BasicDependencies 
        {
            internal BasicDependencies() { }

            internal static Task<IHost> ConfigureDependencies(Thread uiThread, string[]? args = null) 
            {
                Dispatcher uiDispatcher = Dispatcher.FromThread(uiThread) is not Dispatcher dispatcher ? throw new InvalidOperationException() : dispatcher;

                Win32MMCSS win32MMCSS = new Win32MMCSS(uiDispatcher);

                AggregatorInputConditions aggregatorInputCondition = new AggregatorInputConditions();
                BehaviorSubject<bool> requestCompleteAppStartedDisposeInput = new BehaviorSubject<bool>(true);
                BehaviorSubject<bool> requestExternalDisposeInput = new BehaviorSubject<bool>(false);
                Win32ObserverExclusiveMode requestExclusiveModeDisposeInput = new Win32ObserverExclusiveMode(uiDispatcher);
                IObservable<bool> combineConditionsDisposeInput = requestExternalDisposeInput.CombineLatest(requestCompleteAppStartedDisposeInput, (bool AppStarted, bool disposeInput) => AppStarted || disposeInput);

                aggregatorInputCondition.AddIObservable(requestExclusiveModeDisposeInput.ExcusiveMode);
                aggregatorInputCondition.AddIObservable(combineConditionsDisposeInput);
             
                IGetImage iGetImage = new ImageProvider();
   
                BehaviorSubject<IKeyboardHandler?> keyboardHandlerObservableSubject = new BehaviorSubject<IKeyboardHandler?>(null);

                FastWindowManager fastWindowManager = uiDispatcher.Invoke(
                () => _ = new FastWindowManager(uiDispatcher, () => _ = CreateFastWindowViewModelDependencies(iGetImage), keyboardHandlerObservableSubject));
                if(args?.Length > 0)
                {
                    if(args.SingleOrDefault(x => x == "--SCR_NotBR") is not null)
                    {
                        KeyboardShortcut[] defaultShortcuts = fastWindowManager.GetDefaultShortcuts();
                        fastWindowManager.SetNewShortcuts(defaultShortcuts.Where(x => x != defaultShortcuts.Single(x => x.KeyCombo[0] == VKeys.VK_SCROLL)).ToArray());
                    }
                }
          
                BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>> listIInvokeShortcutsBehaviorSubject = new BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>([fastWindowManager]);

                WaitingInputProvider waitingInput = new WaitingInputProvider(aggregatorInputCondition.InputConditionsBehaviorSubject, listIInvokeShortcutsBehaviorSubject);
                waitingInput.CurrentInstanceIKeyboardHandlerOrDefault.Subscribe(keyboardHandlerObservableSubject.OnNext);
        
                TrayIcon trayIcon = CreateAnIconInTheNotificationArea(uiDispatcher);

                IHost host = Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((_, configuration) =>
                { configuration.Sources.Clear(); }).ConfigureServices((__, container) =>
                {
                    container.AddSingleton<Win32MMCSS>(win32MMCSS);
                    container.AddSingleton<Dispatcher>(uiDispatcher);
                    container.AddSingleton<AggregatorInputConditions>(aggregatorInputCondition);
                    container.AddSingleton<IGetImage>(iGetImage);
                    container.AddSingleton<FastWindowManager>(fastWindowManager);
                    container.AddSingleton<WaitingInputProvider>(waitingInput);
                    container.AddSingleton<TrayIcon>(trayIcon);
                }).Build();

                CompositeDisposable disposablesDependencies =
                [
                    win32MMCSS,
                    aggregatorInputCondition,
                    requestExclusiveModeDisposeInput,
                    fastWindowManager,
                    waitingInput,
                    trayIcon,
                ];

                CancellationTokenRegistration? tokenApplicationStartedCallback = null;
                tokenApplicationStartedCallback = 
                host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() =>
                {
                    requestCompleteAppStartedDisposeInput.OnNext(false);
                    tokenApplicationStartedCallback?.Dispose();
                    requestCompleteAppStartedDisposeInput.OnCompleted();
                    requestCompleteAppStartedDisposeInput.Dispose();
                });
                CancellationTokenRegistration? tokenApplicationApplicationStopped = null;
                tokenApplicationApplicationStopped =
                host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
                {
                    disposablesDependencies.Dispose();
                    tokenApplicationApplicationStopped?.Dispose();
                });  
                
                return Task.FromResult(host);
            }


            private static TrayIcon CreateAnIconInTheNotificationArea(Dispatcher uiDispatcher) => uiDispatcher.Invoke(() => _ = new TrayIcon(App.GetResource(Resource.AppIcon).Stream));
            private static FastWindowViewModelDependencies CreateFastWindowViewModelDependencies(IGetImage imageProvider) => _ = new FastWindowViewModelDependencies(imageProvider);
        }
    }
}

