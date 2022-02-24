using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.Interfaces
{
    public interface IActionFunction
    {

        public event Action<object?> Сompleted;

        public bool Enable
        {
            get;
        }

        public abstract string Name { get; }

        /// <summary>
        /// <br> <see cref="CheckAndRegistrationFunction"/> Ожидает получение комбинации клавиш глобального вызова в формате <see cref="FuncKeyHandler.FkeyHandler.VKeys"/>,
        /// c разделителем "+" между клавишами. Клавиши не обязательны. Ищи дополнение в реализации непосредственного класса.
        /// </br>
        /// <br>
        /// </br>
        /// <br>
        /// Образец регистрации клавиш <see cref="string" href=" 'KEY_1 + KEY_2 + KEY_3'"/> 
        /// </br>
        /// </summary>
        public abstract Tuple<bool,string> CheckAndRegistrationFunction(object? parameter = null);

        /// <summary>
        /// <br> <see cref="StartFunction"/> Ожидает получение комбинации клавиш глобального вызова в формате <see cref="FuncKeyHandler.FkeyHandler.VKeys"/>, c разделителем "+" между клавишами. Клавиши не обязательны.</br>
        /// </summary>
        public abstract bool StartFunction(object? parameter = null);

        public abstract bool СancelFunction(object? parameter = null);


    }
    enum TypesActionFail
    {
       

    }
}
