using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;

namespace Flashcards.Controls {
	/// <summary>
	/// A selector similar to Windows Phone Toolkit's ListPicker, however it is optimized for long lists. It
	/// does not support the expanding mode, only the full page selection.
	/// </summary>
	public class ListChooser : ListBox {
		public ListChooser() {
			DefaultStyleKey = typeof(ListChooser);
		}

		protected override void OnTap(System.Windows.Input.GestureEventArgs e) {
			OpenPickerPage();

			e.Handled = true;
		}

		PhoneApplicationFrame frame;

		private void OpenPickerPage() {
			frame = Application.Current.RootVisual as PhoneApplicationFrame;

			if (frame != null) {
				frame.Navigated += FrameNavigated;
				frame.Navigate(new Uri("/Flashcards;component/Views/ListPickerPage.xaml", UriKind.Relative));
			}
		}

		void FrameNavigated(object sender, System.Windows.Navigation.NavigationEventArgs e) {
			frame.Navigated -= FrameNavigated;

			var page = e.Content as Views.ListPickerPage;
			if (page != null)
				page.ListChooser = this;
		}
	}
}
