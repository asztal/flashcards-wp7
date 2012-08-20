using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Flashcards {
	public static class LinqExtensions {
		public static void Remove<T>(this IList<T> list, Predicate<T> predicate) {
			for (int i = 0; i < list.Count; i++)
				if (predicate(list[i]))
					list.RemoveAt(i);
		}

		public static int IndexOf<T>(this IEnumerable<T> source, Predicate<T> predicate) {
			int i = 0;
			foreach (var item in source) {
				if (predicate(item))
					return i;
				i++;
			}
			return -1;
		}
	}

	public static class ShuffleExtension {
		public static void Shuffle<T>(this IList<T> items) {
			Shuffle(items, new Random());
		}

		public static void Shuffle<T>(this IList<T> items, Random random, int start, int count) {
			// Fisher-Yates shuffle
			while (count > 1) {
				count--;
				int k = random.Next(count + 1);
				var t = items[start + k];
				items[start + k] = items[start + count];
				items[start + count] = t;
			}
		}

		public static void Shuffle<T>(this IList<T> items, Random random) {
			Shuffle(items, random, 0, items.Count);
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class PageParameterAttribute : Attribute {
		readonly string parameterName;
		readonly bool required;

		public PageParameterAttribute(string parameterName, bool required = true) {
			this.parameterName = parameterName;
			this.required = required;
		}

		public string ParameterName {
			get { return parameterName; }
		}

		public bool Required {
			get { return required; }
		}
	}

	public static class PageExtensions {
		public static void ApplyQueryParameters(this Microsoft.Phone.Controls.PhoneApplicationPage page) {
			foreach (var member in page.GetType().GetMembers(BindingFlags.Instance | BindingFlags.NonPublic)) {
				foreach (PageParameterAttribute attr in member.GetCustomAttributes(typeof(PageParameterAttribute), false)) {
					string paramValue;
					if (!page.NavigationContext.QueryString.TryGetValue(attr.ParameterName, out paramValue)) {
						if (attr.Required)
							throw new ArgumentException("Required page parameter not supplied: " + attr.ParameterName);
						continue;
					}

					try {
						object value = Convert.ChangeType(paramValue, member.GetType(), CultureInfo.InvariantCulture);

						((FieldInfo) member).SetValue(page, value);
					} catch (FormatException e) {
						throw new ArgumentException("Required page parameter is invalid: " + attr.ParameterName, e);
					} catch (InvalidCastException e) {
						throw new ArgumentException("Required page parameter is invalid: " + attr.ParameterName, e);
					} catch (OverflowException e) {
						throw new ArgumentException("Required page parameter is invalid: " + attr.ParameterName, e);
					}
				}
			}
		}

		public static T GetQueryParameter<T>(this PhoneApplicationPage page, string parameterName) {
			T value;
			page.GetQueryParameter(parameterName, out value);
			return value;
		}

		public static T? GetOptionalQueryParameter<T>(this PhoneApplicationPage page, string parameterName) where T : struct {
			T value;
			if (page.GetQueryParameter(parameterName, out value))
				return value;
			return null;
		}

		public static IEnumerable<T> GetIDListParameter<T>(this PhoneApplicationPage page, string parameterName, bool required = true) where T : struct {
			string paramValue;
			if (!page.NavigationContext.QueryString.TryGetValue(parameterName, out paramValue)) {
				if (required)
					throw new ArgumentException("Required page parameter not supplied: " + parameterName);
				yield break;
			}

			foreach (string s in paramValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				yield return ConvertTo<T>(s, parameterName);
		}

		static T ConvertTo<T>(string str, string parameterName) {
			try {
				return (T)Convert.ChangeType(str, typeof(T), CultureInfo.InvariantCulture);
			} catch (FormatException e) {
				throw new ArgumentException("Required page parameter is invalid: " + parameterName, e);
			} catch (InvalidCastException e) {
				throw new ArgumentException("Required page parameter is invalid: " + parameterName, e);
			} catch (OverflowException e) {
				throw new ArgumentException("Required page parameter is invalid: " + parameterName, e);
			}
		}

		public static bool GetQueryParameter<T>(this PhoneApplicationPage page, string parameterName, out T value, bool required = true, T defaultValue = default(T)) {
			string paramValue;
			if (!page.NavigationContext.QueryString.TryGetValue(parameterName, out paramValue)) {
				if (required)
					throw new ArgumentException("Required page parameter not supplied: " + parameterName);
				value = defaultValue;
				return false;
			}

			value = ConvertTo<T>(paramValue, parameterName);
			return true;
		}

		public static void HideProgressBar(this PhoneApplicationPage page) {
			SystemTray.SetProgressIndicator(page, null);
		}

		public static void SetProgressBar(this PhoneApplicationPage page, string text) {
			SystemTray.SetProgressIndicator(page, new ProgressIndicator {
				IsVisible = true, 
				IsIndeterminate = true, 
				Text = text
			});
		}
	}

	public static class FrameworkExtensions {
		public static object TryFindResource(this FrameworkElement element, object resourceKey) {
			object value;

			while (element != null) {
				if ((element.Resources as IDictionary<object, object>).TryGetValue(resourceKey, out value))
					return value;
				element = element.Parent as FrameworkElement;
			}

			return (Application.Current.Resources as IDictionary<object, object>).TryGetValue(resourceKey, out value) 
				? value 
				: null;
		}

		public static T TryFindResource<T>(this FrameworkElement element, object resourceKey, T defaultValue = default(T)) {
			object boxed = TryFindResource(element, resourceKey);
			return boxed != null ? (T)boxed : defaultValue;
		}
	}
}