// https://zakon.rada.gov.ua/laws/show/z0211-18#n1037
// Програма (автоматизований бланк) перемикань транзитної лінії 110 (150) кВ
using Switching_Programme_for_110_kV_Tie_Line.Commands;
using Switching_Programme_for_110_kV_Tie_Line.Enums;
using Switching_Programme_for_110_kV_Tie_Line.Invoker;
using Switching_Programme_for_110_kV_Tie_Line.Receivers;

namespace Switching_Programme_for_110_kV_Tie_Line;


internal class Program
{
    // Обробник події зміни оперативного стану лінії
    private static void OnLineStateChanged(object? sender, EventArgs e)
    {
        Console.WriteLine($"Зміна оперативного стану лінії: {e}");
    }


    // Допоміжний метод для виводу поточного стану всіх апаратів у консоль
    private static void PrintStates(IEnumerable<SwitchingDevice> devices)
    {
        foreach (var device in devices) { Console.WriteLine(device); }
    }


    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Ініціалізація транзитної лінії 150 кВ між двома підстанціями
        var line = new Line(endAName: "Міняйлівка", endBName: "Тростянець",
            voltage: 150);

        // Підписка на подію зміни стану для автоматичного логування
        line.LineStateChanged += OnLineStateChanged;

        // Створення виконавця (Invoker), який буде керувати чергою команд
        var switchingOrderExecutor = new SwitchingOrderExecutor();


        // 1. Увімкнення лінії

        // 1) та 2) Розземлити обидва кінці лінії (можна виконувати одночасно)
        switchingOrderExecutor.Add(new ParallelCommand(
        [
            new MacroCommand(
            [
                new SwitchDeviceCommand(line.EndA.ForkES,
                    $"Вимкнути заземлювальні ножі розвилки ШР на " +
                    $"ПС {line.EndA.ThisSubstationName}", SwitchPosition.Off),
                new SwitchDeviceCommand(line.EndA.LineES,
                    $"Вимкнути заземлювальні ножі лінії на " +
                    $"ПС {line.EndA.ThisSubstationName}", SwitchPosition.Off)
            ]),

            new MacroCommand(
            [
                new SwitchDeviceCommand(line.EndB.ForkES,
                    $"Вимкнути заземлювальні ножі розвилки ШР на " +
                    $"ПС {line.EndB.ThisSubstationName}", SwitchPosition.Off),
                new SwitchDeviceCommand(line.EndB.LineES,
                    $"Вимкнути заземлювальні ножі лінії на " +
                    $"ПС {line.EndB.ThisSubstationName}", SwitchPosition.Off)
            ]),
        ]));


        // 3) та 4) Увімкнути ШР та ЛР на обох кінцях (можна робити одночасно)
        switchingOrderExecutor.Add(new ParallelCommand(
        [
            new MacroCommand(
            [
                new SwitchDeviceCommand(line.EndB.BusDisc_BB1,
                    $"На ПС {line.EndB.ThisSubstationName} " +
                    $"увімкнути ШР на першу систему шин", SwitchPosition.On),
                new SwitchDeviceCommand(line.EndB.LineDisc,
                    $"Увімкнути лінійний роз'єднувач на " +
                    $"ПС {line.EndB.ThisSubstationName}", SwitchPosition.On)
            ]),

            new MacroCommand(
            [
                new SwitchDeviceCommand(line.EndA.BusDisc_BB2,
                    $"На ПС {line.EndA.ThisSubstationName} " +
                    $"увімкнути ШР на другу систему шин", SwitchPosition.On),
                new SwitchDeviceCommand(line.EndA.LineDisc,
                    $"Увімкнути лінійний роз'єднувач на " +
                    $"ПС {line.EndA.ThisSubstationName}", SwitchPosition.On)
            ]),
        ]));


        // 5) Подати напругу на лінію
        switchingOrderExecutor.Add(new SwitchDeviceCommand(line.EndA.CB,
            $"Увімкнути вимикач на ПС {line.EndA.ThisSubstationName}",
            SwitchPosition.On));


        // 6) Замкнути транзит
        switchingOrderExecutor.Add(new SwitchDeviceCommand(line.EndB.CB,
            $"Увімкнути вимикач на ПС {line.EndB.ThisSubstationName}",
            SwitchPosition.On));


        Console.WriteLine($"\n\t\tПРОГРАМА ПЕРЕМИКАНЬ {line.Name}");
        Console.WriteLine($"\n\n\tУВІМКНЕННЯ ТРАНЗИТНОЇ ЛІНІЇ\n");
        Console.WriteLine("\nВИХІДНА СХЕМА: згідно з додатком 7 до " +
            "Правил виконання оперативних перемикань в електроустановках\n");
        switchingOrderExecutor.ExecuteAll();
        PrintStates(line.GetAllDevices());
        switchingOrderExecutor.Clear(); // очищення для наступної послідовності


        // 2. Вимкнення лінії

        // 1) Розірвати транзит
        switchingOrderExecutor.Add(new SwitchDeviceCommand(line.EndA.CB,
            $"Вимкнути вимикач на ПС {line.EndA.ThisSubstationName}",
            SwitchPosition.Off));


        // 2) Вивести лінію з роботи
        switchingOrderExecutor.Add(new SwitchDeviceCommand(line.EndB.CB,
            $"Вимкнути вимикач на ПС {line.EndB.ThisSubstationName}",
            SwitchPosition.Off));


        // 3) та 4) Розібрати схему роз'єднувачами (можна робити одночасно)
        switchingOrderExecutor.Add(new ParallelCommand(
        [
            new MacroCommand(
            [
                new SwitchDeviceCommand(line.EndB.LineDisc,
                    $"Вимкнути лінійний роз'єднувач на " +
                    $"ПС {line.EndB.ThisSubstationName}", SwitchPosition.Off),
                new SwitchDeviceCommand(line.EndB.BusDisc_BB1,
                    $"На ПС {line.EndB.ThisSubstationName} " +
                    $"Вимкнути ШР першої системи шин", SwitchPosition.Off),
                new VerifyDeviceStateCommand(line.EndB.BusDisc_BB2,
                    $"Упевнитись, що на ПС {line.EndB.ThisSubstationName} " +
                    $"ШР другої системи шин вимкнений", SwitchPosition.Off)
            ]),

            new MacroCommand(
            [
                new SwitchDeviceCommand(line.EndA.LineDisc,
                    $"Вимкнути лінійний роз'єднувач на " +
                    $"ПС {line.EndA.ThisSubstationName}", SwitchPosition.Off),
                new VerifyDeviceStateCommand(line.EndA.BusDisc_BB1,
                    $"Упевнитись, що на ПС {line.EndA.ThisSubstationName} " +
                    $"ШР першої системи шин вимкнений", SwitchPosition.Off),
                new SwitchDeviceCommand(line.EndA.BusDisc_BB2,
                    $"На ПС {line.EndA.ThisSubstationName} " +
                    $"Вимкнути ШР другої системи шин", SwitchPosition.Off)
            ]),
        ]));


        // 5) та 6) Уземлити лінію
        switchingOrderExecutor.Add(new ParallelCommand(
        [
            new MacroCommand(
            [
                new CheckVoltageCommand(line.EndA.LineNode,
                    $"Перевірити відсутність напруги на вводі ПЛ " +
                    $"{line.EndA.Voltage} кВ " +
                    $"{line.EndA.OppositeSubstationName} " +
                    $"на ПС {line.EndA.ThisSubstationName}"),
                new SwitchDeviceCommand(line.EndA.LineES,
                    $"Увімкнути заземлювальні ножі у бік лінії на " +
                    $"ПС {line.EndA.ThisSubstationName}", SwitchPosition.On),
                new CheckVoltageCommand(line.EndA.ForkNode,
                    $"Перевірити відсутність напруги на розгалуженні ШР " +
                    $"у ланці ПЛ {line.EndA.Voltage} кВ " +
                    $"{line.EndA.OppositeSubstationName} " +
                    $"на ПС {line.EndA.ThisSubstationName}"),
                new SwitchDeviceCommand(line.EndA.ForkES,
                    $"Увімкнути заземлювальні ножі у бік вимикача лінії на " +
                    $"ПС {line.EndA.ThisSubstationName}", SwitchPosition.On)
            ]),

            new MacroCommand(
            [
                new CheckVoltageCommand(line.EndB.LineNode,
                    $"Перевірити відсутність напруги на вводі ПЛ " +
                    $"{line.EndB.Voltage} кВ " +
                    $"{line.EndB.OppositeSubstationName} " +
                    $"на ПС {line.EndB.ThisSubstationName}"),
                new SwitchDeviceCommand(line.EndB.LineES,
                    $"Увімкнути заземлювальні ножі у бік лінії на " +
                    $"ПС {line.EndB.ThisSubstationName}", SwitchPosition.On),
                new CheckVoltageCommand(line.EndB.ForkNode,
                    $"Перевірити відсутність напруги на розгалуженні ШР " +
                    $"у ланці ПЛ {line.EndB.Voltage} кВ " +
                    $"{line.EndB.OppositeSubstationName} " +
                    $"на ПС {line.EndB.ThisSubstationName}"),
                new SwitchDeviceCommand(line.EndB.ForkES,
                    $"Увімкнути заземлювальні ножі у бік вимикача лінії на " +
                    $"ПС {line.EndB.ThisSubstationName}", SwitchPosition.On)
            ]),
        ]));


        Console.WriteLine($"\n\n\tВИМКНЕННЯ ТРАНЗИТНОЇ ЛІНІЇ\n");
        switchingOrderExecutor.ExecuteAll();
        PrintStates(line.GetAllDevices());
        switchingOrderExecutor.Clear();
    }
}