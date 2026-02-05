using Switching_Programme_for_110_kV_Tie_Line.Commands;

namespace Switching_Programme_for_110_kV_Tie_Line.Invoker;


// Відповідає за послідовне виконання команд програми (бланка) перемикань.
// Реалізує механізм скасування (Undo) у разі виникнення помилок персоналу
// або спрацювання оперативних блокувань.
public class SwitchingOrderExecutor
{
    // Список усіх запланованих кроків програми перемикань
    private readonly List<ISwitchOperation> _steps = [];

    // Стек для зберігання успішно виконаних команд (для зворотного порядку
    // у разі скасування)
    private readonly Stack<ISwitchOperation> _undoStack = [];

    // Додає нову операцію (команду) до програми (бланка) перемикань
    public void Add(ISwitchOperation cmd) => _steps.Add(cmd);


    // Послідовно виконує всі додані команди. Якщо будь-який крок
    // завершується винятком, автоматично запускається UndoAll
    public void ExecuteAll()
    {
        int step = 1;

        try
        {
            foreach (var cmd in _steps)
            {
                Console.Write($"\tКрок {step++}\n");

                // Спроба виконання команди (перемикання або перевірка)
                cmd.Execute();
                // Якщо виконання успішне — кладемо в стек
                _undoStack.Push(cmd);  // для можливості скасування

                Console.WriteLine();
            }
            Console.WriteLine("\n\tОПЕРАТИВНІ ПЕРЕМИКАННЯ УСПІШНО ВИКОНАНО!\n");
        }

        catch (Exception ex)
        {
            Console.WriteLine($"\n\tТЕХНОЛОГІЧНЕ ПОРУШЕННЯ на кроці " +
                              $"{step - 1}: {ex.Message} !!!");
            UndoAll();  // повертаємо схему в безпечний стан
        }
    }


    // Скасовує всі успішно виконані кроки у зворотному порядку (LIFO),
    // імітуючи дії оперативного персоналу за час аварійної готовності
    private void UndoAll()
    {
        Console.WriteLine("\n\tСКАСУВАННЯ ВСІХ ЗМІН\n");
        while (_undoStack.Count > 0)
        {
            // Виймаємо останню успішну команду і викликаємо її метод Undo
            _undoStack.Pop().Undo();
        }
        Console.WriteLine("Повернуто до безпечного стану.\n");
    }


    // Очищення історії виконання для створення нової програми перемикань
    public void Clear()
    {
        _steps.Clear();
        _undoStack.Clear();
    }
}