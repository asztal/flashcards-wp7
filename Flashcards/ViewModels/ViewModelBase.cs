using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace Flashcards.ViewModels {
	public abstract class ViewModelBase : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		public virtual void Dispose(bool disposing) { }

		public void Dispose() {
			Dispose(true);
		}

		protected void SetProperty<T>(ref T value, T newValue, string propertyName) {
			SetProperty(ref value, newValue, propertyName, EqualityComparer<T>.Default);
		}

		protected void SetProperty<T>(ref T value, T newValue, string propertyName, EqualityComparer<T> eqc) {
			if (eqc.Equals(value, newValue))
				return;

			value = newValue;
			OnPropertyChanged(propertyName);
		}

		protected void OnPropertyChanged(string name) {
			var h = PropertyChanged;
			if (h != null)
				h(this, new PropertyChangedEventArgs(name));
		}
	}
}