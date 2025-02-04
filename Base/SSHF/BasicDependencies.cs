using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using R3;

using FVH.Background.Input.Infrastructure.Interfaces;

using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.TrayIconManagement;
using FVH.SSHF.Infrastructure.Input;
using FVH.SSHF.Infrastructure.Win32;
using FVH.SSHF.FastWindowArea;


namespace FVH.SSHF
{
    internal partial class App
    {
        private class BasicDependencies 
        {
            internal BasicDependencies() { }

            internal static ValueTask<IHost> ConfigureDependencies(Thread uiThread, string[]? args = null) 
            {
                Dispatcher uiDispatcher = Dispatcher.FromThread(uiThread) is not Dispatcher dispatcher ? throw new InvalidOperationException() : dispatcher;

                Win32MMCSS win32MMCSS = new Win32MMCSS(uiDispatcher);

                AggregatorInputConditions aggregatorInputCondition = new AggregatorInputConditions();
                R3.BehaviorSubject<bool> requestCompleteAppStartedDisposeInput = new R3.BehaviorSubject<bool>(true);
                R3.BehaviorSubject<bool> requestExternalDisposeInput = new R3.BehaviorSubject<bool>(false);
                Win32ObserverExclusiveMode requestExclusiveModeDisposeInput = new Win32ObserverExclusiveMode(uiDispatcher);
                Observable<bool> combineConditionsDisposeInput = requestExternalDisposeInput.CombineLatest(requestCompleteAppStartedDisposeInput, (bool AppStarted, bool disposeInput) => AppStarted || disposeInput);

                aggregatorInputCondition.AddIObservable(requestExclusiveModeDisposeInput.ExcusiveMode);
                aggregatorInputCondition.AddIObservable(combineConditionsDisposeInput);
             
                IGetImage iGetImage = new ImageProvider();

                R3.BehaviorSubject<IKeyboardHandler?> keyboardHandlerObservableSubject = new R3.BehaviorSubject<IKeyboardHandler?>(null);

                R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>? listIInvokeShortcutsBehaviorSubject = null;
                Func<BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>> delegateListIInvokeShortcutsBehaviorSubject = 
                   new Func<R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>>(() => listIInvokeShortcutsBehaviorSubject!);

                WaitingInputProvider? waitingInput = new WaitingInputProvider(aggregatorInputCondition.InputConditionsBehaviorSubject, delegateListIInvokeShortcutsBehaviorSubject);

                FastWindowManager fastWindowManager = uiDispatcher.Invoke(
                () => _ = new FastWindowManager(uiDispatcher, () => _ = CreateFastWindowViewModelDependencies(iGetImage), keyboardHandlerObservableSubject, waitingInput));
                if(args?.Length > 0)
                {
                    if(args.SingleOrDefault(x => x == "--SCR_NotBR") is not null)
                    {
                        KeyboardShortcut[] defaultShortcuts = fastWindowManager.GetDefaultShortcuts();
                        fastWindowManager.SetNewShortcuts(defaultShortcuts.Where(x => x != defaultShortcuts.Single(x => x.KeyCombo.CurrentValue[0] == VKeys.VK_SCROLL)).ToArray());
                    }
                }
          
                listIInvokeShortcutsBehaviorSubject = new BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>([fastWindowManager]);

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

                    container.AddSingleton<Win32ObserverExclusiveMode>(requestExclusiveModeDisposeInput);
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
                    fastWindowManager.CreateMainWindow().Wait();

                    requestCompleteAppStartedDisposeInput.OnNext(false);
                    requestCompleteAppStartedDisposeInput.OnCompleted();
                    requestCompleteAppStartedDisposeInput.Dispose();

                    requestExclusiveModeDisposeInput.RegisterShellHook();

                    tokenApplicationStartedCallback?.Dispose();
                });
                CancellationTokenRegistration? tokenApplicationApplicationStopped = null;
                tokenApplicationApplicationStopped =
                host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
                {
                    disposablesDependencies.Dispose();
                    tokenApplicationApplicationStopped?.Dispose();
                });

                return ValueTask.FromResult(host);
            }

            private static TrayIcon CreateAnIconInTheNotificationArea(Dispatcher uiDispatcher) => uiDispatcher.Invoke(() => _ = new TrayIcon(App.GetResource(Resource.AppIcon).Stream));
            private static FastWindowViewModelDependencies CreateFastWindowViewModelDependencies(IGetImage imageProvider) => _ = new FastWindowViewModelDependencies(imageProvider);
        }
    }
}

