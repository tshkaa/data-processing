namespace DataProcessing.Lib.Models;

/// <summary>Результат определения стабилизации веса, построенный <see cref="Analysis.SettlingDetector.Detect"/>.</summary>
/// <param name="Settled">Стабилизировался ли сигнал в пределах сегмента.</param>
/// <param name="StableValue">Установившееся значение (среднее по первому стабильному окну), либо среднее по всему сегменту, если стабилизации не найдено.</param>
/// <param name="SettledAtSampleIndex">Индекс отсчёта, с которого сигнал стабилен; -1, если стабилизации не найдено.</param>
/// <param name="SettledAtSeconds">Время от начала сегмента до стабилизации, в секундах; -1, если стабилизации не найдено.</param>
/// <param name="ToleranceBand">Величина допуска (полуширина коридора), с которым сравнивался разброс сигнала.</param>
public sealed record SettlingResult(
    bool Settled,
    double StableValue,
    int SettledAtSampleIndex,
    double SettledAtSeconds,
    double ToleranceBand);
