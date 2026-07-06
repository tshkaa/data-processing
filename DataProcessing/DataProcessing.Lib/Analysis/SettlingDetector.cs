using DataProcessing.Lib.Models;
using MathNet.Numerics.Statistics;

namespace DataProcessing.Lib.Analysis;

/// <summary>
/// Определяет стабильное (установившееся) значение веса и момент, когда
/// отфильтрованный сигнал впервые перестаёт заметно двигаться.
/// </summary>
public static class SettlingDetector
{
    /// <summary>
    /// Ищет первый момент, начиная с которого отфильтрованный сигнал держится
    /// в пределах шумового допуска (причинный, "смотрящий вперёд только на
    /// текущее окно" критерий — пригоден для оценки времени отклика).
    /// </summary>
    /// <param name="filteredValues">Отфильтрованные значения сигнала; не должны быть пустыми.</param>
    /// <param name="sampleRateHz">Частота дискретизации, Гц.</param>
    /// <param name="warmupSampleCount">
    /// Число отсчётов в начале, которые нужно исключить из поиска, так как это
    /// переходный процесс самих фильтров (см. <see cref="Filtering.WeightFilterPipeline.WarmupSampleCount"/>), а не сигнала.
    /// </param>
    /// <param name="windowSeconds">Длина скользящего окна проверки стабильности, в секундах.</param>
    /// <param name="stdMultiplier">Во сколько раз допуск превышает шумовой пол (10-й перцентиль скользящего СКО).</param>
    /// <returns>
    /// Результат стабилизации: установившееся значение, номер и время отсчёта,
    /// с которого сигнал стабилен, и величину допуска. Если сигнал так и не
    /// стабилизировался, <see cref="SettlingResult.Settled"/> равен false.
    /// </returns>
    public static SettlingResult Detect(
        double[] filteredValues,
        double sampleRateHz,
        int warmupSampleCount = 0,
        double windowSeconds = 0.3,
        double stdMultiplier = 4.0)
    {
        if (filteredValues.Length == 0)
        {
            throw new ArgumentException("Series must not be empty.", nameof(filteredValues));
        }

        var windowSize = Math.Max(4, (int)Math.Round(sampleRateHz * windowSeconds));

        // Первые warmupSampleCount отсчётов — переходный процесс самих
        // онлайн-фильтров (свёртка ещё частично идёт с нулевой историей, а не
        // с реальными данными): там локальная дисперсия может быть обманчиво
        // маленькой, хотя значение ещё не соответствует истинному сигналу.
        // Такие окна не могут считаться установившимся значением.
        var firstValidWindowStart = Math.Max(0, warmupSampleCount);
        var rollingStd = RollingStatistics.TrailingStdDev(filteredValues, windowSize);

        var validStdValues = rollingStd
            .Where((v, i) => !double.IsNaN(v) && i - windowSize + 1 >= firstValidWindowStart)
            .ToArray();
        if (validStdValues.Length == 0)
        {
            return new SettlingResult(false, filteredValues.Mean(), -1, -1, 0);
        }

        // Порог берём от тихих участков (10-й перцентиль скользящего СКО),
        // чтобы не зависеть от абсолютного уровня сигнала.
        var noiseFloor = validStdValues.Percentile(10);
        var toleranceBand = Math.Max(noiseFloor * stdMultiplier, 1e-9);

        // Причинный критерий: вес считается установившимся в первый момент,
        // когда последнее скользящее окно укладывается в допуск — а не когда
        // сигнал БОЛЬШЕ никогда (по всей записи) за него не выходит. Второй
        // вариант требует видеть будущее целиком и делает "время
        // стабилизации" случайной величиной, зависящей от шума спустя
        // десятки секунд, — непригодно для оценки веса за < 2 с.
        for (var i = 0; i < rollingStd.Length; i++)
        {
            var windowStart = i - windowSize + 1;
            if (windowStart < firstValidWindowStart || double.IsNaN(rollingStd[i]) || rollingStd[i] > toleranceBand)
            {
                continue;
            }

            var stableValue = filteredValues.AsSpan(windowStart, windowSize).ToArray().Mean();

            return new SettlingResult(
                Settled: true,
                StableValue: stableValue,
                SettledAtSampleIndex: windowStart,
                SettledAtSeconds: windowStart / sampleRateHz,
                ToleranceBand: toleranceBand);
        }

        return new SettlingResult(false, filteredValues.Mean(), -1, -1, toleranceBand);
    }
}
