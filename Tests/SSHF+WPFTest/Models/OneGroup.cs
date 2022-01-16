using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF_WPFTest.Models
{
    internal class OneGroup
    {   
        /// <summary>Коллекция окон в группе</summary>
        private readonly List<OneGroupWindow> _oneGroups = new List<OneGroupWindow>();
        
        /// <summary>Добавить окно в коллекцию</summary>
        public bool AddWindow(OneGroupWindow window)
        {
            if (window is null || _oneGroups.Contains(window))  return false;
            else
            {
                _oneGroups.Add(window);
                return true;
            }


        }

        /// <summary>Удалить окно из коллекции</summary>
        public bool RemoveWindow(OneGroupWindow window)
        {
            if (window is null) return false;
            if (_oneGroups.Contains(window)) 
            {
                if (!_oneGroups.Remove(window)) return false;
            }
            return true;
        }


        /// <summary>Путь к файлу изображения по умолчанию</summary>
        private string _PathImage = "/Views/Windows/MainWindowRes/F_Logo.png";

        /// <summary>Путь к файлу изображения по умолчанию</summary>
        public string PathImage
        {
            get =>  _PathImage; 
            set => _PathImage = value; 
        }

        /// <summary>Позиция X окна изображения группы на экране, относительно окна быстрого доступа</summary>
        private int _X;

        /// <summary>Позиция X окна изображения группы на экране, относительно окна быстрого доступа</summary>
        public int XPositin
        {
            get =>  _X;
            set => _X = value; 
        }

        /// <summary>Позиция Y окна изображения группы на экране, относительно окна быстрого доступа</summary>
        private int _Y;

        /// <summary>Позиция Y окна изображения группы на экране, относительно окна быстрого доступа</summary>
        public int YPositin
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
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Размер окна группы не может быть отрицательным");
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
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Размер окна группы не может быть отрицательным");

                _Width = value;
            }
        }





    }
}
