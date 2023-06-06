namespace SSHF.Infrastructure
{
    public readonly struct DpiSacaleMonitor
    {
        public DpiSacaleMonitor(double dpiScaleX, double dpiScaleY)
        {
            DpiScaleX = dpiScaleX;
            DpiScaleY = dpiScaleY;
        }
        public double DpiScaleX { get; }
        public double DpiScaleY { get; }
    }
}

