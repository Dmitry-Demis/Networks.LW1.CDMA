namespace CSLearning;

// Интерфейс для генерации кодов Уолша
public interface IWalshCodeGenerator
{
    int[][] GenerateMatrix(int order); // Генерация полной матрицы кодов Уолша заданного порядка
}

// Реализация генератора кодов Уолша
public class WalshCodeGenerator : IWalshCodeGenerator
{
    public int[] GenerateCode(int index) => GenerateMatrix(index).First(); // Возвращает первый код для упрощения примера

    public int[][] GenerateMatrix(int order)
    {
        // Базовый случай: матрица 1x1
        if (order == 0) return new int[][] { new[] { 1 } };

        // Рекурсивное построение матрицы Уолша
        var baseMatrix = GenerateMatrix(order - 1);
        var top = baseMatrix.Select(row => row.Concat(row).ToArray()).ToArray();
        var bottom = baseMatrix.Select(row => row.Concat(row.Select(x => -x).ToArray()).ToArray()).ToArray();

        return top.Concat(bottom).ToArray(); // Объединяем верхнюю и нижнюю части матрицы
    }
}

// Утилитный класс для работы с бинарными данными
public static class BinaryUtils
{
    // Конвертирует бинарный массив в строку
    public static string BinaryArrayToString(List<int> binaryArray)
    {
        return new string(
            binaryArray
                .Select((bit, index) => new { bit, index })
                .GroupBy(x => x.index / 8) // Разбиваем на группы по 8 бит
                .Select(group => string.Concat(group.Select(g => g.bit.ToString()))) // Преобразуем в строку бит
                .Select(binaryString => (char)Convert.ToInt32(binaryString, 2)) // Преобразуем в символ ASCII
                .ToArray() // Преобразуем результат в массив символов
        );
    }


    // Преобразует строку в массив битов
    public static List<int> StringToBinaryArray(string input) =>
        input.SelectMany(c => Convert.ToString(c, 2).PadLeft(8, '0').Select(bit => int.Parse(bit.ToString())))
            .ToList();
}

// Класс для представления станции
public class Station(string name, List<int> wordBinary)
{
    public string Name { get; } = name; // Название станции (например, "A")
    public List<int> WordBinary { get; } = wordBinary; // Бинарное представление слова
    public int[] WalshCode { get; set; } = []; // Код Уолша, изначально пустой
}

// Интерфейс для обработки данных CDMA
public interface IDataProcessor
{
    void AddStation(string stationName, string word); // Добавление станции
    void GenerateWalshCodes(); // Генерация кодов Уолша
    void TransmitData(); // Передача данных
    void DisplayDecodedWords(); // Вывод декодированных слов
}

// Основной класс для работы с CDMA-системой
public class CDMACommunicationSystem(IWalshCodeGenerator walshCodeGenerator) : IDataProcessor
{
    // Генератор кодов Уолша
    private readonly Dictionary<string, Station> _stations = new(); // Словарь станций
    private readonly Dictionary<string, List<int>> _decodedWords = new(); // Декодированные слова
    private int _maxLength = 0; // Максимальная длина слова в бинарном представлении

    // Конструктор, инициализирующий генератор кодов Уолша

    // Добавление станции и её слова
    public void AddStation(string stationName, string word)
    {
        var station = new Station(stationName, BinaryUtils.StringToBinaryArray(word));
        _stations[stationName] = station; // Добавляем станцию в словарь
        _decodedWords[stationName] = []; // Инициализируем пустой список для декодированных данных

        // Обновляем максимальную длину слова
        if (station.WordBinary.Count > _maxLength)
        {
            _maxLength = station.WordBinary.Count;
        }
    }

    // Генерация кодов Уолша для всех станций
    public void GenerateWalshCodes()
    {
        var walshCodesMatrix = walshCodeGenerator.GenerateMatrix((int)Math.Ceiling(Math.Log2(_stations.Count)));

        // Присваиваем каждой станции соответствующий код
        var stationList = _stations.Values.ToList();
        for (var i = 0; i < stationList.Count; ++i) stationList[i].WalshCode = walshCodesMatrix[i];
    }

    // Передача данных (объединение сигналов)
    public void TransmitData()
    {
        var signalSum = new int[_stations.First().Value.WalshCode.Length]; // Инициализация суммы сигналов

        for (var i = 0; i < _maxLength; ++i) // Для каждого бита
        {
            Array.Clear(signalSum, 0, signalSum.Length); // Обнуляем сумму сигналов

            foreach (var station in _stations.Values) // Обрабатываем каждую станцию
            {
                if (i >= station.WordBinary.Count) continue;
                var signal = station.WordBinary[i] == 1 ? 1 : -1; // Преобразуем бит в сигнал
                for (var j = 0; j < signalSum.Length; ++j)
                {
                    signalSum[j] += station.WalshCode[j] * signal; // Суммируем сигнал станции
                }
            }

            DecodeSignal(signalSum); // Декодируем текущую сумму сигналов
        }
    }

    // Декодирование сигнала для каждой станции
    private void DecodeSignal(int[] signalSum)
    {
        foreach (var station in _stations.Values) // Для каждой станции
        {
            var sum = signalSum.Zip(station.WalshCode, (signal, code) => signal * code).Sum(); // Скалярное произведение
            _decodedWords[station.Name].Add(sum > 0 ? 1 : 0); // Определяем бит на основе знака
        }
    }

    // Вывод декодированных слов в консоль
    public void DisplayDecodedWords()
    {
        foreach (var decodedWord in _decodedWords)
        {
            Console.WriteLine($"{decodedWord.Key} says: {BinaryUtils.BinaryArrayToString(decodedWord.Value)}");
        }
    }
}

// Основной класс приложения
public class CDMACommunicationApp(IDataProcessor dataProcessor)
{
    // Обработчик данных

    public void Run()
    {
        Console.WriteLine("Введите название станции и слово через пробел (нажмите 'exit' для завершения):");

        while (true)
        {
            // Считываем ввод пользователя
            var input = Console.ReadLine();

            // Проверяем, не пустое ли это значение или пользователь ввёл 'exit'
            if (string.IsNullOrWhiteSpace(input) || input.Trim().ToLower() == "exit")
            {
                break;  // Завершаем цикл, если пользователь вводит "exit" или пустую строку
            }

            try
            {
                // Разделяем ввод на два компонента: название станции и слово
                var parts = input.Split(' ');

                // Проверяем, что введено ровно два элемента: название станции и слово
                if (parts.Length != 2)
                    throw new ArgumentException("Введите название станции и слово, разделённые пробелом.");

                var stationName = parts[0];  // Название станции
                var word = parts[1];         // Слово, которое передаёт станция

                // Добавляем станцию и её слово в обработчик данных
                dataProcessor.AddStation(stationName, word);
            }
            catch (Exception ex)
            {
                // Обрабатываем ошибки, например, если ввод не соответствует формату
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        // Генерируем коды Уолша для всех станций
        dataProcessor.GenerateWalshCodes();

        // Симулируем передачу данных (суммирование сигналов от станций)
        dataProcessor.TransmitData();

        // Отображаем декодированные слова от каждой станции
        dataProcessor.DisplayDecodedWords();
    }

}

class Program
{
    static void Main(string[] args)
    {
        var walshCodeGenerator = new WalshCodeGenerator(); // Создаём генератор кодов Уолша
        var cdmaSystem = new CDMACommunicationSystem(walshCodeGenerator); // Создаём CDMA систему
        var app = new CDMACommunicationApp(cdmaSystem); // Создаём приложение

        app.Run(); // Запуск приложения
    }
}