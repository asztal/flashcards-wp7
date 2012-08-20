using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Flashcards.Model;

namespace Flashcards.ViewModels {
	public class GroupViewModel : INotifyPropertyChanged {
		readonly MainViewModel mainViewModel;

		public GroupViewModel() {
			Sets = new ObservableCollection<SetViewModel>();
		}

		public GroupViewModel(MainViewModel mainViewModel, GroupInfo group) {
			this.mainViewModel = mainViewModel;
	
			ID = group.ID;
			Name = group.Name;
			Description = group.Description;
			Created = group.Created;
			IsPublic = group.IsPublic;
			HasPassword = group.HasPassword;
			HasAccess = group.HasAccess;
			HasDiscussion = group.HasDiscussion;
			MemberAddSets = group.MemberAddSets;
			
			Sets = @group.Sets != null 
				? new ObservableCollection<SetViewModel>(from si in @group.Sets select mainViewModel.GetSet(si.ID, false)) 
				: new ObservableCollection<SetViewModel>();
		}

		public void Update(GroupInfo group) {
			if (group.ID != ID)
				throw new InvalidOperationException("Cannot update Group view model with data from another group");

			Name = group.Name;
			Description = group.Description;
			Created = group.Created;
			IsPublic = group.IsPublic;
			HasPassword = group.HasPassword;
			HasAccess = group.HasAccess;
			HasDiscussion = group.HasDiscussion;
			MemberAddSets = group.MemberAddSets;

			UpdateSets(group.Sets);
		}

		void UpdateSets(ICollection<SetInfo> newSets) {
			for (int i = 0; i < Sets.Count; i++)
				if (!newSets.Any(si => si.ID == Sets[i].ID))
					Sets.RemoveAt(i);

			foreach (var newSet in newSets)
				if (!Sets.Any(si => si.ID == newSet.ID))
					Sets.Add(new SetViewModel(mainViewModel, newSet));
		}

		#region Properties
		ObservableCollection<SetViewModel> sets;
		public ObservableCollection<SetViewModel> Sets {
			get { return sets; }
			set {
				if (sets == value)
					return;
				
				sets = value;
				OnPropertyChanged("Sets");
			}
		}

		long id;
		public long ID {
			get { return id; }
			set {
				if (id == value)
					return;

				id = value;
				OnPropertyChanged("ID");
			}
		}


		string name;
		public string Name {
			get { return name; }
			set {
				if (name == value)
					return;

				name = value;
				OnPropertyChanged("Name");
			}
		}

		string description;
		public string Description {
			get { return description; }
			set {
				if (description == value)
					return;

				description = value;
				OnPropertyChanged("Description");
			}
		}


		int userCount;
		public int UserCount {
			get { return userCount; }
			set {
				if (userCount == value)
					return;

				userCount = value;
				OnPropertyChanged("UserCount");
			}
		}

		DateTime created;
		public DateTime Created {
			get { return created; }
			set {
				if (created == value)
					return;

				created = value;
				OnPropertyChanged("Created");
			}
		}

		bool isPublic;
		public bool IsPublic {
			get { return isPublic; }
			set {
				if (isPublic == value)
					return;

				isPublic = value;
				OnPropertyChanged("IsPublic");
			}
		}

		bool hasPassword;
		public bool HasPassword {
			get { return hasPassword; }
			set {
				if (hasPassword == value)
					return;

				hasPassword = value;
				OnPropertyChanged("HasPassword");
			}
		}

		bool hasAccess;
		public bool HasAccess {
			get { return hasAccess; }
			set {
				if (hasAccess == value)
					return;

				hasAccess = value;
				OnPropertyChanged("HasAccess");
			}
		}

		bool hasDiscussion;
		public bool HasDiscussion {
			get { return hasDiscussion; }
			set {
				if (hasDiscussion == value)
					return;

				hasDiscussion = value;
				OnPropertyChanged("HasDiscussion");
			}
		}

		bool memberAddSets;
		public bool MemberAddSets {
			get { return memberAddSets; }
			set {
				if (memberAddSets == value)
					return;

				memberAddSets = value;
				OnPropertyChanged("MemberAddSets");
			}
		}
		#endregion

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged(string propertyName) {
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
