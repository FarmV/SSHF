using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SSHF_.ViewModels.Base
{
    internal abstract class ViewModels : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;



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
    