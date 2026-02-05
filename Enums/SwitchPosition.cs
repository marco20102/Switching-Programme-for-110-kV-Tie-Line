namespace Switching_Programme_for_110_kV_Tie_Line.Enums;


// Положення одиночного комутаційного апарата
public enum SwitchPosition
{
    IntermediateState = 0,  // проміжний стан під час перемикань
    Off = 1,                // вимкнено / розімкнено / відкрито
    On = 2,                 // увімкнено / замкнено / закрито
    BadState = 3            // помилка сигналізації / відмова вимикача
}