using System;
using System.Collections.Generic;

namespace SortingLabApp.Models
{
    /// <summary>
    /// Снимок состояния файлов для визуализации внешней сортировки (Задание 2).
    /// Включает разметку серий, летающие карточки и участников поединка слияния.
    /// </summary>
    public class ExternalSortStep
    {
        public int StepNumber { get; set; }
        public int PassNumber { get; set; }
        public string Phase { get; set; } = string.Empty; // "Разбиение", "Слияние", "Итог"

        // Содержимое файлов на данном шаге
        public List<Country> File0Records { get; set; } = new();
        public List<Country> File1Records { get; set; } = new();
        public List<Country> File2Records { get; set; } = new();

        // Списки с цветовой маркировкой серий и разделителями
        public List<CountryRunRecord> File0Items { get; set; } = new();
        public List<CountryRunRecord> File1Items { get; set; } = new();
        public List<CountryRunRecord> File2Items { get; set; } = new();

        // Участники поединка слияния на арене
        public Country? ActiveCountry1 { get; set; }
        public Country? ActiveCountry2 { get; set; }
        public Country? WinnerCountry { get; set; }
        public string ComparisonSymbol { get; set; } = "VS"; // "<=", ">", "VS", "➔"

        // Траектория полета карточки
        public string FlightSource { get; set; } = string.Empty; // "F0", "F1", "F2"
        public string FlightTarget { get; set; } = string.Empty; // "F0", "F1", "F2"
        public Country? FlyingCountry { get; set; }

        public string TargetFile { get; set; } = string.Empty; // "F0", "F1", "F2"

        public long ComparisonsCount { get; set; }
        public long FileOperationsCount { get; set; }

        public string Explanation { get; set; } = string.Empty; // Пояснение для 6-классника
        public string TechnicalLog { get; set; } = string.Empty;

        public static ExternalSortStep CreateSnapshot(
            int stepNum,
            int passNum,
            string phase,
            IEnumerable<CountryRunRecord> f0,
            IEnumerable<CountryRunRecord> f1,
            IEnumerable<CountryRunRecord> f2,
            Country? act1,
            Country? act2,
            Country? winner,
            string compSym,
            string flightSrc,
            string flightDst,
            Country? flying,
            string targetFile,
            long comps,
            long fileOps,
            string explanation,
            string techLog)
        {
            var step = new ExternalSortStep
            {
                StepNumber = stepNum,
                PassNumber = passNum,
                Phase = phase,
                File0Items = new List<CountryRunRecord>(f0),
                File1Items = new List<CountryRunRecord>(f1),
                File2Items = new List<CountryRunRecord>(f2),
                ActiveCountry1 = act1,
                ActiveCountry2 = act2,
                WinnerCountry = winner,
                ComparisonSymbol = compSym,
                FlightSource = flightSrc,
                FlightTarget = flightDst,
                FlyingCountry = flying,
                TargetFile = targetFile,
                ComparisonsCount = comps,
                FileOperationsCount = fileOps,
                Explanation = explanation,
                TechnicalLog = techLog
            };

            // Для совместимости также заполняем плоские списки
            step.File0Records = step.File0Items.ConvertAll(i => i.Country);
            step.File1Records = step.File1Items.ConvertAll(i => i.Country);
            step.File2Records = step.File2Items.ConvertAll(i => i.Country);

            return step;
        }
    }
}
