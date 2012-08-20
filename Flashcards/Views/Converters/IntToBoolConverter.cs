using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Flashcards.Views.Converters {
	public class IntToBoolConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return System.Convert.ToInt64(value) != 0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotSupportedException();
		}
	}
}
