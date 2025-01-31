using System;
using System.Threading.Tasks;



using FVH.Background.Input.Infrastructure.Interfaces;

using R3;

namespace FVH.SSHF
{
    public class KeyboardShortcut 
    {
        public KeyboardShortcut(VKeys[] keyCombo, Func<Task> callbackTask, object? identifier)
        {
            KeyCombo.Value = keyCombo;
            CallbackTask = callbackTask;
            Identifier = identifier;
        }
        public readonly BindableReactiveProperty<VKeys[]> KeyCombo = new BindableReactiveProperty<VKeys[]>([]);
        public Func<Task> CallbackTask
        {
            get;
            set;
        }
        public object? Identifier { get; set; }
    }
}
