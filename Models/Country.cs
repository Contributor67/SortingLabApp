using System;
using System.Globalization;

namespace SortingLabApp.Models
{
    /// <summary>
    /// Модель записи страны для Задания 2 (Внешняя сортировка).
    /// </summary>
    public class Country
    {
        public string Name { get; set; } = string.Empty;
        public string Continent { get; set; } = string.Empty;
        public string Capital { get; set; } = string.Empty;
        public double Area { get; set; } // в кв. км
        public long Population { get; set; } // численность населения

        public Country() { }

        public Country(string name, string continent, string capital, double area, long population)
        {
            Name = name;
            Continent = continent;
            Capital = capital;
            Area = area;
            Population = population;
        }

        public string ToCsvLine()
        {
            return $"{Name};{Continent};{Capital};{Area.ToString(CultureInfo.InvariantCulture)};{Population}";
        }

        public static Country FromCsvLine(string line)
        {
            var parts = line.Split(';');
            if (parts.Length < 5)
            {
                throw new FormatException($"Неверный формат строки CSV: {line}");
            }

            return new Country
            {
                Name = parts[0].Trim(),
                Continent = parts[1].Trim(),
                Capital = parts[2].Trim(),
                Area = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                Population = long.Parse(parts[4].Trim(), CultureInfo.InvariantCulture)
            };
        }

        public object GetAttributeValue(string attributeName) => attributeName switch
        {
            "Население" or "Population" => Population,
            "Площадь" or "Area" => Area,
            "Название" or "Name" => Name,
            "Столица" or "Capital" => Capital,
            _ => Population
        };

        public int CompareTo(Country other, string attributeName, bool ascending = true)
        {
            int cmp = attributeName switch
            {
                "Население" or "Population" => Population.CompareTo(other.Population),
                "Площадь" or "Area" => Area.CompareTo(other.Area),
                "Название" or "Name" => string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase),
                "Столица" or "Capital" => string.Compare(Capital, other.Capital, StringComparison.OrdinalIgnoreCase),
                _ => Population.CompareTo(other.Population)
            };

            return ascending ? cmp : -cmp;
        }

        public override string ToString()
        {
            return $"{Name} ({Continent}) | Нас: {Population:N0} чел. | Пл: {Area:N0} км²";
        }
    }
}
