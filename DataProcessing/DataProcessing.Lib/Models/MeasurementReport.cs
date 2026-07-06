namespace DataProcessing.Lib.Models;

/// <summary>Итоговый отчёт по одному сегменту (одной массе), построенный <see cref="MassMeasurementProcessor.Process"/>.</summary>
/// <param name="Label">Название сегмента (например, "Масса 1").</param>
/// <param name="SampleRateHz">Частота дискретизации сегмента, Гц.</param>
/// <param name="RawProfile">Профиль колебаний исходного (нефильтрованного) сигнала.</param>
/// <param name="FilteredProfile">Профиль колебаний отфильтрованного сигнала (считается по уже установившемуся участку).</param>
/// <param name="Settling">Результат определения момента и значения стабилизации веса.</param>
/// <param name="RawValues">Исходные (нефильтрованные) значения АЦП сегмента.</param>
/// <param name="FilteredValues">Отфильтрованные значения сегмента.</param>
public sealed record MeasurementReport(
    string Label,
    double SampleRateHz,
    OscillationProfile RawProfile,
    OscillationProfile FilteredProfile,
    SettlingResult Settling,
    double[] RawValues,
    double[] FilteredValues);
