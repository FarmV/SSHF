using FVH.Background.Input;

using Linearstar.Windows.RawInput;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FVH.Background.Input
{
    internal class MouseHandler : IMouseHandler
    {
        public event EventHandler<RawInputMouseData>? MouseEvent;
        public void HandlerMouse(RawInputMouseData data)
        {
            MouseEvent?.Invoke(this, data);
        }
    }
}
