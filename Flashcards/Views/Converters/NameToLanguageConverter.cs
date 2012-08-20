using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Flashcards.ViewModels;

namespace Flashcards.Views.Converters {
	public class NameToLanguageConverter : IValueConverter {
		public LanguageCollection Languages { get; set; }
	
		#region IValueConverter Members
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			parameter = parameter ?? Languages;
			if (!(parameter is LanguageCollection))
				return null;
			return value != null ? (parameter as LanguageCollection).GetLanguage(value.ToString()) : null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			if (!(value is LanguageViewModel))
				return null;
			return (value as LanguageViewModel).Code;
		}
		#endregion
	}
}
