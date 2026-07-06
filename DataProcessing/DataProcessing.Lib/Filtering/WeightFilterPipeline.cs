using MathNet.Filtering;
using MathNet.Filtering.FIR;

namespace DataProcessing.Lib.Filtering;

/// <summary>
/// Стабилизирует шумный поток с АЦП весов: медианный фильтр убирает
/// выбросы (спайки), затем ФНЧ FIR гасит остаточные механические
/// колебания весов.
/// </summary>
public sealed class WeightFilterPipeline
{
    private readonly OnlineFilter _denoiseFilter;
    private readonly OnlineFilter _lowpassFilter;

    /// <summary>
    /// Число отсчётов в начале выхода, за время которых внутренняя история
    /// онлайн-фильтров ещё не набрана целиком из реальных данных (свёртка
    /// частично идёт с нулями). На этом участке выход смещён/сглажен
    /// нерепрезентативно и не должен рассматриваться как установившееся
    /// значение, даже если локальная дисперсия там формально мала.
    /// </summary>
    public int WarmupSampleCount { get; }

    /// <summary>Создаёт конвейер медианный+ФНЧ фильтр под конкретную частоту дискретизации.</summary>
    /// <param name="sampleRateHz">Частота дискретизации входного сигнала, Гц.</param>
    /// <param name="cutoffFrequencyHz">Частота среза ФНЧ, Гц.</param>
    /// <param name="medianWindow">Размер окна медианного фильтра (в отсчётах).</param>
    /// <param name="filterHalfOrder">Половина порядка ФНЧ (порядок = 2*filterHalfOrder+1); задаёт групповую задержку в отсчётах.</param>
    public WeightFilterPipeline(double sampleRateHz, double cutoffFrequencyHz = 4.0, int medianWindow = 7, int filterHalfOrder = 48)
    {
        _denoiseFilter = OnlineFilter.CreateDenoise(medianWindow);
        WarmupSampleCount = (medianWindow - 1) / 2 + filterHalfOrder;

        // Групповая задержка линейно-фазового FIR равна halforder отсчётов.
        // Прежний вариант (cutoff=2 Гц, порядок по умолчанию) давал резкий
        // срез рядом с полосой сигнала — ступенчатый отклик такого фильтра
        // долго "звенит" (эффект Гиббса) в районе частоты среза, из-за чего
        // отфильтрованный сигнал ещё десятки секунд колебался с частотой
        // ~1-2 Гц вместо быстрого схождения к весу. Поднимаем частоту среза
        // (24 Гц вибрации АЦП всё равно остаются далеко в полосе подавления)
        // и явно ограничиваем порядок фильтра, чтобы задержка укладывалась
        // в доли секунды и итоговое время определения веса было < 2 с.
        //
        // OnlineFilter.CreateLowpass(..., ImpulseResponse.Finite, ...) в MathNet.Filtering
        // 0.7.0 ошибочно передаёт половину порядка фильтра в параметр dcGain
        // FirCoefficients.LowPass, из-за чего отфильтрованный сигнал получает
        // паразитное усиление в десятки раз. Строим коэффициенты напрямую,
        // чтобы получить корректный единичный коэффициент усиления на постоянном токе.
        var coefficients = FirCoefficients.LowPass(sampleRateHz, cutoffFrequencyHz, 1.0, filterHalfOrder);
        _lowpassFilter = new OnlineFirFilter(coefficients);
    }

    /// <summary>Пропускает сигнал через медианный фильтр, затем через ФНЧ.</summary>
    /// <param name="values">Исходные значения АЦП (может быть с большим постоянным смещением).</param>
    /// <returns>Отфильтрованные значения той же длины, что и <paramref name="values"/>; первые <see cref="WarmupSampleCount"/> отсчётов — переходный процесс фильтров.</returns>
    public double[] Process(double[] values)
    {
        if (values.Length == 0)
        {
            return values;
        }

        // АЦП весов отдаёт значения с большим постоянным смещением (миллионы
        // единиц). OnlineFirFilter стартует с нулевого внутреннего состояния,
        // поэтому без вычитания смещения отфильтрованный сигнал в начале
        // сегмента "разгоняется" от нуля до реального уровня — этот выброс
        // растягивает масштаб графика так, что настоящие колебания сигнала
        // становятся незаметны. Вычитаем смещение перед фильтрацией и
        // возвращаем его обратно после.
        var offset = values[0];
        var shifted = new double[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            shifted[i] = values[i] - offset;
        }

        var denoised = _denoiseFilter.ProcessSamples(shifted);
        var lowpassed = _lowpassFilter.ProcessSamples(denoised);

        for (var i = 0; i < lowpassed.Length; i++)
        {
            lowpassed[i] += offset;
        }

        return lowpassed;
    }
}
