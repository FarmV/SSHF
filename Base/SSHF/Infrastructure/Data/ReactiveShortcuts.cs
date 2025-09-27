using System;
using System.Threading.Tasks;

using R3;

using FVH.Background.Input.Infrastructure.Interfaces;


namespace FVH.SSHF
{
    public class KeyboardShortcut 
    {
        public KeyboardShortcut(VKeys[] keyCombo, Func<ValueTask> callbackTask, object? identifier, Func<bool>? canExecute = null)
        {
            KeyCombo.Value = keyCombo;
            CallbackTask = callbackTask;
            Identifier = identifier;
            if(canExecute is null) CanExecute = static () => true;
            else { CanExecute = canExecute; }
        }
        public readonly BindableReactiveProperty<VKeys[]> KeyCombo = new BindableReactiveProperty<VKeys[]>([]);
        public Func<ValueTask> CallbackTask
        {
            get;
            set;
        }
        public object? Identifier { get; set; }
        public Func<bool> CanExecute { get; }
    }
}
