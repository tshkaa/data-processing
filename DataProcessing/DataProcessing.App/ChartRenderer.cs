using DataProcessing.Lib.Models;
using ScottPlot;

namespace DataProcessing.App;

/// <summary>
/// Строит график "до/после фильтрации" для одного измерения массы и
/// сохраняет его в PNG — наглядная часть итогового отчёта.
/// </summary>
internal static class ChartRenderer
{
    /// <summary>
    /// Строит и сохраняет в PNG график "исходный сигнал / после фильтрации" для
    /// одного измерения массы. Имя файла берётся из <see cref="MeasurementReport.Label"/>.
    /// </summary>
    /// <param name="report">Отчёт по одному сегменту (одной массе) с сырыми и отфильтрованными значениями.</param>
    /// <param name="outputDirectory">Папка для сохранения PNG; создаётся, если не существует.</param>
    /// <returns>Ничего не возвращает — результат сохраняется на диск как файл PNG.</returns>
    public static void Save(MeasurementReport report, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var samplePeriod = 1.0 / report.SampleRateHz;

        var plot = new Plot();
        var rawSignal = plot.Add.Signal(report.RawValues, samplePeriod);
        rawSignal.LegendText = "Исходный сигнал АЦП";
        rawSignal.Color = Colors.Orange;

        var filteredSignal = plot.Add.Signal(report.FilteredValues, samplePeriod);
        filteredSignal.LegendText = "После фильтрации";
        filteredSignal.Color = Colors.Blue;

        plot.Title(report.Label);
        plot.XLabel("Время, с");
        plot.YLabel("Показания АЦП");
        plot.ShowLegend();

        var fileName = $"{SanitizeFileName(report.Label)}.png";
        plot.SavePng(Path.Combine(outputDirectory, fileName), 1200, 600);
    }

    /// <summary>Заменяет в строке символы, недопустимые в имени файла, на подчёркивание.</summary>
    /// <param name="label">Исходное название (например, <see cref="MeasurementReport.Label"/>).</param>
    /// <returns>Строка, пригодная для использования как имя файла.</returns>
    private static string SanitizeFileName(string label)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = label.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
