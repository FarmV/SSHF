using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.ImplementingInterfaces.KeyboardInterfaces
{

    internal class RegisteredFunction
    {
        internal RegisteredFunction(string name, KeyboardInterfaces.VKeys keys, Action<object?>? actionMethod, AsyncCallback? callbackMethod, bool keyIsOne = false)
        {
            _name = name;
            _action = actionMethod;
            _back = callbackMethod;
            _keyisOne = keyIsOne;
        }

        bool _keyisOne = false;

        public bool isOneKey
        {
            get => _keyisOne;
        }

        private string _name;
        public string Name
        {
            get => _name;
        }

        private Action<object?>? _action;
        public Action<object?>? PointerToMethod
        {
            get => _action;
        }

        private AsyncCallback? _back;
        public AsyncCallback? Callback
        {
            get => _back;
        }

        private KeyboardInterfaces.VKeys _keys;
        public KeyboardInterfaces.VKeys KeyKombination
        {
            get => _keys;
        }
    }



}
