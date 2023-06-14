using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FVH.Background.Input
{
    public class RegFunctionGroupKeyboard
    {
        internal RegFunctionGroupKeyboard(int group, VKeys[] keyCombination, List<IRegFunction> listOfRegisteredFunctions)
        {
            Group = group;
            ListOfRegisteredFunctions = listOfRegisteredFunctions;
            KeyCombination = keyCombination;
        }
        public VKeys[] KeyCombination { get; }
        private int _group;
        public int Group
        {
            get { return _group; }
            private set { if (value < 0) throw new InvalidOperationException("The value for the group cannot be negative"); _group = value; }
        }
        public List<IRegFunction> ListOfRegisteredFunctions { get; }
    }
    public record RegFunction : IRegFunction
    {
        internal RegFunction(Func<Task> callBackTask, object? identifier = null)
        {
            CallBackTask = callBackTask;
            Identifier = identifier;
        }
        public object? Identifier { get; }
        public Func<Task> CallBackTask { get; }
    }
}
