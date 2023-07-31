using FVH.Background.Input;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ReactiveUI;

using SSHF.Infrastructure;
using SSHF.Infrastructure.Interfaces;
using SSHF.ViewModels;
using SSHF.ViewModels.MainWindowViewModel;
using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.Views.Windows.Notify;



using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SSHF
{
    internal partial class App
    {
        private static class BasicDependencies
        {
            internal static IHost ConfigureDependencies(Thread uiThread, string[]? args = null) => Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((_, configuration) =>
            { configuration.Sources.Clear(); }).ConfigureServices((_, container) =>
            {
                Dispatcher uiDispatcher = GetWPFUIDispatcher(uiThread);

                uiDispatcher.Invoke(() =>
                {
                    RxApp.MainThreadScheduler = System.Reactive.Concurrency.CurrentThreadScheduler.Instance;

                    container.AddSingleton<Dispatcher>(uiDispatcher);

                    container.AddSingleton<Input>(CreateHadnlerInput(uiDispatcher));

                    container.AddSingleton<IGetImage>(CreateImageProvider());

                    container.AddSingleton<MainWindow>(CreateMainWindow(uiDispatcher));

                    container.AddSingleton<WPFDropImageFile>(
                        new WPFDropImageFile(container.BuildServiceProvider().GetRequiredService<MainWindow>(), 
                        container.BuildServiceProvider().GetRequiredService<Dispatcher>()
                        ));

                    container.AddSingleton<MainWindowViewModel>(CreateMainWindowViewModel(
                        imageProvider:CreateImageProvider(),
                        windowPositionUpdater:CreatePositionUpdaterWin32WPF( 
                            window: container.BuildServiceProvider().GetRequiredService<MainWindow>()),
                        corrector: new DpiCorrector( 
                            window: container.BuildServiceProvider().GetRequiredService<MainWindow>(), 
                            dispatcher: container.BuildServiceProvider().GetRequiredService<Dispatcher>()),
                        setImage:container.BuildServiceProvider().GetRequiredService<WPFDropImageFile>()
                        ));

                    SetDataContextMainWindow(
                        container.BuildServiceProvider().GetRequiredService<MainWindow>(),
                        container.BuildServiceProvider().GetRequiredService<MainWindowViewModel>()
                        );
                 
                    container.AddSingleton<Notificator>(
                        CreateAnIconInTheNotificationArea(container.BuildServiceProvider().GetRequiredService<Input>().GetMouseHandler(), ref container) //todo исправить затычку
                        );

                    container.AddSingleton<MainWindowCommand>(
                        CreateMainWindowCommand(
                          container.BuildServiceProvider().GetRequiredService<MainWindow>(),
                        container.BuildServiceProvider().GetRequiredService<MainWindowViewModel>(), 
                        container.BuildServiceProvider().GetRequiredService<Input>().GetKeyboardHandler())
                        );

                    container.AddSingleton<ShortcutsProvider>(
                              CreateShortcutsManager(
                                                     container.BuildServiceProvider().GetRequiredService<Input>().GetKeyboardCallbackFunction(),
                                                               new IInvokeShortcuts[]
                                                               {
                                                                   container.BuildServiceProvider().GetRequiredService<MainWindowCommand>()
                                                               }));
                });
            }).Build();
            private static Dispatcher GetWPFUIDispatcher(Thread uiThread)  => Dispatcher.FromThread(uiThread) is not Dispatcher uiDispatcher ? throw new InvalidOperationException() : uiDispatcher;
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
                if (mainWindow is null) throw new NullReferenceException();

                return mainWindow;
            }
            private static Notificator CreateAnIconInTheNotificationArea(IMouseHandler mouseHandler, ref IServiceCollection collection)
            {
                Notificator notificationTrayIconWindow = new Notificator();
                TrayIconManager trayIconManager = new TrayIconManager(notificationTrayIconWindow);
                collection.AddSingleton(trayIconManager);
                notificationTrayIconWindow.DataContext = new NotificatorViewModel(notificationTrayIconWindow, trayIconManager, mouseHandler);
                return notificationTrayIconWindow;
            }
            private static Input CreateHadnlerInput(Dispatcher? uiDispatcher = null)
            {
                uiDispatcher ??= System.Windows.Application.Current.Dispatcher;

                Input? inputHendler = null;
                uiDispatcher.Invoke(() => inputHendler = new Input());
                return inputHendler ?? throw new NullReferenceException();
            }
            private static ShortcutsProvider CreateShortcutsManager(IKeyboardCallback keyboardCallback, IEnumerable<IInvokeShortcuts> listFunc) => new ShortcutsProvider(keyboardCallback, listFunc);
            private static MainWindowCommand CreateMainWindowCommand(Window window,MainWindowViewModel viewModel, IKeyboardHandler keyboardHandler) => new MainWindowCommand(window,viewModel, keyboardHandler);
        }
    }
}

