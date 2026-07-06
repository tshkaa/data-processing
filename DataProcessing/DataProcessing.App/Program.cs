using System.Globalization;
using DataProcessing.App;
using DataProcessing.Lib;
using DataProcessing.Lib.IO;
using DataProcessing.Lib.Models;

const double SampleRateHz = 80.0;
const int ExpectedMassCount = 10;

var filePath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "data.txt");

if (!File.Exists(filePath))
{
    Console.WriteLine($"Файл с данными не найден: {filePath}");
    Console.WriteLine("Передайте путь к файлу первым аргументом или поместите data.txt рядом с программой.");
    return;
}

var chartsDirectory = Path.Combine(AppContext.BaseDirectory, "charts");

var series = RawDataFileReader.Read(filePath, SampleRateHz, ExpectedMassCount);
var processor = new MassMeasurementProcessor();

foreach (var massSeries in series)
{
    var report = processor.Process(massSeries);
    PrintReport(report);
    ChartRenderer.Save(report, chartsDirectory);
}

Console.WriteLine($"Графики сохранены в: {chartsDirectory}");


/// <summary>
/// Печатает в консоль сводку по одному измерению массы: параметры исходного
/// и отфильтрованного сигнала, а также результат определения стабилизации.
/// </summary>
/// <param name="report">Готовый отчёт по одному сегменту (одной массе), построенный <see cref="MassMeasurementProcessor.Process"/>.</param>
/// <returns>Ничего не возвращает — результат уходит в стандартный вывод.</returns>
static void PrintReport(MeasurementReport report)
{
    Console.WriteLine($"=== {report.Label} ===");
    Console.WriteLine($"  Исходный сигнал:      среднее={Fmt(report.RawProfile.Mean)}, амплитуда(пик-пик)={Fmt(report.RawProfile.PeakToPeakAmplitude)}, " +
                       $"частота колебаний={Fmt(report.RawProfile.DominantFrequencyHz)} Гц, СКО={Fmt(report.RawProfile.StdDev)}");
    Console.WriteLine($"  Отфильтрованный сигнал: среднее={Fmt(report.FilteredProfile.Mean)}, амплитуда(пик-пик)={Fmt(report.FilteredProfile.PeakToPeakAmplitude)}, " +
                       $"частота колебаний={Fmt(report.FilteredProfile.DominantFrequencyHz)} Гц, СКО={Fmt(report.FilteredProfile.StdDev)}");

    if (report.Settling.Settled)
    {
        Console.WriteLine($"  Стабильный вес: {Fmt(report.Settling.StableValue)} (коридор ±{Fmt(report.Settling.ToleranceBand)}), " +
                           $"установился через {Fmt(report.Settling.SettledAtSeconds)} с " +
                           $"(отсчёт №{report.Settling.SettledAtSampleIndex})");
    }
    else
    {
        Console.WriteLine("  Сигнал не успел стабилизироваться в пределах сегмента.");
    }

    Console.WriteLine();
}

/// <summary>Форматирует число для вывода в консоль: до 3 знаков после запятой, точка как разделитель.</summary>
/// <param name="value">Число для форматирования.</param>
/// <returns>Строковое представление числа.</returns>
static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);