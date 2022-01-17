using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace SSHF_WPFTest
{
    
    

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CommandBinding binding = new CommandBinding(ApplicationCommands.Help);
            binding.Executed += new ExecutedRoutedEventHandler(binding_Executed);
            this.CommandBindings.Add(binding);
           
        }

        void binding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // MessageBox.Show("Команда 'New' была вызвана.");

            //this.box1.Content = this.Left.ToString();
            //this.box2.Content = this.Top.ToString();

            //Point myP = App.Current.MainWindow.PointToScreen(new Point(this.Left, this.Top));

            // Point myPa = new Point();
            Point myB = GetCursorPosition();
            this.box1.Content =$"Это Х : {myB.X}";
            this.box2.Content = $"Это Y : {myB.Y}";

        }


        //[DllImport("user32.dll",CallingConvention = CallingConvention.StdCall,SetLastError = false)]
        //public static extern Point GetCursorPos(out Point CursorPoint);



        //[StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll",CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static Point GetCursorPosition()
        {
            GetCursorPos(out POINT lpPoint);
            return lpPoint;
        }

    }
}
