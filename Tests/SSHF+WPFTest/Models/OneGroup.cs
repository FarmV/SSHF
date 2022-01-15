using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF_WPFTest.Models
{
    internal class OneGroup
    {
        private List<OneGroupWindow> _oneGroups = new List<OneGroupWindow>();
       
        public bool AddWindow(OneGroupWindow window)
        {
            if (window is null || _oneGroups.Contains(window))  return false;
            else
            {
                _oneGroups.Add(window);
                return true;
            }


        }

        public bool RemoveWindow(OneGroupWindow window)
        {
            if (window is null) return false;
            if (_oneGroups.Contains(window)) 
            {
                if (!_oneGroups.Remove(window)) return false;
            }
            return true;
        }

        
        public OneGroup(/*params OneGroup[] groups*/)
        {

        }
       



        private string _PathImage = "/Views/Windows/MainWindowRes/F_Logo.png";
        public string PathImage
        {
            get { return _PathImage; }
            set {_PathImage = value; }
        }







    }
}
