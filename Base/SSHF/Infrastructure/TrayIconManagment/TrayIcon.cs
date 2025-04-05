using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using FVH.SSHF.Infrastructure.TrayIconManagement;

using MahApps.Metro.Controls;

namespace FVH.SSHF.Infrastructure.TrayIconManagement
{
    internal partial class TrayIcon : IDisposable
    {
        private bool _disposed;
        private volatile bool _blockRepeatInvokeMessageBox = false;
        private readonly DPIIconHandler _dpiCorrector;
        private NotifyIcon _taskbarIcon;
        private NotifyIcon? _old;
        public TrayIcon(Stream resourceIcon, int[]? sizesIcon = null)
        {
            _dpiCorrector = new DPIIconHandler(resourceIcon, sizesIcon);
            _taskbarIcon = new NotifyIcon
            {
                Icon = _dpiCorrector.GetDefaultStartProcessIconDPI(),
                Visible = true
            };
            _dpiCorrector.ActualSizeIcon += ActualSizeIconLogic;
            _taskbarIcon.MouseDown += TaskbarIconMouseDownEvent;
        }
        private void TaskbarIconMouseDownEvent(object? sender, MouseEventArgs e) // суда может поасть поток DPI Handler
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if(_blockRepeatInvokeMessageBox is true) return;
                _blockRepeatInvokeMessageBox = true;
                MetroWindow dialogWindow = CreateDialogExit();
                dialogWindow.Closed += (_, __) => _blockRepeatInvokeMessageBox = false;

                dialogWindow.Show();
                _ = dialogWindow.Activate();
            });
        }
        public void Dispose()
        {
            if(_disposed is true) return;
            _dpiCorrector.Dispose();
            _taskbarIcon.Dispose();
            _old?.Dispose();
            _disposed = true;
        }
        private void ActualSizeIconLogic(object? _, System.Drawing.Icon newSizeIcon)
        {
            _taskbarIcon.MouseDown -= TaskbarIconMouseDownEvent;
            _old = _taskbarIcon;
            _old.Dispose();
            _taskbarIcon = new NotifyIcon
            {
                Icon = newSizeIcon,
                Visible = true
            };
            _taskbarIcon.MouseDown += TaskbarIconMouseDownEvent;
        }
        private MetroWindow CreateDialogExit()
        {
            MetroWindow dialogWindow = new MetroWindow()
            {
                Title = "Запрос SSHF",
                SizeToContent = SizeToContent.Manual,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                Topmost = true,
                AllowsTransparency = true,
                WindowTransitionsEnabled = false,

                Height = 200,
                Width = 300,

                TitleBarHeight = 32,
                TitleCharacterCasing = System.Windows.Controls.CharacterCasing.Normal,
                TitleForeground = new SolidColorBrush(new Color(){ R = 230, G= 230, B = 230, A = 255}),
                WindowTitleBrush = new SolidColorBrush(new Color(){ R = 42, G = 42, B = 42, A = 255 }),
                NonActiveWindowTitleBrush = null,

                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(new Color(){ R =215, G= 186, B = 125 , A = 72}),
                NonActiveBorderBrush = new SolidColorBrush(new Color(){ R = 197, G= 197, B = 197 , A = 72}),

                Background = new SolidColorBrush(new Color(){ R =42, G = 42, B = 42, A = 255 }),

                GlowBrush = null,
                NonActiveGlowBrush = null,

                Icon = new IconBitmapDecoder(Resource.AppIcon, BitmapCreateOptions.DelayCreation, BitmapCacheOption.Default).Frames[0]
            };

            DataTemplate template = new DataTemplate();
            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(TextBlock));

            factory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
            factory.SetValue(TextBlock.FontSizeProperty, 18.0);
            factory.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Segoe UI"));
            factory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            factory.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 0, 0));
            factory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            template.VisualTree = factory;
            dialogWindow.TitleTemplate = template;

            Grid grid = new Grid { Margin = new Thickness(5,0,5,0) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            grid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            grid.VerticalAlignment = VerticalAlignment.Center;

            TextBlock messageTextBlock = new TextBlock
            {
                Text = "Закрыть приложение?",
                HorizontalAlignment =  System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 30),
                FontSize = 24,
                FontFamily = new FontFamily("Segoe UI")
            };
            messageTextBlock.Foreground = new SolidColorBrush(new Color() { R = 240, G = 240, B = 240, A = 240 });
            Grid.SetRow(messageTextBlock, 0);
            _ = grid.Children.Add(messageTextBlock);
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Grid.SetRow(buttonPanel, 1);
            _ = grid.Children.Add(buttonPanel);
            System.Windows.Controls.Button yesButton = new System.Windows.Controls.Button
            {
                Content = "Да",
                Width = 105,
                Height = 40,
                Margin = new Thickness(5,5,18,5),
                IsTabStop = true,
                TabIndex = 1
            };
            yesButton.Click += (_, __) =>
            {
                System.Windows.Application.Current.Shutdown();
                dialogWindow.Close();
                _blockRepeatInvokeMessageBox = false;
            };
            _ = buttonPanel.Children.Add(yesButton);
            System.Windows.Controls.Button noButton = new System.Windows.Controls.Button
            {
                Content = "Нет",
                Width = 105,
                Height = 40,
                Margin = new Thickness(18,5,5,5),
                IsCancel = true,
                IsDefault = true,
                IsTabStop = true,
                TabIndex = 0
            };
            noButton.Click += (_, __) =>
            {
                dialogWindow.Close();
                _blockRepeatInvokeMessageBox = false;
            };
            _ = buttonPanel.Children.Add(noButton);

            dialogWindow.Content = grid;

            foreach(string uriResource in Resource.StandartUriPack) dialogWindow.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(uriResource) });

            dialogWindow.Loaded += (_, __) =>
            {
                WindowButtonCommands windowButtonCommands = dialogWindow.FindChild<WindowButtonCommands>();
                Style darkButtonStyle = (Style)dialogWindow.Resources["CustomDarkMetroWindowButtonStyle"];
                windowButtonCommands.LightCloseButtonStyle = darkButtonStyle;
                windowButtonCommands.DarkCloseButtonStyle = darkButtonStyle;

                _ = noButton.Focus();
            };


            return dialogWindow;
        }
    }
}