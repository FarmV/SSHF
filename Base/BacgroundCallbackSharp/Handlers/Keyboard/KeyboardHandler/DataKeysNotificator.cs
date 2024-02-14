using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    public class DataKeysNotificator(VKeys[] keys) : IKeysNotificator
    {
        public VKeys[] Keys { get; } = keys;
    }

}
