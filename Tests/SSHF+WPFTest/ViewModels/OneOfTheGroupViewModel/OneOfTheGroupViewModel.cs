using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SSHF.ViewModels.Base;

namespace SSHF.Models
{
    internal class OneOfTheGroupViewModel: ViewModel
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;
        public OneOfTheGroupViewModel()
        {
        }




        private bool _isVisvible;

        public bool Visvible
        {
            get => _isVisvible;
            set => _isVisvible = value; 
        }



        private string _PathBackground = "/Views/Windows/MainWindowRes/F_Logo.png";
        public string PathBackground
        {
            get => _PathBackground;
            set => _PathBackground = value;
        }

        /// <summary>Позиция X окна изображения группы на экране</summary>
        private int _X;

        /// <summary>Позиция X окна изображения группы на экране</summary>
        public int XPosition
        {
            get => _X; 
            set => _X = value; 
        }

        /// <summary>Позиция Y окна изображения группы на экране</summary>
        private int _Y;

        /// <summary>Позиция Y окна изображения группы на экране</summary>
        public int YPosition
        {
            get => _Y; 
            set => _Y = value; 
        }

        /// <summary>Длинна окна</summary>
        private int _Length;

        /// <summary>Длинна окна</summary>
        public int Length
        {
            get => _Length; 
            set 
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Размер окна не может быть отрицательным");
                _Length = value; 
            }
        }

        /// <summary>Ширина окна</summary>
        private int _Width;

        /// <summary>Ширина окна</summary>
        public int Width
        {
            get => _Width;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value),"Размер окна не может быть отрицательным");
                
                _Width = value; 
            }
        }


    }
}
