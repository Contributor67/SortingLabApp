using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SortingLabApp.Algorithms.External;
using SortingLabApp.Algorithms.Internal;
using SortingLabApp.Algorithms.Strings;
using SortingLabApp.Data;
using SortingLabApp.Models;

namespace SortingLabApp
{
    public partial class MainWindow : Window
    {
        #region Класс визуального элемента столбца (Задание 1)

        private class BarVisualItem
        {
            public int Id { get; set; }
            public int Value { get; set; }
            public Border Container { get; set; } = null!;
            public TextBlock ValueText { get; set; } = null!;
            public TextBlock IndexText { get; set; } = null!;
            public int CurrentSlot { get; set; }
        }

        #endregion

        #region Поля: Внутренняя сортировка (Задание 1)

        private int[] _sourceArray = Array.Empty<int>();
        private List<SortStep> _internalSteps = new();
        private int _internalStepIndex = 0;
        private int _prevInternalStepIndex = -1;
        private bool _isInternalPlaying = false;
        private CancellationTokenSource? _internalCts;
        private int _internalDelay = 150;

        private readonly Dictionary<int, BarVisualItem> _barVisuals = new();

        #endregion

        #region Поля: Внешняя сортировка (Задание 2)

        private List<Country> _sourceCountries = new();
        private List<ExternalSortStep> _externalSteps = new();
        private int _externalStepIndex = 0;
        private bool _isExternalPlaying = false;
        private CancellationTokenSource? _externalCts;
        private int _externalDelay = 350;
        private readonly string _workDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExternalSortData");

        #endregion

        #region Поля: Задание 3 (Слова и бенчмарк)

        private readonly ObservableCollection<WordFrequency> _wordFrequencies = new();
        private readonly ObservableCollection<BenchmarkResult> _benchmarkResults = new();

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // Подключение списков к таблицам
            DgWordFrequencies.ItemsSource = _wordFrequencies;
            DgBenchmarks.ItemsSource = _benchmarkResults;

            // Обработка глобальных горячих клавиш
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            // Инициализация Задания 1
            GenerateInternalArray();

            // Инициализация Задания 2
            _sourceCountries = InitialCountries.GetDefaultCountries();
            PrepareExternalSortSteps();

            // Инициализация Задания 3: сразу генерируем текст
            TxtSourceText.Text = TextGenerator.GenerateText(500);
        }

        #region Обработка горячих клавиш

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is TextBox) return;

            var tabControl = (TabControl)((Grid)Content).Children[1];
            int currentTab = tabControl.SelectedIndex;

            if (currentTab == 0) // Вкладка 1: Внутренняя сортировка
            {
                switch (e.Key)
                {
                    case Key.Space:
                        BtnInternalPlayPause_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.Right:
                        BtnInternalNext_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.Left:
                        BtnInternalPrev_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.R:
                        BtnInternalReset_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                }
            }
            else if (currentTab == 1) // Вкладка 2: Внешняя сортировка
            {
                switch (e.Key)
                {
                    case Key.Space:
                        BtnExternalPlayPause_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.Right:
                        BtnExternalNext_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.Left:
                        BtnExternalPrev_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.R:
                        BtnExternalReset_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                }
            }
        }

        #endregion

        #region Логика и анимация: Внутренняя сортировка (Задание 1)

        private void SliderArraySize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TbArraySizeVal != null)
            {
                TbArraySizeVal.Text = ((int)e.NewValue).ToString();
            }
        }

        private void SliderInternalDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _internalDelay = (int)e.NewValue;
            if (TbInternalDelayVal != null)
            {
                TbInternalDelayVal.Text = $"{_internalDelay} мс";
            }
        }

        private void BtnGenerateArray_Click(object sender, RoutedEventArgs e)
        {
            StopInternalPlaying();
            GenerateInternalArray();
        }

        private void InternalAlgorithm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            StopInternalPlaying();
            RebuildInternalSteps();
        }

        private void GenerateInternalArray()
        {
            int size = (int)(SliderArraySize?.Value ?? 25);
            int typeIndex = CbArrayType?.SelectedIndex ?? 0;
            var rng = new Random();

            _sourceArray = new int[size];

            switch (typeIndex)
            {
                case 0:
                    for (int i = 0; i < size; i++) _sourceArray[i] = rng.Next(5, 100);
                    break;
                case 1:
                    for (int i = 0; i < size; i++) _sourceArray[i] = (int)((i + 1) * (95.0 / size)) + 5;
                    for (int k = 0; k < Math.Max(1, size / 8); k++)
                    {
                        int i1 = rng.Next(size), i2 = rng.Next(size);
                        (_sourceArray[i1], _sourceArray[i2]) = (_sourceArray[i2], _sourceArray[i1]);
                    }
                    break;
                case 2:
                    for (int i = 0; i < size; i++) _sourceArray[i] = (int)((size - i) * (95.0 / size)) + 5;
                    break;
                case 3:
                    int[] pool = { 10, 25, 45, 60, 75, 90 };
                    for (int i = 0; i < size; i++) _sourceArray[i] = pool[rng.Next(pool.Length)];
                    break;
            }

            RebuildInternalSteps();
        }

        private void RebuildInternalSteps()
        {
            if (_sourceArray == null || _sourceArray.Length == 0) return;

            int algo = CbInternalAlgorithm?.SelectedIndex ?? 0;
            _internalSteps = algo switch
            {
                0 => CombSort.GenerateSteps(_sourceArray),
                1 => ShellSort.GenerateSteps(_sourceArray),
                2 => QuickSort.GenerateSteps(_sourceArray),
                3 => TimSort.GenerateSteps(_sourceArray),
                _ => CombSort.GenerateSteps(_sourceArray)
            };

            _internalStepIndex = 0;
            _prevInternalStepIndex = -1;
            LbInternalLog.Items.Clear();

            _barVisuals.Clear();
            CanvasHistogram.Children.Clear();

            RenderCurrentInternalStep(allowAnimation: false);
        }

        private void RenderCurrentInternalStep(bool allowAnimation = true)
        {
            if (_internalSteps == null || _internalSteps.Count == 0) return;
            if (_internalStepIndex < 0) _internalStepIndex = 0;
            if (_internalStepIndex >= _internalSteps.Count) _internalStepIndex = _internalSteps.Count - 1;

            var step = _internalSteps[_internalStepIndex];

            TbInternalStepNum.Text = $"{_internalStepIndex + 1} / {_internalSteps.Count}";
            TbInternalComps.Text = step.ComparisonsCount.ToString();
            TbInternalSwaps.Text = step.SwapsCount.ToString();
            TbInternalExplanation.Text = step.Explanation;

            if (!string.IsNullOrWhiteSpace(step.TechnicalLog))
            {
                LbInternalLog.Items.Add($"[{step.StepNumber}] {step.TechnicalLog}");
                LbInternalLog.ScrollIntoView(LbInternalLog.Items[^1]);
            }

            AnimateHistogram(step, allowAnimation);
        }

        private void AnimateHistogram(SortStep step, bool allowAnimation)
        {
            double width = CanvasHistogram.ActualWidth;
            double height = CanvasHistogram.ActualHeight;

            if (width <= 0 || height <= 0 || step.ArrayState.Length == 0) return;

            int count = step.ArrayState.Length;
            int maxVal = step.ArrayState.Max();
            if (maxVal <= 0) maxVal = 100;

            double spacing = count > 50 ? 2 : (count > 30 ? 3 : 5);
            double totalSpacing = spacing * (count + 1);
            double barWidth = Math.Max(4, (width - totalSpacing) / count);
            double baseBottom = 22.0;

            if (_barVisuals.Count != count || CanvasHistogram.Children.Count == 0)
            {
                CanvasHistogram.Children.Clear();
                _barVisuals.Clear();

                var defaultBrushInit = (Brush)FindResource("BarDefaultBrush");
                var textBrushInit = (Brush)FindResource("TextPrimaryBrush");
                var secBrushInit = (Brush)FindResource("TextSecondaryBrush");

                for (int slot = 0; slot < count; slot++)
                {
                    int id = step.ElementIds.Length > slot ? step.ElementIds[slot] : slot;
                    int val = step.ArrayState[slot];
                    double barHeight = Math.Max(8, (val / (double)maxVal) * (height - 65));

                    var border = new Border
                    {
                        Width = barWidth,
                        Height = barHeight,
                        Background = defaultBrushInit,
                        CornerRadius = new CornerRadius(Math.Min(4, barWidth / 3)),
                        ToolTip = $"Индекс: [{slot}], Значение: {val}"
                    };

                    var tbVal = new TextBlock
                    {
                        Text = val.ToString(),
                        FontSize = Math.Clamp(barWidth * 0.48, 8, 12),
                        FontWeight = FontWeights.Bold,
                        Foreground = textBrushInit,
                        TextAlignment = TextAlignment.Center,
                        Width = barWidth + 8,
                        Visibility = barWidth >= 16 ? Visibility.Visible : Visibility.Collapsed
                    };

                    var tbIdx = new TextBlock
                    {
                        Text = slot.ToString(),
                        FontSize = Math.Clamp(barWidth * 0.42, 8, 11),
                        Foreground = secBrushInit,
                        TextAlignment = TextAlignment.Center,
                        Width = barWidth + 8,
                        Visibility = barWidth >= 20 ? Visibility.Visible : Visibility.Collapsed
                    };

                    double slotX = spacing + slot * (barWidth + spacing);
                    Canvas.SetLeft(border, slotX);
                    Canvas.SetBottom(border, baseBottom);

                    Canvas.SetLeft(tbVal, slotX - 4);
                    Canvas.SetBottom(tbVal, baseBottom + barHeight + 2);

                    Canvas.SetLeft(tbIdx, slotX - 4);
                    Canvas.SetBottom(tbIdx, 2);

                    CanvasHistogram.Children.Add(border);
                    CanvasHistogram.Children.Add(tbVal);
                    CanvasHistogram.Children.Add(tbIdx);

                    _barVisuals[id] = new BarVisualItem
                    {
                        Id = id,
                        Value = val,
                        Container = border,
                        ValueText = tbVal,
                        IndexText = tbIdx,
                        CurrentSlot = slot
                    };
                }

                _prevInternalStepIndex = _internalStepIndex;
                return;
            }

            bool isConsecutive = allowAnimation && Math.Abs(_internalStepIndex - _prevInternalStepIndex) == 1;
            int animDuration = isConsecutive ? Math.Clamp((int)(_internalDelay * 0.75), 35, 450) : 0;
            if (_internalDelay < 35) animDuration = 0;

            var defaultBrush = (Brush)FindResource("BarDefaultBrush");
            var comparingBrush = (Brush)FindResource("BarComparingBrush");
            var swappingBrush = (Brush)FindResource("BarSwappingBrush");
            var pivotBrush = (Brush)FindResource("BarPivotBrush");
            var sortedBrush = (Brush)FindResource("BarSortedBrush");

            for (int slot = 0; slot < count; slot++)
            {
                int barId = step.ElementIds.Length > slot ? step.ElementIds[slot] : slot;
                if (!_barVisuals.TryGetValue(barId, out var barItem)) continue;

                int oldSlot = barItem.CurrentSlot;
                int newSlot = slot;
                double targetX = spacing + newSlot * (barWidth + spacing);

                Brush targetBrush = defaultBrush;
                if (step.SortedIndices.Contains(newSlot))
                {
                    targetBrush = sortedBrush;
                }
                else if (newSlot == step.PivotIndex)
                {
                    targetBrush = pivotBrush;
                }
                else if (newSlot == step.Index1 || newSlot == step.Index2)
                {
                    targetBrush = step.ActionType switch
                    {
                        StepActionType.Compare => comparingBrush,
                        StepActionType.Swap or StepActionType.Assign => swappingBrush,
                        StepActionType.Run => pivotBrush,
                        _ => comparingBrush
                    };
                }

                barItem.Container.Background = targetBrush;

                if (oldSlot != newSlot)
                {
                    if (animDuration > 0)
                    {
                        var animX = new DoubleAnimation
                        {
                            From = Canvas.GetLeft(barItem.Container),
                            To = targetX,
                            Duration = TimeSpan.FromMilliseconds(animDuration),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                        };
                        barItem.Container.BeginAnimation(Canvas.LeftProperty, animX);

                        var animValX = new DoubleAnimation
                        {
                            From = Canvas.GetLeft(barItem.ValueText),
                            To = targetX - 4,
                            Duration = TimeSpan.FromMilliseconds(animDuration),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                        };
                        barItem.ValueText.BeginAnimation(Canvas.LeftProperty, animValX);

                        var animIdxX = new DoubleAnimation
                        {
                            From = Canvas.GetLeft(barItem.IndexText),
                            To = targetX - 4,
                            Duration = TimeSpan.FromMilliseconds(animDuration),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                        };
                        barItem.IndexText.BeginAnimation(Canvas.LeftProperty, animIdxX);

                        double barHeight = barItem.Container.Height;

                        if (oldSlot < newSlot)
                        {
                            double lift = Math.Min(35, height * 0.14);
                            var animY = new DoubleAnimationUsingKeyFrames
                            {
                                Duration = TimeSpan.FromMilliseconds(animDuration)
                            };
                            animY.KeyFrames.Add(new LinearDoubleKeyFrame(baseBottom, KeyTime.FromPercent(0.0)));
                            animY.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom + lift, KeyTime.FromPercent(0.45), new KeySpline(0.2, 0.8, 0.4, 1.0)));
                            animY.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom, KeyTime.FromPercent(1.0), new KeySpline(0.6, 0.0, 0.8, 0.2)));

                            Panel.SetZIndex(barItem.Container, 100);
                            Panel.SetZIndex(barItem.ValueText, 101);
                            barItem.Container.BeginAnimation(Canvas.BottomProperty, animY);

                            var animValY = new DoubleAnimationUsingKeyFrames
                            {
                                Duration = TimeSpan.FromMilliseconds(animDuration)
                            };
                            animValY.KeyFrames.Add(new LinearDoubleKeyFrame(baseBottom + barHeight + 2, KeyTime.FromPercent(0.0)));
                            animValY.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom + barHeight + 2 + lift, KeyTime.FromPercent(0.45), new KeySpline(0.2, 0.8, 0.4, 1.0)));
                            animValY.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom + barHeight + 2, KeyTime.FromPercent(1.0), new KeySpline(0.6, 0.0, 0.8, 0.2)));
                            barItem.ValueText.BeginAnimation(Canvas.BottomProperty, animValY);
                        }
                        else
                        {
                            double dip = Math.Min(12, height * 0.05);
                            var animY = new DoubleAnimationUsingKeyFrames
                            {
                                Duration = TimeSpan.FromMilliseconds(animDuration)
                            };
                            animY.KeyFrames.Add(new LinearDoubleKeyFrame(baseBottom, KeyTime.FromPercent(0.0)));
                            animY.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom - dip, KeyTime.FromPercent(0.5)));
                            animY.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom, KeyTime.FromPercent(1.0)));

                            Panel.SetZIndex(barItem.Container, 30);
                            Panel.SetZIndex(barItem.ValueText, 31);
                            barItem.Container.BeginAnimation(Canvas.BottomProperty, animY);

                            var animValY = new DoubleAnimationUsingKeyFrames
                            {
                                Duration = TimeSpan.FromMilliseconds(animDuration)
                            };
                            animValY.KeyFrames.Add(new LinearDoubleKeyFrame(baseBottom + barHeight + 2, KeyTime.FromPercent(0.0)));
                            animValY.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom + barHeight + 2 - dip, KeyTime.FromPercent(0.5)));
                            animValY.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom + barHeight + 2, KeyTime.FromPercent(1.0)));
                            barItem.ValueText.BeginAnimation(Canvas.BottomProperty, animValY);
                        }
                    }
                    else
                    {
                        barItem.Container.BeginAnimation(Canvas.LeftProperty, null);
                        barItem.Container.BeginAnimation(Canvas.BottomProperty, null);
                        barItem.ValueText.BeginAnimation(Canvas.LeftProperty, null);
                        barItem.ValueText.BeginAnimation(Canvas.BottomProperty, null);
                        barItem.IndexText.BeginAnimation(Canvas.LeftProperty, null);

                        Canvas.SetLeft(barItem.Container, targetX);
                        Canvas.SetBottom(barItem.Container, baseBottom);

                        Canvas.SetLeft(barItem.ValueText, targetX - 4);
                        Canvas.SetBottom(barItem.ValueText, baseBottom + barItem.Container.Height + 2);

                        Canvas.SetLeft(barItem.IndexText, targetX - 4);
                        Canvas.SetBottom(barItem.IndexText, 2);

                        Panel.SetZIndex(barItem.Container, 50);
                        Panel.SetZIndex(barItem.ValueText, 51);
                    }

                    barItem.CurrentSlot = newSlot;
                    barItem.IndexText.Text = newSlot.ToString();
                }
                else
                {
                    if ((newSlot == step.Index1 || newSlot == step.Index2) && step.ActionType == StepActionType.Compare && animDuration > 0)
                    {
                        double barHeight = barItem.Container.Height;
                        var bounce = new DoubleAnimationUsingKeyFrames
                        {
                            Duration = TimeSpan.FromMilliseconds(animDuration)
                        };
                        bounce.KeyFrames.Add(new LinearDoubleKeyFrame(baseBottom, KeyTime.FromPercent(0.0)));
                        bounce.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom + 8, KeyTime.FromPercent(0.4), new KeySpline(0.2, 0.8, 0.4, 1.0)));
                        bounce.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom, KeyTime.FromPercent(1.0), new KeySpline(0.6, 0.0, 0.8, 0.2)));

                        Panel.SetZIndex(barItem.Container, 80);
                        Panel.SetZIndex(barItem.ValueText, 81);
                        barItem.Container.BeginAnimation(Canvas.BottomProperty, bounce);

                        var valBounce = new DoubleAnimationUsingKeyFrames
                        {
                            Duration = TimeSpan.FromMilliseconds(animDuration)
                        };
                        valBounce.KeyFrames.Add(new LinearDoubleKeyFrame(baseBottom + barHeight + 2, KeyTime.FromPercent(0.0)));
                        valBounce.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom + barHeight + 2 + 8, KeyTime.FromPercent(0.4)));
                        valBounce.KeyFrames.Add(new SplineDoubleKeyFrame(baseBottom + barHeight + 2, KeyTime.FromPercent(1.0)));
                        barItem.ValueText.BeginAnimation(Canvas.BottomProperty, valBounce);
                    }
                    else
                    {
                        barItem.Container.BeginAnimation(Canvas.BottomProperty, null);
                        barItem.ValueText.BeginAnimation(Canvas.BottomProperty, null);
                        Canvas.SetBottom(barItem.Container, baseBottom);
                        Canvas.SetBottom(barItem.ValueText, baseBottom + barItem.Container.Height + 2);
                        Panel.SetZIndex(barItem.Container, 50);
                        Panel.SetZIndex(barItem.ValueText, 51);
                    }
                }
            }

            _prevInternalStepIndex = _internalStepIndex;
        }

        private void CanvasHistogram_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _barVisuals.Clear();
            CanvasHistogram.Children.Clear();
            RenderCurrentInternalStep(allowAnimation: false);
        }

        private async void BtnInternalPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isInternalPlaying)
            {
                StopInternalPlaying();
            }
            else
            {
                await StartInternalPlayingAsync();
            }
        }

        private async Task StartInternalPlayingAsync()
        {
            if (_internalSteps == null || _internalSteps.Count == 0) return;

            if (_internalStepIndex >= _internalSteps.Count - 1)
            {
                _internalStepIndex = 0;
                _prevInternalStepIndex = -1;
                LbInternalLog.Items.Clear();
                _barVisuals.Clear();
                CanvasHistogram.Children.Clear();
                RenderCurrentInternalStep(allowAnimation: false);
            }

            _isInternalPlaying = true;
            BtnInternalPlayPause.Content = "⏸ Пауза [Пробел]";
            _internalCts = new CancellationTokenSource();
            var token = _internalCts.Token;

            try
            {
                while (_internalStepIndex < _internalSteps.Count - 1 && !token.IsCancellationRequested)
                {
                    _internalStepIndex++;
                    RenderCurrentInternalStep(allowAnimation: true);
                    await Task.Delay(_internalDelay, token);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                StopInternalPlaying();
            }
        }

        private void StopInternalPlaying()
        {
            _isInternalPlaying = false;
            _internalCts?.Cancel();
            _internalCts = null;
            if (BtnInternalPlayPause != null)
            {
                BtnInternalPlayPause.Content = "▶ Старт [Пробел]";
            }
        }

        private void BtnInternalNext_Click(object sender, RoutedEventArgs e)
        {
            StopInternalPlaying();
            if (_internalStepIndex < _internalSteps.Count - 1)
            {
                _internalStepIndex++;
                RenderCurrentInternalStep(allowAnimation: true);
            }
        }

        private void BtnInternalPrev_Click(object sender, RoutedEventArgs e)
        {
            StopInternalPlaying();
            if (_internalStepIndex > 0)
            {
                _internalStepIndex--;
                RenderCurrentInternalStep(allowAnimation: true);
            }
        }

        private void BtnInternalReset_Click(object sender, RoutedEventArgs e)
        {
            StopInternalPlaying();
            _internalStepIndex = 0;
            _prevInternalStepIndex = -1;
            LbInternalLog.Items.Clear();
            _barVisuals.Clear();
            CanvasHistogram.Children.Clear();
            RenderCurrentInternalStep(allowAnimation: false);
        }

        #endregion

        #region Логика и анимация: Внешняя сортировка (Задание 2)

        private void ExternalParameters_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            StopExternalPlaying();
            PrepareExternalSortSteps();
        }

        private void SliderExternalDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _externalDelay = (int)e.NewValue;
            if (TbExternalDelayVal != null)
            {
                TbExternalDelayVal.Text = $"{_externalDelay} мс";
            }
        }

        private void BtnLoadDefaultCountries_Click(object sender, RoutedEventArgs e)
        {
            StopExternalPlaying();
            _sourceCountries = InitialCountries.GetDefaultCountries();
            PrepareExternalSortSteps();
        }

        private void BtnGenerateRandomCountries_Click(object sender, RoutedEventArgs e)
        {
            StopExternalPlaying();
            _sourceCountries = InitialCountries.GenerateRandomCountries(30);
            PrepareExternalSortSteps();
        }

        private void PrepareExternalSortSteps()
        {
            if (_sourceCountries == null || _sourceCountries.Count == 0) return;

            var method = CbExternalMethod.SelectedIndex == 1 
                ? ExternalSortMethod.NaturalMerge 
                : ExternalSortMethod.DirectMerge;

            string sortAttr = ((ComboBoxItem)CbExternalSortKey.SelectedItem).Content.ToString() ?? "Население";
            string continentFilter = ((ComboBoxItem)CbExternalContinent.SelectedItem).Content.ToString() ?? "Все континенты";
            bool asc = CbExternalSortDir.SelectedIndex == 0;

            _externalSteps = ExternalSortService.ExecuteExternalSort(
                _sourceCountries,
                continentFilter,
                sortAttr,
                asc,
                method,
                _workDir);

            _externalStepIndex = 0;
            LbExternalLog.Items.Clear();
            RenderCurrentExternalStep(allowAnimation: false);
        }

        private void RenderCurrentExternalStep(bool allowAnimation = true)
        {
            if (_externalSteps == null || _externalSteps.Count == 0) return;
            if (_externalStepIndex < 0) _externalStepIndex = 0;
            if (_externalStepIndex >= _externalSteps.Count) _externalStepIndex = _externalSteps.Count - 1;

            var step = _externalSteps[_externalStepIndex];

            // 1. Индикаторы
            TbExternalStepNum.Text = $"{_externalStepIndex + 1} / {_externalSteps.Count}";
            TbExternalPhase.Text = step.Phase;
            TbExternalPass.Text = step.PassNumber.ToString();
            TbExternalComps.Text = step.ComparisonsCount.ToString();
            TbExternalFileOps.Text = step.FileOperationsCount.ToString();
            TbExternalExplanation.Text = step.Explanation;

            // 2. Списки файлов с цветными сериями
            LbFile0.ItemsSource = step.File0Items;
            LbFile1.ItemsSource = step.File1Items;
            LbFile2.ItemsSource = step.File2Items;

            TbF0Count.Text = $"{step.File0Items.Count} записей";
            TbF1Count.Text = $"{step.File1Items.Count} записей";
            TbF2Count.Text = $"{step.File2Items.Count} записей";

            // Скроллинг к последним добавленным записям
            if (LbFile0.Items.Count > 0) LbFile0.ScrollIntoView(LbFile0.Items[^1]);
            if (LbFile1.Items.Count > 0) LbFile1.ScrollIntoView(LbFile1.Items[^1]);
            if (LbFile2.Items.Count > 0) LbFile2.ScrollIntoView(LbFile2.Items[^1]);

            // 3. Арена поединка стран
            string sortAttr = ((ComboBoxItem)CbExternalSortKey.SelectedItem).Content.ToString() ?? "Население";

            if (step.ActiveCountry1 != null)
            {
                TbArenaCountry1.Text = step.ActiveCountry1.Name;
                TbArenaContinent1.Text = step.ActiveCountry1.Continent;
                TbArenaCapital1.Text = $"Столица: {step.ActiveCountry1.Capital}";
                TbArenaVal1.Text = $"{sortAttr}: {step.ActiveCountry1.GetAttributeValue(sortAttr):N0}";
                CardArenaF1.Opacity = 1.0;
            }
            else
            {
                TbArenaCountry1.Text = "—";
                TbArenaContinent1.Text = "";
                TbArenaCapital1.Text = "";
                TbArenaVal1.Text = "";
                CardArenaF1.Opacity = 0.45;
            }

            if (step.ActiveCountry2 != null)
            {
                TbArenaCountry2.Text = step.ActiveCountry2.Name;
                TbArenaContinent2.Text = step.ActiveCountry2.Continent;
                TbArenaCapital2.Text = $"Столица: {step.ActiveCountry2.Capital}";
                TbArenaVal2.Text = $"{sortAttr}: {step.ActiveCountry2.GetAttributeValue(sortAttr):N0}";
                CardArenaF2.Opacity = 1.0;
            }
            else
            {
                TbArenaCountry2.Text = "—";
                TbArenaContinent2.Text = "";
                TbArenaCapital2.Text = "";
                TbArenaVal2.Text = "";
                CardArenaF2.Opacity = 0.45;
            }

            TbArenaSymbol.Text = !string.IsNullOrWhiteSpace(step.ComparisonSymbol) ? step.ComparisonSymbol : "VS";

            // Подсветка победителя
            var sortedBrush = (Brush)FindResource("BarSortedBrush");
            var comparingBrush = (Brush)FindResource("BarComparingBrush");
            var swappingBrush = (Brush)FindResource("BarSwappingBrush");
            var defaultBorder = (Brush)FindResource("BorderBrush");

            if (step.WinnerCountry != null && step.ActiveCountry1 != null && step.WinnerCountry.Name == step.ActiveCountry1.Name)
            {
                CardArenaF1.BorderBrush = sortedBrush;
                CardArenaF2.BorderBrush = defaultBorder;
                TbArenaWinnerNotice.Text = $"⭐ Победа F1 ({step.WinnerCountry.Name}) ➔ Запись в F0";
            }
            else if (step.WinnerCountry != null && step.ActiveCountry2 != null && step.WinnerCountry.Name == step.ActiveCountry2.Name)
            {
                CardArenaF2.BorderBrush = sortedBrush;
                CardArenaF1.BorderBrush = defaultBorder;
                TbArenaWinnerNotice.Text = $"⭐ Победа F2 ({step.WinnerCountry.Name}) ➔ Запись в F0";
            }
            else
            {
                CardArenaF1.BorderBrush = comparingBrush;
                CardArenaF2.BorderBrush = swappingBrush;
                TbArenaWinnerNotice.Text = !string.IsNullOrWhiteSpace(step.FlightTarget) ? $"➔ Запись в {step.FlightTarget}" : "Сравнение";
            }

            // 4. Летающая карточка между файлами
            AnimateFlyingCard(step, allowAnimation, sortAttr);

            // 5. Лог
            if (!string.IsNullOrWhiteSpace(step.TechnicalLog))
            {
                LbExternalLog.Items.Add($"[Шаг {step.StepNumber}] {step.TechnicalLog}");
                LbExternalLog.ScrollIntoView(LbExternalLog.Items[^1]);
            }
        }

        private void AnimateFlyingCard(ExternalSortStep step, bool allowAnimation, string sortAttr)
        {
            double canvasWidth = CanvasExternalFlight.ActualWidth;
            double canvasHeight = CanvasExternalFlight.ActualHeight;

            if (canvasWidth <= 0 || step.FlyingCountry == null || !allowAnimation || _externalDelay < 50)
            {
                FlyingCard.Visibility = Visibility.Collapsed;
                return;
            }

            TbFlyingCountryName.Text = step.FlyingCountry.Name;
            TbFlyingCountryVal.Text = $"{step.FlyingCountry.GetAttributeValue(sortAttr):N0}";

            // Вычисляем координаты X для колонок F0 (0.16), F1 (0.50), F2 (0.83)
            double col0X = canvasWidth * 0.16 - 100;
            double col1X = canvasWidth * 0.50 - 100;
            double col2X = canvasWidth * 0.83 - 100;

            double fromX = col0X;
            double toX = col1X;

            if (step.FlightSource == "F0" && step.FlightTarget == "F1") { fromX = col0X; toX = col1X; }
            else if (step.FlightSource == "F0" && step.FlightTarget == "F2") { fromX = col0X; toX = col2X; }
            else if (step.FlightSource == "F1" && step.FlightTarget == "F0") { fromX = col1X; toX = col0X; }
            else if (step.FlightSource == "F2" && step.FlightTarget == "F0") { fromX = col2X; toX = col0X; }

            int animDuration = Math.Clamp((int)(_externalDelay * 0.7), 60, 500);

            FlyingCard.Visibility = Visibility.Visible;
            Panel.SetZIndex(FlyingCard, 200);

            // Горизонтальный полет
            var animX = new DoubleAnimation
            {
                From = fromX,
                To = toX,
                Duration = TimeSpan.FromMilliseconds(animDuration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            FlyingCard.BeginAnimation(Canvas.LeftProperty, animX);

            // Вертикальная параболическая траектория полета
            var animY = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(animDuration)
            };
            animY.KeyFrames.Add(new LinearDoubleKeyFrame(20, KeyTime.FromPercent(0.0)));
            animY.KeyFrames.Add(new SplineDoubleKeyFrame(65, KeyTime.FromPercent(0.5), new KeySpline(0.2, 0.8, 0.4, 1.0)));
            animY.KeyFrames.Add(new SplineDoubleKeyFrame(15, KeyTime.FromPercent(1.0), new KeySpline(0.6, 0.0, 0.8, 0.2)));
            FlyingCard.BeginAnimation(Canvas.TopProperty, animY);
        }

        private async void BtnExternalPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isExternalPlaying)
            {
                StopExternalPlaying();
            }
            else
            {
                await StartExternalPlayingAsync();
            }
        }

        private async Task StartExternalPlayingAsync()
        {
            if (_externalSteps == null || _externalSteps.Count == 0) return;

            if (_externalStepIndex >= _externalSteps.Count - 1)
            {
                _externalStepIndex = 0;
                LbExternalLog.Items.Clear();
            }

            _isExternalPlaying = true;
            BtnExternalPlayPause.Content = "⏸ Пауза [Пробел]";
            _externalCts = new CancellationTokenSource();
            var token = _externalCts.Token;

            try
            {
                while (_externalStepIndex < _externalSteps.Count - 1 && !token.IsCancellationRequested)
                {
                    _externalStepIndex++;
                    RenderCurrentExternalStep(allowAnimation: true);
                    await Task.Delay(_externalDelay, token);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                StopExternalPlaying();
            }
        }

        private void StopExternalPlaying()
        {
            _isExternalPlaying = false;
            _externalCts?.Cancel();
            _externalCts = null;
            if (BtnExternalPlayPause != null)
            {
                BtnExternalPlayPause.Content = "▶ Старт [Пробел]";
            }
        }

        private void BtnExternalNext_Click(object sender, RoutedEventArgs e)
        {
            StopExternalPlaying();
            if (_externalStepIndex < _externalSteps.Count - 1)
            {
                _externalStepIndex++;
                RenderCurrentExternalStep(allowAnimation: true);
            }
        }

        private void BtnExternalPrev_Click(object sender, RoutedEventArgs e)
        {
            StopExternalPlaying();
            if (_externalStepIndex > 0)
            {
                _externalStepIndex--;
                RenderCurrentExternalStep(allowAnimation: true);
            }
        }

        private void BtnExternalReset_Click(object sender, RoutedEventArgs e)
        {
            StopExternalPlaying();
            _externalStepIndex = 0;
            LbExternalLog.Items.Clear();
            RenderCurrentExternalStep(allowAnimation: false);
        }

        #endregion

        #region Логика: Задание 3 (Слова и бенчмарк)

        private void TxtSourceText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var words = TextGenerator.Tokenize(TxtSourceText.Text);
            TbWordCountHeader.Text = $"(слов: {words.Length:N0})";
        }

        private void BtnGenText100_Click(object sender, RoutedEventArgs e) => TxtSourceText.Text = TextGenerator.GenerateText(100);
        private void BtnGenText500_Click(object sender, RoutedEventArgs e) => TxtSourceText.Text = TextGenerator.GenerateText(500);
        private void BtnGenText1000_Click(object sender, RoutedEventArgs e) => TxtSourceText.Text = TextGenerator.GenerateText(1000);
        private void BtnGenText2000_Click(object sender, RoutedEventArgs e) => TxtSourceText.Text = TextGenerator.GenerateText(2000);
        private void BtnGenText5000_Click(object sender, RoutedEventArgs e) => TxtSourceText.Text = TextGenerator.GenerateText(5000);

        private void BtnCalculateFrequencies_Click(object sender, RoutedEventArgs e)
        {
            string[] words = TextGenerator.Tokenize(TxtSourceText.Text);
            if (words.Length == 0)
            {
                MessageBox.Show("Пожалуйста, введите или сгенерируйте текст для анализа!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string[] sorted = (string[])words.Clone();
            MsdRadixSort.Sort(sorted);

            var frequencies = TextGenerator.CountFrequenciesFromSorted(sorted);

            _wordFrequencies.Clear();
            foreach (var wf in frequencies)
            {
                _wordFrequencies.Add(wf);
            }

            TbTotalWordsStat.Text = $"Всего слов: {words.Length:N0}";
            TbUniqueWordsStat.Text = $"Уникальных слов: {frequencies.Count:N0}";
        }

        private async void BtnRunAllBenchmarks_Click(object sender, RoutedEventArgs e)
        {
            BtnRunAllBenchmarks.IsEnabled = false;
            BtnRunAllBenchmarks.Content = "⏳ Измерение...";
            _benchmarkResults.Clear();

            int[] testSizes = { 100, 500, 1000, 2000, 5000, 10000 };

            await Task.Run(() =>
            {
                var warmupWords = TextGenerator.Tokenize(TextGenerator.GenerateText(200));
                StringQuickSort.Sort((string[])warmupWords.Clone());
                MsdRadixSort.Sort((string[])warmupWords.Clone());

                foreach (int size in testSizes)
                {
                    string text = TextGenerator.GenerateText(size);
                    string[] tokens = TextGenerator.Tokenize(text);

                    int iterations = size <= 1000 ? 5 : 3;
                    double totalQuickMs = 0;
                    double totalRadixMs = 0;

                    var sw = new Stopwatch();

                    for (int it = 0; it < iterations; it++)
                    {
                        var arrQuick = (string[])tokens.Clone();
                        var arrRadix = (string[])tokens.Clone();

                        sw.Restart();
                        StringQuickSort.Sort(arrQuick);
                        sw.Stop();
                        totalQuickMs += sw.Elapsed.TotalMilliseconds;

                        sw.Restart();
                        MsdRadixSort.Sort(arrRadix);
                        sw.Stop();
                        totalRadixMs += sw.Elapsed.TotalMilliseconds;
                    }

                    double avgQuick = totalQuickMs / iterations;
                    double avgRadix = totalRadixMs / iterations;

                    string winner;
                    string ratio;

                    if (avgQuick < avgRadix)
                    {
                        double times = avgRadix / (avgQuick > 0 ? avgQuick : 0.0001);
                        winner = "QuickSort";
                        ratio = $"В {times:F2}x быстрее";
                    }
                    else
                    {
                        double times = avgQuick / (avgRadix > 0 ? avgRadix : 0.0001);
                        winner = "MSD RadixSort";
                        ratio = $"В {times:F2}x быстрее";
                    }

                    var result = new BenchmarkResult
                    {
                        WordCount = size,
                        QuickSortTimeMs = avgQuick,
                        RadixSortTimeMs = avgRadix,
                        FasterAlgorithm = winner,
                        Ratio = ratio
                    };

                    Dispatcher.Invoke(() => _benchmarkResults.Add(result));
                }
            });

            BtnRunAllBenchmarks.IsEnabled = true;
            BtnRunAllBenchmarks.Content = "⚡ Запустить все эксперименты";
        }

        #endregion
    }
}
