using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Windows.Input;
using Flashcards.ViewModels;
using Microsoft.Phone.Shell;

namespace Flashcards.Views {
	[NavigationUri("/Views/Search.xaml")]
	public partial class Search {
		public Search() {
			InitializeComponent();

			Loaded += PageLoaded;
		}

		void PageLoaded(object sender, RoutedEventArgs e) {
			viewBox.MaxHeight = viewBox.ActualHeight;

			searchText.ItemsSource = Configuration.SearchHistory;
		}

		private void SearchClicked(object sender, RoutedEventArgs _) {
			string query = searchText.Text.Trim();

			var searchHistory = Configuration.SearchHistory != null ? Configuration.SearchHistory.ToList() : new List<string>();
			if (searchHistory.All(item => string.Compare(item, query, StringComparison.CurrentCultureIgnoreCase) != 0))
				searchHistory.Add(query);
			Configuration.SearchHistory = searchHistory;
			searchText.ItemsSource = searchHistory;

			SystemTray.SetProgressIndicator(this, new ProgressIndicator {
				IsIndeterminate = true,
				IsVisible = true,
				Text = "Searching..."
			});

			App.ViewModel.Search(methodPicker.SelectedItem == searchTerms, query,
			results => {
				SystemTray.SetProgressIndicator(this, null);
				resultsList.DataContext = results;
			},
			e => {
				SystemTray.SetProgressIndicator(this, null);
				MessageBox.Show(e.Message, "Search failed", MessageBoxButton.OK); 
			});
		}

		private void ResultTapped(object sender, System.Windows.Input.GestureEventArgs e) {
			var set = ((FrameworkElement) sender).DataContext as SetViewModel;
			if (set == null)
				return;

			App.ViewModel.RegisterSetInfo(set); 

			Navigate<Set>("id", set.ID);
		}

		private void SearchTextKeyPress(object sender, KeyEventArgs e) {
			if (e.Key != Key.Enter)
				return;
			
			e.Handled = true;
			SearchClicked(sender, e);
		}
	}
}