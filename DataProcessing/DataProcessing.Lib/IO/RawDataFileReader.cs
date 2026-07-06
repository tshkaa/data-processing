using System.Globalization;
using DataProcessing.Lib.Models;

namespace DataProcessing.Lib.IO;

/// <summary>
/// Читает текстовый файл с показаниями АЦП. Ожидаемый формат: первая колонка —
/// индекс отсчёта, вторая — исходное значение АЦП. Остальные колонки (если
/// есть, например уже отфильтрованные на производстве значения) игнорируются.
/// Разделитель определяется автоматически (запятая, точка с запятой, табуляция
/// или пробелы). Поток считается непрерывным и автоматически разбивается на
/// сегменты по массам через <see cref="SeriesSegmenter"/>.
/// </summary>
public static class RawDataFileReader
{
    private static readonly char[] Delimiters = [',', ';', '\t'];

    /// <summary>Читает файл с показаниями АЦП и разбивает его на сегменты по массам.</summary>
    /// <param name="filePath">Путь к текстовому файлу с данными.</param>
    /// <param name="sampleRateHz">Частота дискретизации записи, Гц.</param>
    /// <param name="expectedSegmentCount">Сколько сегментов (масс) искать в потоке; см. <see cref="SeriesSegmenter.Segment"/>.</param>
    /// <returns>Список сегментов — по одному на каждую найденную массу.</returns>
    /// <exception cref="InvalidOperationException">Файл пуст или не содержит минимум 2 колонки.</exception>
    public static IReadOnlyList<MassSeries> Read(string filePath, double sampleRateHz, int expectedSegmentCount = 10)
    {
        var lines = File.ReadAllLines(filePath)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
        {
            throw new InvalidOperationException($"Файл '{filePath}' пуст.");
        }

        var delimiter = DetectDelimiter(lines[0]);
        var rows = lines.Select(l => SplitLine(l, delimiter)).ToList();

        // Пропускаем строку заголовка, если первая ячейка не число.
        if (!double.TryParse(rows[0][0], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            rows.RemoveAt(0);
        }

        if (rows[0].Length < 2)
        {
            throw new InvalidOperationException("Файл должен содержать минимум 2 колонки: индекс и значение АЦП.");
        }

        var samples = new List<SampleRecord>(rows.Count);
        foreach (var row in rows)
        {
            var index = int.Parse(row[0], CultureInfo.InvariantCulture);
            var value = double.Parse(row[1], NumberStyles.Float, CultureInfo.InvariantCulture);
            samples.Add(new SampleRecord(index, value));
        }

        return SeriesSegmenter.Segment(samples, sampleRateHz, expectedSegmentCount);
    }

    /// <summary>Определяет разделитель колонок по первой строке файла.</summary>
    /// <param name="headerLine">Первая строка файла (заголовок или строка данных).</param>
    /// <returns>Найденный символ-разделитель (запятая, точка с запятой или таб); пробел, если ни один не найден.</returns>
    private static char DetectDelimiter(string headerLine)
    {
        foreach (var candidate in Delimiters)
        {
            if (headerLine.Contains(candidate))
            {
                return candidate;
            }
        }

        return ' ';
    }

    /// <summary>Разбивает строку файла на колонки по заданному разделителю, обрезая пробелы.</summary>
    /// <param name="line">Строка для разбора.</param>
    /// <param name="delimiter">Символ-разделитель колонок (пробел означает разбиение по любому количеству пробельных символов).</param>
    /// <returns>Массив значений колонок строки.</returns>
    private static string[] SplitLine(string line, char delimiter)
    {
        var splitOptions = StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries;
        return delimiter == ' '
            ? line.Split((char[]?)null, splitOptions)
            : line.Split(delimiter, splitOptions);
    }
}
