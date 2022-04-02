﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using SSHF.Infrastructure;
using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.ViewModels.MainWindowViewModel;
using SSHF.Views.Windows.Notify;
using GlobalLowLevelHooks;
using System.Threading;
using Linearstar.Windows.RawInput;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Runtime.CompilerServices;
using SSHF.Infrastructure.SharedFunctions;
using System.Collections.Concurrent;
using System.Windows.Threading;
using SSHF.ViewModels;

namespace SSHF
{

    public partial class App: System.Windows.Application
    {

        internal static event EventHandler<SSHF.Infrastructure.Algorithms.Input.RawInputEvent>? Input;

        internal static event EventHandler? DPIChange;

        //  static bool _SingleCopy = default;

        internal static readonly DisplayRegistry RegistartorWindows = new DisplayRegistry();

        internal static bool IsDesignMode { get; private set; } = true;

        internal static int CheckCount = default;

        internal volatile static ConcurrentDictionary<string, KeyValuePair<Window, Dispatcher>> WindowsIsOpen = new ConcurrentDictionary<string, KeyValuePair<Window, Dispatcher>>();

        internal const string GetWindowNotification = "Нотификация окно - поток";
        internal const string GetMyMainWindow = "Главное окно приложения - поток";

        private static NotificatorViewModel? Notificator;



        internal static NotificatorViewModel GetNotificator()
        {
            if (Notificator is null) throw new NullReferenceException("Нотификатор не инициализирован");
            return Notificator;
        }

        // private static EventWaitHandle WaitTestThread = new EventWaitHandle(false, EventResetMode.ManualReset);

        static private Task StartConfigurations() => _ = Task.Run(() =>
         {
             Thread[] startInit = new Thread[]
             {
                 new Thread(() =>
                 {
                      Dispatcher dispThreadMainWindow = Dispatcher.CurrentDispatcher;
                      Thread.CurrentThread.Name = GetMyMainWindow;
                      MainWindow mainWindow = new MainWindow
                      {
                          Tag = GetMyMainWindow,
                          DataContext = new MainWindowViewModel()
                      };
                      mainWindow.Show();
                      if(WindowsIsOpen.TryAdd(GetMyMainWindow, new KeyValuePair<Window,Dispatcher>(mainWindow, dispThreadMainWindow)) is not true) throw new InvalidOperationException();
                      System.Windows.Threading.Dispatcher.Run();
                 }),
                 new Thread(() =>
                 {
                     Dispatcher dispThreadNotification = Dispatcher.CurrentDispatcher;

                     Thread.CurrentThread.Name = GetWindowNotification;
                     Notificator myNotification = new Notificator
                     {
                         Tag = GetWindowNotification,
                     };
                     NotificatorViewModel noti = new NotificatorViewModel();
                     Notificator = noti;
                     myNotification.DataContext = noti;

                     myNotification.Show();
                     myNotification.Hide();

                     TrayIcon icon = new TrayIcon();

                     if(WindowsIsOpen.TryAdd(GetWindowNotification, new KeyValuePair<Window,Dispatcher>(myNotification, dispThreadNotification)) is not true) throw new InvalidOperationException();

                     System.Windows.Threading.Dispatcher.Run();
                     //WaitTestThread.WaitOne();
                 })
             };
             //Array.ForEach(startInit, (x) =>
             //{ 
             //    x.SetApartmentState(ApartmentState.STA);
             //    x.Start();
             //});

             _ = Task.Run(() =>
             {
                 startInit.AsParallel().ForAll(x =>
                 {
                     x.SetApartmentState(ApartmentState.STA);
                     x.Start();
                 });

                 //Array.ForEach(startInit, (x) =>
                 //{
                 //    x.SetApartmentState(ApartmentState.STA);
                 //    x.Start();                     
                 //});
                 while (WindowsIsOpen.Count is not 2)
                 {

                 }

             }).ContinueWith(async t =>
             {
                 Dispatcher dispThreadMainWindow = WindowsIsOpen[GetMyMainWindow].Key.Dispatcher;
                 await dispThreadMainWindow.InvokeAsync(() =>
                 {
                     Thread dispMain = dispThreadMainWindow.Thread;
                     dispMain.Name = GetMyMainWindow;
                     Dispatcher disMainWindow = WindowsIsOpen[GetMyMainWindow].Key.Dispatcher;

                     Window mainWindow = WindowsIsOpen[GetMyMainWindow].Key;

                     if (WindowsIsOpen.TryUpdate(GetMyMainWindow, new KeyValuePair<Window, Dispatcher>(mainWindow, disMainWindow), WindowsIsOpen[GetMyMainWindow]) is not true) throw new InvalidOperationException();
                 }, DispatcherPriority.Render);



                 _ = WindowsIsOpen[GetMyMainWindow].Value.InvokeAsync(() =>
                 {
                     if (PresentationSource.FromVisual(WindowsIsOpen[GetMyMainWindow].Key) is not HwndSource source) throw new NullReferenceException("Не удалось получить HwndSource окна");

                     RawInputDeviceRegistration[] devices =
                     {
                             new RawInputDeviceRegistration(HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, source.Handle),
                             new RawInputDeviceRegistration(HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, source.Handle)
                     };

                     RawInputDevice.RegisterDevice(devices);
                     source.AddHook(WndProc);


                     static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                     {
                         const int WM_INPUT = 0x00FF;
                         const int WM_DPICHANGED = 0x02E0;
                         switch (msg)
                         {
                             case WM_INPUT:
                                 {                                   
                                     RawInputData ? data = RawInputData.FromHandle(lParam); // нельзя асинхронно
                                     _ = Task.Run(() => { Input?.Invoke(WindowsIsOpen[GetMyMainWindow].Key, new Infrastructure.Algorithms.Input.RawInputEvent(data)); });
                                 }
                                 break;
                             case WM_DPICHANGED:
                                 {
                                     _ = Task.Run(() => { DPIChange?.Invoke(new object(), EventArgs.Empty); }).ConfigureAwait(false);
                                 }
                                 break;
                         }
                         return hwnd;
                     }
                 }, DispatcherPriority.Render);

             }).ContinueWith((taskInTask) =>
             {
                 Dispatcher dispThreadMainWindow = WindowsIsOpen[GetWindowNotification].Key.Dispatcher;

                 _ = dispThreadMainWindow.InvokeAsync(() =>
                 {
                     Thread dispMain = dispThreadMainWindow.Thread;
                     dispMain.Name = GetMyMainWindow;
                     Dispatcher disMainWindow = WindowsIsOpen[GetWindowNotification].Key.Dispatcher;

                     Window mainWindow = WindowsIsOpen[GetWindowNotification].Key;

                     if (WindowsIsOpen.TryUpdate(GetWindowNotification, new KeyValuePair<Window, Dispatcher>(mainWindow, disMainWindow), WindowsIsOpen[GetWindowNotification]) is not true) throw new InvalidOperationException();
                 }, DispatcherPriority.Render);
             });
         });


        protected override void OnStartup(StartupEventArgs e)
        {
            Thread.CurrentThread.Name = "General stream!";
            IsDesignMode = false;
            // SystemParameters
            if (IsDesignMode is false)
            {
                _ = StartConfigurations();
            }
            base.OnStartup(e);

        }





    }
}
