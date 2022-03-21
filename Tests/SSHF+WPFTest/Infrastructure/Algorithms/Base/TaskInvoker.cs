using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.Algorithms.Base
{
    internal static class TasksList
    {
        internal static Dictionary<string, Task> Tasks = new Dictionary<string, Task>();   //?
    } 

    internal class TaskInvoker
    {
        public TaskInvoker()
        {
        }

        private static class Tasks<T> where T : Task
        {
            private static List<T> Functions = new List<T>();
        }

        void GetFunctionsList()
        {

        }
        internal void Call<T>(T keyCombo) where T : Task
        {
            Tasks<T>.FunctionsCallback[keyCombo].Start();
        }
    }
    internal static class TaskInvokerExtensions
    {
        internal static void TA(this Task task, Dic)
        {

        }
    }
}
