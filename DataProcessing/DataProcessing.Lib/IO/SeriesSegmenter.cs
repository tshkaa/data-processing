using DataProcessing.Lib.Analysis;
using DataProcessing.Lib.Models;

namespace DataProcessing.Lib.IO;

/// <summary>
/// Разбивает один непрерывный поток "индекс/значение" (когда на весы по
/// очереди ставили разные массы) на отдельные сегменты — по одному на
/// каждую массу — с помощью поиска стабильных участков сигнала.
/// </summary>
public static class SeriesSegmenter
{
    /// <summary>
    /// Разбивает непрерывный поток отсчётов на сегменты по массам: находит
    /// стабильные участки (плато), берёт из них самые длинные и восстанавливает
    /// их хронологический порядок.
    /// </summary>
    /// <param name="samples">Все отсчёты потока (индекс + значение АЦП) по порядку.</param>
    /// <param name="sampleRateHz">Частота дискретизации, Гц.</param>
    /// <param name="expectedSegmentCount">Сколько самых длинных плато оставить (по числу масс, которые ожидаются в потоке).</param>
    /// <returns>Список сегментов — по одному <see cref="MassSeries"/> на каждую найденную массу, в хронологическом порядке.</returns>
    /// <exception cref="InvalidOperationException">В потоке не найдено ни одного стабильного участка.</exception>
    public static IReadOnlyList<MassSeries> Segment(
        IReadOnlyList<SampleRecord> samples,
        double sampleRateHz,
        int expectedSegmentCount = 10)
    {
        var values = samples.Select(s => s.Value).ToArray();

        var windowSize = Math.Max(4, (int)Math.Round(sampleRateHz * 0.5));
        var minPlateauLength = Math.Max(windowSize, (int)Math.Round(sampleRateHz * 1.0));

        var plateaus = PlateauDetector.FindPlateaus(values, windowSize, minPlateauLength);

        if (plateaus.Count == 0)
        {
            throw new InvalidOperationException(
                "Не удалось найти ни одного стабильного участка в исходном сигнале для сегментации по массам.");
        }

        // Берём самые длинные плато (наиболее достоверные), затем
        // восстанавливаем хронологический порядок постановки масс.
        var selected = plateaus
            .OrderByDescending(p => p.Length)
            .Take(expectedSegmentCount)
            .OrderBy(p => p.Start)
            .ToList();

        var result = new List<MassSeries>(selected.Count);
        for (var i = 0; i < selected.Count; i++)
        {
            var (start, length) = selected[i];
            result.Add(new MassSeries
            {
                Label = $"Масса {i + 1}",
                SampleRateHz = sampleRateHz,
                Samples = samples.Skip(start).Take(length).ToList(),
            });
        }

        return result;
    }
}
