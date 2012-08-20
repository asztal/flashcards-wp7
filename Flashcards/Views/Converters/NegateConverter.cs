using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Flashcards.Views.Converters {
	public class NegateConverter : IValueConverter {
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return !((bool) value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return !((bool) value);
		}

		#endregion
	}
}
