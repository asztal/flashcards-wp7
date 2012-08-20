using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Flashcards.Controls {
	[TemplateVisualState(GroupName = "ExpansionStates", Name = "Expanded")]
	[TemplateVisualState(GroupName = "ExpansionStates", Name = "Collapsed")]
	[TemplatePart(Name = "expanderButton", Type = typeof(FrameworkElement))]
	public class Expander : ContentControl {
		Grid grid;

		public Expander() {
			DefaultStyleKey = typeof(Expander);
		}

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			grid = GetTemplateChild("grid") as Grid;

			var toggle = GetTemplateChild("expanderButton") as FrameworkElement;
			if (toggle != null)
				toggle.Tap += ChangeState;
		}

		void ChangeState(object sender, GestureEventArgs gestureEventArgs) {
			IsExpanded = !IsExpanded;
		}

		#region Properties
		static Expander() {
			ExpanderProperty = DependencyProperty.Register("ExpandedContent", typeof(UIElement), typeof(Expander), new PropertyMetadata(null));
			IsExpandedProperty = DependencyProperty.Register("IsExpanded", typeof(bool), typeof(Expander), new PropertyMetadata(false, IsExpandedChanged));
		}

		static void IsExpandedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			VisualStateManager.GoToState((Control) o, (bool)e.NewValue ? "Expanded" : "Collapsed", true);
		}

		public UIElement ExpandedContent {
			get { return (UIElement)GetValue(ExpanderProperty); }
			set { SetValue(ExpanderProperty, value); }
		}

		public bool IsExpanded {
			get { return (bool)GetValue(IsExpandedProperty); }
			set { SetValue(IsExpandedProperty, value); }
		}
	
		public static readonly DependencyProperty ExpanderProperty, IsExpandedProperty;
		#endregion
	}
}
