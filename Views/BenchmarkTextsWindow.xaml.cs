using System.Windows;
using System.Windows.Media;
using SortingLabApp.Models;

namespace SortingLabApp.Views
{
    public partial class BenchmarkTextsWindow : Window
    {
        private readonly BenchmarkResult _result;

        public BenchmarkTextsWindow(BenchmarkResult result)
        {
            InitializeComponent();
            _result = result;

            TbHeaderTitle.Text = $"Эксперимент: {result.WordCount:N0} слов (победитель: {result.FasterAlgorithm})";
            TxtInitialText.Text = result.SourceText;
            TbSourceStats.Text = $"{result.WordCount:N0} слов";

            bool exactMatch = result.QuickSortedText == result.RadixSortedText;
            if (exactMatch)
            {
                TbMatchStatus.Text = "✔ Отсортированные последовательности QuickSort и MSD RadixSort полностью совпадают";
                TbMatchStatus.Foreground = (Brush)FindResource("BarSortedBrush");
            }
            else
            {
                TbMatchStatus.Text = "⚠ Результаты QuickSort и MSD RadixSort различаются";
                TbMatchStatus.Foreground = (Brush)FindResource("BarSwappingBrush");
            }

            UpdateSortedText();
        }

        private void AlgorithmRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateSortedText();
        }

        private void UpdateSortedText()
        {
            if (RbQuickSort.IsChecked == true)
            {
                TbSortedTitle.Text = "Конечный текст (QuickSort)";
                TbSortedTime.Text = $"Время: {_result.QuickSortFormatted}";
                TxtSortedText.Text = _result.QuickSortedText;
            }
            else
            {
                TbSortedTitle.Text = "Конечный текст (MSD RadixSort)";
                TbSortedTime.Text = $"Время: {_result.RadixSortFormatted}";
                TxtSortedText.Text = _result.RadixSortedText;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
