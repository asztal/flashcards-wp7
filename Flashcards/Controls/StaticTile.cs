using System.Windows;
using System.Windows.Controls;

namespace Flashcards.Controls {
	public class StaticTile : Control {
		static StaticTile() {
			TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(StaticTile), new PropertyMetadata(string.Empty));
			SmallTextProperty = DependencyProperty.Register("SmallText", typeof(string), typeof(StaticTile), new PropertyMetadata(string.Empty));
		}

		public StaticTile() {
			DefaultStyleKey = typeof(StaticTile);
		}
		
		#region Properties
		public string Text {
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public string SmallText {
			get { return (string)GetValue(SmallTextProperty); }
			set { SetValue(SmallTextProperty, value); }
		}

		public static readonly DependencyProperty SmallTextProperty;
		public static readonly DependencyProperty TextProperty;
		#endregion
	}
}
