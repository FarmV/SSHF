using System;
using System.Collections.Generic;
using System.Linq;

using FVH.Background.Input.Infrastructure.Interfaces;

using FVH.SSHF.Infrastructure.Interfaces;

namespace FVH.SSHF.Infrastructure
{
    internal class ShortcutsProvider(IKeyboardCallback keyboardCallback, IEnumerable<IInvokeShortcuts> listFunc)
    {
        private readonly IEnumerable<IInvokeShortcuts> _listFunc = listFunc;
        private readonly IKeyboardCallback _keyboardCallback = keyboardCallback;
        private bool _isInit = false;

        public void RegisterShortcuts()
        {
            if (_isInit is true) throw new InvalidOperationException();
            _listFunc.ToList().ForEach((listShortcuts) =>
            {
                listShortcuts.GetShortcuts().ToList().ForEach(shortcut =>
                {
                    _keyboardCallback.AddCallBackTask(shortcut.KeyCombo, shortcut.CallbackTask, shortcut.Identifier ?? shortcut.CallbackTask.Method.Name);
                });
            });
            _isInit = true;
        }
    }
}
