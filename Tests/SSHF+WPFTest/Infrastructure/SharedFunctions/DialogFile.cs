using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace SSHF.Infrastructure.SharedFunctions
{
    internal class DialogFile
    {
        bool OpenFile(string Title, out string? SelectedFile, string Filter = "Все файлы (*.*)|*.*")
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

        bool OpenFiles(string Title, out IEnumerable<string> SelectedFile, string Filter = "Все файлы (*.*)|*.*")
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
    }
}
