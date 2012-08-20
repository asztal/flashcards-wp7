using System;
using System.Linq;
using System.Windows.Controls;

namespace Flashcards.Views {
	[NavigationUri("/Views/ListPickerPage.xaml")]
	public partial class ListPickerPage {
		public ListPickerPage() {
			InitializeComponent();
		}

		Controls.ListChooser listChooser;
		public Controls.ListChooser ListChooser {
			get {
				return listChooser;
			}
			set {
				listChooser = value;
				items.DataContext = value.ItemsSource;
				items.ItemTemplate = value.ItemTemplate;
				items.SelectedItem = value.SelectedItem;
				items.SelectionChanged += SelectionChanged;
			}
		}

		protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e) {
			base.OnNavigatedFrom(e);

			listChooser.SelectedItem = items.SelectedItem;
		}

		private void SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (items.SelectedItem != null)
				NavigationService.GoBack();
		}
	}
}