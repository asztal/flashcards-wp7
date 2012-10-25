using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Flashcards.ViewModels;
using Microsoft.Phone.Shell;

namespace Flashcards.Views {
	[NavigationUri("/Views/User.xaml")]
	public partial class User {
		string userName;
		UserViewModel user;

		public User() {
			InitializeComponent();

			Loaded += OnLoaded;
		}

		void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
			if (userName == null)
				return;

			App.ViewModel.FetchUserData(userName, 
				data => {
					DataContext = user = data;
					if (data.Sets.Count > 0)
						(ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
					loadingScreen.FadeOut();
				},
				err => {
					MessageBox.Show(err.Message, "Error fetching user data", MessageBoxButton.OK);
					NavigationService.GoBack();
				});
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			userName = this.GetQueryParameter<string>("userName");
			DataContext = new UserViewModel { UserName = userName };
		}

		void SetItemTapped(object sender, GestureEventArgs e) {
			Navigate<Set>("id", ((sender as FrameworkElement).DataContext as SetViewModel).ID);
		}

		void PracticeAllSets(object sender, EventArgs e) {
			if (user == null || user.Sets.Count == 0)
				return;

			Navigate<Practice>("sets", string.Join(",", from set in user.Sets select set.ID));
		}
	}
}
