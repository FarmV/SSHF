using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.Interfaces
{
    public interface IActionFunction
    {
        public bool Enable
        {
            get;
        }

        public abstract string Name { get; }

        /// <summary>
        /// Аргумет подразумевает передачу комбинации клавиш для глобального вызова.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns>Результат проверки регистрациии функции</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public abstract Tuple<bool,string> CheckAndRegistrationFunction(object? parameter = null);

        public abstract bool StartFunction(object? parameter = null);

        public abstract bool СancelFunction(object? parameter = null);


    }
    enum TypesActionFail
    {
       

    }
}
