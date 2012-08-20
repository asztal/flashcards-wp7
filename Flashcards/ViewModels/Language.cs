using System.Linq;
using System;

namespace Flashcards.ViewModels {
	public class LanguageViewModel {
		string code;
		public string Code {
			get { return code; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				code = value;
			}
		}

		string description;
		public string Description {
			get { return description; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				description = value;
			}
		}

		public override string ToString() {
			return Description;
		}
	}
}
