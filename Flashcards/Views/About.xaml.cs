using System;
using System.Linq;
using System.Windows;
using Microsoft.Phone.Tasks;

namespace Flashcards.Views {
	[NavigationUri("/Views/About.xaml")]
	public partial class About : Page {
		public About() {
			InitializeComponent();
		}

		private void ContactAuthor(object sender, RoutedEventArgs e) {
			new EmailComposeTask {
				To = "lee@asztal.net",
				Subject = "Flashcards app for WP7",
			}.Show();
		}

		private void VisitQuizlet(object sender, RoutedEventArgs e) {
			new WebBrowserTask { Uri = new Uri("http://quizlet.com/", UriKind.Absolute) }.Show();
		}

		private void VisitQuizletBlog(object sender, RoutedEventArgs e) {
			new WebBrowserTask { Uri = new Uri("http://quizlet.com/blog", UriKind.Absolute) }.Show();
		}
	}
}