using System;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Controls.Primitives;
using Flashcards.Views.Converters;
using System.Windows.Data;

namespace Flashcards.Controls {
	public partial class LogSlider {
		readonly SliderLogarithmicConverter converter;

		public LogSlider() {
			InitializeComponent();
			converter = new SliderLogarithmicConverter { Slider = slider, LogSlider = this };
			Loaded += OnLoaded;
		}

		public void OnLoaded(object sender, RoutedEventArgs args) {
			var binding = new Binding("Value") {
				Source = this,
				Converter = converter,
				Mode = BindingMode.TwoWay,
			};
			slider.SetBinding(RangeBase.ValueProperty, binding); 
		}

		public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(LogSlider), new PropertyMetadata(0.0, OnLimitChanged));
		public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(LogSlider), new PropertyMetadata(0.0, OnLimitChanged));
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(LogSlider), new PropertyMetadata(0.0));
		public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(LogSlider), new PropertyMetadata(Orientation.Horizontal));

		private static void OnLimitChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
			var ls = obj as LogSlider;
			ls.slider.Value = ls.converter.ConvertCore(ls.Value, null, CultureInfo.CurrentCulture);
		}

		public double Value {
			get { return (double)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}

		public double Minimum {
			get { return (double)GetValue(MinimumProperty); }
			set { SetValue(MinimumProperty, value); }
		}

		public double Maximum {
			get { return (double)GetValue(MaximumProperty); }
			set { SetValue(MaximumProperty, value); }
		}

		public Orientation Orientation {
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}
	}

	class SliderLogarithmicConverter : ValueConverter<double, object, double> {
		public Slider Slider { get; set; }
		public LogSlider LogSlider { get; set; }

		public override double ConvertCore(double logicalValue, object _, CultureInfo cultureInfo) {
			double minp = Slider.Minimum, maxp = Slider.Maximum;
			double minv = Math.Log(LogSlider.Minimum), maxv = Math.Log(LogSlider.Maximum);
			if (maxv <= minv)
				return minp;

			if (minv < 1) {
				minv += 1;
				maxv += 1;
			}

			var scale = (maxp - minp) / (maxv - minv);
			return (Math.Log(logicalValue) - minv) * scale + minp;
		}

		public override double ConvertBackCore(double physicalValue, object _, CultureInfo cultureInfo) {
			double minp = Slider.Minimum, maxp = Slider.Maximum;
			double minv = Math.Log(LogSlider.Minimum), maxv = Math.Log(LogSlider.Maximum);
			if (maxv < minv)
				maxv = minv;

			if (minv < 1) {
				minv += 1;
				maxv += 1;
			}
			
			var scale = (maxv - minv) / (maxp - minp);
			return Math.Exp(minv + scale * (physicalValue - minp));
		}
	}
}
