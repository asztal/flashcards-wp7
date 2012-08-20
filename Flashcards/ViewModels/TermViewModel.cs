using System.Linq;
using System;
using System.ComponentModel;

namespace Flashcards.ViewModels {
	public class TermViewModel : INotifyPropertyChanged {
		public TermViewModel(Model.TermInfo ti) {
			ID = ti.ID;
			Term = ti.Term;
			Definition = ti.Definition;
			if (ti.Image != null)
				Image = new Uri(ti.Image);
		}

		public TermViewModel() {
			Term = "";
			Definition = "";
		}
	
		long id;
		public long ID {
			get { return id; }
			set {
				if (value == id) return;
				
				id = value;
				OnPropertyChanged("TermID");
			}
		}

		string term;
		public string Term {
			get { return term; }
			set {
				if (value == term) return;
				
				term = value;
				OnPropertyChanged("Term");
			}
		}

		string definition;
		public string Definition {
			get { return definition; }
			set {
				if (value == definition) return;
				
				definition = value;
				OnPropertyChanged("Definition");
			}
		}

		Uri image;
		public Uri Image {
			get { return image; }
			set {
				if (value == image) return;
				
				image = value;
				OnPropertyChanged("Image");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string propertyName) {
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}