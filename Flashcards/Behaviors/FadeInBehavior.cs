using System;
using System.Linq;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Media.Animation;
using System.Windows.Data;
using System.ComponentModel;

namespace Flashcards.Behaviors {
	/// <summary>
	/// When attached to an element, the element fades in from transparent to its intended opacity value whenever
	/// it is added to the visual tree or the <c>Visibility</c> property changes.
	/// </summary>
	public class FadeInBehavior : Behavior<FrameworkElement> {
		private double finalOpacity = 1;

		public Duration Duration {
			get { return (Duration)GetValue(DurationProperty); }
			set { SetValue(DurationProperty, value); }
		}

		public static readonly DependencyProperty DurationProperty =
			DependencyProperty.Register("Duration", typeof(Duration), typeof(FadeInBehavior), new PropertyMetadata(new Duration(TimeSpan.FromSeconds(0.4))));

		public static readonly DependencyProperty TargetVisibilityProperty =
			DependencyProperty.RegisterAttached("TargetVisibility", typeof(Visibility), typeof(FadeInBehavior), new PropertyMetadata(Visibility.Collapsed, OnTargetVisibilityChanged));

		/// <summary>
		/// Tracks the visibility of the attached element by binding its visibility to this property.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool? TargetVisibility {
			get { return (bool?)GetValue(TargetVisibilityProperty); }
			set { SetValue(TargetVisibilityProperty, value); }
		}

		private static void OnTargetVisibilityChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
			if ((Visibility) args.NewValue != Visibility.Visible || (Visibility) args.OldValue != Visibility.Collapsed) 
				return;

			foreach (var behavior in Interaction.GetBehaviors(obj as FrameworkElement).OfType<FadeInBehavior>()) {
				behavior.FadeIn();
				break;
			}
		}

		protected override void OnAttached() {
			AssociatedObject.Loaded += Loaded;
			AssociatedObject.SetBinding(TargetVisibilityProperty, new Binding("Visibility") { Source = AssociatedObject });

			finalOpacity = AssociatedObject.Opacity;
			AssociatedObject.Opacity = 0;

			base.OnAttached();
		}

		protected override void OnDetaching() {
			// Remove the binding.
			AssociatedObject.ClearValue(TargetVisibilityProperty);

			AssociatedObject.Loaded -= Loaded;
		}

		void Loaded(object sender, RoutedEventArgs routedEventArgs) {
			FadeIn();
		}

		void FadeIn() {
			var s = new Storyboard();
			var anim = new DoubleAnimation {
				From = 0,
				To = finalOpacity,
				FillBehavior = FillBehavior.HoldEnd,
				Duration = Duration,
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
			};

			s.Children.Add(anim);
			Storyboard.SetTarget(anim, AssociatedObject);
			Storyboard.SetTargetProperty(anim, new PropertyPath("Opacity"));
			s.Begin();
		}
	}
}
