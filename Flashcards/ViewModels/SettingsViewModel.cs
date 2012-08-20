using System.Linq;
using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace Flashcards.ViewModels {
	public class SettingsViewModel : INotifyPropertyChanged, IWeakEventListener<IConfiguration, SettingChangedEventArgs> {
		public SettingsViewModel() {
			if (Configuration.Default != null)
			    Configuration.Default.SettingChangedEventManager.AddListener(this);
		}

		public void ReceiveWeakEvent(IConfiguration sender, SettingChangedEventArgs args) {
			OnPropertyChanged(args.SettingName);
		}

		public bool ResumeSessionOnOpen {
			get { return Configuration.ResumeSessionOnOpen; }
			set { Configuration.ResumeSessionOnOpen = value; }
		}

		public bool VerifyCertificates {
			get { return Configuration.VerifyCertificates; }
			set { Configuration.VerifyCertificates = value; }
		}

		public bool SyncOver3G {
			get { return Configuration.SyncOver3G; }
			set { Configuration.SyncOver3G = value; }
		}

		public bool SyncOnOpen {
			get { return Configuration.SyncOnOpen; }
			set { Configuration.SyncOnOpen = value; }
		}

		public List<string> SearchHistory {
			get { return Configuration.SearchHistory ?? new List<string>(); }
			set { Configuration.SearchHistory = value; }
		}

		public string DefaultTermLanguageCode {
			get { return Configuration.DefaultTermLanguageCode; }
			set { Configuration.DefaultTermLanguageCode = value; }
		}

		public string DefaultDefinitionLanguageCode {
			get { return Configuration.DefaultDefinitionLanguageCode; }
			set { Configuration.DefaultDefinitionLanguageCode = value; }
		}

		public string PromptWithLanguage {
			get { return Configuration.PromptWithLanguage; }
			set { Configuration.PromptWithLanguage = value; }
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string propertyName) {
			var handler = PropertyChanged;
			if (handler != null) {
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
