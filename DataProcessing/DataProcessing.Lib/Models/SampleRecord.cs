namespace DataProcessing.Lib.Models;

/// <summary>Один отсчёт исходного потока АЦП.</summary>
/// <param name="Index">Порядковый индекс отсчёта в исходном файле.</param>
/// <param name="Value">Значение АЦП в этом отсчёте.</param>
public readonly record struct SampleRecord(int Index, double Value);
