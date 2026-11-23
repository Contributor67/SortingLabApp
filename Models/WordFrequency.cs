using System;

namespace SortingLabApp.Models
{
    public class WordFrequency
    {
        public int Rank { get; set; }
        public string Word { get; set; } = string.Empty;
        public int Count { get; set; }
        public double FrequencyPercent { get; set; }

        public string FrequencyFormatted => $"{FrequencyPercent:F2}%";
    }

    public class BenchmarkResult
    {
        public int WordCount { get; set; }
        public double QuickSortTimeMs { get; set; }
        public double RadixSortTimeMs { get; set; }
        public string FasterAlgorithm { get; set; } = string.Empty;
        public string Ratio { get; set; } = string.Empty;

        public string QuickSortFormatted => $"{QuickSortTimeMs:F4} мс";
        public string RadixSortFormatted => $"{RadixSortTimeMs:F4} мс";
    }
}
