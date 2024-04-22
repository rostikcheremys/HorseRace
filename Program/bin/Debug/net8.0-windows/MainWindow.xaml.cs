using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Program
{
	public partial class MainWindow
	{
		private ObservableCollection<HorseRace> _horses = new();
		private readonly Stopwatch _raceStopwatch = new();
		private readonly DispatcherTimer _animationTimer = new();

		private readonly List<string> _horsesNames = ["Lucky", "Ranger", "Willow", "Tucker", "Sabrina"];
		private List<string> _activeHorsesNames = ["Lucky"];
		private int _currentActiveHorseIndex;
		
		private int CurrentActiveHorseIndex 
		{
			get => _currentActiveHorseIndex;

			set
			{
				if (value < 0) _currentActiveHorseIndex = _activeHorsesNames.Count - 1;
				else if (value > _activeHorsesNames.Count - 1) _currentActiveHorseIndex = 0;
				else _currentActiveHorseIndex = value;

				OnCrtActiveHorseIndexChanging();
			}
		}

		public string CurrentActiveHorse => _activeHorsesNames[_currentActiveHorseIndex];

		public List<int> Bets { get; } = [10, 20, 50, 100, 200, 300, 500, 1000];
		
		private int _currentBetIndex;

		private int CurrentBetIndex
		{
			get => _currentBetIndex;

			set
			{
				if (value < 0) _currentBetIndex = Bets.Count - 1;
				else if (value > Bets.Count - 1) _currentBetIndex = 0;
				else _currentBetIndex = value;

				OnCrtBetIndexChanging();
			}
		}

		private readonly List<Color> _jockeyColors = [Colors.Red, Colors.Blue, Colors.Green, Colors.Purple, Colors.Orange];

		private int _finishedCount;

		private double _balance = 1000;

		public double Balance 
		{
			get => _balance; 

			private set
			{
				if (value <= 0)
				{
					_balance = 0;
					BetBtn.IsEnabled = false;
				}
				else _balance = value;

				OnBalanceChanging();
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			InitializeAnimationTimer();
			OnCrtBetIndexChanging();
			Balance = 1000;
		}

		private void InitializeHorses()
		{
			int offsetY = 150;

			int racetrackHeight = 200;

			int numberOfHorses = int.Parse((NumberOfHorsesComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? string.Empty);

			int space = racetrackHeight / (numberOfHorses - 1);

			for (int i = 0; i < numberOfHorses; i++)
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
			_animationTimer.Tick += async (_, _) => await UpdateHorsePositionsAsync();
		}

		private async Task UpdateHorsePositionsAsync()
		{
			var distanceTasks = _horses.Select(horse => horse.MoveAsync()).ToList();

			 await Task.WhenAll(distanceTasks);

			foreach (var horse in _horses)
			{
				Canvas.SetLeft(horse.HorseImage, horse.PositionX);
				Canvas.SetTop(horse.HorseImage, horse.PositionY);
				Canvas.SetLeft(horse.JockeyImage, horse.PositionX);
				Canvas.SetTop(horse.JockeyImage, horse.PositionY - 30);
				

				if (horse is { PositionX: >= 950, Finished: false })
				{
					horse.Time = _raceStopwatch.Elapsed;
					_finishedCount++;
					
					if (_finishedCount >= _horses.Count - 1)
					{
						EndSimulation();
						break;
					}
				}
			}

			_horses = new ObservableCollection<HorseRace>(_horses.OrderByDescending(horse => horse.PositionX));
			HorsesDataGrid.ItemsSource = _horses;

			for (var i = 0; i < _horses.Count; i++) 
			{
				_horses[i].CurrentPosition = i + 1;
			}
		}

		private void Play_Button_Click(object sender, RoutedEventArgs e)
		{
			InitializeHorses();

			_activeHorsesNames = _horses.Select(h => h.Name).ToList();
			if (Balance >= 0) BetBtn.IsEnabled = Balance > 0;

			_animationTimer.Start();
			_raceStopwatch.Start();
			
			PlayPanel.Visibility = Visibility.Collapsed;
			_finishedCount = 0;
		}

		private void EndSimulation()
		{
			_animationTimer.Stop();
			_raceStopwatch.Stop();

			PlayPanel.Visibility = Visibility.Visible;

			_horses.Clear();
			
			for (int i = RaceTrack.Children.Count - 1; i >= 0; i--)
			{
				var child = RaceTrack.Children[i];
				
				if (child is FrameworkElement element && element.Tag as string == "tmp")
				{
					RaceTrack.Children.RemoveAt(i);
				}
			}
		}

		private void Previous_Bet_Btn_Click(object sender, RoutedEventArgs e) => CurrentBetIndex--;

		private void Next_Bet_Btn_Click(object sender, RoutedEventArgs e) => CurrentBetIndex++;

		private void Previous_Horse_Btn_Click(object sender, RoutedEventArgs e)
		{
			CurrentActiveHorseIndex--;
			if (_horses.Count != 0) BetBtn.IsEnabled = !_horses.First(h => h.Name == _activeHorsesNames[CurrentActiveHorseIndex]).IsBidClosed && Balance > 0;
		}

		private void Next_Horse_Btn_Click(object sender, RoutedEventArgs e) 
		{
			CurrentActiveHorseIndex++;
			if (_horses.Count != 0) BetBtn.IsEnabled = !_horses.First(h => h.Name == _activeHorsesNames[CurrentActiveHorseIndex]).IsBidClosed && Balance > 0;
		} 

		private void BetBtn_Click(object sender, RoutedEventArgs e)
		{
			Balance -= Bets[CurrentBetIndex];
			if (_horses.Count != 0)
			{
				_horses.First(h => h.Name == _activeHorsesNames[CurrentActiveHorseIndex]).Money += Bets[CurrentBetIndex];
				BetBtn.IsEnabled = false;
			}
		}
			
		private void OnCrtBetIndexChanging() => BetDisplay.Text = $"{Bets[CurrentBetIndex]}$";

		private void OnBalanceChanging() => DisplayBalance.Text = $"Balance: {Math.Round(Balance, 2)}$";

		private void OnCrtActiveHorseIndexChanging() => ActiveHorseNameDisplay.Text = _activeHorsesNames[CurrentActiveHorseIndex];

	}
}