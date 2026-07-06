namespace DataProcessing.Lib.Models;

/// <summary>Статистический профиль колебаний сигнала, построенный <see cref="Analysis.OscillationAnalyzer.Analyze"/>.</summary>
/// <param name="Mean">Среднее значение сигнала.</param>
/// <param name="StdDev">Стандартное отклонение сигнала.</param>
/// <param name="Min">Минимальное значение сигнала.</param>
/// <param name="Max">Максимальное значение сигнала.</param>
/// <param name="PeakToPeakAmplitude">Размах сигнала (Max - Min).</param>
/// <param name="DominantFrequencyHz">Частота наиболее выраженного колебания, Гц (0, если не найдено).</param>
/// <param name="DominantFrequencyAmplitude">Амплитуда доминирующей частоты.</param>
public sealed record OscillationProfile(
    double Mean,
    double StdDev,
    double Min,
    double Max,
    double PeakToPeakAmplitude,
    double DominantFrequencyHz,
    double DominantFrequencyAmplitude);
