using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using SSHF.Infrastructure.Interfaces;

namespace SSHF.Infrastructure.Algorithms
{

    internal class FunctionGetTranslate: Freezable, IActionFunction
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

        internal string DeeplDirectory = @"C:\Users\Vikto\AppData\Local\DeepL\DeepL.exe";

        internal string ScreenshotReaderDirectory = @"D:\_MyHome\Требуется сортировка барахла\Portable ABBYY Screenshot Reader\ScreenshotReader.exe";

    }
}
