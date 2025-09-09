using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using FVH.Background.Input.Infrastructure.Interfaces;
using FVH.SSHF.FastWindowArea;
using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Input;
using FVH.SSHF.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.TrayIconManagement;
using FVH.SSHF.Infrastructure.Win32;
using FVH.SSHF.Infrastructure.Win32.FVH.SSHF.Infrastructure.Win32;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using R3;


namespace FVH.SSHF
{
    internal partial class App
    {
        private class BasicDependencies 
        {
            internal BasicDependencies() { }
            internal static ValueTask<IHost> ConfigureDependencies(Thread uiThread, string[]? args = null)
            {
                if(Dispatcher.FromThread(uiThread) is not Dispatcher uiDispatcher) ThrowNotDispatcher(); [DoesNotReturn] static void ThrowNotDispatcher() => throw new InvalidOperationException();

                TheThreadWorkerContext theThreadWorkerContext = new TheThreadWorkerContext(); // IAsyncDisposable

                ObserverExclusiveMode observerExclusiveMode = new ObserverExclusiveMode(uiDispatcher);
                ObserverMsScreenClipExecuting observerMsScreenClipExecuting = new ObserverMsScreenClipExecuting();
                ShellHookPriorityHandlers shellHookPriorityHandlers = new ShellHookPriorityHandlers(observerExclusiveMode, observerMsScreenClipExecuting);
                HookManager win32HookManager = new HookManager(uiDispatcher,shellHookPriorityHandlers);

                KeyboardHookStateAggregator keyboardHookStateAggregator = new KeyboardHookStateAggregator(observerExclusiveMode.IsInExclusiveMode);

                R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>? listIInvokeShortcutsBehaviorSubject = null;

                Func<BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>> delegateListIInvokeShortcutsBehaviorSubject =
                 new Func<R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>>(() => listIInvokeShortcutsBehaviorSubject!);

                WaitingInputProvider? waitingInputProvider = new WaitingInputProvider(uiDispatcher, keyboardHookStateAggregator.HookCanBeActive, delegateListIInvokeShortcutsBehaviorSubject,theThreadWorkerContext.Context);

                ImageProvider ImageProvider = new ImageProvider();
                FastWindowManager fastWindowManager = uiDispatcher.Invoke(
                 () => _ = new FastWindowManager(uiDispatcher, () => _ = CreateFastWindowViewModelDependencies(ImageProvider), waitingInputProvider, observerExclusiveMode.IsInExclusiveMode, observerMsScreenClipExecuting));
                if(args?.Length > 0)
                {
                    if(args.SingleOrDefault(x => x == "--SCR_NotBR") is not null)
                    {
                        KeyboardShortcut[] defaultShortcuts = fastWindowManager.GetDefaultShortcuts();
                        fastWindowManager.SetNewShortcuts(defaultShortcuts.Where(x => x != defaultShortcuts.Single(x => x.KeyCombo.CurrentValue[0] == VKeys.VK_SCROLL)).ToArray());
                    }
                }

                listIInvokeShortcutsBehaviorSubject = new BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>([fastWindowManager]);

                TrayIcon trayIcon = CreateAnIconInTheNotificationArea(uiDispatcher);

                IHost host = Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((_, configuration) =>
                {   configuration.Sources.Clear(); }).ConfigureServices((__, container) =>
                    {
                        _ = container.AddSingleton<Dispatcher>(uiDispatcher);
                        _ = container.AddSingleton<KeyboardHookStateAggregator>(keyboardHookStateAggregator);
                        _ = container.AddSingleton<ImageProvider>(ImageProvider);
                        _ = container.AddSingleton<FastWindowManager>(fastWindowManager);
                        _ = container.AddSingleton<WaitingInputProvider>(waitingInputProvider);
                        _ = container.AddSingleton<TrayIcon>(trayIcon);

                        _ = container.AddSingleton<ObserverExclusiveMode>(observerExclusiveMode);
                        _ = container.AddSingleton<HookManager>(win32HookManager);
                    }).Build();

                CompositeDisposable disposablesDependencies =
                [
                     keyboardHookStateAggregator,
                     observerExclusiveMode,
                     fastWindowManager,
                     waitingInputProvider,
                     trayIcon,
                     win32HookManager
                ];

                CancellationTokenRegistration? tokenApplicationStartedCallback = null;
                tokenApplicationStartedCallback =
                 host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() =>
                 {
                     FastWindow mainWindow = fastWindowManager.CreateMainWindow().GetAwaiter().GetResult();

                     _ = uiDispatcher.Invoke(() => System.Windows.Application.Current.MainWindow = mainWindow);
                   
                     uiDispatcher.Invoke(waitingInputProvider.RegisterShortcuts);

                     keyboardHookStateAggregator.NotifyAppInitialized();

                     win32HookManager.RegisterShellHook();

                     tokenApplicationStartedCallback?.Dispose();
                 });

                CancellationTokenRegistration? tokenApplicationApplicationStopped = null;

                tokenApplicationApplicationStopped = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
                {
                    static void DisposePostAppCleanerAsync(IAsyncDisposable[] disposables)
                    {
                        if(disposables.Length is 0) return;
                        Thread threadCleaner = new Thread(() =>
                        {
                            Task[] disposeTasks = Array.ConvertAll(disposables, (IAsyncDisposable d) => d.DisposeAsync().AsTask());

                            try { Task.WaitAll(disposeTasks); }
                            catch (AggregateException) { }

                            List<System.Exception>? exceptions = null;

                            Array.ForEach(disposeTasks, (Task task) =>
                            {
                                if (task.IsFaulted && task.Exception is not null)
                                {
                                    exceptions ??= new List<System.Exception>();
                                    exceptions.AddRange(task.Exception.InnerExceptions);
                                }
                            });
                            if (exceptions is not null) Throw(exceptions); [DoesNotReturn] static void Throw(List<System.Exception> listException) => throw new AggregateException(listException);
                        })
                        { IsBackground = false, Name = $"FVH: PostAsyncCleaner" };
                        threadCleaner.UnsafeStart();
                    }
                    
                    disposablesDependencies.Dispose();
                    tokenApplicationApplicationStopped?.Dispose();
                    DisposePostAppCleanerAsync([theThreadWorkerContext]);
                });

                return ValueTask.FromResult(host);
            }
            private static TrayIcon CreateAnIconInTheNotificationArea(Dispatcher uiDispatcher) => uiDispatcher.Invoke(() => _ = new TrayIcon(App.GetResource(Resource.AppIcon).Stream));
            private static FastWindowViewModelDependencies CreateFastWindowViewModelDependencies(ImageProvider imageProvider) => _ = new FastWindowViewModelDependencies(imageProvider);
        }
    }
}