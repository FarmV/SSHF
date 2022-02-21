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
    internal class FunctionScreenShot: Freezable , IActionFunction
    {

        string IActionFunction.Name => "ScreenShot";

        protected override Freezable CreateInstanceCore()
        {
            return new FunctionScreenShot();
        }

        bool IActionFunction.CheckFunction()
        {
            return true;
        }

        bool IActionFunction.StartFunction()
        {
            throw new NotImplementedException();
        }

        bool IActionFunction.СancelFunction()
        {
            return false;
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
