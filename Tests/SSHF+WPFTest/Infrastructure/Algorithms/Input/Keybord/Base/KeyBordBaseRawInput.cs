using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;

using SSHF.Infrastructure.Algorithms.Base;
using SSHF.Infrastructure.Algorithms.Input;
using SSHF.Infrastructure.Algorithms.Input.Keybord.Base;

namespace SSHF.Infrastructure.Algorithms.KeyBoards.Base
{

    internal class KeyBordBaseRawInput
    {

        private readonly static KeyBordBaseRawInput Instance = new KeyBordBaseRawInput();

        internal static event EventHandler<NotifyCollectionChangedEventArgs>? ChangeTheKeyPressure;

        private static readonly ObservableCollection<VKeys> IsPressedKeys = new ObservableCollection<VKeys>();

        private KeyBordBaseRawInput()
        {
            App.Input += RawInputHandler;
            //IsPressedKeys.CollectionChanged += (obj, data) => { ChangeTheKeyPressure?.Invoke(this, data); };
            IsPressedKeys.CollectionChanged += IsPressedKeys_CollectionChanged; 
        }

        private void IsPressedKeys_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
             ChangeTheKeyPressure?.Invoke(this, e);
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

            }
            if (keyboardData.Keyboard.Flags is RawKeyboardFlags.Up | keyboardData.Keyboard.Flags == chekUPE0)  // клавиша KeyUp
            {
                IsPressedKeys.Remove(FlagVkeys);
            }
        }
    }
}
