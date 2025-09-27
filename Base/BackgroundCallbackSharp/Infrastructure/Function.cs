using FVH.Background.Input.Infrastructure.Interfaces;

using static ABI.System.Windows.Input.ICommand_Delegates;

namespace FVH.Background.Input.Infrastructure
{
    public sealed record Function 
    {
        internal Function(Func<ValueTask> callback, object? identifier = null, Func<bool>? canExecute = null)
        {
            Callback = callback;
            Identifier = identifier;
            if(canExecute is null) CanExecute = static () => true;
            else { CanExecute = canExecute; }
        }
        public object? Identifier { get; }
        public Func<ValueTask> Callback { get; }
        public Func<bool> CanExecute { get; }
    }
}
