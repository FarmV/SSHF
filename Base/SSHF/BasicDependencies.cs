using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI;

using FVH.Background.Input;
using FVH.Background.Input.Infrastructure.Interfaces;

using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.TrayIconManagment;
using System.Reactive.Disposables;


namespace FVH.SSHF
{
    internal partial class App
    {
        private class BasicDependencies : IDisposable
        {
            private readonly CompositeDisposable _compositeDisposable = new CompositeDisposable();

            public BasicDependencies() { }

            public void Dispose()
            {
                if(_compositeDisposable.IsDisposed is true) return;
                _compositeDisposable.Dispose();
                GC.SuppressFinalize(this);
            }

            internal IHost ConfigureDependencies(Thread uiThread, string[]? args = null) => Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((_, configuration) =>
            { configuration.Sources.Clear(); }).ConfigureServices((_, container) =>
            {
                Dispatcher uiDispatcher = GetWPFUiDispatcher(uiThread);

                uiDispatcher.Invoke(() => RxApp.MainThreadScheduler = System.Reactive.Concurrency.CurrentThreadScheduler.Instance);

                Input input = uiDispatcher.Invoke(() => CreateHandlerInput(uiDispatcher));
                _compositeDisposable.Add(input);

                IGetImage imageProvider = uiDispatcher.Invoke(CreateImageProvider);

                FastWindowManager fastWindowManager = uiDispatcher.Invoke(() => new FastWindowManager(uiDispatcher, () => CreateFastWindowViewModelDependencies(imageProvider), input.GetKeyboardHandler()));
                _compositeDisposable.Add(fastWindowManager);

                fastWindowManager.Initialize();

                TrayIcon trayIcon = uiDispatcher.Invoke(() => CreateAnIconInTheNotificationArea());
                _compositeDisposable.Add(trayIcon);
               


                if(args is not null)
                {
                    if(args.SingleOrDefault(x => x == "--SCR_NotBR") is not null)
                    {
                        FastWindowManager fastWindowCommandManager = container.BuildServiceProvider().GetRequiredService<FastWindowManager>();
                        Shortcuts[] defaultShortcuts = fastWindowCommandManager.GetDefaultShortcuts();
                        fastWindowCommandManager.SetNewShortcuts(defaultShortcuts.Where(x => x != defaultShortcuts.Single(x => x.KeyCombo[0] == VKeys.VK_SCROLL)).ToArray());
                    }
                }

                container.AddSingleton<ShortcutsProvider>
                (
                 CreateShortcutsManager
                 (
                  input.GetKeyboardCallbackFunction(),
                  [fastWindowManager]
                 )
                );

//                uiDispatcher.Invoke(() =>
//                {
//                    RxApp.MainThreadScheduler = System.Reactive.Concurrency.CurrentThreadScheduler.Instance;

//                    container.AddSingleton<Dispatcher>(uiDispatcher);

//                    Input input = CreateHandlerInput(uiDispatcher);
//                    _compositeDisposable.Add(input);

//                    container.AddSingleton<Input>(input);

//                    container.AddSingleton<IGetImage>(CreateImageProvider());

//                    container.AddSingleton<FastWindowManager>
//                    (
//                     CreateFastWindowManager
//                     (
//                      container.BuildServiceProvider().GetRequiredService<Dispatcher>(),
//                      () => CreateFastWindowViewModelDependencies
//                            (
//                             container.BuildServiceProvider().GetRequiredService<IGetImage>(),
//                             container.BuildServiceProvider().GetRequiredService<IWindowPositionUpdater>()
//                            ),
//                      input.GetKeyboardHandler()
//                     )
//                    );

//                    _compositeDisposable.Add(container.BuildServiceProvider().GetRequiredService<FastWindowManager>());                 
           
//                    container.AddSingleton<TrayIcon>(CreateAnIconInTheNotificationArea());
//                    _compositeDisposable.Add(container.BuildServiceProvider().GetRequiredService<TrayIcon>());

//                    if(args is not null)
//                    {
//                        if(args.SingleOrDefault(x => x == "--SCR_NotBR") is not null)
//                        {
//                            FastWindowManager fastWindowCommandManager = container.BuildServiceProvider().GetRequiredService<FastWindowManager>();
//                            Shortcuts[] defaultShortcuts = fastWindowCommandManager.GetDefaultShortcuts();
//                            fastWindowCommandManager.SetNewShortcuts(defaultShortcuts.Where(x => x != defaultShortcuts.Single(x => x.KeyCombo[0] == VKeys.VK_SCROLL)).ToArray());
//                        }
//                    }

//                    FastWindowManager fastWindowManager = container.BuildServiceProvider().GetRequiredService<FastWindowManager>();

//                    System.Threading.Tasks.Task _ = fastWindowManager.Initialize();


//#if DEBUG
//                    System.Diagnostics.Debugger.Break();
//#endif

//                    container.AddSingleton<ShortcutsProvider>
//                    (
//                     CreateShortcutsManager
//                     (
//                      container.BuildServiceProvider().GetRequiredService<Input>().GetKeyboardCallbackFunction(),
//                      [container.BuildServiceProvider().GetRequiredService<FastWindowManager>()]
//                     )
//                    );
//                });
            }).Build();
            private static Dispatcher GetWPFUiDispatcher(Thread uiThread) => Dispatcher.FromThread(uiThread) is not Dispatcher uiDispatcher ? throw new InvalidOperationException() : uiDispatcher;
          //  private static IWindowPositionUpdater CreatePositionUpdaterWin32WPF(Window window) => new Win32WPFWindowPositionUpdater(window);
            private static IGetImage CreateImageProvider() => new ImageProvider();
            private static TrayIcon CreateAnIconInTheNotificationArea()
            {
                TrayIcon trayIcon = new TrayIcon(App.GetResource(Resource.AppIcon).Stream);
                return trayIcon;
            }
            private static Input CreateHandlerInput(Dispatcher uiDispatcher)
            {
                Input inputHandler = uiDispatcher.Invoke(() =>  new Input());
                ArgumentNullException.ThrowIfNull(inputHandler, nameof(inputHandler));
                return inputHandler;
            }
            private static ShortcutsProvider CreateShortcutsManager(IKeyboardCallback keyboardCallback, IEnumerable<IInvokeShortcuts> listFunc) => new ShortcutsProvider(keyboardCallback, listFunc);
            private static FastWindowViewModelDependencies CreateFastWindowViewModelDependencies(IGetImage imageProvider) => new FastWindowViewModelDependencies(imageProvider);
            private static FastWindowManager CreateFastWindowManager(Dispatcher dispatcher, Func<FastWindowViewModelDependencies> fastWindowViewModelDependencies, IKeyboardHandler keyboardHandler) => new FastWindowManager(dispatcher, fastWindowViewModelDependencies, keyboardHandler);
            //private static FastWindowViewModel CreateMainWindowViewModel(IGetImage imageProvider, IWindowPositionUpdater windowPositionUpdater, DpiCorrector corrector, WPFDropImageFile setImage) =>
            //               new FastWindowViewModel(imageProvider, windowPositionUpdater, corrector, setImage);
            //private static void SetDataContextMainWindow(Window window, FastWindowViewModel mainWindowViewModel)
            //{
            //    window.DataContext = mainWindowViewModel;
            //    ((IViewFor)window).ViewModel = mainWindowViewModel;
            //}
            //private static FastWindow CreateMainWindow(Dispatcher? uiDispatcher = null)
            //{
            //    uiDispatcher ??= System.Windows.Application.Current.Dispatcher;

            //    FastWindow? mainWindow = null;
            //    uiDispatcher.Invoke(() =>
            //    {
            //        mainWindow = new FastWindow();
            //        mainWindow.Show();
            //    });
            //    if(mainWindow is null) throw new NullReferenceException();

            //    return mainWindow;
            //}
      //      private static FastWindowCommand CreateMainWindowCommand(Window window, FastWindowViewModel viewModel) => new FastWindowCommand(window, viewModel);
      //      private static FastWindowExternalConditions CreateMainWindowExternalConditions(FastWindowViewModel mainWindowViewModel, IKeyboardHandler keyboardHandler) => new FastWindowExternalConditions(mainWindowViewModel, keyboardHandler);



        }
    }
}

