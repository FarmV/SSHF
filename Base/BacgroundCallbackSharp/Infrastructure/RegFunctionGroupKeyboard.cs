using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input.Infrastructure
{
    public class RegFunctionGroupKeyboard
    {
        internal RegFunctionGroupKeyboard(VKeys[] keyCombination, List<IRegFunction> listOfRegisteredFunctions)
        {
           
            ListOfRegisteredFunctions = listOfRegisteredFunctions;
            KeyCombination = keyCombination;
        }
        public VKeys[] KeyCombination { get; }
        public List<IRegFunction> ListOfRegisteredFunctions { get; }     
    }
}
