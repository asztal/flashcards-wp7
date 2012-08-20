using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Flashcards.ViewModels;

namespace Flashcards.Views {
	[NavigationUri("/Views/Settings.xaml")]
	public partial class Settings {
		private readonly SettingsViewModel settings;

		public Settings() {
			InitializeComponent();

			DataContext = settings = new SettingsViewModel();

			var promptWith = ((LanguageCollection)App.Current.Resources["LanguageCodes"]).ToList();
			promptWith.Insert(0, new LanguageViewModel { Code = "$term", Description = "Prompt with terms" });
			promptWith.Insert(1, new LanguageViewModel { Code = "$definition", Description = "Prompt with definitions" });
			promptWithLanguages.ItemsSource = promptWith;
		}

		protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e) {
			base.OnNavigatedFrom(e);

			Configuration.Save();
		}

		private void ClearSearchHistory(object sender, RoutedEventArgs e) {
			settings.SearchHistory = new List<string>();
		}
	}
}