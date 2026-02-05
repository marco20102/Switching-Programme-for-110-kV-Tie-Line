using Switching_Programme_for_110_kV_Tie_Line.Enums;
using Switching_Programme_for_110_kV_Tie_Line.Receivers;

namespace Switching_Programme_for_110_kV_Tie_Line.Commands;


// Конкретна команда для перевірки стану оперативним персоналом
public class VerifyDeviceStateCommand(
    SwitchingDevice device,
    string checkOrder,
    SwitchPosition required) : ISwitchOperation
{
    private readonly SwitchingDevice _device = device;
    private readonly string _checkOrder = checkOrder;  // зміст перевірки
    private readonly SwitchPosition _required = required;  // очікуваний стан


    public void Execute()
    {
        Console.WriteLine($"{DateTime.Now:dd.MM.yyyy HH:mm:ss.fffff} " +
            $"ПЕРЕВІРКА {_checkOrder}... ");

        if (_device.Current != _required)
        {
            Console.WriteLine($"{DateTime.Now:dd.MM.yyyy HH:mm:ss.fffff} " +
                $"КРИТИЧНА ПОМИЛКА! Очікується [{_required}], " +
                $"а фактично є [{_device.Current}]");
            throw new InvalidOperationException($"Не виконано: {_checkOrder}");
        }

        Console.WriteLine($"{DateTime.Now:dd.MM.yyyy HH:mm:ss.fffff} " +
            $"ПІДТВЕРДЖЕНО: стан {_device.Name} [{_required}]");
    }


    public void Undo()
    {
        // перевірка не змінює стан!
    }
}