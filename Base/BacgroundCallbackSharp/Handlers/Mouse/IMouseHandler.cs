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
    /// <br>This interface declares a contract to handle mouse events.</br>
    /// <br><see langword="Ru"/></br>
    /// <br>Этот интерфейс объявляет контракт для обработки событий мыши.</br>
    /// </summary>
    public interface IMouseHandler
    {
        public event EventHandler<RawInputMouseData>? MouseEvent;
        public void HandlerMouse(RawInputMouseData data);
    }

}
