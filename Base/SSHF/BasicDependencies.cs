﻿using System;
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
using FVH.SSHF.ViewModels.MainWindowViewModel;
using FVH.SSHF.Windows.MainWindow;
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
                Dispatcher uiDispatcher = GetWPFUIDispatcher(uiThread);

                uiDispatcher.Invoke(() =>
                {
                    RxApp.MainThreadScheduler = System.Reactive.Concurrency.CurrentThreadScheduler.Instance;

                    container.AddSingleton<Dispatcher>(uiDispatcher);

                    Input input = CreateHandlerInput(uiDispatcher);
                    _compositeDisposable.Add(input);

                    container.AddSingleton<Input>(input);

                    container.AddSingleton<IGetImage>(CreateImageProvider());

                    container.AddSingleton<MainWindow>(CreateMainWindow(uiDispatcher));

                    WPFDropImageFile wpfDropImageFile = new WPFDropImageFile(container.BuildServiceProvider().GetRequiredService<MainWindow>());
                    _compositeDisposable.Add(wpfDropImageFile);

                    container.AddSingleton<WPFDropImageFile>(wpfDropImageFile);
                 

                    container.AddSingleton<MainWindowViewModel>
                    (
                     CreateMainWindowViewModel
                     (
                        imageProvider: CreateImageProvider(),
                        windowPositionUpdater: CreatePositionUpdaterWin32WPF
                        (
                         window: container.BuildServiceProvider().GetRequiredService<MainWindow>()),
                         corrector: new DpiCorrector
                         (
                          window: container.BuildServiceProvider().GetRequiredService<MainWindow>(),
                          dispatcher: container.BuildServiceProvider().GetRequiredService<Dispatcher>()
                         ),
                        setImage: container.BuildServiceProvider().GetRequiredService<WPFDropImageFile>()
                      )
                    );

                    SetDataContextMainWindow
                    (
                     container.BuildServiceProvider().GetRequiredService<MainWindow>(),
                     container.BuildServiceProvider().GetRequiredService<MainWindowViewModel>()
                    );

                    TrayIcon trayIcon = CreateAnIconInTheNotificationArea();
                    _compositeDisposable.Add(trayIcon);

                    container.AddSingleton<TrayIcon>(trayIcon);

                    container.AddSingleton<MainWindowExternalConditions>
                    (
                     CreateMainWindowExternalConditions
                     (
                      container.BuildServiceProvider().GetRequiredService<MainWindowViewModel>(),
                      container.BuildServiceProvider().GetRequiredService<Input>().GetKeyboardHandler()
                     )
                    );

                    container.AddSingleton<MainWindowCommand>
                    (
                     CreateMainWindowCommand
                     (
                      container.BuildServiceProvider().GetRequiredService<MainWindow>(),
                      container.BuildServiceProvider().GetRequiredService<MainWindowViewModel>()
                     )
                    );

                    if(args is not null)
                    {
                        if(args.SingleOrDefault(x => x == "--SCR_NotBR") is not null)
                        {
                            MainWindowCommand mainWindowCommand = container.BuildServiceProvider().GetRequiredService<MainWindowCommand>();
                            Shortcuts[] defaultShortcuts = mainWindowCommand.GetDefaultShortcuts();
                            mainWindowCommand.SetNewShortcuts(defaultShortcuts.Where(x => x != defaultShortcuts.Single(x => x.KeyCombo[0] == VKeys.VK_SCROLL)).ToArray());
                        }
                    }

                    container.AddSingleton<ShortcutsProvider>
                    (
                     CreateShortcutsManager
                     (
                      container.BuildServiceProvider().GetRequiredService<Input>().GetKeyboardCallbackFunction(),
                      [container.BuildServiceProvider().GetRequiredService<MainWindowCommand>()]
                     )
                    );
                });
            }).Build();
            private static Dispatcher GetWPFUIDispatcher(Thread uiThread) => Dispatcher.FromThread(uiThread) is not Dispatcher uiDispatcher ? throw new InvalidOperationException() : uiDispatcher;
            private static IWindowPositionUpdater CreatePositionUpdaterWin32WPF(Window window) => new Win32WPFWindowPositionUpdater(window);
            private static IGetImage CreateImageProvider() => new ImageProvider();
            private static MainWindowViewModel CreateMainWindowViewModel(IGetImage imageProvider, IWindowPositionUpdater windowPositionUpdater, DpiCorrector corrector, WPFDropImageFile setImage) =>
                           new MainWindowViewModel(imageProvider, windowPositionUpdater, corrector, setImage);
            private static void SetDataContextMainWindow(Window window, MainWindowViewModel mainWindowViewModel)
            {
                window.DataContext = mainWindowViewModel;
                ((IViewFor)window).ViewModel = mainWindowViewModel;
            }
            private static MainWindow CreateMainWindow(Dispatcher? uiDispatcher = null)
            {
                uiDispatcher ??= System.Windows.Application.Current.Dispatcher;

                MainWindow? mainWindow = null;
                uiDispatcher.Invoke(() =>
                {
                    mainWindow = new MainWindow();
                    mainWindow.Show();
                });
                if(mainWindow is null) throw new NullReferenceException();

                return mainWindow;
            }
            private static TrayIcon CreateAnIconInTheNotificationArea()
            {
                TrayIcon trayIcon = new TrayIcon(App.GetResource(Resource.AppIcon).Stream);
                return trayIcon;
            }
            private static Input CreateHandlerInput(Dispatcher? uiDispatcher = null)
            {
                uiDispatcher ??= System.Windows.Application.Current.Dispatcher;

                Input? inputHandler = null;
                uiDispatcher.Invoke(() => inputHandler = new Input());
                return inputHandler ?? throw new NullReferenceException();
            }
            private static ShortcutsProvider CreateShortcutsManager(IKeyboardCallback keyboardCallback, IEnumerable<IInvokeShortcuts> listFunc) => new ShortcutsProvider(keyboardCallback, listFunc);
            private static MainWindowCommand CreateMainWindowCommand(Window window, MainWindowViewModel viewModel) => new MainWindowCommand(window, viewModel);
            private static MainWindowExternalConditions CreateMainWindowExternalConditions(MainWindowViewModel mainWindowViewModel, IKeyboardHandler keyboardHandler) => new MainWindowExternalConditions(mainWindowViewModel, keyboardHandler);
           
        }
    }
}

