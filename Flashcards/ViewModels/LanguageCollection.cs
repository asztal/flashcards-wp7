using System.Linq;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Flashcards.ViewModels {
	public class LanguageCollection : ObservableCollection<LanguageViewModel> {
		readonly Dictionary<string, LanguageViewModel> languages;

		public LanguageCollection() {
			languages = new Dictionary<string, LanguageViewModel>();
			CollectionChanged += (_, e) => {
				if (e.Action == NotifyCollectionChangedAction.Add) {
					foreach (LanguageViewModel item in e.NewItems)
						languages[item.Code] = item;
				} else if (e.Action == NotifyCollectionChangedAction.Remove) {
					foreach (LanguageViewModel item in e.OldItems)
						languages.Remove(item.Code);
				}
			};
		}

		public LanguageViewModel GetLanguage(string code) {
			LanguageViewModel lang;
			languages.TryGetValue(code, out lang);
			return lang;
		}
	}
}
