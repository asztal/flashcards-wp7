using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Flashcards.Views.Converters {
	public class VisibilityConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (value is bool)
				return (bool) value ? Visibility.Visible : Visibility.Collapsed;
			if (value is int)
				return ((int)value) != 0 ? Visibility.Visible : Visibility.Collapsed;

			return value != null ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return (Visibility) value == Visibility.Visible;
		}
	}
}
