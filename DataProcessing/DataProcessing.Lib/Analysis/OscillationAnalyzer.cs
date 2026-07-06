using System.Numerics;
using DataProcessing.Lib.Models;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Statistics;

namespace DataProcessing.Lib.Analysis;

public static class OscillationAnalyzer
{
    /// <summary>
    /// Считает статистический профиль сигнала (среднее, СКО, размах) и находит
    /// доминирующую частоту колебаний через БПФ.
    /// </summary>
    /// <param name="values">Массив значений сигнала (сырых или отфильтрованных); не должен быть пустым.</param>
    /// <param name="sampleRateHz">Частота дискретизации, Гц — нужна для перевода бина БПФ в герцы.</param>
    /// <returns>Профиль колебаний: среднее, СКО, мин/макс, размах пик-пик, доминирующая частота и её амплитуда.</returns>
    /// <exception cref="ArgumentException"><paramref name="values"/> пуст.</exception>
    public static OscillationProfile Analyze(double[] values, double sampleRateHz)
    {
        if (values.Length == 0)
        {
            throw new ArgumentException("Series must not be empty.", nameof(values));
        }

        var mean = values.Mean();
        var stdDev = values.StandardDeviation();
        var min = values.Minimum();
        var max = values.Maximum();

        var (frequencyHz, amplitude) = FindDominantOscillation(values, mean, sampleRateHz);

        return new OscillationProfile(
            Mean: mean,
            StdDev: stdDev,
            Min: min,
            Max: max,
            PeakToPeakAmplitude: max - min,
            DominantFrequencyHz: frequencyHz,
            DominantFrequencyAmplitude: amplitude);
    }

    /// <summary>Ищет частоту с наибольшей амплитудой в спектре сигнала (кроме постоянной составляющей).</summary>
    /// <param name="values">Значения сигнала.</param>
    /// <param name="mean">Среднее значение сигнала — вычитается перед БПФ, чтобы убрать постоянную составляющую.</param>
    /// <param name="sampleRateHz">Частота дискретизации, Гц.</param>
    /// <returns>Доминирующая частота в Гц и её амплитуда; (0, 0), если сигнал слишком короток или колебаний не найдено.</returns>
    private static (double FrequencyHz, double Amplitude) FindDominantOscillation(double[] values, double mean, double sampleRateHz)
    {
        if (values.Length < 4)
        {
            return (0, 0);
        }

        var spectrum = new Complex[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            spectrum[i] = new Complex(values[i] - mean, 0);
        }

        Fourier.Forward(spectrum, FourierOptions.Matlab);

        var halfLength = spectrum.Length / 2;
        var bestBin = -1;
        var bestMagnitude = 0.0;

        // Пропускаем бин 0 (постоянная составляющая уже убрана вычитанием среднего).
        for (var bin = 1; bin < halfLength; bin++)
        {
            var magnitude = spectrum[bin].Magnitude;
            if (magnitude > bestMagnitude)
            {
                bestMagnitude = magnitude;
                bestBin = bin;
            }
        }

        if (bestBin <= 0)
        {
            return (0, 0);
        }

        var frequencyHz = bestBin * sampleRateHz / values.Length;
        var amplitude = 2.0 * bestMagnitude / values.Length;
        return (frequencyHz, amplitude);
    }
}
