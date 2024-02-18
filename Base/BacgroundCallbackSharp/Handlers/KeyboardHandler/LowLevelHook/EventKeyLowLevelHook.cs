using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    internal partial class LowLevelKeyHook
    {
        internal class EventKeyLowLevelHook(VKeys key, bool breakKey = false)
        {
            internal VKeys Key { get; init; } = key;
            /// <summary>
            /// KeyUpEvent - ignore Break
            /// </summary>
            internal bool Break { get; set; } = breakKey;
        }
    }
}

