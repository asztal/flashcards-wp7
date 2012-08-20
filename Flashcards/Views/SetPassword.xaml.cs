using System;
using System.Linq;
using System.Windows;
using Flashcards.ViewModels;
using Microsoft.Phone.Shell;

namespace Flashcards.Views {
	[NavigationUri("/Views/SetPassword.xaml")]
	public partial class SetPassword {
		SetViewModel set;
		
		public SetPassword() {
			InitializeComponent();
		}

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
			string idString;
			long setID;
			if (!NavigationContext.QueryString.TryGetValue("id", out idString)) {
				MessageBox.Show("Can't group the password for this group because the group ID was not specified.");
				NavigationService.GoBack();
				return;
			}

			if (!long.TryParse(idString, out setID)) {
				MessageBox.Show("Can't submit a password for this group because the group ID is invalid.");
				NavigationService.GoBack();
				return;
			}

			set = App.ViewModel.GetSet(setID, false);
			if (set == null) {
				MessageBox.Show("Can't submit a password for this group because it is unknown to this application.");
				NavigationService.GoBack();
			}
		}

		private void SubmitPassword(object sender, EventArgs e) {
			SystemTray.SetProgressIndicator(this, new ProgressIndicator {
				IsIndeterminate = true,
				IsVisible = true,
			}); 

			set.SubmitPassword(
				password.Password,
				() => NavigationService.GoBack(),
				err => {
					SystemTray.SetProgressIndicator(this, null);
					MessageBox.Show(err.Message);
				});
		}

		private void Override(object sender, EventArgs e) {
			set.HasAccess = true;
			NavigationService.GoBack();
		}
	}
}