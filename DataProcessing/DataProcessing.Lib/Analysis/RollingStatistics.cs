using MathNet.Numerics.Statistics;

namespace DataProcessing.Lib.Analysis;

internal static class RollingStatistics
{
    /// <summary>Считает скользящее (по предшествующим отсчётам) стандартное отклонение.</summary>
    /// <param name="values">Значения сигнала.</param>
    /// <param name="windowSize">Размер окна в отсчётах.</param>
    /// <returns>
    /// Массив той же длины, что и <paramref name="values"/>: элемент i — СКО
    /// окна values[i-windowSize+1..i]; для i &lt; windowSize-1 — <see cref="double.NaN"/> (окно ещё не набрано).
    /// </returns>
    public static double[] TrailingStdDev(double[] values, int windowSize)
    {
        var result = new double[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            if (i < windowSize - 1)
            {
                result[i] = double.NaN;
                continue;
            }

            result[i] = values.AsSpan(i - windowSize + 1, windowSize).ToArray().StandardDeviation();
        }

        return result;
    }
}
