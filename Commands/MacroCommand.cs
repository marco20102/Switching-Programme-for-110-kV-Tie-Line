namespace Switching_Programme_for_110_kV_Tie_Line.Commands;


// Послідовне виконання кількох команд
// https://refactoring.guru/uk/design-patterns/composite
public class MacroCommand : ISwitchOperation
{
    // Список команд, які виконуватимуться по черзі
    private readonly List<ISwitchOperation> _commands = [];


    // Приймає будь-яку колекцію команд і зберігає їх у заданому порядку
    public MacroCommand(IEnumerable<ISwitchOperation> commands)
    {
        _commands.AddRange(commands);
    }

    // Виконує всі команди одну за одною в порядку додавання
    public void Execute()
    {
        _commands.ForEach((cmd) => cmd.Execute());
    }

    // Скасовуємо команди у зворотному порядку (принцип LIFO).
    // Це важливо для безпеки: спочатку скасовуємо останню дію
    public void Undo()
    {
        for (int i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
}