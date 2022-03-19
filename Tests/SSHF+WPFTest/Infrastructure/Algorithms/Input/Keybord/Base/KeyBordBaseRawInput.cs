using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using SSHF.Infrastructure.Algorithms.Input;
using SSHF.Infrastructure.Algorithms.KeyBoards.Base.Input;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using SSHF.Infrastructure.Algorithms.Input.Keybord.Base;

namespace SSHF.Infrastructure.Algorithms.KeyBoards.Base
{
    internal class KeyBordBaseRawInput
    {
        
        private EventHandler<RawInputEvent>? RawInput;

        internal readonly ObservableCollection<VKeys[]> IsPressedKeys = new ObservableCollection<VKeys[]>();

        public KeyBordBaseRawInput(EventHandler<RawInputEvent> KeyHandlerRawInput)
        {
            RawInput = KeyHandlerRawInput;

            RawInput += RawInputHandler;
        }

        private void RawInputHandler(object? sender, RawInputEvent e)
        {
            
        }
    }
}
