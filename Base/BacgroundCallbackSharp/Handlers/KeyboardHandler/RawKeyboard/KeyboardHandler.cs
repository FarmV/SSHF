using System.Diagnostics;

using Linearstar.Windows.RawInput.Native;
using Linearstar.Windows.RawInput;

using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    internal class KeyboardHandler : IKeyboardHandler
    {
        public List<VKeys> PressedKeys => _isPressedKeys.ToList();
        public event EventHandler<IKeysNotifier>? KeyPressEvent;
        public event EventHandler<IKeysNotifier>? KeyUpPressEvent;

        private const int TimerTimeout = 500;
        private readonly List<VKeys> _isPressedKeys = new List<VKeys>();
        private readonly Timer Cleartimer;

        public KeyboardHandler()
        {
            Cleartimer = new Timer(new TimerCallback((_) =>
            {
                this._isPressedKeys.Clear();
            }
            ), null, 0, TimerTimeout);
        }

        public void HandlerKeyboard(RawInputKeyboardData data)
        {
            if (data is not RawInputKeyboardData keyboardData) return; 

            Cleartimer.Change(TimerTimeout, Timeout.Infinite); // заглушка Пропадают ивенты отжатия при безудержном вводе именно левой и правой части клавиатуры одновременно (но отдельно вроде нет?)
                                                               // контрол + esacpe = ескейп только отжатый

            if (keyboardData.Keyboard.VirutalKey is 255) return; //todo Расмотреть возможность добавление обработки дополнительных клавиш (key 255)

            if (Enum.ToObject(typeof(VKeys), keyboardData.Keyboard.VirutalKey) is not VKeys FlagVkeys) throw new InvalidOperationException($"A virtual key is not an object {nameof(VKeys)}.");

            RawKeyboardFlags chekUPE0 = RawKeyboardFlags.Up | RawKeyboardFlags.KeyE0;
            RawKeyboardFlags chekUPE1 = RawKeyboardFlags.Up | RawKeyboardFlags.KeyE1;

            if ((keyboardData.Keyboard.Flags is RawKeyboardFlags.None | keyboardData.Keyboard.Flags is RawKeyboardFlags.KeyE0) || keyboardData.Keyboard.Flags is RawKeyboardFlags.KeyE1) // клавиша KeyDown
            {
                if (_isPressedKeys.Contains(FlagVkeys)) return;
                  Trace.WriteLine($"{Environment.CurrentManagedThreadId} {Thread.CurrentThread.Name} Add  {FlagVkeys.ToString()}");
                _isPressedKeys.Add(FlagVkeys);
                KeyPressEvent?.Invoke(null, new DataKeysNotificator(_isPressedKeys.ToArray()));
                return;
            }
            if ((keyboardData.Keyboard.Flags is RawKeyboardFlags.Up | keyboardData.Keyboard.Flags == chekUPE0) | keyboardData.Keyboard.Flags == chekUPE1)  // клавиша KeyUp
            {
                if (_isPressedKeys.Contains(FlagVkeys) is not true) return;
                  Trace.WriteLine($"{Environment.CurrentManagedThreadId} {Thread.CurrentThread.Name} Delete  {FlagVkeys.ToString()}");
                _isPressedKeys.Remove(FlagVkeys);
                KeyUpPressEvent?.Invoke(null, new DataKeysNotificator(PressedKeys.ToArray()));
                return;
            }

            throw new InvalidOperationException("Key Handler Error");
        }
    }

}
