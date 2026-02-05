using Switching_Programme_for_110_kV_Tie_Line.Enums;
using Switching_Programme_for_110_kV_Tie_Line.Receivers;

namespace Switching_Programme_for_110_kV_Tie_Line.Commands;


// Клас конкретної команди на оперативні перемикання
public class SwitchDeviceCommand(
    SwitchingDevice device,
    string order,
    SwitchPosition target) : ISwitchOperation
{
    private readonly SwitchingDevice _device = device;
    private readonly string _order = order;  // розпорядження на перемикання
    private readonly SwitchPosition _target = target;  // цільове положення

    private SwitchPosition _prev;  // потрібне для коректного Undo


    public void Execute()  // виконати оперативне перемикання
    {
        _prev = _device.Current;
        _device.SetPosition(_order, _target);
    }


    public void Undo()
    {
        Console.WriteLine($"Повернення до початкового стану: " +
                          $"{_device.Name} -> {_prev}");
        _device.SetPosition($"Відміна: {_order}", _prev);
    }
}