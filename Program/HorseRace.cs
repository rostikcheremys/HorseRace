﻿using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace Program
{
	class HorseRace
	{
		private readonly Random _random = new();
		
		private readonly Action<double> _changeBalance;

		public Image HorseImage { get; set; }

		public Image JockeyImage { get; set; }

		public string Name { get; private set; }

		public Color Color { get; private set; }

		private TimeSpan _time;

		public TimeSpan Time 
		{
			get => _time;
			set
			{
				_time = value;
				Finished = true;
			}
		}

		private int Speed { get; set; }

		public int CurrentPosition { get; set; }

		public int PositionX { get; private set; }

		public int PositionY { get; private set; }

		private int _animationFrame;

		private int AnimationFrame
		{
			get => _animationFrame;
			set => _animationFrame = value % 8;
		}

		public bool IsBidClosed { get; set; }

		private double _money;

        public double Money 
		{
			get => _money;
			set
			{
				_money = Math.Round(value * Coefficient, 2);
				IsBidClosed = true;
			} 
		}

		private double _coefficient;

		public double Coefficient 
		{
			get => _coefficient; 
			
			set 
			{
				_coefficient = value;
				if (_coefficient <= 1) IsBidClosed = true;
			}
		}

		private bool _finished;

		public bool Finished 
		{
			get => _finished;
			private set 
			{
				_finished = value;
				if (value)
				{
					IsBidClosed = true;
					if (CurrentPosition == 1) _changeBalance(Money * Coefficient);
				}
			} 
		}

		
		public HorseRace(string name, Color color, int x, int y, Action<double> changeBalance)
		{
			Name = name;
			Color = color;
			IsBidClosed = false;
			_changeBalance += changeBalance;

			Speed = _random.Next(3, 7);
			Coefficient = 1.7 - Speed / 10.0;
			AnimationFrame = 0;

			HorseImage = new Image
			{
				Source = new BitmapImage(new Uri("Images/Horses/WithOutBorder_0000.png", UriKind.Relative)),
				Width = 100,
				Tag = "tmp"
			};

			JockeyImage = new Image
			{
				Source = new BitmapImage(new Uri($"Images/HorsesMask/mask_0000.png", UriKind.Relative)),
				Width = 50,
				Tag = "tmp"
			};

			PositionX = x;
			PositionY = y;
		}

		public async Task MoveAsync()
		{
			int distance = await Task.Run(() => (int)(Speed * (_random.Next(4, 10) / 10.0)));
			PositionX += distance;

			AnimationFrame++;

			string fileNumber = _animationFrame.ToString().Length > 1 ? _animationFrame.ToString() : "0" + _animationFrame.ToString();

			HorseImage.Source = new BitmapImage(new Uri($"Images/Horses/WithOutBorder_00{fileNumber}.png", UriKind.Relative));
			JockeyImage.Source = new BitmapImage(new Uri($"Images/HorsesMask/mask_00{fileNumber}.png", UriKind.Relative));

			if (!IsBidClosed)
			{
				CalculateCoefficient();
			}
		}

		private void CalculateCoefficient()
		{
			Coefficient = Math.Round(1.7 - Speed / 10.0 + CurrentPosition / 10.0 - (CurrentPosition == 1 ? PositionX / 2500.0 : 0), 2);
		}
	}
}