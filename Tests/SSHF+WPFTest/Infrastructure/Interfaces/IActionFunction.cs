using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.Interfaces
{       /// <summary>
        /// Интерфейс функции приложения.
        /// </summary>
    public interface IActionFunction
    {
        /// <summary>
        /// Проискходит по завершеню операции.Удачной или нет.
        /// </summary>
        /// <returns>
        /// Подразумевается <see cref="bool"/> как результат операции.
        /// Или возможно иные выходные данные.
        /// <br>
        /// Ищи дополнение в реализации непосредственного класса.
        /// </br>
        /// </returns>
        public event Action<object?> Сompleted;
        /// <summary>
        /// Статус регистрации функции (Можно ли её выполнить)
        /// </summary>
        public bool Enable
        {
            get;
        }

        public abstract string Name { get; }

        /// <summary>
        /// <br>Ожидает получение комбинации клавиш глобального вызова в формате <see cref="FuncKeyHandler.FkeyHandler.VKeys"/>,
        /// c разделителем "+" между клавишами. Клавиши не обязательны.
        /// 
        /// Метод можно вызывать повторно для изменения комбинации клавиш, или возможно функции обратного вызова.
        /// <br>
        /// </br>
        /// <br>
        /// Ищи дополнение в реализации непосредственного класса.
        /// </br>
        /// </br>
        /// <br>
        /// </br>
        /// <br>
        /// Образец регистрации клавиш тип <see cref="string" href=" 'KEY_1 + KEY_2 + KEY_3'"/> 
        /// </br>
        /// </summary>
        /// <returns>
        /// Первый аргумент типа <see cref="bool"/> возращает <see href="true"/> в случаи успешной регистрации.
        /// <br>
        /// Вторй аргумент типа <see cref="string"/> указывает причину по кторой не удалсь зарегистрировать функцию.
        /// </br>
        /// </returns>
        public abstract Tuple<bool,string> CheckAndRegistrationFunction(object? parameter = null);

        /// <summary>
        /// Старутет функцию.
        /// </summary>
        /// <returns>Результат операции. Так же возращает <see cref="bool" href=" = false"/> в случаии, если предыдущий вызов операция еще не завершился.</returns>
        public abstract bool StartFunction(object? parameter = null);

        /// <summary>   
        /// Команда отмены.
        /// </summary>
        /// <returns>Возращает <see cref="bool" href=" = false"/> при неудачной отмене, или если операция впринципе не может быть отменена.</returns>
        public abstract bool СancelFunction(object? parameter = null);


    }
    enum TypesActionFail
    {
       

    }
}
