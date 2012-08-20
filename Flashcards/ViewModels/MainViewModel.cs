using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using Flashcards.Model;
using Flashcards.Model.API;

namespace Flashcards.ViewModels
{
	public class MainViewModel : INotifyPropertyChanged
	{
		ModelCache cache;
		readonly QuizletAPI api;

		public MainViewModel()
		{
			MySets = new ObservableCollection<SetViewModel>();
			FavouriteSets = new ObservableCollection<SetViewModel>();
			RecentSets = new ObservableCollection<SetViewModel>();
			Groups = new ObservableCollection<GroupViewModel>();

			using (var storage = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication())
				if (storage.FileExists("cache.sdf"))
					storage.DeleteFile("cache.sdf");

			api = QuizletAPI.Default;
		}

		public bool LoadCachedToken() {
			try {
				cache = new ModelCache();

				return cache.Credentials != null && api.Authenticate(cache.Credentials);
			} catch (CacheException) {
				return false;
			}
		}

		public void LoadData() {
			if (IsDataLoaded)
				return;

			try {
				Metrics.Measure("MainViewModel.LoadData()", delegate {
					Metrics.Measure("Load ModelCache", delegate {
						cache = api.Credentials != null
							? new ModelCache(api.Credentials)
							: new ModelCache();
					});

					if (cache.Credentials == null)
						return;

					if (!api.Authenticate(cache.Credentials)) {
						cache.LogOut();
						LogOut();
						return;
					}

					UserName = cache.Credentials.UserName;

					Metrics.Measure("Create group view models", delegate {
						foreach (var si in cache.GetMySets())
							MySets.Add(new SetViewModel(this, si));

						foreach (var si in cache.GetRecentSets().Take(8))
							RecentSets.Add(new SetViewModel(this, si));

						foreach (var si in cache.GetFavourites())
							FavouriteSets.Add(new SetViewModel(this, si) { Starred = true });
					});

					Metrics.Measure("Create group view models", delegate {
						foreach (var gi in cache.GetGroups())
							Groups.Add(new GroupViewModel(this, gi));
					});

					Metrics.Measure("Load profile image", LoadProfileImage);
					
					IsDataLoaded = true;
				});
			} catch (CacheIndexMissingException) {
				// XXX This will never happen
			}
		}

		internal QuizletAPI API {
			get { return api; }
		}

		#region Sync
		private class SyncOperation {
			readonly Action<SyncOperation> completed;
			readonly Action<Exception> errorHandler;
			readonly Action<int, int> progressChanged;

			bool finished;
			Exception exception;
			List<SetInfo> mySets, favourites;
			List<GroupInfo> groups; 
			bool profileImageLoaded;

			int progress;

			public SyncOperation(Action<SyncOperation> completed, Action<int, int> progressChanged, Action<Exception> errorHandler) {
				this.completed = completed;
				this.progressChanged = progressChanged;
				this.errorHandler = errorHandler;
			}

			public void Failed(Exception error) {
				if (exception != null)
					// Don't signal more than one error. Just pick the first one that happens.
					return;

				exception = error;
				errorHandler(error);
			}

			public List<SetInfo> MySets {
				get { return mySets; }
				set {
					if (exception != null)
						return;

					mySets = value;

					CheckForCompletion();
				}
			}

			public List<SetInfo> Favourites {
				get { return favourites; }
				set {
					if (exception != null)
						return;

					favourites = value;

					CheckForCompletion();
				}
			}

			public List<GroupInfo> Groups {
				get { return groups; }
				set { 
					if (exception != null)
						return;

					groups = value;

					CheckForCompletion();
				}
			}

			public bool ProfileImageLoaded {
				set {
					if (exception != null)
						return;

					profileImageLoaded = value;

					CheckForCompletion();
				}
			}

			private void CheckForCompletion() {
				if (finished)
					return;

				if (mySets == null || favourites == null || groups == null || !profileImageLoaded) {
					progress++;
					if (progressChanged != null)
						progressChanged(progress, 4);
					return;
				}

				finished = true;
				completed(this);
			}
		}

		public void Synchronize(Action completion, Action<string, double> progressChanged, Action<Exception> errorHandler) {
			if (IsSynchronizing)
				return;

			if (cache.Credentials == null)
				throw new InvalidOperationException("Cannot update the cache when not logged in");

			IsSynchronizing = true;
			SyncStatus = "Synchronizing...";

			var operation = new SyncOperation(
				op => {
					cache.UpdateMySets(op.MySets);
					UpdateSets(MySets, op.MySets);

					cache.UpdateFavourites(op.Favourites);
					UpdateSets(FavouriteSets, op.Favourites);

					// Because the new sets won't have this property group to true.
					foreach (var set in FavouriteSets)
						set.Starred = true;

					cache.Update(op.Groups);
					UpdateGroups(op.Groups);

					LoadProfileImage();

					IsSynchronizing = false;
					HasSynchronized = true;

					cache.Write();
					completion();
				},
				(n, total) => {
					if (progressChanged != null)
						progressChanged(string.Format("Synchronizing ({0}/{1})...", n, total), n/(double)total);
				},
				e => {
					IsSynchronizing = false;
					errorHandler(e);
				});

			// Fetch the four data sets in parallel.
			api.FetchUserSets(userName, sets => operation.MySets = sets, operation.Failed, new CancellationToken());
			api.FetchUserFavourites(userName, sets => operation.Favourites = sets, operation.Failed, new CancellationToken());
			api.FetchUserGroups(userName, newGroups => operation.Groups = newGroups, operation.Failed, new CancellationToken());
			api.FetchUserData(
				userName,
				uri => {
					if (uri == null) {
						cache.DeleteProfileImage();
						operation.ProfileImageLoaded = true;
						return;
					}

					if (uri == cache.ProfileImage) {
						operation.ProfileImageLoaded = true;
						return;
					}

					cache.ProfileImage = uri;
					cache.FetchProfileImage(e => {
						if (e != null)
							operation.Failed(e);
						else
							operation.ProfileImageLoaded = true;
					});
				},
				operation.Failed,
				new CancellationToken());
		}

		void LoadProfileImage() {
			var storage = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication();

			const string fileName = "ProfileImage.jpg";
			if (!storage.FileExists(fileName))
				return;
			try {
				using (var file = storage.OpenFile(fileName, System.IO.FileMode.Open)) {
					var bm = new BitmapImage();
					bm.SetSource(file);
					ProfileImage = bm;
				}
				return;
			} catch (System.IO.IOException) {
			} catch (System.IO.IsolatedStorage.IsolatedStorageException) {
			}
			ProfileImage = null;
		}

		void UpdateSets(IList<SetViewModel> viewModels, List<SetInfo> updates) {
			for (int i = 0; i < viewModels.Count; ++i) {
				var newSet = updates.Where(x => x.ID == viewModels[i].ID).ToArray();
				if (newSet.Length == 0)
					viewModels.RemoveAt(i);
				else if (newSet[0].Modified > viewModels[i].Modified)
					viewModels[i].Update(newSet[0]);
			}

			foreach (var update in updates)
				if (viewModels.All(x => x.ID != update.ID))
					viewModels.Add(new SetViewModel(this, update));
		}

		void UpdateGroups(List<GroupInfo> updates) {
			for (int i = 0; i < Groups.Count; ++i) {
				var newGroup = updates.Where(x => x.ID == Groups[i].ID).ToArray();
				if (newGroup.Length == 0)
					Groups.RemoveAt(i);
				else
					Groups[i].Update(newGroup[0]);
			}

			foreach (var update in updates)
				if (Groups.All(x => x.ID != update.ID))
					Groups.Add(new GroupViewModel(this, update));
		}
		#endregion

		public void LogIn(Credentials credentials) {
			if (api.Credentials != credentials && !api.Authenticate(credentials))
				throw new ArgumentException("Invalid credentials provided.");

			isDataLoaded = false;
			LoadData();
		}

		public void LogOut() {
			if (cache != null)
				cache.LogOut();
			api.LogOut();
			cache = null;
			IsDataLoaded = false;

			MySets = new ObservableCollection<SetViewModel>();
			FavouriteSets = new ObservableCollection<SetViewModel>();
			RecentSets = new ObservableCollection<SetViewModel>();
			Groups = new ObservableCollection<GroupViewModel>();
		}

		public void SaveData() {
			// App could be deactivated before the main page is loaded.
			if (cache != null)
				cache.Write();
		}

		#region Sets
		public void Search(bool searchTerms, string query, Action<List<SetViewModel>> completed, Action<Exception> failure) {
			api.SearchSets(
				query,
				searchTerms,
				model => completed((from si in model select new SetViewModel(this, si)).ToList()),
				failure,
				new CancellationToken());
		}

		public void RegisterSetInfo(SetViewModel set) {
			if (cache == null)
				return;

			cache.AddSetInfo(set.ToSetInfo());
		}

		public SetViewModel GetSet(long id, bool loadTerms) {
			// Try to return an instance we already have, if possible.

			var existing = MySets.FirstOrDefault(s => s.ID == id) 
				?? FavouriteSets.FirstOrDefault(s => s.ID == id) 
				?? Groups.SelectMany(g => g.Sets).FirstOrDefault(s => s.ID == id);
			if (existing != null) {
				if (loadTerms && !existing.TermsLoaded)
					LoadTerms(existing);
				return existing;
			}

			var si = cache.GetSet(id, loadTerms);
			if (si == null)
				return null;
			bool starred = cache.GetFavourites().Any(x => x.ID == si.Value.ID);
			return new SetViewModel(this, si.Value) { Starred = starred };
		}

		public void LoadTerms(SetViewModel set) {
			var si = cache.GetSet(set.ID, true);
			if (si == null)
				return;

			set.Update(si.Value);
		}

		public void FetchSetTerms(IList<SetViewModel> sets, Action completed, Action<Exception> errorHandler) {
			api.FetchSets(
				sets.Select(set => set.ID),
				setInfos => {
					foreach (var si in setInfos) {
						cache.Update(si);
						foreach(var set in sets)
							if (set.ID == si.ID)
								set.Update(si);
					}
					completed();
				},
				errorHandler,
				new CancellationToken());
		}

		public void FetchSetTerms(SetViewModel setViewModel, Action completed, Action<Exception> errorHandler) {
			api.FetchSet(
				setViewModel.ID,
				si => {
					cache.Update(si);
					setViewModel.Update(si);
					completed();
				},
				errorHandler,
				new CancellationToken()
				);

		}

		public void CreateSet(SetViewModel setViewModel, string password, IEnumerable<long> groupIDs, Action completed, Action<Exception> errorHandler) {
			setViewModel.Author = UserName;
			var si = setViewModel.ToSetInfo();
			api.CreateSet(
				si,
				password,
				groupIDs,
				(id, uri) => {
					si.ID = setViewModel.ID = id;
					si.Uri = setViewModel.Uri = uri;

					cache.AddSetInfo(si);
					MySets.Insert(0, setViewModel);

					completed();
				},
				errorHandler,
				new CancellationToken());
		}

		public void Delete(SetViewModel set, Action success, Action<Exception> errorHandler) {
			Action completion = delegate {
				MySets.Remove(s => s.ID == set.ID);
				FavouriteSets.Remove(s => s.ID == set.ID);
				foreach (var g in Groups)
					g.Sets.Remove(s => s.ID == set.ID);

				cache.RemoveSet(set.ID);

				success();
			};

			api.DeleteSet(
				set.ID, 
				completion,
				err => {
					if (err is ItemDeletedException)
						completion(); // This case can hardly be considered an error.
					else 
						errorHandler(err);
				},
				new CancellationToken());
		}

		public void SetStarred(SetViewModel set, bool starred, Action completed, Action<Exception> errorHandler) {
			if (starred == set.Starred)
				return;

			api.SetStarred(
				set.ID,
				starred,
				delegate {
					cache.SetStarred(set.ID, starred);

					if (starred)
						FavouriteSets.Add(set);
					else
						FavouriteSets.Remove(set);

					set.Starred = starred;
					completed();
				},
				errorHandler,
				new CancellationToken());
		}

		public void CommitEdits(SetViewModel setViewModel, string newPassword, SetVisibility? newVisibility, SetEditPermissions? newPermissions, IEnumerable<long> groupIDs, Action completed, Action<Exception> errorHandler) {
			string editable = newPermissions.HasValue ? newPermissions.Value.ToApiString() : null;
			string visibility = newVisibility.HasValue ? newVisibility.Value.ToApiString() : null;

			api.EditSet(setViewModel.ToSetInfo(), newPassword, visibility, editable, groupIDs, completed, errorHandler, new CancellationToken());
		}

		public void AddRecentSet(SetViewModel set) {
			if (RecentSets == null)
				RecentSets = new ObservableCollection<SetViewModel>();

			RecentSets.Remove(s => s.ID == set.ID);
			while (RecentSets.Count >= 8)
				RecentSets.RemoveAt(7);
			RecentSets.Insert(0, set);
			cache.MakeSetRecent(set.ID);
		}
		#endregion

		#region Groups
		public GroupViewModel GetGroup(long id) {
			// Try to return an instance we already have, if possible.

			var existing = Groups.FirstOrDefault(g => g.ID == id);
			if (existing != null)
				return existing;

			var gi = cache.GetGroup(id);
			return gi.HasValue ? new GroupViewModel(this, gi.Value) : null;
		}
		#endregion

		#region Properties
		string userName;
		public string UserName {
			get { return userName; }
			private set {
				if (value == userName)
					return;

				userName = value;
				OnPropertyChanged("UserName");
			}
		}

		ImageSource profileImage;
		public ImageSource ProfileImage {
			get { return profileImage; }
			set {
				if (value == profileImage)
					return;

				profileImage = value;
				OnPropertyChanged("ProfileImage");
			}
		}

		ObservableCollection<SetViewModel> mySets;
		public ObservableCollection<SetViewModel> MySets {
			get { return mySets; }
			set {
				if (value == mySets)
					return;

				mySets = value;
				OnPropertyChanged("MySets");
			}
		}

		ObservableCollection<SetViewModel> recentSets;
		public ObservableCollection<SetViewModel> RecentSets {
			get { return recentSets; }
			set {
				if (value == recentSets)
					return;

				recentSets = value;
				OnPropertyChanged("RecentSets");
			}
		}

		ObservableCollection<SetViewModel> favouriteSets;
		public ObservableCollection<SetViewModel> FavouriteSets {
			get { return favouriteSets; }
			set {
				if (value == favouriteSets)
					return;

				favouriteSets = value;
				OnPropertyChanged("FavouriteSets");
			}
		}

		ObservableCollection<GroupViewModel> groups;
		public ObservableCollection<GroupViewModel> Groups {
			get { return groups; }
			set {
				if (groups == value)
					return;

				groups = value;
				OnPropertyChanged("Groups");
			}
		}

		bool isDataLoaded;
		public bool IsDataLoaded {
			get { return isDataLoaded; }
			set {
				if (value == isDataLoaded)
					return;

				isDataLoaded = value;
				OnPropertyChanged("IsDataLoaded");
			}
		}

		bool isSyncing;
		public bool IsSynchronizing {
			get { return isSyncing; }
			set {
				if (value == isSyncing)
					return;

				isSyncing = value;
				OnPropertyChanged("IsSynchronizing");
			}
		}

		string syncStatus;
		public string SyncStatus {
			get { return syncStatus; }
			set {
				if (value == syncStatus)
					return;

				syncStatus = value;
				OnPropertyChanged("SyncStatus");
			}
		} 
		#endregion

		public bool HasSynchronized { get; private set; }

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName) {
			var handler = PropertyChanged;
			if (handler != null) {
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}