using System;
using System.Linq;
using System.Windows;
using Flashcards.Model.API;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;

namespace Flashcards.Views {
	[NavigationUri("/Views/Login.xaml")]
	public partial class Login {
		string state;
		bool completed;

		public Login() {
			InitializeComponent();
			Loaded += LoginLoaded;
			Browser.Navigating += BrowserNavigating;
			Browser.NavigationFailed += BrowserNavigationFailed;
		}

		void ProcessUri(Uri uri) {
			string code = null;
			string confirmState = null;
			string error = null;
			string errorDesc = null;

			// TODO: Check for "error" GET variable
			foreach (var term in uri.Query.Split('&', '?')) {
				var parts = term.Split('=');
				if (parts.Length < 2)
					continue;

				if (parts[0] == "error")
					error = parts[1];

				if (parts[0] == "error_description")
					errorDesc = parts[1];

				if (parts[0] == "code")
					code = parts[1];

				if (parts[0] == "state")
					confirmState = parts[1];
			}
			
			if (confirmState != null && code != null) {
				if (state == confirmState) {
					ProgressBar.Visibility = Visibility.Visible;
					Browser.Visibility = Visibility.Collapsed;

					QuizletAPI.Default.Authenticate(
						code,
						() => Dispatcher.BeginInvoke(delegate {
							App.ViewModel.LogIn(QuizletAPI.Default.Credentials);

							Configuration.AccessToken = QuizletAPI.Default.Credentials.Token;
							Configuration.UserName = QuizletAPI.Default.Credentials.UserName;
							Configuration.AccessTokenExpiry = QuizletAPI.Default.Credentials.Expiry;

							// Navigating back twice throws an exception.
							if (!completed)
								NavigationService.GoBack();
							completed = true;
						}),
						e => Dispatcher.BeginInvoke(delegate {
							MessageBox.Show(e.Message, "Could not log in to quizlet", MessageBoxButton.OK);
							NavigationService.GoBack();
						}), new CancellationToken());
				} else {
					MessageBox.Show("Server returned an invalid response (possible CSRF attack)");
					NavigationService.GoBack();
				}
			} else if (error != null) {
				Dispatcher.BeginInvoke(delegate {
					if (errorDesc != null)
						MessageBox.Show(errorDesc, "Could not log in to quizlet", MessageBoxButton.OK);
					else
						MessageBox.Show("Could not log in to quizlet");
					NavigationService.GoBack();
				});
			}
		}

		void BrowserNavigating(object sender, NavigatingEventArgs e) {
			ProcessUri(e.Uri);
		}

		void BrowserNavigationFailed(object sender, NavigationFailedEventArgs e) {
			if (completed || e.Exception == null)
				return;
			string statusCode = "no status code given";
			if (e.Exception is WebBrowserNavigationException)
				statusCode = ((WebBrowserNavigationException)e.Exception).StatusCode.ToString();
			MessageBox.Show(string.Format("{0} ({1})", e.Exception.Message, statusCode), "Error logging in", MessageBoxButton.OK);
		}

		void LoginLoaded(object sender, RoutedEventArgs e) {
			state = Guid.NewGuid().ToString();
			Browser.Navigate(new Uri("https://quizlet.com/authorize/?response_type=code&client_id=" + QuizletAPI.ClientID + "&scope=read write_set write_group&state=" + state));
		}

		private void RefreshBrowser(object sender, EventArgs e) {
			// It would be a waste of time.
			if (completed)
				return;
			LoginLoaded(sender, new RoutedEventArgs());
		}
	}
}