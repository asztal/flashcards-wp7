using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using Flashcards.ViewModels;
using System.IO.IsolatedStorage;

namespace Flashcards.Views {
	[NavigationUri("/Views/Practice.xaml")]
	public partial class Practice {
		PracticeViewModel session;
		bool isNewInstance;
		IList<SetViewModel> gameSets;

		public Practice() {
			InitializeComponent();

			session = new PracticeViewModel();

			isNewInstance = true;
			Loaded += OnLoaded;
		}

		void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
			if (!isNewInstance)
				return;

			isNewInstance = false;

			if (NavigationContext.QueryString.ContainsKey("resume")) {
				DataContext = session;
				gamePanel.Visibility = Visibility.Visible;
				return;
			}

			if (State.ContainsKey("Session")) {
				DataContext = session = RestoreJsonState<PracticeViewModel>("Session");
				gamePanel.Visibility = Visibility.Visible;
				return;
			}

			// TODO: Doesn't download terms from API if not cached
			var sets = ((from setID in this.GetIDListParameter<long>("sets", required: false)
						 select App.ViewModel.GetSet(setID, true))
				.Concat(from g in this.GetIDListParameter<long>("groups", required: false).Select(App.ViewModel.GetGroup)
						where g != null
						from set in g.Sets
						select set)).ToArray();

			foreach (var set in sets)
				set.LoadTerms();

			TryDownloadTerms(sets);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			if (NavigationContext.QueryString.ContainsKey("resume") || (isNewInstance && e.NavigationMode != NavigationMode.New)) {
				try {
					using (var storage = IsolatedStorageFile.GetUserStoreForApplication()) {
						using (var file = storage.OpenFile("Session", FileMode.Open, FileAccess.Read, FileShare.Read)) {
							using (var sr = new StreamReader(file)) {
								var json = JsonValue.Parse(sr);
								session = new JsonContext().FromJson<PracticeViewModel>(json);
							}
						}
					}
				} 
				catch (IsolatedStorageException) { } 
				catch (IOException) { } 
				catch (FormatException) {}
				return;
			}
			
			base.OnNavigatedTo(e);
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
			// If the session is still in progress, save it and we can resume it later.
			// TODO Save what the user has currently typed into the box

			try {
				using (var storage = IsolatedStorageFile.GetUserStoreForApplication()) {
					// Treat the back key as exiting and not saving
					if ((session != null && !session.IsFinished) && e.NavigationMode != NavigationMode.Back) {
						using (var file = storage.OpenFile("Session", FileMode.Create, FileAccess.Write, FileShare.None)) {
							using (var sw = new StreamWriter(file)) {
								var json = session.ToJson(new JsonContext());
								JsonValue.WriteMin(json, sw);
							}
						}
					} else {
						storage.DeleteFile("Session");
					}
				}
			} catch (IsolatedStorageException) {
				return;
			} catch (IOException) {
				return;
			} catch (FormatException) {
				return;
			}

			base.OnNavigatingFrom(e);
		}

		void TryDownloadTerms(IList<SetViewModel> sets) {
			var setsNeedingTerms = sets.Where(s => !s.TermsLoaded).ToArray();
			if (setsNeedingTerms.Length > 0) {
				this.SetProgressBar(string.Format("Fetching terms for {0}/{1} sets", setsNeedingTerms.Length, sets.Count));
				App.ViewModel.FetchSetTerms(setsNeedingTerms, () => TermsLoaded(sets), e => TermLoadingFailed(sets, e));
			} else {
				TermsLoaded(sets);
			}
		}

		void TermLoadingFailed(IList<SetViewModel> sets, Exception e) {
			this.HideProgressBar();
			
			int existingTermCount = sets.Sum(s => s.Terms.Count);
			var r =
				MessageBox.Show(
					string.Format(
						"{0} of the sets selected to practice had to be fetched from the internet, but this action failed. Press OK to retry downloading the terms from the internet.",
						existingTermCount == 0 ? "All" : "Some"), "Retry downloading terms?", MessageBoxButton.OKCancel);
			
			if (r == MessageBoxResult.Cancel && existingTermCount == 0) {
				NavigationService.GoBack();
				return;
			}

			if (r == MessageBoxResult.OK) {
				TryDownloadTerms(sets);
			} else {
				// Just use the terms we have.
				TermsLoaded(sets);
			}
		}

		void TermsLoaded(IEnumerable<SetViewModel> sets) {
			this.HideProgressBar();

			gameSets = sets.ToArray();
			settingsPanel.DataContext = session;
			settingsPanel.Visibility = Visibility.Visible;
			
			var availableLanguages = gameSets.SelectMany(s => new[] { s.TermLanguageCode, s.DefinitionLanguageCode }).Distinct().ToList();
			availableLanguages.Remove(null);

			var languages =
				(from lang in (LanguageCollection)App.Current.Resources["LanguageCodes"]
				 where availableLanguages.Contains(lang.Code)
				 let terms = CountTerms(gameSets, lang.Code)
				 orderby terms descending
				 select lang).ToList();
				
			languages.Insert(0, new LanguageViewModel { Code = "$term", Description = "Prompt with terms" });
			languages.Insert(1, new LanguageViewModel { Code = "$definition", Description = "Prompt with definitions" });
			promptWith.ItemsSource = languages;

			string defaultPromptLanguage = Configuration.PromptWithLanguage;
			if (defaultPromptLanguage != null) {
				int i = languages.IndexOf(lang => lang.Code == defaultPromptLanguage);
				if (i >= 0)
					promptWith.SelectedIndex = i; 
			}

			if (availableLanguages.Count > 0)
				promptWithPanel.Visibility = Visibility.Visible;

			SetSliderBounds();

			/*if (termCount <= 20)
				StartGame(this, new RoutedEventArgs());*/
		}

		int CountTerms(IEnumerable<SetViewModel> sets, string promptLanguage) {
			return gameSets
				.Where(set => promptLanguage == null 
				       || promptLanguage.StartsWith("$") 
					   || set.TermLanguageCode == promptLanguage 
					   || set.DefinitionLanguageCode == promptLanguage)
				.Sum(set => set.Terms.Count);
		}

		void SetSliderBounds() {
			string promptLanguage = promptWith.SelectedItem is LanguageViewModel ? (promptWith.SelectedItem as LanguageViewModel).Code : null;

			var termCount = CountTerms(gameSets, promptLanguage);
			slider.Maximum = termCount;
			slider.Minimum = Math.Min(termCount, 10);
		}

		private void PromptLanguageChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
			SetSliderBounds();
		}

		private void StartGame(object sender, RoutedEventArgs e) {
			settingsPanel.Visibility = Visibility.Collapsed;
			gamePanel.Visibility = Visibility.Visible;

			string promptLanguage = null;
			if (promptWith.SelectedItem is LanguageViewModel)
				promptLanguage = (promptWith.SelectedItem as LanguageViewModel).Code;

			session.StartGame(gameSets, promptLanguage);
			DataContext = session;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e) {
			if (session != null)
				SaveJsonState("Session", session);
			else
				State.Remove("Session");
		}

		void EndSession(object sender, EventArgs e) {
			session = null;
			NavigationService.GoBack();
		}
	}
}