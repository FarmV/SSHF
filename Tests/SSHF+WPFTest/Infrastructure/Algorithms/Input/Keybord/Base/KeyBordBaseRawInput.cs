using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;

using SSHF.Infrastructure.Algorithms.Base;
using SSHF.Infrastructure.Algorithms.Input;
using SSHF.Infrastructure.Algorithms.Input.Keybord.Base;

namespace SSHF.Infrastructure.Algorithms.KeyBoards.Base
{
    
    internal class DataKeysNotificator
    {
        internal DataKeysNotificator(List<VKeys> keys)
        {
            Keys = keys;
        }

        internal List<VKeys> Keys{ get; }
    }
    internal class KeyBordBaseRawInput
    {

            
        internal static event EventHandler<DataKeysNotificator>? ChangeTheKeyPressure;

        private static readonly List<VKeys> IsPressedKeys = IsPressedKeys = new List<VKeys>();

        private readonly static KeyBordBaseRawInput Instance = new KeyBordBaseRawInput();

        private KeyBordBaseRawInput()
        {
            App.Input += RawInputHandler;                 
        }

        internal static KeyBordBaseRawInput GetInstance() => Instance;


        internal enum RawInputHandlerFail
        {
            None,
            VirutalKeyNonVKeys
        }
        private static void RawInputHandler(object? sender, RawInputEvent e)
        {
            if (e.Data is not RawInputKeyboardData keyboardData) return;

            var RawInputHandlerFails = ExHelp.GetLazzyDictionaryFails
            (
               new System.Collections.Generic.KeyValuePair<RawInputHandlerFail, string>(RawInputHandlerFail.VirutalKeyNonVKeys, $"Виртуальный ключ не явлется объектом {nameof(VKeys)}.") //0
            );

            if (keyboardData.Keyboard.VirutalKey is 255) return; //todo Расмотреть возможность добавление дополнитльных функций клваитуры (key 255)

            if (Enum.ToObject(typeof(VKeys), keyboardData.Keyboard.VirutalKey) is not VKeys FlagVkeys) throw new InvalidOperationException().Report(RawInputHandlerFails.Value[0]);

            RawKeyboardFlags chekUPE0 = RawKeyboardFlags.Up | RawKeyboardFlags.KeyE0;

            if (keyboardData.Keyboard.Flags is RawKeyboardFlags.None | keyboardData.Keyboard.Flags is RawKeyboardFlags.KeyE0) // клавиша KeyDown
            {
                IsPressedKeys.Add(FlagVkeys);
                ChangeTheKeyPressure?.Invoke(null, new DataKeysNotificator(IsPressedKeys));
            }
            if (keyboardData.Keyboard.Flags is RawKeyboardFlags.Up | keyboardData.Keyboard.Flags == chekUPE0)  // клавиша KeyUp
            {
                IsPressedKeys.Remove(FlagVkeys);
                ChangeTheKeyPressure?.Invoke(null, new DataKeysNotificator(IsPressedKeys));
            }
        }
    }
}
