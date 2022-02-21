using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.Interfaces
{
    internal interface IActionFunction
    {
        internal abstract string Name { get; }

        internal abstract bool CheckFunction();

        internal abstract bool StartFunction();

        internal abstract bool СancelFunction();


    }
}
