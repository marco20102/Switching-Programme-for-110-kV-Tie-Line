using Switching_Programme_for_110_kV_Tie_Line.Enums;

namespace Switching_Programme_for_110_kV_Tie_Line.Events;


// Надає дані для події зміни оперативного стану повітряної лінії.
// Цей клас фіксує "зліпок" стану системи в момент перемикання або порушення.
public class LineStateChangedEventArgs(
    OperationalState previous,
    OperationalState current,
    string? reason = null,
    string? initiator = null) : EventArgs
{
    // Стан, у якому перебувала лінія до настання події
    public OperationalState PreviousState { get; } = previous;
    // Стан лінії після завершення перемикання або спрацювання захисту
    public OperationalState NewState { get; } = current;
    public DateTime ChangedAt { get; } = DateTime.Now;  // точний час події
    public string? Reason { get; } = reason ?? "Не з'ясовано";
    public string? Initiator { get; } = initiator ?? "РЗА";  // що спричинило
    // Раптові зміни часто викликані роботою релейного захисту та автоматики

    public override string ToString()  // запис в оперативному журналі
    {
        return $"[{PreviousState} -> {NewState}] " +
               $"{ChangedAt:dd.MM.yyyy HH:mm:ss.fffff}" +
               $"\n\tПРИЧИНА: {Reason} /{Initiator}/";
        // зокрема, в інформаційно-діагностичному комплексі "Регіна"
        // дискретизація часу реєстрації подій складає 1 мікросекунда
    }
}