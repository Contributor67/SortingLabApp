using System;
using System.Collections.Generic;
using System.Globalization;

namespace SortingLabApp.Models
{
    /// <summary>
    /// Модель записи таблицы для Задания 2 (Внешняя сортировка).
    /// Поддерживает как стандартную схему стран (5 полей), так и произвольные пользовательские таблицы.
    /// </summary>
    public class Country
    {
        public string Name { get; set; } = string.Empty;
        public string Continent { get; set; } = string.Empty;
        public string Capital { get; set; } = string.Empty;
        public double Area { get; set; } // в кв. км
        public long Population { get; set; } // численность населения

        // Поддержка произвольных пользовательских таблиц
        public bool IsCustomRecord { get; set; }
        public List<string> CustomColumnOrder { get; set; } = new();
        public Dictionary<string, string> CustomAttributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> NumericAttributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string DisplayRightText { get; set; } = string.Empty;

        public Country() { }

        public Country(string name, string continent, string capital, double area, long population)
        {
            Name = name;
            Continent = continent;
            Capital = capital;
            Area = area;
            Population = population;
            DisplayRightText = $"Нас: {population:N0}";
        }

        public string ToCsvLine()
        {
            if (IsCustomRecord && CustomColumnOrder.Count > 0)
            {
                var values = new List<string>(CustomColumnOrder.Count);
                foreach (var col in CustomColumnOrder)
                {
                    values.Add(CustomAttributes.TryGetValue(col, out var val) ? val : string.Empty);
                }
                return string.Join(";", values);
            }
            return $"{Name};{Continent};{Capital};{Area.ToString(CultureInfo.InvariantCulture)};{Population}";
        }

        public static Country FromCsvLine(string line)
        {
            var parts = line.Split(';');
            if (parts.Length < 5)
            {
                throw new FormatException($"Неверный формат строки CSV: {line}");
            }

            double area = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
            long pop = long.Parse(parts[4].Trim(), CultureInfo.InvariantCulture);

            return new Country(
                parts[0].Trim(),
                parts[1].Trim(),
                parts[2].Trim(),
                area,
                pop);
        }

        public object GetAttributeValue(string attributeName)
        {
            if (IsCustomRecord && CustomAttributes.TryGetValue(attributeName, out var rawVal))
            {
                if (NumericAttributes.Contains(attributeName) &&
                    double.TryParse(rawVal.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                {
                    return num;
                }
                return rawVal;
            }

            return attributeName switch
            {
                "Население" or "Population" => Population,
                "Площадь" or "Area" => Area,
                "Название" or "Name" => Name,
                "Столица" or "Capital" => Capital,
                _ => Population
            };
        }

        public int CompareTo(Country other, string attributeName, bool ascending = true)
        {
            int cmp;
            if (IsCustomRecord && other.IsCustomRecord)
            {
                CustomAttributes.TryGetValue(attributeName, out var v1);
                other.CustomAttributes.TryGetValue(attributeName, out var v2);
                v1 ??= string.Empty;
                v2 ??= string.Empty;

                if (NumericAttributes.Contains(attributeName) &&
                    double.TryParse(v1.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double d1) &&
                    double.TryParse(v2.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double d2))
                {
                    cmp = d1.CompareTo(d2);
                }
                else
                {
                    cmp = string.Compare(v1, v2, StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                cmp = attributeName switch
                {
                    "Население" or "Population" => Population.CompareTo(other.Population),
                    "Площадь" or "Area" => Area.CompareTo(other.Area),
                    "Название" or "Name" => string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase),
                    "Столица" or "Capital" => string.Compare(Capital, other.Capital, StringComparison.OrdinalIgnoreCase),
                    _ => Population.CompareTo(other.Population)
                };
            }

            return ascending ? cmp : -cmp;
        }

        public override string ToString()
        {
            if (IsCustomRecord)
            {
                return $"{Name} | {DisplayRightText}";
            }
            return $"{Name} ({Continent}) | Нас: {Population:N0} чел. | Пл: {Area:N0} км²";
        }
    }
}
