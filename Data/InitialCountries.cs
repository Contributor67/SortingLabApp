using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SortingLabApp.Models;

namespace SortingLabApp.Data
{
    public class TableLoadResult
    {
        public bool IsValid { get; set; }
        public List<Country> Records { get; set; } = new();
        public List<string> Columns { get; set; } = new();
        public HashSet<string> NumericColumns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string? FilterColumnName { get; set; }
        public List<string> FilterValues { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool IsStandardCountrySchema { get; set; }
    }

    public static class InitialCountries
    {
        public static List<Country> GetDefaultCountries()
        {
            return new List<Country>
            {
                new("Франция", "Европа", "Париж", 643801, 67750000),
                new("Германия", "Европа", "Берлин", 357022, 84300000),
                new("Италия", "Европа", "Рим", 301340, 58850000),
                new("Испания", "Европа", "Мадрид", 505990, 47420000),
                new("Польша", "Европа", "Варшава", 312696, 37750000),
                new("Норвегия", "Европа", "Осло", 385207, 5450000),
                new("Греция", "Европа", "Афины", 131957, 10430000),
                new("Швейцария", "Европа", "Берн", 41285, 8700000),
                new("Исландия", "Европа", "Рейкьявик", 103000, 376000),
                new("Китай", "Азия", "Пекин", 9596961, 1412000000),
                new("Индия", "Азия", "Нью-Дели", 3287263, 1408000000),
                new("Япония", "Азия", "Токио", 377975, 125100000),
                new("Южная Корея", "Азия", "Сеул", 100210, 51740000),
                new("Казахстан", "Азия", "Астана", 2724900, 19600000),
                new("Таиланд", "Азия", "Бангкок", 513120, 71600000),
                new("Вьетнам", "Азия", "Ханой", 331212, 98190000),
                new("Египет", "Африка", "Каир", 1002450, 109300000),
                new("Нигерия", "Африка", "Абуджа", 923768, 218500000),
                new("ЮАР", "Африка", "Претория", 1221037, 59890000),
                new("Марокко", "Африка", "Рабат", 446550, 37460000),
                new("Кения", "Африка", "Найроби", 580367, 54030000),
                new("Эфиопия", "Африка", "Аддис-Абеба", 1104300, 123400000),
                new("США", "Северная Америка", "Вашингтон", 9833517, 333300000),
                new("Канада", "Северная Америка", "Оттава", 9984670, 38930000),
                new("Мексика", "Северная Америка", "Мехико", 1964375, 127500000),
                new("Бразилия", "Южная Америка", "Бразилиа", 8515767, 215300000),
                new("Аргентина", "Южная Америка", "Буэнос-Айрес", 2780400, 45810000),
                new("Колумбия", "Южная Америка", "Богота", 1141748, 51870000),
                new("Чили", "Южная Америка", "Сантьяго", 756102, 19600000),
                new("Перу", "Южная Америка", "Лима", 1285216, 34050000),
                new("Австралия", "Океания", "Канберра", 7692024, 25690000),
                new("Новая Зеландия", "Океания", "Веллингтон", 268838, 5120000),
                new("Фиджи", "Океания", "Сува", 18274, 926000)
            };
        }

        public static List<Country> GenerateRandomCountries(int count)
        {
            var rng = new Random();
            var continents = new[] { "Европа", "Азия", "Африка", "Северная Америка", "Южная Америка", "Океания" };
            var prefixes = new[] { "Новая", "Северная", "Южная", "Великая", "Западная", "Восточная", "Островная", "Горная" };
            var roots = new[] { "Ландия", "Стания", "Ария", "Ония", "Глория", "Терра", "Вальдия", "Вия", "Эрика" };

            var list = new List<Country>();
            for (int i = 1; i <= count; i++)
            {
                string name = $"{prefixes[rng.Next(prefixes.Length)]} {roots[rng.Next(roots.Length)]} #{i}";
                string continent = continents[rng.Next(continents.Length)];
                string capital = $"Город-{rng.Next(100, 999)}";
                double area = rng.Next(10000, 5000000);
                long pop = (long)rng.Next(50000, 100000000);

                list.Add(new Country(name, continent, capital, area, pop));
            }
            return list;
        }

        public static void SaveToFile(string filePath, IEnumerable<Country> countries)
        {
            var list = countries as IList<Country> ?? countries.ToList();
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

            if (list.Count > 0 && list[0].IsCustomRecord && list[0].CustomColumnOrder.Count > 0)
            {
                writer.WriteLine(string.Join(";", list[0].CustomColumnOrder));
            }
            else
            {
                writer.WriteLine("Name;Continent;Capital;Area;Population");
            }

            foreach (var c in list)
            {
                writer.WriteLine(c.ToCsvLine());
            }
        }

        public static List<Country> LoadFromFile(string filePath)
        {
            var list = new List<Country>();
            using var reader = new StreamReader(filePath, System.Text.Encoding.UTF8);
            string? header = reader.ReadLine(); // заголовок
            if (string.IsNullOrWhiteSpace(header)) return list;

            bool isDefaultHeader = header.Trim().Equals("Name;Continent;Capital;Area;Population", StringComparison.OrdinalIgnoreCase);
            if (isDefaultHeader)
            {
                while (!reader.EndOfStream)
                {
                    string? line = reader.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        list.Add(Country.FromCsvLine(line));
                    }
                }
                return list;
            }

            var cols = header.Split(';').Select(c => c.Trim()).ToList();
            var rawRows = new List<string[]>();
            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    rawRows.Add(line.Split(';'));
                }
            }

            var numericCols = DetectNumericColumns(cols, rawRows);
            foreach (var parts in rawRows)
            {
                list.Add(BuildCustomCountry(cols, parts, numericCols));
            }
            return list;
        }

        public static TableLoadResult ValidateAndLoadCustomTable(string filePath)
        {
            var result = new TableLoadResult();

            if (!File.Exists(filePath))
            {
                result.Errors.Add("Выбранный файл не существует.");
                return result;
            }

            string[] allLines;
            try
            {
                allLines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Не удалось прочитать файл: {ex.Message}");
                return result;
            }

            // Пропускаем пустые строки в начале и конце, но сохраняем реальные номера строк
            var nonEmptyLines = new List<(int LineNumber, string Content)>();
            for (int i = 0; i < allLines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(allLines[i]))
                {
                    nonEmptyLines.Add((i + 1, allLines[i]));
                }
            }

            if (nonEmptyLines.Count == 0)
            {
                result.Errors.Add("Файл пустой. Таблица должна содержать строку заголовков и хотя бы одну строку данных.");
                return result;
            }

            string headerLine = nonEmptyLines[0].Content;
            char separator = DetectSeparator(headerLine);

            var columns = headerLine.Split(separator).Select(c => c.Trim()).ToList();
            if (columns.Count == 0 || columns.Any(string.IsNullOrWhiteSpace))
            {
                result.Errors.Add($"Строка {nonEmptyLines[0].LineNumber} (заголовок): обнаружены пустые названия столбцов.");
                return result;
            }

            var duplicateCols = columns.GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
                                       .Where(g => g.Count() > 1)
                                       .Select(g => g.Key)
                                       .ToList();
            if (duplicateCols.Count > 0)
            {
                result.Errors.Add($"Строка {nonEmptyLines[0].LineNumber} (заголовок): повторяющиеся столбцы: {string.Join(", ", duplicateCols)}.");
                return result;
            }

            if (nonEmptyLines.Count < 2)
            {
                result.Errors.Add("В таблице есть только заголовок, но нет ни одной строки данных.");
                return result;
            }

            result.Columns = columns;

            // Проверяем, является ли схема стандартной таблицей стран (5 столбцов)
            bool isEngCountry = columns.Count == 5 &&
                columns[0].Equals("Name", StringComparison.OrdinalIgnoreCase) &&
                columns[1].Equals("Continent", StringComparison.OrdinalIgnoreCase) &&
                columns[2].Equals("Capital", StringComparison.OrdinalIgnoreCase) &&
                columns[3].Equals("Area", StringComparison.OrdinalIgnoreCase) &&
                columns[4].Equals("Population", StringComparison.OrdinalIgnoreCase);

            bool isRusCountry = columns.Count == 5 &&
                columns[0].Equals("Название", StringComparison.OrdinalIgnoreCase) &&
                columns[1].Equals("Континент", StringComparison.OrdinalIgnoreCase) &&
                columns[2].Equals("Столица", StringComparison.OrdinalIgnoreCase) &&
                columns[3].Equals("Площадь", StringComparison.OrdinalIgnoreCase) &&
                columns[4].Equals("Население", StringComparison.OrdinalIgnoreCase);

            result.IsStandardCountrySchema = isEngCountry || isRusCountry;

            var parsedRows = new List<(int LineNum, string[] Parts)>();
            for (int i = 1; i < nonEmptyLines.Count; i++)
            {
                var (lineNum, content) = nonEmptyLines[i];
                var parts = content.Split(separator);
                if (parts.Length != columns.Count)
                {
                    result.Errors.Add($"Строка {lineNum}: ожидалось {columns.Count} столбцов, найдено {parts.Length}.");
                    if (result.Errors.Count >= 10) break;
                    continue;
                }
                for (int c = 0; c < parts.Length; c++) parts[c] = parts[c].Trim();
                parsedRows.Add((lineNum, parts));
            }

            if (result.Errors.Count > 0)
            {
                return result;
            }

            if (result.IsStandardCountrySchema)
            {
                foreach (var (lineNum, parts) in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(parts[0]))
                        result.Errors.Add($"Строка {lineNum}: пустое название страны.");
                    if (string.IsNullOrWhiteSpace(parts[1]))
                        result.Errors.Add($"Строка {lineNum}: пустой континент.");
                    if (string.IsNullOrWhiteSpace(parts[2]))
                        result.Errors.Add($"Строка {lineNum}: пустая столица.");

                    if (!double.TryParse(parts[3].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double area) || area < 0)
                        result.Errors.Add($"Строка {lineNum}: некорректная площадь «{parts[3]}» (ожидается неотрицательное число).");

                    if (!long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out long pop) || pop < 0)
                        result.Errors.Add($"Строка {lineNum}: некорректное население «{parts[4]}» (ожидается целое неотрицательное число).");

                    if (result.Errors.Count >= 10) break;

                    if (result.Errors.Count == 0)
                    {
                        result.Records.Add(new Country(parts[0], parts[1], parts[2], area, pop));
                    }
                }

                if (result.Errors.Count > 0) return result;

                result.Columns = new List<string> { "Население", "Площадь", "Название", "Столица" };
                result.NumericColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Население", "Площадь" };
                result.FilterColumnName = "Континент";
                result.FilterValues = result.Records.Select(r => r.Continent).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
                result.IsValid = true;
                return result;
            }

            // Произвольная таблица: проверяем обязательный первый столбец и целостность типов столбцов
            foreach (var (lineNum, parts) in parsedRows)
            {
                if (string.IsNullOrWhiteSpace(parts[0]))
                {
                    result.Errors.Add($"Строка {lineNum}: пустое значение в первом столбце «{columns[0]}».");
                    if (result.Errors.Count >= 10) break;
                }
            }

            // Проверяем смешанные типы в столбцах (если большинство строк — числа, но в какой-то строке ошибка)
            var numericCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < columns.Count; c++)
            {
                int numCount = 0;
                int textCount = 0;
                int firstBadLine = -1;
                string firstBadVal = string.Empty;

                foreach (var (lineNum, parts) in parsedRows)
                {
                    string val = parts[c];
                    if (string.IsNullOrWhiteSpace(val)) continue;

                    if (double.TryParse(val.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    {
                        numCount++;
                    }
                    else
                    {
                        textCount++;
                        if (firstBadLine == -1)
                        {
                            firstBadLine = lineNum;
                            firstBadVal = val;
                        }
                    }
                }

                if (numCount > 0 && textCount == 0)
                {
                    numericCols.Add(columns[c]);
                }
                else if (numCount > textCount && textCount > 0)
                {
                    result.Errors.Add($"Столбец «{columns[c]}» (строка {firstBadLine}): значение «{firstBadVal}» не является числом, хотя столбец числовой.");
                }
            }

            if (result.Errors.Count > 0)
            {
                return result;
            }

            result.NumericColumns = numericCols;
            foreach (var (_, parts) in parsedRows)
            {
                result.Records.Add(BuildCustomCountry(columns, parts, numericCols));
            }

            // Для фильтрации выбираем первый текстовый столбец кроме первого (или null, если такого нет)
            string? filterCol = columns.Skip(1).FirstOrDefault(col => !numericCols.Contains(col));
            result.FilterColumnName = filterCol;
            if (filterCol != null)
            {
                result.FilterValues = result.Records
                    .Select(r => r.CustomAttributes.TryGetValue(filterCol, out var v) ? v : string.Empty)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
            }

            result.IsValid = true;
            return result;
        }

        private static char DetectSeparator(string headerLine)
        {
            if (headerLine.Contains(';')) return ';';
            if (headerLine.Contains('\t')) return '\t';
            if (headerLine.Contains(',')) return ',';
            return ';';
        }

        private static HashSet<string> DetectNumericColumns(List<string> columns, List<string[]> rows)
        {
            var numericCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < columns.Count; c++)
            {
                bool hasAny = false;
                bool allNumeric = true;
                foreach (var parts in rows)
                {
                    if (c >= parts.Length) continue;
                    string val = parts[c].Trim();
                    if (string.IsNullOrWhiteSpace(val)) continue;
                    hasAny = true;
                    if (!double.TryParse(val.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    {
                        allNumeric = false;
                        break;
                    }
                }
                if (hasAny && allNumeric) numericCols.Add(columns[c]);
            }
            return numericCols;
        }

        private static Country BuildCustomCountry(List<string> columns, string[] parts, HashSet<string> numericCols)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < columns.Count; c++)
            {
                dict[columns[c]] = c < parts.Length ? parts[c].Trim() : string.Empty;
            }

            string name = parts.Length > 0 ? parts[0].Trim() : string.Empty;
            string? firstTextExtra = columns.Skip(1).FirstOrDefault(col => !numericCols.Contains(col));
            string category = firstTextExtra != null && dict.TryGetValue(firstTextExtra, out var catVal) ? catVal : string.Empty;

            string? firstNumericCol = columns.FirstOrDefault(col => numericCols.Contains(col));
            string rightText;
            if (firstNumericCol != null && dict.TryGetValue(firstNumericCol, out var numRaw))
            {
                rightText = $"{firstNumericCol}: {numRaw}";
            }
            else if (columns.Count > 1)
            {
                rightText = $"{columns[1]}: {dict[columns[1]]}";
            }
            else
            {
                rightText = string.Empty;
            }

            return new Country
            {
                Name = name,
                Continent = category,
                IsCustomRecord = true,
                CustomColumnOrder = new List<string>(columns),
                CustomAttributes = dict,
                NumericAttributes = new HashSet<string>(numericCols, StringComparer.OrdinalIgnoreCase),
                DisplayRightText = rightText
            };
        }
    }
}
