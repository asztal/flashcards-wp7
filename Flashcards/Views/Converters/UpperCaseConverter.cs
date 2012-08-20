using System;
using System.Globalization;
using System.Windows.Data;
namespace Flashcards.Views.Converters {
	public class UpperCaseConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var s = value as string;

			return s != null
				? s.ToUpper(culture)
				: value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}