using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    public interface IKeysNotificator
    {
        VKeys[] Keys { get; }
    }

}
