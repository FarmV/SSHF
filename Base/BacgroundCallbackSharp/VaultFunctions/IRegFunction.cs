namespace FVH.Background.Input
{
    public interface IRegFunction
    {
        Func<Task> CallBackTask { get; }
        object? Identifier { get; }
    }
}