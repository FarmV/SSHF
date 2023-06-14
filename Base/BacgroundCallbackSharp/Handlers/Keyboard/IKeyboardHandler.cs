using FVH.Background.Input;
using Linearstar.Windows.RawInput;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FVH.Background.Input
{


    /// <summary>
    /// <br><see langword="En"/></br>
    /// <br>This interface declares a contract for handling keyboard events.</br>
    /// <br><see langword="Ru"/></br>
    /// <br>Этот интерфейс объявляет контракт для обработки событий клавиатуры.</br>
    /// </summary>
    public interface IKeyboardHandler
    {
        /// <summary>
        /// <br><see langword="En"/></br>
        /// <br>The currently pressed keys on the keyboard.</br>
        /// <br><see langword="Ru"/></br>
        /// <br>Нажатые в данный момент клавиши клавиатуры.</br>
        /// </summary>
        public List<VKeys> PressedKeys { get; }
        /// <summary>
        /// <br><see langword="En"/></br>
        /// <br>When you press a key, it returns the entire collection of keys pressed on the keyboard.</br>
        /// <br><see langword="Ru"/></br>
        /// <br>При нажатии клавиши возвращается вся коллекция клавиш, нажатых на клавиатуре.</br>
        /// </summary>
        public event EventHandler<IKeysNotificator>? KeyPressEvent;
        /// <summary>
        /// <br><see langword="En"/></br>
        /// <br>Releasing a key returns the entire collection of keys pressed on the keyboard.</br>
        /// <br><see langword="Ru"/></br>
        /// <br>При отпускании клавиши возвращается вся коллекция клавиш, нажатых на клавиатуре.</br>
        /// </summary>
        public event EventHandler<IKeysNotificator>? KeyUpPressEvent;

        /// <summary>
        /// <br><see langword="En"/></br>
        /// <br>Responsible for filling and removing items in the collection, calling the appropriate events. </br>
        /// <br><see langword="Ru"/></br>
        /// <br>Отвечает за наполнение и удаление предметов коллекции, вызывая соответствующие события.</br>
        /// </summary>
        internal void HandlerKeyboard(RawInputKeyboardData data);
    }
}
