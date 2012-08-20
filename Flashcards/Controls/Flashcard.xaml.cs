using System;
using System.Linq;
using System.Windows;

namespace Flashcards.Controls {
	public partial class Flashcard {
		public Flashcard() {
			InitializeComponent();

			VisualStateManager.GoToState(this, "Heads", true);

			Tap += delegate {
				Flipped = !Flipped;
			};
		}

		static Flashcard() {
			FlippedProperty =
				DependencyProperty.Register("Flipped", typeof (bool), typeof (Flashcard),
					new PropertyMetadata(false, FlippedChanged));
			FrontTextProperty =
				DependencyProperty.Register("FrontText", typeof (string), typeof (Flashcard),
					new PropertyMetadata(null));
			BackTextProperty =
				DependencyProperty.Register("BackText", typeof (string), typeof (Flashcard),
					new PropertyMetadata(null));
			DisplayedTextProperty =
				DependencyProperty.Register("DisplayedText", typeof (string), typeof (Flashcard),
					new PropertyMetadata(null));
		}

		public static readonly DependencyProperty FlippedProperty, FrontTextProperty, BackTextProperty, DisplayedTextProperty;

		public bool Flipped {
			get { return (bool) GetValue(FlippedProperty); }
			set { SetValue(FlippedProperty, value); }
		}

		public string FrontText {
			get { return (string) GetValue(FrontTextProperty); }
			set { SetValue(FrontTextProperty, value); }
		}

		public string BackText {
			get { return (string) GetValue(BackTextProperty); }
			set { SetValue(BackTextProperty, value); }
		}

		public string DisplayedText {
			get { return (string) GetValue(DisplayedTextProperty); }
			set { SetValue(DisplayedTextProperty, value); }
		}

		static void FlippedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			var flipped = (bool) e.NewValue;
			var control = (Flashcard) o;
			VisualStateManager.GoToState(control, flipped ? "Tails" : "Heads", true);
			control.DisplayedText = flipped ? control.BackText : control.FrontText;
		}
	}
}
