using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    public interface IKeysNotifier
    {
        VKeys[] Keys { get; }
    }

}
