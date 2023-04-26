using DynamicData;

using FVH.Background.Input;

using ReactiveUI;

using SSHF.ViewModels.MainWindowViewModel;

using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF
{

    public class Shortcuts : ReactiveUI.ReactiveObject
    {
        private VKeys[] _keyCombo;
        public Shortcuts(VKeys[] keyCombo, Func<Task> callbackTask, object? identifier)
        {
            _keyCombo = keyCombo;
            CallbackTask = callbackTask;
            Identifier = identifier;
        }

        public VKeys[] KeyCombo
        {
            get => _keyCombo;
            set => this.RaiseAndSetIfChanged(ref _keyCombo, value);
        }
        public Func<Task> CallbackTask 
        { 
            get; 
            set; 
        }
        public object? Identifier { get; set; }
    }
    public interface IInvokeShortcuts
    {
        IEnumerable<Shortcuts> GetShortcuts();
    }
    internal class ShortcutsManager
    {
        IEnumerable<IInvokeShortcuts> _listFunc;
        IKeyboardCallback _keyboardCallback;
        private bool _isInit = false;
        public void RegisterShortcuts()
        {
            if (_isInit is true) throw new InvalidOperationException();
            _listFunc.ToList().ForEach((IInvokeShortcuts listShortcuts) =>
            {
                listShortcuts.GetShortcuts().ToList().ForEach(shortcut =>
                {
                    _keyboardCallback.AddCallbackTask(shortcut.KeyCombo, shortcut.CallbackTask, shortcut.Identifier ?? shortcut.CallbackTask.Method.Name);
                });
            });
            _isInit = true;
        }
        public ShortcutsManager(IKeyboardCallback keyboardCallback, IEnumerable<IInvokeShortcuts> listFunc)
        {
            _keyboardCallback = keyboardCallback;
            _listFunc = listFunc;            
        }
    }

    internal class ShortcutsMainViewModel : IInvokeShortcuts
    {
        private MainWindowViewModel _mainWindowViewModel;
        public ShortcutsMainViewModel(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
        }
        public IEnumerable<Shortcuts> GetShortcuts()
        {
            List<Shortcuts> list = new List<Shortcuts>();
            list.Add(new Shortcuts(new VKeys[]
            {
                VKeys.VK_LWIN,
                VKeys.VK_SHIFT,
                VKeys.VK_KEY_A
            },
            () => new Task(() =>
            {
                _mainWindowViewModel.RefreshWindowInvoke.Execute();
            }), null));

            return list;
        }
    }
}
