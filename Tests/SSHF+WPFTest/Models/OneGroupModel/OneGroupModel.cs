using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF_WPFTest.Models.OneGroupModel
{
    internal class OneGroupModel
    {
        /// <summary>Добавить окно в коллекцию</summary>
        public bool AddWindow(ObservableCollection<OneGroupWindow> Collection, OneGroupWindow Window)
        {
            if (Window is null || Collection.Contains(Window)) return false;
            else
            {
                Collection.Add(Window);
                return true;

            }
        }

        /// <summary>Удалить окно из коллекции</summary>
        public bool RemoveWindow(ObservableCollection<OneGroupWindow> Collection, OneGroupWindow Window)
        {
            if (Window is null) return false;
            if (Collection.Contains(Window))
            {
                if (!Collection.Remove(Window)) return false;
            }
            return true;
        }
    }
}
