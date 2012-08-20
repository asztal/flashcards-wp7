using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Flashcards.Model.API;
using Flashcards.ViewModels;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.Windows.Navigation;
using Microsoft.Phone.Net.NetworkInformation;
using System.IO.IsolatedStorage;

namespace Flashcards.Views {
	[NavigationUri("/Views/MainPage.xaml")]
	public partial class MainPage {
		static bool firstLoad = true;

		public MainPage() {
			InitializeComponent();

			Loaded += OnLoaded;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);

			if (QuizletAPI.Default.Credentials == null && !App.ViewModel.LoadCachedToken()) {
				Navigate<Login>();
				return;
			}

			if (firstLoad && Configuration.ResumeSessionOnOpen)
				using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
					if (storage.FileExists("Session"))
						Navigate<Practice>("resume", "");

			firstLoad = false;
		}

		// Load data for the ViewModel Items
		private void OnLoaded(object sender, RoutedEventArgs e) {
			// panorama.Margin = new Thickness(0, 0, 0, ApplicationBar.DefaultSize);

			if (!App.ViewModel.IsDataLoaded)
				App.ViewModel.LoadData();

			Debug.Assert(App.ViewModel.IsDataLoaded, "App.ViewModel.IsDataLoaded");

			if (!App.ViewModel.HasSynchronized && Configuration.SyncOnOpen && (Configuration.SyncOver3G || DeviceNetworkInformation.IsWiFiEnabled))
				App.ViewModel.Synchronize(delegate { }, null, err => { });

			if (DataContext == null)
				DataContext = App.ViewModel;
		}

		private void LinkClicked(object sender, RoutedEventArgs e) {
			new WebBrowserTask { Uri = new Uri("http://quizlet.com") }.Show();
		}

		private void SetItemTapped(object sender, System.Windows.Input.GestureEventArgs ge) {
			Navigate<Set>("id", ((sender as FrameworkElement).DataContext as SetViewModel).ID);
		}

		private void Logout(object sender, EventArgs e) {
			App.ViewModel.LogOut();
			Navigate<Login>();
		}

		private void Sync(object sender, EventArgs e) {
			SystemTray.SetIsVisible(this, true);
			this.SetProgressBar("Synchronizing (0/4)...");

			App.ViewModel.Synchronize(
				() => SystemTray.SetIsVisible(this, false), 
				(text, progress) => 
					SystemTray.SetProgressIndicator(this, new ProgressIndicator {
						Text = text,
						Value = progress,
						IsVisible = true
					}),
				err => {
					SystemTray.SetIsVisible(this, false);
					MessageBox.Show(err.Message);
				});
		}

		private void Settings(object sender, EventArgs e) {
			Navigate<Settings>();
		}

		private void NewList(object sender, EventArgs e) {
			Navigate<EditSet>();
		}

		private void Search(object sender, EventArgs e) {
			Navigate<Search>();
		}

		void About(object sender, EventArgs e) {
			Navigate<About>();
		}

		private void GroupItemTapped(object sender, System.Windows.Input.GestureEventArgs e) {
			var groupID = ((sender as FrameworkElement).DataContext as GroupViewModel).ID;

			Navigate<Group>("id", groupID);
		}

		void EditSet(object sender, RoutedEventArgs e) {
			Navigate<EditSet>("id", ((sender as FrameworkElement).DataContext as SetViewModel).ID);
		}

		void PracticeSet(object sender, RoutedEventArgs e) {
			Navigate<Practice>("sets", ((sender as FrameworkElement).DataContext as SetViewModel).ID);
		}

		void DeleteSet(object sender, RoutedEventArgs e) {
			if (MessageBox.Show("Are you sure you want to delete this group? It can't be undone!", "", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
				return;

			var set = (sender as FrameworkElement).DataContext as SetViewModel;

			this.SetProgressBar("Deleting group...");
			SystemTray.SetIsVisible(this, true);

			App.ViewModel.Delete(
				set,
				() => SystemTray.SetIsVisible(this, false),
				err => {
					SystemTray.SetIsVisible(this, false);
					MessageBox.Show(err.Message);
				});
		}

		void RemoveFromRecent(object sender, RoutedEventArgs e) {
			App.ViewModel.RecentSets.Remove((sender as FrameworkElement).DataContext as SetViewModel);
		}
	}
}