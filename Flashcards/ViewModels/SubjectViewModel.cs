using System.Linq;
using System;
using System.ComponentModel;

namespace Flashcards.ViewModels {
	public class SubjectViewModel : INotifyPropertyChanged {
		string subject;
		public string Subject {
			get { return subject; }
			set {
				if (value == subject) return;
				
				subject = value;
				OnPropertyChanged("Value");
			}
		}

		public SubjectViewModel() {
			Subject = string.Empty;
		}

		public SubjectViewModel(string subject) {
			this.subject = subject;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName) {
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
