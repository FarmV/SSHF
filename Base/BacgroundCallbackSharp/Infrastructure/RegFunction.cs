using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input.Infrastructure
{
    public record RegFunction : IRegFunction
    {
        internal RegFunction(Func<Task> callBackTask, object? identifier = null)
        {
            CallbackTask = callBackTask;
            Identifier = identifier;
        }
        public object? Identifier { get; }
        public Func<Task> CallbackTask { get; }
    }
}
