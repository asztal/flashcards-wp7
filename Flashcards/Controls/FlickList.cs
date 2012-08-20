using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using System.Diagnostics;

namespace Flashcards.Controls {
	public class FlickList : ItemsControl {
		int index;
		ContentPresenter left, middle, right;
		Canvas canvas;
		Storyboard storyboard;
		double initialScrollOffset;

		public FlickList() {
			DefaultStyleKey = typeof(FlickList);
		}

		protected override void OnManipulationStarted(ManipulationStartedEventArgs e) {
			this.CaptureMouse();
			
			initialScrollOffset = ScrollOffset;
			if (storyboard != null) {
				var temp = storyboard;
				storyboard = null;
				temp.Stop();
				ScrollOffset = initialScrollOffset; // Reinstate partially animated value
			}

			e.ManipulationContainer = this;
			e.Handled = true;
		}

		protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e) {
			this.ReleaseMouseCapture();
			if (Items.Count == 0) {
				ScrollOffset = 0;
				return;
			}

			e.Handled = true;

			int direction = 0;
			double dx = e.TotalManipulation.Translation.X + initialScrollOffset;

			if (dx > ActualWidth / 4 && index > 0) {
				// Scroll left
				direction = -1;
				ScrollOffset -= ActualWidth;
				initialScrollOffset -= ActualWidth;
			} else if (dx < -ActualWidth / 4 && index < Items.Count - 1) {
				// Scroll right
				direction = +1;
				initialScrollOffset += ActualWidth;
				ScrollOffset += ActualWidth;
			}
			
			index += direction;
			if (direction != 0)
				UpdateContentPresenters();
			Debug.Assert(index >= 0 && index < Items.Count);

			EventHandler animationCompleted = delegate {
				ScrollOffset = 0;
			};

			AnimateDouble(this, "ScrollOffset", ScrollOffset, 0, 0.4, direction == 0, animationCompleted);
		}

		protected override void OnManipulationDelta(ManipulationDeltaEventArgs e) {
			var translation = e.CumulativeManipulation.Translation;
			ScrollOffset = translation.X + initialScrollOffset;
			e.Handled = true;
		}

		private void AnimateDouble(DependencyObject o, string property, double start, double end, double time, bool bounce, EventHandler completed) {
			storyboard = new Storyboard();
			var anim = new DoubleAnimation {
				From = start,
				To = end,
				Duration = new Duration(TimeSpan.FromSeconds(time)),
				FillBehavior = FillBehavior.HoldEnd,
			};

			if (bounce)
				anim.EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 1 };
			else
				anim.EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut };

			storyboard.Children.Add(anim);
			Storyboard.SetTarget(anim, o);
			Storyboard.SetTargetProperty(anim, new PropertyPath(property));
			storyboard.Completed += (sender, e) => {
				if (storyboard != null) {
					// It might be null if the animation was stopped by a touch event

					storyboard.Stop();
					completed(sender, e);
					storyboard = null;
				}
			};
			storyboard.Begin();
		}

		protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) {
			if (left != null) {
				left.Arrange(new Rect(new Point(), finalSize));
				middle.Arrange(new Rect(new Point(), finalSize));
				right.Arrange(new Rect(new Point(), finalSize));
			}

			SetLefts(ScrollOffset, finalSize.Width);
			return finalSize;
		}

		protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) {
			return base.MeasureOverride(availableSize);
		}

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			canvas = GetTemplateChild("canvas") as Canvas;
			UpdateContentPresenters();
			OnScrollOffsetChanged(ScrollOffset);
		}

		private ContentPresenter NewContentPresenter(int index) {
			Debug.Assert(Dispatcher.CheckAccess(), "Illegal cross-thread access");

			var cp = new ContentPresenter { ContentTemplate = ItemTemplate, Content = Item(index) };
			canvas.Children.Add(cp);
			return cp;
		}

		private void UpdateContentPresenters() {
			Debug.Assert(Dispatcher.CheckAccess(), "Illegal cross-thread access");

			if (!Dispatcher.CheckAccess()) {
				Dispatcher.BeginInvoke(UpdateContentPresenters);
				return;
			}

			// Evidently changing the Content property of a ContentPresenter causes it to just disappear.
			// Just create new ones.
			if (left != null) left.Content = null;
			if (middle != null) middle.Content = null;
			if (right != null) right.Content = null;
			canvas.Children.Clear();
			left = NewContentPresenter(index - 1);
			middle = NewContentPresenter(index);
			right = NewContentPresenter(index + 1);

			InvalidateArrange();
		}

		private string DebugIndex(int i) {
			return i < 0 || i >= Items.Count ? "null" : i.ToString();
		}

		private object Item(int i) {
			return i < 0 || i >= Items.Count ? null : Items[i];
		}

		#region ScrollOffset
		public double ScrollOffset {
			get { return (double)GetValue(ScrollOffsetProperty); }
			set { SetValue(ScrollOffsetProperty, value); }
		}

		public static readonly DependencyProperty ScrollOffsetProperty =
			DependencyProperty.Register("ScrollOffset", typeof(double), typeof(FlickList), new PropertyMetadata(0.0, OnScrollOffsetChanged));
		
		private static void OnScrollOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			((FlickList)d).OnScrollOffsetChanged((double)e.NewValue);
		}

		private void OnScrollOffsetChanged(double newValue) {
			SetLefts(newValue, ActualWidth);
		}

		private void SetLefts(double offset, double width) {
			Debug.Assert(Dispatcher.CheckAccess(), "Illegal cross-thread access"); 
			Canvas.SetLeft(left, offset - width);
			Canvas.SetLeft(middle, offset);
			Canvas.SetLeft(right, offset + width);
		}
		#endregion
	}
}