using DataProcessing.Lib.Analysis;
using DataProcessing.Lib.Filtering;
using DataProcessing.Lib.Models;

namespace DataProcessing.Lib;

/// <summary>
/// Полный цикл обработки одного измерения массы: анализ исходных колебаний,
/// фильтрация сигнала и определение момента стабилизации веса.
/// </summary>
public sealed class MassMeasurementProcessor
{
    private readonly double _filterCutoffHz;
    private readonly int _medianWindow;
    private readonly int _filterHalfOrder;

    /// <summary>Создаёт обработчик с заданными параметрами фильтрации.</summary>
    /// <param name="filterCutoffHz">Частота среза ФНЧ в Гц (см. <see cref="WeightFilterPipeline"/>).</param>
    /// <param name="medianWindow">Размер окна медианного фильтра (подавление выбросов).</param>
    /// <param name="filterHalfOrder">Половина порядка ФНЧ; определяет групповую задержку фильтра в отсчётах.</param>
    public MassMeasurementProcessor(double filterCutoffHz = 4.0, int medianWindow = 7, int filterHalfOrder = 48)
    {
        _filterCutoffHz = filterCutoffHz;
        _medianWindow = medianWindow;
        _filterHalfOrder = filterHalfOrder;
    }

    /// <summary>
    /// Прогоняет один сегмент (одну массу) через полный конвейер: анализ
    /// исходных колебаний, фильтрация и определение момента стабилизации.
    /// </summary>
    /// <param name="series">Сегмент показаний АЦП для одной массы (с меткой и частотой дискретизации).</param>
    /// <returns>Итоговый отчёт: профили сигнала до/после фильтрации, результат стабилизации и оба массива значений.</returns>
    public MeasurementReport Process(MassSeries series)
    {
        var rawValues = series.Values;
        var rawProfile = OscillationAnalyzer.Analyze(rawValues, series.SampleRateHz);

        var pipeline = new WeightFilterPipeline(series.SampleRateHz, _filterCutoffHz, _medianWindow, _filterHalfOrder);
        var filteredValues = pipeline.Process(rawValues);

        var settling = SettlingDetector.Detect(filteredValues, series.SampleRateHz, pipeline.WarmupSampleCount);

        // Профиль "после фильтра" считаем по уже установившемуся участку, а не
        // по всему сегменту — иначе переходный процесс фильтра искажает оценку
        // остаточного шума/амплитуды.
        var settledPortion = settling.Settled
            ? filteredValues[settling.SettledAtSampleIndex..]
            : filteredValues;
        var filteredProfile = OscillationAnalyzer.Analyze(settledPortion, series.SampleRateHz);

        return new MeasurementReport(series.Label, series.SampleRateHz, rawProfile, filteredProfile, settling, rawValues, filteredValues);
    }
}
