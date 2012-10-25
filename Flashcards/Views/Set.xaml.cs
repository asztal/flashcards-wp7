using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Flashcards.ViewModels;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace Flashcards.Views {
	[NavigationUri("/Views/Set.xaml")]
	public partial class Set {
		private long? setID;
		private SetViewModel set;

		public Set() {
			InitializeComponent();

			Loaded += OnLoaded;
		}

		void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
			if (set != null)
				return;

			if (!setID.HasValue)
				return;

			Metrics.Measure("MainViewModel.GetSet", delegate {
				set = App.ViewModel.GetSet(setID.Value, true);
			});
			Metrics.Measure("Set.xaml: Set DataContext", delegate {
				DataContext = set;
			});

			if (set == null) {
				MessageBox.Show("There was an error navigating to this group.");
				NavigationService.GoBack();
				return;
			}

			App.ViewModel.AddRecentSet(set);
			SetBinding(StarredInUIProperty, new Binding("Starred") { Source = set });

			bool shouldFetch = false;

			if (!set.TermsLoaded) {
				shouldFetch = true;
			} else if (NavigationContext.QueryString.ContainsKey("modifiedDate")) {
				string modifiedDate = NavigationContext.QueryString["modifiedDate"];

				long ticks;
				if (modifiedDate != null
					&& long.TryParse(modifiedDate, out ticks)
						&& set.Modified.Ticks < ticks)
					// We have the group cached, but it has been updated since then.
					// Show the cached version but load the new one in the background.
					shouldFetch = true;
			}

			if (shouldFetch)
				set.FetchTerms(
					delegate { progressBar.Visibility = Visibility.Collapsed; },
					err => {
						progressBar.Visibility = Visibility.Collapsed;
						MessageBox.Show(err.Message, "Unable to retrieve terms", MessageBoxButton.OK);
					});

			((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = set.IsEditable;
			ApplicationBar.IsMenuEnabled = set.Author == App.ViewModel.UserName;
			//((ApplicationBarIconButton)ApplicationBar.MenuItems[0]).IsEnabled = group.Author == App.ViewModel.UserName;

			if (set.TermsLoaded)
				progressBar.Visibility = Visibility.Collapsed;
		}

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
			if (set != null)
				return;
			
			setID = this.GetQueryParameter<long>("id"); ;
		}

		private void Share(object sender, EventArgs e) {
			Debug.Assert(set != null, "group != null");

			var task = new Microsoft.Phone.Tasks.ShareLinkTask {
				LinkUri = set.Uri,
				Title = set.Title
			};

			task.Show();
		}

		private void Edit(object sender, EventArgs e) {
			Debug.Assert(set != null, "group != null");

			Navigate<EditSet>("id", set.ID);
		}

		private void ToggleFavourite(object sender, EventArgs e) {
			Debug.Assert(set != null, "group != null");

			SystemTray.SetProgressIndicator(this, new ProgressIndicator { 
				IsIndeterminate = true, 
				IsVisible = true, 
				Text = set.Starred ? "Removing from favourites..." : "Adding to favourites..."
			});

			set.SetStarred(
				!set.Starred,
				() => SystemTray.SetProgressIndicator(this, null), // The binding will update the UI.
				err => { });
		}

		public static readonly DependencyProperty StarredInUIProperty = DependencyProperty.Register("StarredInUI", typeof(bool), typeof(Set), new PropertyMetadata(false, OnStarredInUIChanged));

		static void OnStarredInUIChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			((Set) o).OnStarredInUIChanged();
		}

		void OnStarredInUIChanged() {
			if (ApplicationBar.Buttons.Count < 4 || set == null)
				return;
			var button = (ApplicationBarIconButton)ApplicationBar.Buttons[3];
			button.Text = set.Starred ? "unfavourite" : "add to favourites";
			button.IconUri = set.Starred ? new Uri("/Images/favs.remove.png", UriKind.Relative) : new Uri("/Images/favs.addto.png", UriKind.Relative);
		}

		public bool StarredInUI {
			get { return (bool)GetValue(StarredInUIProperty); }
			set { SetValue(StarredInUIProperty, value); }
		}

		void Delete(object sender, EventArgs e) {
			if (MessageBox.Show("Are you sure you want to delete this group? It can't be undone!", "", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
				return;

			SystemTray.SetProgressIndicator(this, 
				new ProgressIndicator {
					IsIndeterminate = true,
					Text = "Deleting group...",
					IsVisible = true
				});

			App.ViewModel.Delete(
				set,
				() => {
					NavigationService.GoBack();
					SystemTray.SetProgressIndicator(this, null);
				}, 
				err => {
					SystemTray.SetProgressIndicator(this, null);
					MessageBox.Show(err.Message);
				});
		}

		void Practice(object sender, EventArgs e) {
			Navigate<Practice>("sets", set.ID);
		}

		private void OpenInBrowser(object sender, EventArgs e) {
			new WebBrowserTask { Uri = set.Uri }.Show();
		}

		private void ShowSetDiscussion(object sender, RoutedEventArgs e) {
			if (set == null)
				return;

			var uri = new Uri(string.Format("http://quizlet.com/ajax/discuss.php?popout&id={0}&type=1", set.ID));
			new WebBrowserTask { Uri = uri }.Show();
		}

		private void SubjectTapped(object sender, System.Windows.Input.GestureEventArgs e) {
			var subject = ((sender as FrameworkElement).DataContext as SubjectViewModel).Subject;
			Navigate<Subject>("subject", subject);
		}

		private void AuthorTapped(object sender, System.Windows.Input.GestureEventArgs e) {
			var author = ((sender as FrameworkElement).DataContext as SetViewModel).Author;
			Navigate<User>("userName", author);
		}
	}
}