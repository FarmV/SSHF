using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SSHF.Infrastructure
{
    internal class DisplayRegistry
    {
        internal Dictionary<Type, Type> vmToWindowMapping = new Dictionary<Type, Type>();

        internal void RegisterWindowType<VM, Win>() where Win : Window, new() where VM : class
        {
            Type vmType = typeof(VM);
            if (vmType.IsInterface) throw new ArgumentException("Невозможно зарегистрировать интерфейс");
            if (vmToWindowMapping.ContainsKey(vmType)) throw new InvalidOperationException($"Type {vmType.FullName} уже зарегистрирован");
            vmToWindowMapping[vmType] = typeof(Win);
        }

        internal void UnregisterWindowType<VM>()
        {
            var vmType = typeof(VM);
            if (vmType.IsInterface) throw new ArgumentException("VM не может быть интерфейсом");
            if (!vmToWindowMapping.ContainsKey(vmType)) throw new InvalidOperationException($"Type {vmType.FullName} не зарегистрирован");
            vmToWindowMapping.Remove(vmType);
        }

        internal Window CreateWindowInstanceWithVM(object vm)
        {
            if (vm is null) throw new ArgumentNullException(nameof(vm),"VM была NULL");
           
            Type? windowType = null;

            Type? vmType = vm.GetType();

            while (vmType is not null && !vmToWindowMapping.TryGetValue(vmType, out windowType)) vmType = vmType.BaseType;

            if (windowType is null) throw new ArgumentException($"Не зарегистрирован тип окна для типа аргумента {vm.GetType().FullName}");

            if (Activator.CreateInstance(windowType) is not Window window) throw new ArgumentNullException(nameof(window), "window");

            window.DataContext = vm;
            return window;
        }

        internal readonly Dictionary<object, Window> openWindows = new Dictionary<object, Window>();
        internal void PresentationON(object vm)
        {
            if (vm is null) throw new ArgumentNullException(nameof(vm), "VM is null");
            if (openWindows.ContainsKey(vm)) throw new InvalidOperationException("Пользовательский интерфейс для этой ViewModel уже отображается");
            var window = CreateWindowInstanceWithVM(vm);
            window.Show();
            openWindows[vm] = window;
        }

        internal void CloseAndRemovePresentation(object vm)
        {
            if (!openWindows.TryGetValue(vm, out Window? window)) throw new InvalidOperationException("Пользовательский интерфейс для ViewModel не отображается");
            window.Close();
            openWindows.Remove(vm);
        }

        internal bool HideView(object vm)
        {
            if (!openWindows.TryGetValue(vm, out Window? window)) return false;
            window.Hide();
            return true;
        }
        internal bool ShowView(object vm)
        {
            if (!openWindows.TryGetValue(vm, out Window? window)) return false;
            window.Show();
            return true;
        }

        internal Window? GetWindow(object View)
        {
            if (!openWindows.TryGetValue(View, out Window? window)) return null;
            return window;
        }
    

        public async Task ShowModalPresentation(object vm)
        {
            Window window = CreateWindowInstanceWithVM(vm);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            await window.Dispatcher.InvokeAsync(() => window.ShowDialog());
        }


    }
}
