using System;
using System.Threading.Tasks;

using ReactiveUI;

using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.SSHF
{
    public class KeyboardShortcut(VKeys[] keyCombo, Func<Task> callbackTask, object? identifier) : ReactiveUI.ReactiveObject
    {
        private VKeys[] _keyCombo = keyCombo;
        public VKeys[] KeyCombo
        {
            get => _keyCombo;
            set => this.RaiseAndSetIfChanged(ref _keyCombo, value);
        }
        public Func<Task> CallbackTask
        {
            get;
            set;
        } = callbackTask;
        public object? Identifier { get; set; } = identifier;
    }
}
