using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

using SSHF.Infrastructure.Interfaces;

namespace SSHF.Infrastructure.Algorithms
{
    internal class FunctionScreenShot: Freezable 
    {

       /// string IActionFunction.Name => "ScreenShot";

        protected override Freezable CreateInstanceCore()
        {
            return new FunctionScreenShot();
        }

     


        private BitmapImage? _ImageScreen;

        

        public BitmapImage? Image
        {
            get
            {
                return _ImageScreen;
            }
            set
            {
                _ImageScreen = value;
            }
        }





    }
}
