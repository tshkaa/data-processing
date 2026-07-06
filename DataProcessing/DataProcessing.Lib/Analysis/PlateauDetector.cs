using MathNet.Numerics.Statistics;

namespace DataProcessing.Lib.Analysis;

/// <summary>
/// Находит участки сигнала с низким локальным шумом, то есть моменты,
/// когда весы "успокоились" на каком-то значении массы. Используется как
/// для разбиения одного непрерывного потока АЦП на сегменты по массам,
/// так и для подтверждения момента стабилизации внутри сегмента.
/// </summary>
public static class PlateauDetector
{
    /// <summary>Находит в массиве все подряд идущие участки с низким скользящим СКО ("плато").</summary>
    /// <param name="values">Значения сигнала для анализа.</param>
    /// <param name="windowSize">Размер скользящего окна (в отсчётах), по которому считается локальное СКО.</param>
    /// <param name="minPlateauLength">Минимальная длина участка (в отсчётах), чтобы считаться плато.</param>
    /// <param name="stdMultiplier">Во сколько раз порог "тишины" превышает шумовой пол (10-й перцентиль СКО).</param>
    /// <returns>Список плато как пар (индекс начала, длина в отсчётах), отсортированный по возрастанию индекса начала.</returns>
    public static IReadOnlyList<(int Start, int Length)> FindPlateaus(
        double[] values,
        int windowSize,
        int minPlateauLength,
        double stdMultiplier = 4.0)
    {
        var rollingStd = RollingStatistics.TrailingStdDev(values, windowSize);

        var validStdValues = rollingStd.Where(v => !double.IsNaN(v)).ToArray();
        if (validStdValues.Length == 0)
        {
            return [];
        }

        // Порог считаем от тихих участков (10-й перцентиль), чтобы не зависеть
        // от абсолютного уровня сигнала, который меняется от массы к массе.
        var noiseFloor = validStdValues.Percentile(10);
        var threshold = Math.Max(noiseFloor * stdMultiplier, 1e-9);

        var plateaus = new List<(int Start, int Length)>();
        int? runStart = null;
        for (var i = 0; i < rollingStd.Length; i++)
        {
            var isStable = !double.IsNaN(rollingStd[i]) && rollingStd[i] <= threshold;
            if (isStable)
            {
                runStart ??= i;
            }
            else if (runStart is int start)
            {
                plateaus.Add((start, i - start));
                runStart = null;
            }
        }

        if (runStart is int lastStart)
        {
            plateaus.Add((lastStart, rollingStd.Length - lastStart));
        }

        return plateaus.Where(p => p.Length >= minPlateauLength).ToList();
    }
}
