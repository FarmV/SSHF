using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input.Infrastructure
{
    public record Function 
    {
        internal Function(Func<Task> callback, object? identifier = null)
        {
            Callback = callback;
            Identifier = identifier;
        }
        public object? Identifier { get; }
        public Func<Task> Callback { get; }
    }
}
