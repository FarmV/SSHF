using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Linearstar.Windows.RawInput;

namespace SSHF.Infrastructure.Algorithms.Input
{
    internal class RawInputEvent: EventArgs
    {
        internal RawInputEvent(RawInputData data) { Data = data;}

        internal RawInputData Data { get; }

    }
}
