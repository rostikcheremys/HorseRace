﻿using System.Windows;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Program
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeAnimationTimer();
            BetIndexChanging();

            MinWidth = MaxWidth = 1200;
            MinHeight = MaxHeight = 800;
        }

        private List<HorseRace> _horses = new ();
        private readonly Stopwatch _raceStopwatch = new ();
        private readonly DispatcherTimer _animationTimer = new ();
        private List<string> _activeHorsesNames = ["Lucky"];
        private readonly List<string> _horsesNames = ["Lucky", "Ranger", "Willow", "Coco", "Spirit", "Rocky", "Blaze"];
        private readonly List<Color> _jockeyColors = [Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.DodgerBlue, Colors.Blue, Colors.Indigo];
        private int _finishedCount;
        public List<int> Bets { get; } = [10, 20, 50, 100, 200, 300, 500, 1000];

        private int _currentActiveHorseIndex;
        private int CurrentActiveHorseIndex
        {
            get => _currentActiveHorseIndex;

            set
            {
                if (value < 0)
                {
                    _currentActiveHorseIndex = _activeHorsesNames.Count - 1;
                }
                else if (value > _activeHorsesNames.Count - 1)
                {
                    _currentActiveHorseIndex = 0;
                }
                else
                {
                    _currentActiveHorseIndex = value;
                }

                HorseIndexChanging();
            }
        }

        public string CurrentActiveHorse => _activeHorsesNames[_currentActiveHorseIndex];

        private int _currentBetIndex;

        private int CurrentBetIndex
        {
            get => _currentBetIndex;

            set
            {
                if (value < 0)
                {
                    _currentBetIndex = Bets.Count - 1;
                }
                else if (value > Bets.Count - 1)
                {
                    _currentBetIndex = 0;
                }
                else
                {
                    _currentBetIndex = value;
                }

                BetIndexChanging();
            }
        }
        
        private double _balance = 1000;

        public double Balance
        {
            get => _balance;

            private set
            {
                if (value <= 0)
                {
                    _balance = 0;
                    BetButton.IsEnabled = false;
                }
                
                else _balance = value;

                BalanceChanging();
            }
        }
        
        private void BetIndexChanging() => BetDisplay.Text = $"{Bets[CurrentBetIndex]}$";

        private void BalanceChanging() => DisplayBalance.Text = $"Balance: {Math.Round(Balance, 2)}$";

        private void HorseIndexChanging() => ActiveHorseNameDisplay.Text = _activeHorsesNames[CurrentActiveHorseIndex];

        private void InitializeHorses()
        {
            int offsetY = 210;

            int heightRaceTrack = 250;

            int numberHorses = int.Parse((NumberHorses.SelectedItem as ComboBoxItem)?.Content.ToString() ?? string.Empty);

            int space = heightRaceTrack / (numberHorses - 1);

            for (int i = 0; i < numberHorses; i++)
            {
                HorseRace horse = new HorseRace(_horsesNames[i], _jockeyColors[i], 20, offsetY, val => Balance += val);

                _horses.Add(horse);

                offsetY += space;

                Rectangle jockeyRectangle = new Rectangle
                {
                    Width = horse.JockeyImage.Width,
                    Height = horse.JockeyImage.Height,
                    Fill = new SolidColorBrush(horse.Color),
                    OpacityMask = new ImageBrush
                    {
                        ImageSource = horse.JockeyImage.Source
                    }
                };

                RaceTrack.Children.Add(jockeyRectangle);

                RaceTrack.Children.Add(horse.HorseImage);

                Canvas.SetLeft(horse.HorseImage, horse.PositionX);
                Canvas.SetTop(horse.HorseImage, horse.PositionY);
                Canvas.SetLeft(jockeyRectangle, horse.PositionX);
                Canvas.SetTop(jockeyRectangle, horse.PositionY - 30);
            }
        }

        private void InitializeAnimationTimer()
        {
            _animationTimer.Interval = TimeSpan.FromMilliseconds(80);
            _animationTimer.Tick += async (_, _) => await UpdatePositionsHorse();
        }

        private async Task UpdatePositionsHorse()
        {
            List<Task> distanceTasks = _horses.Select(horse => horse.MoveAsync()).ToList();

            await Task.WhenAll(distanceTasks);

            foreach (var horse in _horses)
            {
                Canvas.SetLeft(horse.HorseImage, horse.PositionX);
                Canvas.SetTop(horse.HorseImage, horse.PositionY);
                Canvas.SetLeft(horse.JockeyImage, horse.PositionX);
                Canvas.SetTop(horse.JockeyImage, horse.PositionY - 30);

                if (horse is { PositionX: >= 840, Finished: false })
                {
                    horse.Time = _raceStopwatch.Elapsed;
                    _finishedCount++;

                    if (_finishedCount >= _horses.Count - 1)
                    {
                        StopRace();
                        break;
                    }
                }
            }

            _horses = _horses.OrderByDescending(horse => horse.PositionX).ToList();
            
            HorsesDataGrid.ItemsSource = null;
            HorsesDataGrid.ItemsSource = _horses;

            for (var i = 0; i < _horses.Count; i++)
            {
                _horses[i].CurrentPosition = i + 1;
            }
        }

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            InitializeHorses();

            _activeHorsesNames = _horses.Select(h => h.Name).ToList();
            if (Balance >= 0) BetButton.IsEnabled = Balance > 0;

            _animationTimer.Start();
            _raceStopwatch.Restart();

            StartPanel.Visibility = Visibility.Collapsed;
            _finishedCount = 0;
        }

        private void StopRace()
        {
            _animationTimer.Stop();
            _raceStopwatch.Stop();

            StartPanel.Visibility = Visibility.Visible;

            _horses.Clear();

            for (int i = RaceTrack.Children.Count - 1; i >= 0; i--)
            {
                UIElement? child = RaceTrack.Children[i];

                if (child is FrameworkElement element && element.Tag as string == "tmp")
                {
                    RaceTrack.Children.RemoveAt(i);
                }
            }
        }

        private void Previous_Bet_Button_Click(object sender, RoutedEventArgs e) => CurrentBetIndex--;

        private void Next_Bet_Button_Click(object sender, RoutedEventArgs e) => CurrentBetIndex++;

        private void Previous_Horse_Button_Click(object sender, RoutedEventArgs e)
        {
            CurrentActiveHorseIndex--;
            if (_horses.Count != 0) BetButton.IsEnabled = !_horses.First(h => h.Name == _activeHorsesNames[CurrentActiveHorseIndex]).IsBidClosed && Balance > 0;
        }

        private void Next_Horse_Button_Click(object sender, RoutedEventArgs e)
        {
            CurrentActiveHorseIndex++;
            if (_horses.Count != 0) BetButton.IsEnabled = !_horses.First(h => h.Name == _activeHorsesNames[CurrentActiveHorseIndex]).IsBidClosed && Balance > 0;
        }

        private void Bet_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Bets[CurrentBetIndex] <= Balance)
            {
                Balance -= Bets[CurrentBetIndex];

                if (_horses.Count != 0)
                {
                    _horses.First(h => h.Name == _activeHorsesNames[CurrentActiveHorseIndex]).Money += Bets[CurrentBetIndex];
                    BetButton.IsEnabled = false;
                }
            }
        }
    }
}