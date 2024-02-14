namespace FVH.Background.Input
{
    internal partial class LowLevlKeyHook
    {
        private enum WMEvent : uint
        {
            WM_KEYDOWN = 256,
            WM_SYSKEYDOWN = 260,
            WM_KEYUP = 257,
            WM_SYSKEYUP = 261
        }
    }
}

