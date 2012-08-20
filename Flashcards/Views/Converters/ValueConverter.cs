using System;
using System.Windows.Data;
using System.Globalization;
using System.Diagnostics;

namespace Flashcards.Views.Converters {
	public abstract class ValueConverter<TFrom, TParam, TTo> : IValueConverter {
		public object Convert(object boxedValue, Type targetType, object boxedParameter, CultureInfo culture) {
			Debug.Assert(targetType != null);
			//if (!targetType.IsAssignableFrom(typeof(TTo)))
			//    throw new NotSupportedException("This converter does not support converting to the given type");
			//if (boxedParameter != null && !typeof(TParam).IsAssignableFrom(boxedParameter.GetType()))
			//    throw new NotSupportedException("This converter does not support a parameter of the given type");

			var value = (TFrom)System.Convert.ChangeType(boxedValue, typeof(TFrom), culture);
			var parameter = (TParam)System.Convert.ChangeType(boxedParameter, typeof(TParam), culture);
			var result = ConvertCore(value, parameter, culture);
			return System.Convert.ChangeType(result, targetType, culture);
		}

		public object ConvertBack(object boxedValue, Type targetType, object boxedParameter, CultureInfo culture) {
			Debug.Assert(targetType != null);
			//if (!targetType.IsAssignableFrom(typeof(TFrom)))
			//    throw new NotSupportedException("This converter does not support converting to the given type");
			//if (boxedParameter != null && !typeof(TParam).IsAssignableFrom(boxedParameter.GetType()))
			//    throw new NotSupportedException("This converter does not support a parameter of the given type");

			var value = (TTo)System.Convert.ChangeType(boxedValue, typeof(TTo), culture);
			var parameter = (TParam)System.Convert.ChangeType(boxedParameter, typeof(TParam), culture);
			var result = ConvertBackCore(value, parameter, culture);
			return System.Convert.ChangeType(result, targetType, culture);
		}

		public abstract TTo ConvertCore(TFrom obj, TParam param, CultureInfo cultureInfo);

		public abstract TFrom ConvertBackCore(TTo obj, TParam param, CultureInfo cultureInfo);
	}

	/// <summary>
	/// Methods for creating one-off value converters.
	/// </summary>
	public static class ValueConverter {
		public static IValueConverter Create<TFrom, TResult>(Func<TFrom, TResult> convert, Func<TResult, TFrom> convertBack = null) {
			return new DelegateValueConverter<TFrom, object, TResult>(convert, convertBack);
		}

		public static IValueConverter Create<TFrom, TParam, TResult>(Func<TFrom, TParam, TResult> convert, Func<TResult, TParam, TFrom> convertBack = null) {
			return new DelegateValueConverter<TFrom, TParam, TResult>(convert, convertBack);
		}

		public static IValueConverter Create<TFrom, TParam, TResult>(Func<TFrom, TParam, CultureInfo, TResult> convert, Func<TResult, TParam, CultureInfo, TFrom> convertBack = null) {
			return new DelegateValueConverter<TFrom, TParam, TResult>(convert, convertBack);
		}
	}

	public class DelegateValueConverter<TFrom, TParam, TTo> : ValueConverter<TFrom, TParam, TTo> {
		private readonly Func<TFrom, TParam, CultureInfo, TTo> convert;
		private readonly Func<TTo, TParam, CultureInfo, TFrom> convertBack;

		public DelegateValueConverter(Func<TFrom, TTo> convert, Func<TTo, TFrom> convertBack = null) {
			this.convert = (x, _, __) => convert(x);
			if (convertBack != null)
				this.convertBack = (x, _, __) => convertBack(x);
		}

		public DelegateValueConverter(Func<TFrom, TParam, TTo> convert, Func<TTo, TParam, TFrom> convertBack = null) {
			this.convert = (x, p, _) => convert(x, p);
			if (convertBack != null) 
				this.convertBack = (x, p, _) => convertBack(x, p);
		}

		public DelegateValueConverter(Func<TFrom, TParam, CultureInfo, TTo> convert, Func<TTo, TParam, CultureInfo, TFrom> convertBack = null) {
			this.convert = convert;
			this.convertBack = convertBack;
		}

		public override TTo ConvertCore(TFrom obj, TParam param, CultureInfo cultureInfo) {
			return convert(obj, param, cultureInfo);
		}

		public override TFrom ConvertBackCore(TTo obj, TParam param, CultureInfo cultureInfo) {
			if (convertBack == null)
				throw new NotSupportedException("Cannot ConvertBack: this is a one-way Value converter");
			return convertBack(obj, param, cultureInfo);
		}
	}
}
