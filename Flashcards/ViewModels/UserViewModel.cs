using System.Collections.ObjectModel;
using System.Linq;
using System;

namespace Flashcards.ViewModels {
	public class UserViewModel : ViewModelBase {
		readonly MainViewModel mainViewModel;

		public UserViewModel() {
			Sets = new ObservableCollection<SetViewModel>();
			Groups = new ObservableCollection<GroupViewModel>();
		}

		public UserViewModel(MainViewModel mainViewModel, Model.UserInfo user) {
			this.mainViewModel = mainViewModel;

			userName = user.UserName;
			accountType = user.AccountType;
			profileImage = user.ProfileImage;
			signUpDate = user.SignUpDate;

			Sets = new ObservableCollection<SetViewModel>(from si in user.Sets select new SetViewModel(mainViewModel, si));
			Groups = new ObservableCollection<GroupViewModel>(from gi in user.Groups select new GroupViewModel(mainViewModel, gi));
		}

		#region Properties
		string userName;
		public string UserName {
			get { return userName; }
			set {
				SetProperty(ref userName, value, "UserName");
			}
		}

		string accountType;
		public string AccountType {
			get { return accountType; }
			set { SetProperty(ref accountType, value, "AccountType"); }
		}

		DateTime signUpDate;
		public DateTime SignUpDate {
			get { return signUpDate; }
			set { SetProperty(ref signUpDate, value, "SignUpDate"); }
		}

		Uri profileImage;
		public Uri ProfileImage {
			get { return profileImage; }
			set { SetProperty(ref profileImage, value, "ProfileImage"); }
		}

		ObservableCollection<SetViewModel> sets;
		public ObservableCollection<SetViewModel> Sets {
			get { return sets; }
			set { SetProperty(ref sets, value, "Sets"); }
		}

		ObservableCollection<GroupViewModel> groups;
		public ObservableCollection<GroupViewModel> Groups {
			get { return groups; }
			set { SetProperty(ref groups, value, "Groups"); }
		}

		// TODO: Stats
		#endregion
	}
}
