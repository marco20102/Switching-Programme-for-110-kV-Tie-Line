using Switching_Programme_for_110_kV_Tie_Line.Commands;
using Switching_Programme_for_110_kV_Tie_Line.Enums;

namespace Switching_Programme_for_110_kV_Tie_Line.Receivers;


// Клас для комутаційних апаратів, якими здійснюють оперативні перемикання
public class SwitchingDevice(DeviceType type, string name,
    SwitchPosition initial = SwitchPosition.Off,
    Action<string>? notifyLine = null)
// https://learn.microsoft.com/en-us/dotnet/api/system.action?view=net-10.0
{
    public DeviceType Type { get; } = type;
    public string Name { get; } = name;  // диспетчерське найменування
    public SwitchPosition Current { get; private set; } = initial;

    // Делегат для запиту на перерахунок стану лінії після кожної дії
    private readonly Action<string>? _recalculationRequest = notifyLine;


    // Заглушка для блокування: за замовчуванням завжди дозволяє (true)
    // Можна буде підмінити в майбутньому на реальну логіку:
    // device.Interlock = () => _line.IsLive == false;
    public Func<SwitchPosition, (bool IsAllowed, string Message)>
        Interlock
    { get; set; } = (target) => (true,
        "Оперативне блокування дозволяє дію");
    // https://learn.microsoft.com/en-us/dotnet/api/system.func-2?view=net-10.0


    // Основний метод для зміни стану апарата за розпорядженням
    public void SetPosition(string action, SwitchPosition target)
    {
        // Перевірка умов блокування перед початком операції
        var (IsAllowed, Message) = Interlock(target);
        if (!IsAllowed)
        {
            Console.WriteLine($"{DateTime.Now:dd.MM.yyyy HH:mm:ss.fffff} " +
                              $"БЛОКУВАННЯ (ВІДМОВА) {Name}: {Message}");
            return;  // перемикання неможливе
        }

        // Збереження історії для виводу в консоль
        SwitchPosition previous = Current;
        Current = target;

        // Фіксація виконання дії в оперативному журналі (тобто в консолі)
        Console.WriteLine($"{DateTime.Now:dd.MM.yyyy HH:mm:ss.fffff} " +
                          $"{action} :\n\t[{previous} -> {target}] {Name}");

        // Повідомлення класу LineBay про необхідність оновити оперативний стан
        _recalculationRequest?.Invoke(action);
    }


    // Форматований вивід поточного стану комутаційного апарата для звітності
    public override string ToString()
    {
        return $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} " +
               $"СТАН: {Current,-15} {Name,-45} [{Type}]";
    }
}


// Методи розширення для зручного виклику команд увімкнення/вимкнення
public static class SwitchingDeviceExtensions
{
    public static void TurnOn(this SwitchingDevice device, string reason)
    {
        new SwitchDeviceCommand(device, reason, SwitchPosition.On).Execute();
    }

    public static void TurnOff(this SwitchingDevice device, string reason)
    {
        new SwitchDeviceCommand(device, reason, SwitchPosition.Off).Execute();
    }
}