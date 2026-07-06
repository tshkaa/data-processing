namespace DataProcessing.Lib.Models;

/// <summary>Один сегмент непрерывного потока АЦП, соответствующий одной поставленной на весы массе.</summary>
public sealed class MassSeries
{
    /// <summary>Название сегмента для отчётов и имени файла графика (например, "Масса 1").</summary>
    public required string Label { get; init; }

    /// <summary>Частота дискретизации отсчётов сегмента, Гц.</summary>
    public required double SampleRateHz { get; init; }

    /// <summary>Отсчёты сегмента (индекс + значение АЦП) в хронологическом порядке.</summary>
    public required IReadOnlyList<SampleRecord> Samples { get; init; }

    /// <summary>Значения АЦП из <see cref="Samples"/>, извлечённые в массив и закэшированные при первом обращении.</summary>
    public double[] Values => _values ??= Samples.Select(s => s.Value).ToArray();

    private double[]? _values;
}
