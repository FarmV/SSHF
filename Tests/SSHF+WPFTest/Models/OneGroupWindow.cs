using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF_WPFTest.Models
{
    internal class OneGroupWindow
    {
        public OneGroupWindow(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(path, "Путь к файлу не может быть пустым");
                       
           _Path = path;
           
        }

        private string _Path;
        public string PathBackground
        {
            get => _Path;
            set => _Path = value;
        }

        private int _X;

        public int XPosition
        {
            get { return _X; }
            set { _X = value; }
        }

        private int _Y;

        public int YPosition
        {
            get { return _Y; }
            set { _X = value; }
        }

        private int _Length;

        public int Length
        {
            get { return _Length; }
            set 
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Размер окна не может быть отрицательным");
                _Length = value; 
            }
        }
 
        private int _Width;

        public int Width
        {
            get { return _Width; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value),"Размер окна не может быть отрицательным");
                
                _Width = value; 
            }
        }






    }
}
