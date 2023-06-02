using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SSHF.Windows
{
    internal class Manager
    {
        private Dictionary<string, Window> _keyValuePairs = new Dictionary<string, Window>();
        public Manager()
        {
        }

       private void SetWindows(Window window)
       {
            _keyValuePairs[window.Title] = window;          
       }
        private Window GetWindow(string key)
        {
            return _keyValuePairs[key];
        }
    }
}
