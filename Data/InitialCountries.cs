using System;
using System.Collections.Generic;
using System.IO;
using SortingLabApp.Models;

namespace SortingLabApp.Data
{
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
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            writer.WriteLine("Name;Continent;Capital;Area;Population");
            foreach (var c in countries)
            {
                writer.WriteLine(c.ToCsvLine());
            }
        }

        public static List<Country> LoadFromFile(string filePath)
        {
            var list = new List<Country>();
            using var reader = new StreamReader(filePath, System.Text.Encoding.UTF8);
            string? header = reader.ReadLine(); // заголовок
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
    }
}
