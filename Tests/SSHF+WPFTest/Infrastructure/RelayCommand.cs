﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SSHF_WPFTest.Infrastructure
{
    class RelayCommand : ICommand
    {
        readonly Action<object> _execute;
        readonly Predicate<object>? _canExecute;

        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<object> execute, Predicate<object>?canExecute = null)
        {
            if (execute is null) throw new ArgumentNullException(nameof(execute));
 

            _execute = execute;
            _canExecute = canExecute;
        }
            
        public bool CanExecute(object? parameter)
        {
           // if (_canExecute is null) return true; else _canExecute.Invoke(parameter);


            return _canExecute == null || _canExecute.Invoke(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        public void Execute(object? parameter)
        {
            _execute.Invoke(parameter);
        }
    }
}
