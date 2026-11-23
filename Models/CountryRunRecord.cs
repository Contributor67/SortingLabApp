using System;
using System.Windows;

namespace SortingLabApp.Models
{
    /// <summary>
    /// Обертка записи страны с информацией о серии (Run) для внешней сортировки.
    /// Позволяет визуализировать границы серий и цветовую маркировку.
    /// </summary>
    public class CountryRunRecord
    {
        public Country Country { get; set; } = new();
        public int RunId { get; set; }
        public bool IsRunHeader { get; set; }
        public string RunColorHex { get; set; } = "#3B82F6";
        public bool IsActive { get; set; }

        public string RunLabel => $"Серия #{RunId + 1}";
        public Visibility HeaderVisibility => IsRunHeader ? Visibility.Visible : Visibility.Collapsed;

        public static string GetRunColor(int runId)
        {
            string[] colors = { "#3B82F6", "#8B5CF6", "#EC4899", "#F59E0B", "#10B981", "#06B6D4", "#F97316" };
            return colors[Math.Abs(runId) % colors.Length];
        }

        public static CountryRunRecord Create(Country country, int runId, bool isHeader)
        {
            return new CountryRunRecord
            {
                Country = country,
                RunId = runId,
                IsRunHeader = isHeader,
                RunColorHex = GetRunColor(runId)
            };
        }
    }
}
