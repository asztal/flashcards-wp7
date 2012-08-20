using System.Collections;
using System.Globalization;
using System.Linq;
using System;
using System.Text;
using Microsoft.Phone.Controls;
using System.IO;

namespace Flashcards.Views {
	public class Page : PhoneApplicationPage {
		protected void Navigate<T>(params object[] parameters) where T : Page {
			var query = new StringBuilder();

			for (int i = 0; i+1 < parameters.Length; i+=2) {
				var name = (string)parameters[i];
				var value = ToParameterString(parameters[i + 1]);
				if (query.Length == 0)
					query.Append('?');

				query.Append(name).Append('=').Append(value);
			}

			foreach (NavigationUriAttribute attr in typeof(T).GetCustomAttributes(typeof(NavigationUriAttribute), false)) {
				NavigationService.Navigate(new Uri(attr.Uri + query, UriKind.Relative));
				return;
			}
		}

		private static string ToParameterString(object value) {
			var enumerable = value as IEnumerable;
			if (enumerable != null)
				string.Join(",", enumerable.Cast<object>().Select(ToParameterString));

			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		protected void SaveJsonState(string name, IJsonConvertible state) {
			var json = new JsonContext().ToJson(state);
			using (var sw = new StringWriter()) {
				JsonValue.WriteMin(json, sw);
				State[name] = sw.GetStringBuilder().ToString();
			}
		}

		protected T RestoreJsonState<T>(string name) where T : IJsonConvertible {
			var serialized = (string)State[name];
			JsonValue json;
			using (var sr = new StringReader(serialized))
				json = JsonValue.Parse(sr);
			return new JsonContext().FromJson<T>(json);
		}
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	sealed class NavigationUriAttribute : Attribute {
		readonly string uri;

		// This is a positional argument
		public NavigationUriAttribute(string uri) {
			this.uri = uri;
		}

		public string Uri {
			get { return uri; }
		}
	}
}