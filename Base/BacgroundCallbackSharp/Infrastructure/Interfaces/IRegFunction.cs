namespace FVH.Background.Input.Infrastructure.Interfaces
{
    public interface IRegFunction
    {
        Func<Task> CallbackTask { get; }
        object? Identifier { get; }
    }
}