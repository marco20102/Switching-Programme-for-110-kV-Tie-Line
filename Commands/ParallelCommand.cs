namespace Switching_Programme_for_110_kV_Tie_Line.Commands;


// Паралельне виконання
// структурно https://refactoring.guru/uk/design-patterns/composite
// логічно https://refactoring.guru/uk/design-patterns/strategy
public class ParallelCommand : ISwitchOperation
{
    private readonly List<ISwitchOperation> _commands = [];

    public ParallelCommand(IEnumerable<ISwitchOperation> commands)
    {
        _commands.AddRange(commands);
    }

    public void Execute()
    {
        // https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task?view=net-10.0
        // https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.run?view=net-10.0
        var tasks = _commands
            .Select(cmd => Task.Run(() => cmd.Execute()))
            .ToArray();
        Task.WaitAll(tasks);
        // https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.waitall?view=net-10.0
    }

    public void Undo()
    {
        for (int i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
}

// Чому код синхронний?
// https://grok.com/share/c2hhcmQtNQ_01eb333c-abdd-4101-907c-066fcae8bb02?rid=6593555f-6939-4b11-adfc-b165f7234e1a