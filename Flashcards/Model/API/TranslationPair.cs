using System.Linq;
using System;

namespace Flashcards.Model.API {
	/// <summary>
	/// Represents a translation of a phrase, supporting property binding and cloning interfaces.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay("{Phrase} -> {Translation}")]
	public class TranslationPair
		: IComparable<TranslationPair>
	{
		public string Phrase { get; set; }

		public string Translation { get; set; }

		public TranslationPair(string phrase, string translation) {
			Phrase = phrase;
			Translation = translation;
		}

		/// <summary>A default constructor for the purposes of data binding lists and such.
		/// Not really intended to be used from code which knows better.</summary>
		public TranslationPair() {
			Phrase = string.Empty;
			Translation = string.Empty;
		}

		public int CompareTo(TranslationPair other) {
			return string.CompareOrdinal(Phrase, other.Phrase);
		}
	}
}