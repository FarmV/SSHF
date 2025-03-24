using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input.Infrastructure
{
    public class GroupFunctions
    {
        internal GroupFunctions(VKeys[] combination, List<Function> functions)
        {          
            Functions = functions;
            Combination = combination;

        }
        public VKeys[] Combination { get; }
        public List<Function> Functions { get; }
    }
}
