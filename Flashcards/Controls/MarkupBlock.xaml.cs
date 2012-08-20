using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace Flashcards.Controls {
	public partial class MarkupBlock {
		public MarkupBlock() {
			InitializeComponent();
		}

		#region TextAlignment
		public TextAlignment TextAlignment {
			get { return (TextAlignment) GetValue(TextAlignmentProperty); }
			set { SetValue(TextAlignmentProperty, value); }
		}

		public static readonly DependencyProperty TextAlignmentProperty =
			DependencyProperty.Register("TextAlignment", typeof (TextAlignment), typeof (MarkupBlock),
				new PropertyMetadata(TextAlignmentChanged));

		static void TextAlignmentChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			((MarkupBlock) o).TextAlignmentChanged();
		}

		void TextAlignmentChanged() {
			textBlock.TextAlignment = TextAlignment;
		}
#endregion

		#region TextWrapping
		public TextWrapping TextWrapping {
			get { return (TextWrapping) GetValue(TextWrappingProperty); }
			set { SetValue(TextWrappingProperty, value); }
		}

		public static readonly DependencyProperty TextWrappingProperty =
			DependencyProperty.Register("TextWrapping", typeof (TextWrapping), typeof (MarkupBlock),
				new PropertyMetadata(TextWrapping.NoWrap, TextWrappingChanged));

		static void TextWrappingChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			((MarkupBlock) o).TextWrappingChanged();
		}

		void TextWrappingChanged() {
			textBlock.TextWrapping = TextWrapping;
		}
		#endregion

		#region TextStyle
		public Style TextStyle {
			get { return (Style) GetValue(TextStyleProperty); }
			set { SetValue(TextStyleProperty, value); }
		}

		public static readonly DependencyProperty TextStyleProperty =
			DependencyProperty.Register("TextStyle", typeof (Style), typeof (MarkupBlock),
				new PropertyMetadata(TextStyleChanged));

		static void TextStyleChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			((MarkupBlock) o).TextStyleChanged();
		}

		void TextStyleChanged() {
			textBlock.Style = TextStyle;
		}
		#endregion

		#region Markup
		public string Markup {
			get { return (string) GetValue(MarkupProperty); }
			set { SetValue(MarkupProperty, value); }
		}

		public static readonly DependencyProperty MarkupProperty =
			DependencyProperty.Register("Markup", typeof (string), typeof (MarkupBlock),
				new PropertyMetadata(MarkupChanged));

		static void MarkupChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			((MarkupBlock) o).MarkupChanged((string) e.NewValue);
		}

		void MarkupChanged(string newValue) {
			if (string.IsNullOrWhiteSpace(newValue)) {
				textBlock.Inlines.Clear();
				return;
			}

			if (newValue.Count(c => c == '*') < 2) {
				textBlock.Text = newValue;
				return;
			}

			textBlock.Inlines.Clear();
			for (int i = 0;;) {
				int star = newValue.IndexOf('*', i);

				if (star == -1) {
					var text = newValue.Substring(i);
					if (text.Length != 0)
						textBlock.Inlines.Add(new Run { Text = text });
					break;
				}

				var normalText = newValue.Substring(i, star - i);
				if (normalText.Length != 0)
					textBlock.Inlines.Add(new Run { Text = normalText });

				int nextStar = newValue.IndexOf('*', star + 1);
				if (nextStar == -1) {
					var text = newValue.Substring(star); // Include the star.
					if (text.Length != 0)
						textBlock.Inlines.Add(new Run { Text = text });
					break;
				}

				var boldText = newValue.Substring(star + 1, nextStar - star - 1);
				if (boldText.Length != 0)
					textBlock.Inlines.Add(new Run { Text = boldText, FontWeight = FontWeights.Bold });

				i = nextStar + 1;
			}
		}
		#endregion
	}
}
