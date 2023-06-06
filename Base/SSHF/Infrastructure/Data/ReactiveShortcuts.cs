using FVH.Background.Input;

using ReactiveUI;

using System;
using System.Threading.Tasks;

namespace SSHF
{
    public class Shortcuts : ReactiveUI.ReactiveObject
    {
        private VKeys[] _keyCombo;
        public Shortcuts(VKeys[] keyCombo, Func<Task> callbackTask, object? identifier)
        {
            _keyCombo = keyCombo;
            CallbackTask = callbackTask;
            Identifier = identifier;
        }

        public VKeys[] KeyCombo
        {
            get => _keyCombo;
            set => this.RaiseAndSetIfChanged(ref _keyCombo, value);
        }
        public Func<Task> CallbackTask 
        { 
            get; 
            set; 
        }
        public object? Identifier { get; set; }
    }
}
