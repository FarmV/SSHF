using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    public class DataKeysNotificator(VKeys[] keys) : IKeysNotifier
    {
        public VKeys[] Keys { get; } = keys;
    }

}
