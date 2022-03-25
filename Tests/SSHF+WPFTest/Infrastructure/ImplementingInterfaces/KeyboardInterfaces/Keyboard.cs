using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;

using SSHF.Infrastructure.ImplementingInterfaces.KeyboardInterfaces;
using SSHF.Infrastructure.Interfaces;

namespace SSHF.Infrastructure.ImplementingInterfaces
{
    //internal class Keyboard: IActionFunction
    //{


    //    public string Name => "Keyboard";

    //    bool isProcessing = false;

    //    bool _status = false;

    //    public bool Enable
    //    {
    //        get => _status;
    //    }


    //    public event Action<object?>? Сompleted;

    //    public Task<(bool MethodResult, string MethodMessage)> CheckAndRegistrationFunction(object? parameter = null)
    //    {
    //        _status = true;
    //        return Task.FromResult((true, "Функция успешно зарегистрирована"));
    //    }

    //    public async Task<(bool MethodResult, object? MethodOutputs, string MethodMessage)> StartFunction(object? parameter = null)
    //    {

    //        if (_status is false) return (false, null, $"Опрерация не зарегистрирована. Вызовите мотод {nameof(CheckAndRegistrationFunction)}");

    //        isProcessing = true;

    //        if (parameter is not Tuple<TypeAction, RegisteredFunction?> source)
    //        {
    //            isProcessing = false;
    //            return (false, null, $"Неверный формат входных данных. Верный формат {typeof(Tuple<TypeAction, RegisteredFunction?>)}");
    //        }

    //        (
    //          TypeAction typeActionactionArgument,
    //          RegisteredFunction? functionArgument
    //        ) = (source.Item1, source.Item2);

    //        if (Enum.IsDefined(typeof(TypeAction), typeActionactionArgument) is not true)
    //        {
    //            return (false, null, $"Неверный тип входных данных {nameof(typeActionactionArgument)}");
    //        }

    //        if (typeActionactionArgument is TypeAction.NotRegistered)
    //        {
    //            if (isProcessing is not true)
    //            {
    //                App.Input += App_Input;
    //                isProcessing = true;
    //            }

    //            return (true, null, $"Инициирован вызов события комибанации нажатия клавиш в {nameof(Сompleted)}");
    //        }
    //        if (isProcessing is not true)
    //        {
    //            App.Input += App_Input;
    //            isProcessing = true;
    //        }

    //        if (typeActionactionArgument is TypeAction.RegistrationFunction)
    //        {
    //            if (functionArgument is not RegisteredFunction function) return (false, null, $"{nameof(functionArgument)} is NULL");




    //        }





    //        return Tuple.Create<bool, object?, string>(true, new object(), "Функция успешно зарегистрирована");
    //    }

    //    public bool СancelFunction(object? parameter = null)
    //    {
    //        App.Input -= App_Input;
    //        isProcessing = false;
    //        return true;
    //    }

    //    Task<(bool MethodResult, object? MethodOutputs, string MethodMessage)> Registration(RegisteredFunction function)
    //    {
    //        if (function is null) return Task.FromResult(((bool, object?, string))(false, null, $"{nameof(function)} is NULL"));
    //        if (string.IsNullOrEmpty(function.Name)) return Task.FromResult(((bool, object?, string))(false, null, $"{nameof(function.Name)} пустая строка или NULL"));

    //        if (Enum.IsDefined(typeof(TypeAction), function.KeyKombination) is not true) return Task.FromResult(((bool, object?, string))(false, null, $"{function.KeyKombination} значение находится вне диапазона"));



    //        if (function.KeyKombination.HasFlag(function.KeyKombination))
    //        return Task.FromResult(((bool, object?, string))(true, new object(), $"Функция {function.Name} успешно зарегистрирована"));
    //    }
    //    (bool MethodResult, object? MethodOutputs, string MethodMessage) Registration2(RegisteredFunction function)
    //    {





    //        return (true, new object(), "Функция успешно зарегистрирована");
    //    }
    //    Task<Tuple<bool, object?, string>> ReplaceRegisteredFunction(RegisteredFunction function)
    //    {


    //        return Task.FromResult(Tuple.Create<bool, object?, string>(true, new object(), "Функция успешно заменена"));
    //    }

    //    Task<Tuple<bool, object?, string>> ContainsFunctionName(RegisteredFunction function)
    //    {


    //        return Task.FromResult(Tuple.Create<bool, object?, string>(true, new object(), "Функция успешно выполнена"));
    //    }




    //    ConcurrentDictionary<KeyboardInterfaces.VKeys, bool> KeyBools = new ConcurrentDictionary<KeyboardInterfaces.VKeys, bool>();

    //    ConcurrentBag<RegisteredFunction> RegFunc = new ConcurrentBag<RegisteredFunction>();

    //    Task CheckRegFunc()
    //    {
    //        if (RegFunc.Count is 0) return Task.CompletedTask;

    //        KeyboardInterfaces.VKeys? keyKombine = null;
    //        foreach (KeyValuePair<KeyboardInterfaces.VKeys, bool> pair in KeyBools)
    //        {
    //            if (pair.Value is true)
    //            {
    //                keyKombine |= pair.Key;
    //            }
    //        };
    //        if (keyKombine is null) return Task.CompletedTask;
    //        Сompleted?.Invoke(keyKombine);
    //        return Task.CompletedTask;
    //    }


    //    private void App_Input(object? sender, RawInputEventArgs e)
    //    {
    //        if (e.Data is RawInputMouseData) return;
    //        if (e.Data is RawInputKeyboardData InputData)
    //        {

    //            int key = InputData.Keyboard.VirutalKey;

    //            KeyboardInterfaces.VKeys FlagVkeys = (KeyboardInterfaces.VKeys)Enum.Parse(typeof(KeyboardInterfaces.VKeys), key.ToString());

    //            if ((int)FlagVkeys is 255) return;

    //            if ((Enum.IsDefined(typeof(KeyboardInterfaces.VKeys), FlagVkeys) is false)) throw new InvalidOperationException("Неправельный тип Vkeys");


    //            RawKeyboardFlags chekUPE0 = RawKeyboardFlags.Up | RawKeyboardFlags.KeyE0;

    //            if (InputData.Keyboard.Flags is Linearstar.Windows.RawInput.Native.RawKeyboardFlags.None | InputData.Keyboard.Flags is RawKeyboardFlags.KeyE0)
    //            {
    //                KeyBools[FlagVkeys] = true;
    //                CheckRegFunc();
    //                //  KeyboardHook_KeyDown(FlagVkeys);
    //            }
    //            if (InputData.Keyboard.Flags is Linearstar.Windows.RawInput.Native.RawKeyboardFlags.Up | InputData.Keyboard.Flags == chekUPE0)
    //            {
    //                KeyBools.TryRemove(new KeyValuePair<KeyboardInterfaces.VKeys, bool>(FlagVkeys, true));
    //                //  KeyBools[FlagVkeys] = false;
    //                // KeyboardHook_KeyUp(FlagVkeys);
    //            }
    //        }
    //    }
    //}

















}


