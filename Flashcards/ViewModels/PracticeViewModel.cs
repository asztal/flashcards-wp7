using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Flashcards.ViewModels {
	public class PracticeViewModel : INotifyPropertyChanged, IJsonConvertible {
		List<PracticeItem> items;
		List<PracticeItem> nextItems;

		bool failed;

		public PracticeViewModel() { 
			allRounds = new ObservableCollection<RoundInfo>();
			maxItemCount = 20;
		}

		void UpdateItem() {
			CurrentItem = items != null && currentIndex < items.Count 
				? items[currentIndex] 
				: null;
		}

		public void CompleteAttempt(bool success) {
			if (items == null || nextItems == null)
				throw new InvalidOperationException();

			if (success && !failed)
				Score++;

			if (success)
				NextItem();
			else 
				failed = true;
		}

		public bool SubmitAnswer(string answer) {
			if (currentItem == null)
				throw new InvalidOperationException();

			bool correct = new AnswerChecker().IsAcceptable(answer, CurrentItem.Definition);
			CompleteAttempt(correct);
			return correct;
		}

		public void OverrideWrongAnswer() {
			failed = false;
			CompleteAttempt(true);
		}

		void NextItem() {
			if (items == null)
				throw new InvalidOperationException();

			if (failed)
				nextItems.Add(CurrentItem);

			failed = false;

			CurrentIndex++;
			UpdateItem();
			
			if (currentIndex < ItemCount)
				return;
			
			if (nextItems.Count > 0)
				OnFinishedRound();
			else 
				OnFinished();
		}

		public void StartGame(IEnumerable<SetViewModel> sets, string promptLanguage = null) {
			if (sets == null)
				return;

			// Don't enumerate it twice.
			var setArray = sets.ToArray();

			// Don't take too many group titles.
			string newTitle = string.Join(", ", setArray.Take(3).Select(s => s.Title));
			if (setArray.Length > 3)
				newTitle += ", ...";
			if (string.IsNullOrWhiteSpace(newTitle))
				newTitle = "Practice";
			Title = newTitle;

			nextItems = (from set in setArray
			             from term in set.Terms
			             select MakePracticeItem(promptLanguage, set, term)).Where(x => x != null).ToList();
			round = -1;

			AllItems = nextItems.ToList();

			if (maxItemCount < nextItems.Count) {
				nextItems.Shuffle();
				nextItems.RemoveRange(maxItemCount, nextItems.Count - maxItemCount);
			}

			StartRound();
		}

		private PracticeItem MakePracticeItem(string promptLanguage, SetViewModel set, TermViewModel term) {
			if (set.DefinitionLanguageCode == promptLanguage || promptLanguage == "$definition")
				return new PracticeItem(term.Definition, term.Term);
			else if (set.TermLanguageCode == promptLanguage || promptLanguage == "$term" || promptLanguage == null)
				return new PracticeItem(term.Term, term.Definition);
			return null;
		}

		public void StartRound() {
			if (nextItems.Count == 0) {
				OnFinished();
				return;
			}

			failed = false;

			items = nextItems;
			nextItems = new List<PracticeItem>();

			items.Shuffle();

			Round++;
			Score = 0;
			CurrentIndex = 0;
			ItemCount = items.Count;

			UpdateItem();
		}

		public int Count {
			get { return items.Count; }
		}

		public string FormattedScore {
			get {
				return currentIndex < ItemCount 
					? string.Format("Score: {0}/{1} correct, {2} terms left", score, currentIndex, ItemCount - currentIndex) 
					: string.Format("Score: {0}/{1} correct", score, currentIndex);
			}
		}

		#region Events
		void AddRoundInfo() {
			allRounds.Add(LastRoundInfo = new RoundInfo(allRounds.Count + 1, Score, ItemCount));
		}

		public event RoutedEventHandler FinishedRound;
		protected void OnFinishedRound() {
			AddRoundInfo();

			var h = FinishedRound;
			if (h != null)
				h(this, new RoutedEventArgs());
		}

		public event RoutedEventHandler Finished;
		protected void OnFinished() {
			AddRoundInfo();

			var h = Finished;
			if (h != null)
				h(this, new RoutedEventArgs());
		}
		#endregion

		#region Properties
		List<PracticeItem> allItems;
		public List<PracticeItem> AllItems {
			get { return allItems; }
			protected set {
				if (allItems == value)
					return;

				allItems = value;
				OnPropertyChanged("AllItems");
			}
		}
			
		PracticeItem currentItem;
		public PracticeItem CurrentItem {
			get { return currentItem; }
			protected set {
				if (value == currentItem)
					return;
				currentItem = value;
				OnPropertyChanged("CurrentItem");
			}
		}

		int currentIndex;
		public int CurrentIndex {
			get { return currentIndex; }
			protected set {
				if (currentIndex == value)
					return;

				currentIndex = value;
				OnPropertyChanged("CurrentIndex");
				OnPropertyChanged("FormattedScore");
			}
		}

		int itemCount;
		public int ItemCount {
			get { return itemCount; }
			protected set {
				if (itemCount == value)
					return;

				itemCount = value;
				OnPropertyChanged("ItemCount");
				OnPropertyChanged("FormattedScore");
			}
		}

		int score;
		public int Score {
			get { return score; }
			protected set {
				if (score == value)
					return;

				score = value;
				OnPropertyChanged("Score");
				OnPropertyChanged("FormattedScore");
			}
		}

		int round;
		public int Round {
			get { return round; }
			protected set {
				if (value == round)
					return;

				round = value;
				OnPropertyChanged("Round");
			}
		}

		RoundInfo lastRoundInfo;
		public RoundInfo LastRoundInfo {
			get { return lastRoundInfo; }
			protected set {
				lastRoundInfo = value;
				OnPropertyChanged("LastRoundInfo");
			}
		}

		int maxItemCount;
		public int MaxItemCount {
			get { return maxItemCount; }
			set {
				if (maxItemCount == value)
					return;

				maxItemCount = value;
				OnPropertyChanged("MaxItemCount");
			}
		}


		public bool IsFinished {
			get {
				return currentIndex >= ItemCount && (nextItems == null || nextItems.Count == 0);
			}
		}

		ObservableCollection<RoundInfo> allRounds;
		public ObservableCollection<RoundInfo> AllRounds {
			get { return allRounds; }
			protected set {
				if (allRounds == value)
					return;

				allRounds = value;
				OnPropertyChanged("allRounds");
			}
		}

		string title = "Practice";
		public string Title {
			get { return title; }
			set {
				if (title == value)
					return;

				title = value;
				OnPropertyChanged("Title");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string name) {
			var h = PropertyChanged;
			if (h != null)
				h(this, new PropertyChangedEventArgs(name));
		}
		#endregion

		#region Serialization
		public JsonValue ToJson(IJsonContext ctx) {
			return new JsonDictionary()
				.Set("items", ctx.ToJson(items))
				.Set("nextItems", ctx.ToJson(nextItems))
				.Set("failed", new JsonBool(failed))
				.Set("currentItem", ctx.ToJson(CurrentItem))
				.Set("currentIndex", new JsonNumber(CurrentIndex))
				.Set("itemCount", new JsonNumber(ItemCount))
				.Set("score", new JsonNumber(Score))
				.Set("round", new JsonNumber(Round))
				.Set("lastRoundInfo", ctx.ToJson(LastRoundInfo))
				.Set("allRounds", ctx.ToJson(AllRounds.ToList()))
				.Set("title", new JsonString(Title))
				.Set("maxItemCount", new JsonNumber(maxItemCount));
		}

		public PracticeViewModel(JsonValue json, IJsonContext ctx) {
			var dict = JsonDictionary.FromValue(json);
			items = ctx.FromJson<List<PracticeItem>>(dict["items"]);
			nextItems = ctx.FromJson<List<PracticeItem>>(dict["nextItems"]);
			failed = ctx.FromJson<bool>(dict["failed"]);
			currentItem = ctx.FromJson<PracticeItem>(dict["currentItem"]);
			currentIndex = ctx.FromJson<int>(dict["currentIndex"]);
			itemCount = ctx.FromJson<int>(dict["itemCount"]);
			score = ctx.FromJson<int>(dict["score"]);
			round = ctx.FromJson<int>(dict["round"]);
			lastRoundInfo = ctx.FromJson<RoundInfo>(dict["lastRoundInfo"]);
			allRounds = new ObservableCollection<RoundInfo>(ctx.FromJson<List<RoundInfo>>(dict["allRounds"]));
			maxItemCount = ctx.FromJson<int>(dict["maxItemCount"]);
			title = ctx.FromJson<string>(dict["title"]);
		}
		#endregion
	}

	public class RoundInfo : IJsonConvertible {
		public RoundInfo(int roundNumber, int score, int itemCount) {
			RoundNumber = roundNumber;
			Score = score;
			ItemCount = itemCount;
		}

		public int RoundNumber { get; protected set; }
		public int Score { get; protected set; }
		public int ItemCount { get; protected set; }

		public JsonValue ToJson(IJsonContext context) {
			return new JsonDictionary()
				.Set("roundNumber", new JsonNumber(RoundNumber))
				.Set("score", new JsonNumber(Score))
				.Set("itemCount", new JsonNumber(ItemCount));
		}

		public RoundInfo(JsonValue json, IJsonContext ctx) {
			var dict = JsonDictionary.FromValue(json);
			RoundNumber = ctx.FromJson<int>(dict["roundNumber"]);
			Score = ctx.FromJson<int>(dict["score"]);
			ItemCount = ctx.FromJson<int>(dict["itemCount"]);
		}
	}

	class AnswerChecker {
		readonly bool fixSpaces, fixPunct, fixParens, fixCase;

		public AnswerChecker() {
			fixSpaces = true;
			fixPunct = true;
			fixParens = true;
			fixCase = true;
		}

		public bool IsAcceptable(string guess, string answer) {
			guess = Fix(guess);

			if (guess == Fix(answer))
				return true;

			return answer.Split(';').Any(s => guess == Fix(s)) || answer.Split(',').Any(s => guess == Fix(s));
		}

		// TODO: Possible enhancements: regex-based or word-based replacements in given answer, such as:
		//       sg -> something
		//       vkit -> valakit
		string Fix(string s) {
			var sb = new System.Text.StringBuilder();

			bool wasSpace = false;
			int parens = 0;

			// Normalization is impossible. I guess we'll have to hope no issues occur.
			foreach (var ch in s.Trim()) {
				char c = ch;
				bool add = true;

				if (fixCase)
					c = char.ToLowerInvariant(c);

				if (fixParens) {
					if (c == '(' || c == '[')
						parens++;
					else if (c == ')' || c == ']')
						parens--;
				}

				if (parens < 0)
					parens = 0;

				if (fixParens && parens > 0)
					add = false;
				else if (fixPunct && char.IsPunctuation(c))
					add = false;
				else if (fixSpaces) {
					bool isSpace = char.IsWhiteSpace(c);
					if (wasSpace) {
						if (isSpace)
							add = false;
					}

					// This only occurs if c wasn't a punctuation mark -- thus, " ... " will be collapsed into " ".
					wasSpace = isSpace;
				}

				if (add)
					sb.Append(c);
			}

			return sb.ToString().Trim();
		}
	}

	// Practice items are immutable, for now.
	public class PracticeItem : IJsonConvertible {
		readonly string term;
		readonly string definition;

		public PracticeItem(string term, string definition) {
			this.term = term;
			this.definition = definition;
		}

		public string Term {
			get { return term; }
		}

		public string Definition {
			get { return definition; }
		}

		public JsonValue ToJson(IJsonContext context) {
			return new JsonDictionary()
				.Set("term", term)
				.Set("definition", definition);
		}

		public PracticeItem(JsonValue json, IJsonContext ctx) {
			var dict = JsonDictionary.FromValue(json);
			term = ctx.FromJson<string>(dict["term"]);
			definition  = ctx.FromJson<string>(dict["definition"]);
		}
	}
}
