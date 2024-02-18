using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FVH.SSHF.Infrastructure.Converters
{
    public class ConverterImageDPIViewport : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ImageSource imageSource)
            {
                ImageBrush brush = new ImageBrush(imageSource)
                {
                    Stretch = Stretch.Uniform,
                    ViewportUnits = BrushMappingMode.Absolute
                };
                DpiScale dpiScale = VisualTreeHelper.GetDpi(Application.Current.MainWindow);
                brush.Viewport = new Rect(0, 0, imageSource.Width / dpiScale.DpiScaleX, imageSource.Height / dpiScale.DpiScaleY);

                return brush;
            }
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
