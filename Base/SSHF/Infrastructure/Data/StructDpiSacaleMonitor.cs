namespace FVH.SSHF.Infrastructure
{
    public readonly struct DpiSacaleMonitor(double dpiScaleX, double dpiScaleY)
    {
        public double DpiScaleX { get; } = dpiScaleX;
        public double DpiScaleY { get; } = dpiScaleY;
    }
}

