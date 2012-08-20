using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Flashcards.ViewModels;
using Microsoft.Phone.Tasks;

namespace Flashcards.Views {
	[NavigationUri("/Views/Group.xaml")]
	public partial class Group {
		long? groupID;
		GroupViewModel group;

		public Group() {
			InitializeComponent();

			Loaded += OnLoaded;
		}

		void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
			if (groupID.HasValue)
				DataContext = group = App.ViewModel.GetGroup(groupID.Value);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			groupID = this.GetQueryParameter<long>("id");
		}

		void SetItemTapped(object sender, GestureEventArgs e) {
			Navigate<Set>("id", ((sender as FrameworkElement).DataContext as SetViewModel).ID);
		}

		void PracticeAllSets(object sender, EventArgs e) {
			if (group == null)
				return;

			Navigate<Practice>("groups", group.ID);
		}

		private void ShowGroupDiscussion(object sender, RoutedEventArgs e) {
			if (groupID == null)
				return;

			var uri = new Uri(string.Format("http://quizlet.com/ajax/discuss.php?popout&id={0}&type=2", groupID));
			new WebBrowserTask { Uri = uri }.Show();
		}
	}
}
