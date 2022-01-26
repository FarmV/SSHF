using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SSHF.ViewModels.Base;
using SSHF.Models.OneGroupModel;

namespace SSHF.Models
{
    internal class GroupViewModel: ViewModel
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;



        /// <summary>Коллекция окон в группе</summary>
        private ObservableCollection<OneOfTheGroupViewModel>? _oneGroups;
        private OneGroupModel.OneGroupModel _OneGroupModel = new OneGroupModel.OneGroupModel();

        public ObservableCollection<OneOfTheGroupViewModel> CollectionWindow
        {
            get
            {
                if (_oneGroups is null) _oneGroups = new ObservableCollection<OneOfTheGroupViewModel>();

                return _oneGroups; 
            }
            set 
            {
                if (value is null) return;
                if (_oneGroups is null) _oneGroups = new ObservableCollection<OneOfTheGroupViewModel>();

            }
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
