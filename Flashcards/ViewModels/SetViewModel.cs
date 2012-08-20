using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Flashcards.Model;
using Flashcards.Model.API;

namespace Flashcards.ViewModels
{
	public class SetViewModel : ViewModelBase {
		#region Properties
		string title;
		public string Title {
			get { return title; }
			set { SetProperty(ref title, value, "Title"); }
		}

		string author;
		public string Author {
			get { return author; }
			set { SetProperty(ref author, value, "Author"); }
		}

		string description;
		public string Description {
			get { return description; }
			set { SetProperty(ref description, value, "Description"); }
		}

		DateTime created;
		public DateTime Created {
			get { return created; }
			set { SetProperty(ref created, value, "Created"); }
		}

		DateTime modified;
		public DateTime Modified {
			get { return modified; }
			set { SetProperty(ref modified, value, "Modified"); }
		}

		long id;
		public long ID {
			get { return id; }
			internal set { SetProperty(ref id, value, "ID"); }
		}

		Uri uri;
		public Uri Uri {
			get { return uri; }
			set { SetProperty(ref uri, value, "Uri"); }
		}

		int termCount;
		public int TermCount {
			get { return termCount; }
			set { SetProperty(ref termCount, value, "TermCount"); }
		}

		ObservableCollection<SubjectViewModel> subjects;
		public ObservableCollection<SubjectViewModel> Subjects {
			get { return subjects; }
			set { SetProperty(ref subjects, value, "Subjects"); }
		}

		ObservableCollection<TermViewModel> terms;
		public ObservableCollection<TermViewModel> Terms {
			get { return terms; }
			set { SetProperty(ref terms, value, "Terms"); }
		}

		string termLanguage;
		public string TermLanguageCode {
			get { return termLanguage; }
			set { SetProperty(ref termLanguage, value, "TermLanguageCode"); }
		}

		string definitionLanguage;
		public string DefinitionLanguageCode {
			get { return definitionLanguage; }
			set { SetProperty(ref definitionLanguage, value, "DefinitionLanguageCode"); }
		}

		bool starred;
		public bool Starred {
			get { return starred; }
			set { SetProperty(ref starred, value, "Starred"); }
		}

		bool hasAccess;
		public bool HasAccess {
			get { return hasAccess; }
			set { SetProperty(ref hasAccess, value, "HasAccess"); }
		}

		bool hasDiscussion;
		public bool HasDiscussion {
			get { return hasDiscussion; }
			set { SetProperty(ref hasDiscussion, value, "HasDiscussion"); }
		}

		SetEditPermissions editPermissions;
		public SetEditPermissions EditPermissions {
			get { return editPermissions; }
			set { SetProperty(ref editPermissions, value, "EditPermissions"); }
		}

		SetVisibility visibility;
		public SetVisibility Visibility {
			get { return visibility; }
			set { SetProperty(ref visibility, value, "Visibility"); }
		}

		public bool IsEditable {
			get { 
				if (mainViewModel == null)
					throw new InvalidOperationException("Cannot determine if group is editable when it is not linked to a View Model");

				switch (editPermissions) {
					case SetEditPermissions.OnlyMe:
						return Author == mainViewModel.UserName;
					case SetEditPermissions.Groups:
						// This could result in false positives, since the groups it is editable by could be any group of groups at all,
						// and there is no telling which groups can edit it...
						return mainViewModel.Groups.Any(g => g.Sets.Any(s => s.ID == ID));
					default:
						// Assume the user knows the password
						return true;
				}
			}
		}

		public bool TermsLoaded { get; set; }
		#endregion

		readonly MainViewModel mainViewModel;

		public SetViewModel() {
			Subjects = new ObservableCollection<SubjectViewModel>();
			Terms = new ObservableCollection<TermViewModel>();
		}

		public SetViewModel(MainViewModel mainViewModel) : this() {
			this.mainViewModel = mainViewModel;
		}

		public SetViewModel(MainViewModel mainViewModel, SetInfo si) : this(mainViewModel) {
			ID = si.ID;
			Update(si);
		}

		public void Update(SetInfo si) {
			if (ID != si.ID)
				throw new InvalidOperationException("Cannot update Set view model with data from another group");

			Title = si.Title;
			Author = si.Author;
			Created = si.Created;
			Modified = si.Modified;

			for (int i = 0; i < Subjects.Count; ++i)
				if (!si.Subjects.Contains(Subjects[i].Subject))
					Subjects.RemoveAt(i);

			foreach (var subject in si.Subjects)
				if (Subjects.All(svm => svm.Subject != subject))
					Subjects.Add(new SubjectViewModel(subject));

			switch (si.Editable) {
				case "groups": editPermissions = SetEditPermissions.Groups; break;
				case "password": editPermissions = SetEditPermissions.Password; break;
				default: editPermissions = SetEditPermissions.OnlyMe; break;
			}

			switch (si.Visibility) {
				case "public": visibility = SetVisibility.Public; break;
				case "groups": visibility = SetVisibility.Groups; break;
				case "password": visibility = SetVisibility.Password; break;
				default: visibility = SetVisibility.OnlyMe; break;
			}

			HasAccess = si.HasAccess;
			hasDiscussion = si.HasDiscussion;
			Description = si.Description;
			Uri = si.Uri;

			if (si.Terms != null) {
				TermsLoaded = true;

				Terms.Clear();
				foreach (var ti in (IEnumerable<TermInfo>)si.Terms)
					Terms.Add(new TermViewModel(ti));

				TermCount = Terms.Count;
			} else {
				TermCount = si.TermCount;
			}

			TermLanguageCode = si.TermLanguageCode;
			DefinitionLanguageCode = si.DefinitionLanguageCode;
		}

		public SetInfo ToSetInfo() {
			TermCount = terms.Count;

			var si = new SetInfo {
				Author = Author,
				Created = Created,
				DefinitionLanguageCode = DefinitionLanguageCode,
				Description = Description,
				Editable = EditPermissions.ToApiString(),
				HasAccess = HasAccess,
				ID = ID,
				Modified = Modified,
				Subjects = Subjects.Select(svm => svm.Subject).ToList(),
				TermCount = TermCount,
				TermLanguageCode = TermLanguageCode,
				Terms = TermsLoaded ? new List<TermInfo>() : null,
				Title = Title,
				Uri = Uri,
				Visibility = Visibility.ToApiString()
			};

			if(TermsLoaded)
				foreach (var t in Terms)
					si.Terms.Add(new TermInfo {Term = t.Term, Definition = t.Definition});

			return si;
		}

		public void SetStarred(bool newStarred, Action completed, Action<Exception> errorHandler) {
			if (mainViewModel == null)
				throw new InvalidOperationException("Cannot add or remove from favourites when it is not linked to a main view model");

			mainViewModel.SetStarred(this, newStarred, completed, errorHandler);
		}

		public void FetchTerms(Action completed, Action<Exception> errorHandler) {
			if (mainViewModel == null)
				throw new InvalidOperationException("Cannot fetch terms for a group that is not linked to a main view model");

			mainViewModel.FetchSetTerms(this, completed, errorHandler);
		}

		public void SubmitPassword(string password, Action success, Action<Exception> failure) {
			mainViewModel.API.SubmitPassword(
				ID, 
				password, 
				() => {
					HasAccess = true;
					success();
				},
				failure,
				new CancellationToken());
		}

		public void CommitEdits(string newPassword, SetVisibility? newVisibility, SetEditPermissions? newEditable, IEnumerable<long> groupIDs, Action completed, Action<Exception> errorHandler) {
			mainViewModel.CommitEdits(this, newPassword, newVisibility, newEditable, groupIDs,
				() => {
					if (newVisibility.HasValue)
						Visibility = newVisibility.Value;
					if (newEditable.HasValue)
						EditPermissions = newEditable.Value;
					completed();
				}, errorHandler);
		}

		public void LoadTerms() {
			mainViewModel.LoadTerms(this);
		}
	}
}