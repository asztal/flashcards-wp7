using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Flashcards.ViewModels;
using Microsoft.Phone.Tasks;

namespace Flashcards.Views {
	[NavigationUri("/Views/Subject.xaml")]
	public partial class Subject {
		string subject;
		SetViewModel[] results;

		public Subject() {
			InitializeComponent();

			Loaded += OnLoaded;
		}

		void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
			if (subject != null)
				DataContext = results =
					(from set in App.ViewModel.AllSets
					where set.Subjects.Select(s => s.Subject).Contains(subject, StringComparer.CurrentCultureIgnoreCase)
					orderby set.Title
					select set).ToArray();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			(Resources["SubjectName"] as SubjectViewModel).Subject = subject = this.GetQueryParameter<string>("subject");
		}

		void SetItemTapped(object sender, GestureEventArgs e) {
			Navigate<Set>("id", ((sender as FrameworkElement).DataContext as SetViewModel).ID);
		}

		private void AuthorTapped(object sender, GestureEventArgs e) {
			Navigate<User>("userName", ((sender as FrameworkElement).DataContext as SetViewModel).Author);
		}

		void PracticeAllSets(object sender, EventArgs e) {
			if (subject == null)
				return;

			if (results.Length > 0)
				Navigate<Practice>("sets", string.Join(",", from set in results select set.ID));
		}

		private void FetchMoreResults(object sender, RoutedEventArgs e) {
			// TODO Fetch from interweb
		}
	}
}
