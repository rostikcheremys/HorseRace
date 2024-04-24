using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

namespace Program
{
    class ColorConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is Color color)
			{
				return new SolidColorBrush(color);
			}

			return DependencyProperty.UnsetValue;
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
