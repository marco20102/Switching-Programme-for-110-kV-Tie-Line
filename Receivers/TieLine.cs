using Switching_Programme_for_110_kV_Tie_Line.Enums;
using Switching_Programme_for_110_kV_Tie_Line.Events;

namespace Switching_Programme_for_110_kV_Tie_Line.Receivers;


// Приєднання лінії до підстанції з двома системами шин 110 або 150 кВ
public class LineBay(string oppositeSubstationName, string thisSubstationName,
    ushort voltage, Action<string> notifyStateChange)
{
    public string OppositeSubstationName { get; } = oppositeSubstationName;
    public string ThisSubstationName { get; } = thisSubstationName;
    public ushort Voltage { get; } = voltage;


    // Ввід лінії (перевірка напруги у точці між ПЛ та лінійним роз'єднувачем)
    public ElectricNode LineNode { get; } = new ElectricNode($"на вводі ПЛ " +
        $"{voltage} кВ {oppositeSubstationName} на ПС {thisSubstationName}");

    // Розгалуження шинних роз'єднувачів (точка між вимикачем і роз'єднувачами)
    public ElectricNode ForkNode { get; } = new ElectricNode($"на " +
        $"розгалуженні ШР-{voltage} {oppositeSubstationName}");

    // Початковий стан вузлів (оскільки лінія заземлена у разі ініціалізації)
    private void InitializeNodes()
    {
        LineNode.HasVoltage = false;
        ForkNode.HasVoltage = false;
        LineNode.IsGrounded = true;
        ForkNode.IsGrounded = true;
    }


    // Вимикач повітряної лінії (елегазовий, вакуумний, повітряний)
    public SwitchingDevice CB { get; } = new(
        DeviceType.CircuitBreaker,
        $"В-{voltage} {oppositeSubstationName}",
        SwitchPosition.Off, notifyStateChange);

    // Лінійний роз'єднувач
    public SwitchingDevice LineDisc { get; } = new(
        DeviceType.Disconnector,
        $"ЛР-{voltage} {oppositeSubstationName}",
        SwitchPosition.Off, notifyStateChange);

    // Заземлювальний ніж лінії
    public SwitchingDevice LineES { get; } = new(
        DeviceType.EarthingSwitch,
        $"ЗН ЛР-{voltage} {oppositeSubstationName} у бік ПЛ",
        SwitchPosition.On, notifyStateChange);

    // Шинний роз'єднувач першої системи шин
    public SwitchingDevice BusDisc_BB1 { get; } = new(
        DeviceType.Disconnector,
        $"ШР-{voltage}-1 ПЛ {oppositeSubstationName}",
        SwitchPosition.Off, notifyStateChange);  // on the first busbar

    // Шинний роз'єднувач другої системи шин 
    public SwitchingDevice BusDisc_BB2 { get; } = new(
        DeviceType.Disconnector,
        $"ШР-{voltage}-2 ПЛ {oppositeSubstationName}",
        SwitchPosition.Off, notifyStateChange);  // on the second busbar

    // Заземлювальний ніж розвилки шинних роз'єднувачів
    public SwitchingDevice ForkES { get; } = new(
        DeviceType.EarthingSwitch,
        $"ЗН ШР-{voltage} ПЛ {oppositeSubstationName} у бік вимикача",
        SwitchPosition.On, notifyStateChange);


    // Перевірка, чи підключена лінійна комірка хоча б до однієї системи шин
    public bool IsConnectedToBus =>
        BusDisc_BB1.Current == SwitchPosition.On ||
        BusDisc_BB2.Current == SwitchPosition.On;

    // Чи накладено хоча б одне стаціонарне заземлення в межах цієї комірки
    public bool HasAnyEarthingOn =>
        LineES.Current == SwitchPosition.On ||
        ForkES.Current == SwitchPosition.On;


    // Зручний доступ до всіх апаратів приєднання для циклів або пошуку
    public IReadOnlyList<SwitchingDevice> GetAllDevices =>
        [CB, LineDisc, LineES, BusDisc_BB1, BusDisc_BB2, ForkES];
    // https://learn.microsoft.com/ru-ru/dotnet/api/system.collections.generic.ireadonlylist-1?view=net-10.0
}


// Окремий клас, відповідальний тільки за визначення оперативного стану ПЛ
// шляхом аналізу сукупності положень апаратів на обох кінцях
public class LineStateEvaluator
{
    private static OperationalState EvaluateInternal(
        bool bothBreakersOn,
        bool bothLineDisconnectorsOn,
        bool bothLineDisconnectorsOff,
        bool bothLineEarthSwitchesOn,
        bool connectedToBusbars,
        bool anyEarthingOn)
    {
        if (bothBreakersOn && bothLineDisconnectorsOn && !anyEarthingOn &&
            connectedToBusbars)
            return OperationalState.InService;

        if (!bothBreakersOn && bothLineDisconnectorsOn && !anyEarthingOn &&
            connectedToBusbars)
            return OperationalState.OutOfService;

        if (bothLineDisconnectorsOff && bothLineEarthSwitchesOn)
            return OperationalState.Earthed;

        if (bothLineDisconnectorsOff && !anyEarthingOn)
            return OperationalState.Isolated;

        // Якщо жоден з чітких випадків не підійшов
        return OperationalState.Switching;
    }


    // Визначає оперативний стан лінії на основі реальних об'єктів LineBay
    public static OperationalState Evaluate(LineBay lineAonSubstB,
        LineBay lineBonSubstA)
    {
        // Збір агрегованих даних з двох кінців лінії
        bool bothBreakersOn =
            lineAonSubstB.CB.Current == SwitchPosition.On &&
            lineBonSubstA.CB.Current == SwitchPosition.On;

        bool bothLineDisconnectorsOn =
            lineAonSubstB.LineDisc.Current == SwitchPosition.On &&
            lineBonSubstA.LineDisc.Current == SwitchPosition.On;

        bool bothLineDisconnectorsOff =
            lineAonSubstB.LineDisc.Current == SwitchPosition.Off &&
            lineBonSubstA.LineDisc.Current == SwitchPosition.Off;

        bool bothLineEarthSwitchesOn =
            lineAonSubstB.LineES.Current == SwitchPosition.On &&
            lineBonSubstA.LineES.Current == SwitchPosition.On;

        bool connectedToBusbars =
            lineAonSubstB.IsConnectedToBus && lineBonSubstA.IsConnectedToBus;

        bool anyEarthingOn =
            lineAonSubstB.HasAnyEarthingOn || lineBonSubstA.HasAnyEarthingOn;

        return EvaluateInternal(bothBreakersOn, bothLineDisconnectorsOn,
            bothLineDisconnectorsOff, bothLineEarthSwitchesOn,
            connectedToBusbars, anyEarthingOn);
    }


    // Перевантаження для тестів (без об'єктів LineBay)
    public static OperationalState Evaluate(
        bool bothBreakersOn,
        bool bothLineDisconnectorsOn,
        bool bothLineDisconnectorsOff,
        bool bothLineEarthSwitchesOn,
        bool connectedToBusbars,
        bool anyEarthingOn)
    {
        return EvaluateInternal(bothBreakersOn, bothLineDisconnectorsOn,
            bothLineDisconnectorsOff, bothLineEarthSwitchesOn,
            connectedToBusbars, anyEarthingOn);
    }
}


// Модель транзитної лінії електропересилання (повітряної лінії).
// Містить дві комірки (EndA й EndB) з протилежних кінців та координує їх стани.
public class Line
{
    public string Name { get; }
    public ushort Voltage { get; }

    public LineBay EndA { get; }  // або ж підстанція А
    public LineBay EndB { get; }  // підстанція Б

    // Поточний оперативний стан лінії
    public OperationalState CurrentOperationalState { get; private set; } =
        OperationalState.Earthed;

    // Подія зміни оперативного стану, на яку підписується "оперативний журнал"
    public event EventHandler<LineStateChangedEventArgs>? LineStateChanged;


    public Line(string endAName, string endBName, ushort voltage = 110)
    {
        Name = $"ПЛ {voltage} кВ {endAName} – {endBName}";
        Voltage = voltage;

        // Під час створення лінії передаємо RecalculateState як зворотний
        // виклик (callback). Це дозволяє комутаційним апаратам автоматично
        // "будити" лінію в разі зміни свого положення.
        EndA = new LineBay(endBName, endAName, voltage, RecalculateState);
        EndB = new LineBay(endAName, endBName, voltage, RecalculateState);

        RecalculateState("Ініціалізація лінії");
    }


    // Автоматичний розрахунок оперативного стану лінії на основі положень 
    // комутаційних апаратів на її кінцях
    private void RecalculateState(string reason = "Автоматичний перерахунок")
    {
        OperationalState newState = LineStateEvaluator.Evaluate(EndA, EndB);

        if (newState != CurrentOperationalState)
        {
            var old = CurrentOperationalState;
            CurrentOperationalState = newState;

            // Візуальне виділення зміни стану в консолі (синім кольором)
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            LineStateChanged?.Invoke(this, new LineStateChangedEventArgs(
                old, newState, reason, "Автоматичний аналіз стану"));
            Console.ResetColor();
        }
    }


    // Повертає перелік усіх комутаційних апаратів на обох кінцях лінії
    public IEnumerable<SwitchingDevice> GetAllDevices()
    {
        // Об'єднуємо пристрої з обох кінців
        return EndA.GetAllDevices.Concat(EndB.GetAllDevices);
        // https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.concat?view=net-10.0
    }


    public override string ToString()
    {
        return $"{DateTime.Now:dd.MM.yyyy HH:mm:ss.fffff} {Name,-45} -> " +
               $"{CurrentOperationalState}";
    }
}