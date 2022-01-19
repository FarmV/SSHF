using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

using GlobalLowLevelHooks;

namespace FuncKeyHandler
{
    class FkeyHandler
    {
        public FkeyHandler(string ConcatenationString = "")
        {
            _ConcatenationString = ConcatenationString;
            _KeyboardHook.Install();
            _KeyboardHook.KeyUp += new KeyboardHook.KeyboardHookCallback(KeyboardHook_KeyUp);
            _KeyboardHook.KeyDown += new KeyboardHook.KeyboardHookCallback(KeyboardHook_KeyDown);
            MyEnvetKeys += CheckAndSwitchKeyInKeyBools;
        }

        public string _ConcatenationString { get; } 

        KeyboardHook _KeyboardHook = new KeyboardHook();

        Dictionary<string, bool> KeyBools = new Dictionary<string, bool>();

        List<RegisteredFunction> Functions = new List<RegisteredFunction>();


        private event KeyEventHandler MyEnvetKeys;

        public delegate void KeyEventHandler(object sender, KeyUPorDown e);
        
        public delegate void KeyEventForUser(object sender, NotificationPressKeys e);

        public event KeyEventForUser? EnvetKeys;

        public class KeyUPorDown
        {
            public KeyUPorDown(bool KeyDown)
            {
                _isKeyDown = KeyDown;
            }
            public bool _isKeyDown { get; } // readonly
        }
        public class NotificationPressKeys
        {
            public NotificationPressKeys(string KeyDowns)
            {
                _KeyDowns = KeyDowns;
            }
            public string _KeyDowns { get; } // readonly          
        }

        private void KeyboardHook_KeyUp(KeyboardHook.VKeys key)
        {           
             MyEnvetKeys?.Invoke(Enum.Parse(typeof(KeyboardHook.VKeys), key.ToString()).ToString(), new KeyUPorDown(false));
        }

        private void KeyboardHook_KeyDown(KeyboardHook.VKeys key)
        {
            MyEnvetKeys?.Invoke(Enum.Parse(typeof(KeyboardHook.VKeys), key.ToString()).ToString(), new KeyUPorDown(true));
        }

        private void CheckAndSwitchKeyInKeyBools(object sender, KeyUPorDown e)
        {
            if (e._isKeyDown)
            {
                KeyBools[sender.ToString()] = true;
                EnvetKeys?.Invoke(this, new NotificationPressKeys(PrintKeysIsDown()));           
            }
            else
            {
                KeyBools[sender.ToString()] = false;
            }
            MyCheckKeysToEnableFunc();
        }
       
        
        private string PrintKeysIsDown()
        {
            string resultKeysIsDown = string.Empty;

            foreach (var item in KeyBools)
            {
                if (item.Value is true)
                {
                    if (resultKeysIsDown == string.Empty)
                    {
                        resultKeysIsDown += item.Key;

                    }
                    else
                    {
                        resultKeysIsDown += $" {_ConcatenationString} {item.Key}";
                    }
                }
            }
            return resultKeysIsDown;
        }


        void MyCheckKeysToEnableFunc()
        {
            string result = string.Empty;
            List<string> keys = new List<string>();

            if (KeyBools.ContainsKey("RETURN"))
            {
                KeyBools.Remove("RETURN");
            }
            foreach (var item in KeyBools)
            {
                if (item.Value == true)
                {

                    
                    keys.Add(item.Key);
                    keys.Add(" ");
                    keys.Add(_ConcatenationString);
                    keys.Add(" ");
                }

                //if (item.Value == true)
                //{
                //    string pattern = @"\w+";
                //    Regex regex = new Regex(pattern);

                //    string repaceParsing = regex.Replace(item.Key, $"{item.Key} ");

                //    keys.Add(repaceParsing);
                //}

            }
            string[] massKeys = keys.ToArray();
            result = string.Concat(massKeys).Trim().TrimEnd(_ConcatenationString.ToCharArray()).Trim();
            bool ChekFunc = false;
            string myFunc = string.Empty;
            string keyFunc = string.Empty;
            Action? action = default;
            bool isInvoce = default;
            foreach (var item in Functions)
            {
                if (item._KeyCombination == result)
                {
                    ChekFunc = true;
                    myFunc = item._Name;
                    keyFunc = item._KeyCombination;
                    action = item._Action;
                    isInvoce = item._Invoce;
                    break;
                }
            }

            if (ChekFunc)
            {
               
               // string str = string.Empty;
                //foreach (var item in Functions)
                //{
                //    if (item._KeyCombination == result)
                //    {

                //        str = item._KeyCombination;
                //        break;
                //    }

                //}
                if (string.IsNullOrWhiteSpace(myFunc))
                {
                    return;
                }

                FuncHandler?.Invoke(this, new MyInvoceFuntion(myFunc,keyFunc, action, isInvoce));
             
            }

        }
        public event EventHandler<MyInvoceFuntion>? FuncHandler = null;
        
        public class MyInvoceFuntion
        {
            public MyInvoceFuntion(string Function, string KeyCombination, Action? Action,bool Invoce = false)
            {
                _Function = Function;
                _KeyCombination = KeyCombination;
                _Action = Action;
                _Invoce = Invoce;

                if (Action is not null && Invoce == true) Action.Invoke();

            }
            public string _Function { get; }
            public string _KeyCombination { get; }
            public Action? _Action { get; }
            public bool _Invoce { get; }


        }
        public void RegisterAFunction(string Name, string DefoltKeyCombination, Action? Action = null, bool Invoce = false)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new ArgumentException($"Line is empty or NULL", Name);
            }
            foreach (RegisteredFunction item in Functions)
            {
                if (item._Name == Name)
                {
                    throw new ArgumentException($"The {Name} is already in the collection", Name);
                }
            }

            Functions.Add(new RegisteredFunction(Name, DefoltKeyCombination, Action, Invoce));

        }

        public bool ContainsListRegisteredFunctionKeyCombination(string KeyCombination)
        {
            RegisteredFunction Function = Functions.Find(x=>x._KeyCombination == KeyCombination);
            if (Function is null)
            {
                return false;
            }
            return true;
        }

        public bool ContainsListRegisteredFunctionName(string Name)
        {
            RegisteredFunction Function = Functions.Find(x => x._Name == Name);
            if (Function is null)
            {
                return false;
            }
            return true;
        }

        void FunctionNotDuplicate(RegisteredFunction Function)
        {
            RegisteredFunction FunctionNew = Function;
          
            while (true)
            {
                RegisteredFunction findFuncton = Functions.Find(x => x._Name == FunctionNew._Name);
                if (findFuncton is null)
                {
                    return;
                }
                else
                {
                    Functions.Remove(findFuncton);
                }
            }


        } 

        public bool ReplaceRegisteredFunction(string Name,string KeyCombination, Action? Action)
        {
            RegisteredFunction Function = Functions.Find(x => x._Name == Name);
            if (Function is null)
            {
                return false;
            }
            Functions.Remove(Function);
            RegisteredFunction functionNew = new RegisteredFunction(Name, KeyCombination, Action);
            FunctionNotDuplicate(functionNew);
            Functions.Add(functionNew);


            return true;
        }
        
        public class RegisteredFunction
        {
            public string _Name;

            public string _KeyCombination;

            public Action? _Action;

            public bool _Invoce;

            public RegisteredFunction(string Name, string KeyCombination, Action? Action = null, bool Invoce = false)
            {
                _Name = Name;
                _KeyCombination = KeyCombination;
                _Action = Action;
            }
        }

    }





}



