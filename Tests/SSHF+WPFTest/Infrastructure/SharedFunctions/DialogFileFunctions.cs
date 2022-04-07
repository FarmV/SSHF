using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace SSHF.Infrastructure.SharedFunctions
{
    internal static class DialogFileFunctions
    {
        internal static bool OpenFile(string Title, out string? SelectedFile, string Filter = "Все файлы (*.*)|*.*")
        {
            OpenFileDialog? fileDialog = new OpenFileDialog
            {
                Title = Title,
                Filter = Filter,
            };


            if(fileDialog.ShowDialog() is not true)
            {
                SelectedFile = null;
                return false;
            }
            
            SelectedFile = fileDialog.FileName;

            return true;
        }

        internal static bool OpenFiles(string Title, out IEnumerable<string> SelectedFile, string Filter = "Все файлы (*.*)|*.*")
        {
            OpenFileDialog? fileDialog = new OpenFileDialog
            {
                Title = Title,
                Filter = Filter,
            };


            if (fileDialog.ShowDialog() is not true)
            {
                SelectedFile = Enumerable.Empty<string>();
                return false;
            }

            SelectedFile = fileDialog.FileNames;

            return true;

        }

        internal static string? SaveFileDirectory(string Title, string Filter = "Все файлы (*.*)|*.*", IEnumerable<string>? Extension = null)
        {
            SaveFileDialog? fileDialog = new SaveFileDialog
            {
                Title = Title,
                Filter = Filter,
            };
           

            if (fileDialog.ShowDialog() is not true)
            {
                return null;
            }

            return fileDialog.FileName;// todo Оценить необходимость фильтрации типов                      
        }
    }
}
