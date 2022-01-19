using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SSHF.ViewModels.Base
{
    internal abstract class ViewModel : INotifyPropertyChanged, IDisposable
    {
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
    