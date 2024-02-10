using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FVH.Background.Input.Infrastructure.Interfaces
{
    /// <summary>
    /// <br><see langword="En"/></br>
    /// <br>This interface declares a contract to manage the tasks of the keyboard input class.</br>
    /// <br><see langword="Ru"/></br>
    /// <br>Этот интерфейс объявляет контракт для управления задачами класса клавиатурного ввода.</br>
    /// </summary>
    public interface IKeyboardCallback
    {
        /// <summary>
        /// <br><see langword="En"/></br>
        /// <br>Adds a new task to the pending queue. If there is a task with an identical key combination, it is added to the group and will subsequently be called in parallel.</br>
        /// <br><see langword="Ru"/></br>
        /// <br>Добавляет новую задачу в очередь ожидающих. Если существует задача с идентичной комбинацией клавиш, она добавляется в группу и впоследствии будет вызываться параллельно.</br>
        /// </summary>
        /// <param name="keyCombo">
        /// <br><see langword="En"/></br>
        /// <br>Key combination.</br>
        /// <br><see langword="Ru"/></br>
        /// <br>Комбинация клавиш.</br>
        /// </param>
        public Task AddCallBackTask(VKeys[] keyCombo, Func<Task> callbackTask, object? identifier = null);
        public Task<bool> DeleteATaskByAnIdentifier(object identifier); //todo удалить null и добавить удаление по группе, возможно метод по полной полной очистке?
        public Task<bool> ContainsKeyComibantion(VKeys[] keyCombo);
        public List<RegFunctionGroupKeyboard> ReturnGroupRegFunctions();
    }
}
