using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;

using SSHF.Infrastructure.Interfaces;

namespace SSHF.Infrastructure.ImplementingInterfaces
{
    internal class Keyboard: IActionFunction
    {
        

        public string Name => "Keyboard";

        bool isProcessing = false;

        bool _status = false;
       
        public bool Enable
        {
            get => _status;
        }


        public event Action<object?>? Сompleted;

        public Task<Tuple<bool, string>> CheckAndRegistrationFunction(object? parameter = null)
        {
            _status = true;
            return Task.FromResult(Tuple.Create<bool, string>(true, "Функция успешно зарегистрирована"));
        }

        public Task<Tuple<bool, object?, string>> StartFunction(object? parameter = null)
        {
            if (_status is false) return Task.FromResult(Tuple.Create<bool, object?, string>(false,null, $"Опрерация не зарегистрирована.Вызовите мотод {nameof(CheckAndRegistrationFunction)}"));
            
            if(isProcessing is true) return Task.FromResult(Tuple.Create<bool, object?, string>(false, null, "Функция уже выполнятеся и не подразумевает паралельный вызов"));

            isProcessing = true;

            if (parameter is not Tuple<KeyboardInterfaces.EnumsKeyboard?, Action<object?>, AsyncCallback?, string, bool?> parameterFor)
            {
                return Task.FromResult(Tuple.Create<bool, object?, string>(false, null, "Функция уже выполнятеся и не подразумевает паралельный вызов"));
            }


            App.Input += App_Input;

 
            Task<Tuple<bool, object?, string>> res = Registration(parameterFor.Item1, parameterFor.Item2, parameterFor.Item3, parameterFor.Item4, parameterFor.Item5);

            isProcessing = false;


            return Task.FromResult(Tuple.Create<bool, object?, string>(true,new object(), "Функция успешно зарегистрирована"));
        }

        public bool СancelFunction(object? parameter = null)
        {
            App.Input -= App_Input;
            return true;
        }

        Task<Tuple<bool, object?, string>> Registration(KeyboardInterfaces.EnumsKeyboard? keyCombinateon, Action<object?> actionMethod, AsyncCallback? callbackMethod, string ConcatenationString = "", bool? keyisOne = false)
        {
            

            return Task.FromResult(Tuple.Create<bool, object?, string>(true, new object(), "Функция успешно зарегистрирована"));
        }


        public string? _ConcatenationString
        {
            get;
        }


        ConcurrentDictionary<VKeys, bool> KeyBools = new ConcurrentDictionary<TKey, TValue>();




       

        readonly List<RegisteredFunction> Functions = new List<RegisteredFunction>();


        private event KeyEventHandler MyEnvetKeys;

        public delegate void KeyEventHandler(object sender, KeyUPorDown e);

        public delegate void KeyEventForUser(object sender, NotificationPressKeys e);

        public event KeyEventForUser? EnvetKeys;



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






        private void App_Input(object? sender, RawInputEventArgs e)
        {
            if (e.Data is RawInputMouseData) return;
            if (e.Data is RawInputKeyboardData InputData)
            {

                int key = InputData.Keyboard.VirutalKey;

                VKeys FlagVkeys = (VKeys)Enum.Parse(typeof(VKeys), key.ToString());

                if ((int)FlagVkeys is 255) return;

                if ((Enum.IsDefined(typeof(VKeys), FlagVkeys) is false)) throw new InvalidOperationException("Неправельный тип Vkeys");


                RawKeyboardFlags chekUPE0 = RawKeyboardFlags.Up | RawKeyboardFlags.KeyE0;

                if (InputData.Keyboard.Flags is Linearstar.Windows.RawInput.Native.RawKeyboardFlags.None | InputData.Keyboard.Flags is RawKeyboardFlags.KeyE0)
                {
                    KeyboardHook_KeyDown(FlagVkeys);
                }
                if (InputData.Keyboard.Flags is Linearstar.Windows.RawInput.Native.RawKeyboardFlags.Up | InputData.Keyboard.Flags == chekUPE0)
                {
                    KeyboardHook_KeyUp(FlagVkeys);
                }
            }
        }
    }




    public class MyInvoceFuntion
    {
        public MyInvoceFuntion(string Function, string KeyCombination, Action? Action, bool Invoce = false)
        {
            _Function = Function;
            _KeyCombination = KeyCombination;
            _Action = Action;
            _Invoce = Invoce;

            if (Action is not null && Invoce == true) Action.Invoke();

        }
        public string _Function
        {
            get;
        }
        public string _KeyCombination
        {
            get;
        }
        public Action? _Action
        {
            get;
        }
        public bool _Invoce
        {
            get;
        }

        public override int GetHashCode()
        {
            int function = _Function.GetHashCode();
            int keyCombination = _KeyCombination.GetHashCode();
            int action = _Action?.GetHashCode() ?? 0;
            int invoce = _Invoce.GetHashCode();

            return HashCode.Combine(function, keyCombination, action, invoce);
        }
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            MyInvoceFuntion? inFun = obj as MyInvoceFuntion;
            if (inFun is null) return false;

            if (inFun._Action is null) return inFun._Function == this._Function && inFun._KeyCombination == this._KeyCombination && inFun._Invoce == this._Invoce;
            else return inFun._Function == this._Function && inFun._KeyCombination == this._KeyCombination && inFun._Action == this._Action && inFun._Invoce == this._Invoce;


        }

    }
}
