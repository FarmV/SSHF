using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Markup;

namespace SSHF.ViewModels.Base
{
    internal abstract class ViewModel : MarkupExtension, INotifyPropertyChanged, IDisposable
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => throw new NotImplementedException("Наследник базового класса не перопределил провайдера");


        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose()
        {
            Dispose(true);
        }
        private bool _Disposed;
        protected virtual void Dispose(bool Disposing)
        {
            if (!Disposing || _Disposed) return;
            _Disposed = true;
        }

        //~ViewModels()
        //{
        //    Dispose(false);
        //}

        protected virtual void OnProertyChanged([CallerMemberName] string ?PropertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string? PropertyName = null)
        {
            if(Equals(field, value)) return false;
            field = value;
            OnProertyChanged(PropertyName);
            return true;
        }
    }
}
    