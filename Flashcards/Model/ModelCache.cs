using System;
using System.Collections;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Flashcards.Model {
	public class ModelCache : IDisposable {
		readonly IsolatedStorageFile storage;

		public Credentials Credentials { get; private set; }

		DateTime indexDate;

		Dictionary<long, SetInfo> sets;
		Dictionary<long, GroupInfo> groups;
		List<long> mySets, favouriteSets, recentSets;

		public ModelCache() {
			storage = IsolatedStorageFile.GetUserStoreForApplication();

			try {
				LoadIndex();
			} catch (CacheIndexMissingException) {
				sets = new Dictionary<long, SetInfo>();
				groups = new Dictionary<long, GroupInfo>();
			}
		}

		public ModelCache(Credentials credentials) {
			storage = IsolatedStorageFile.GetUserStoreForApplication();

			bool resetUserData = false;

			try { 
				LoadIndex();

				if (credentials.UserName != Credentials.UserName)
					resetUserData = true;
			} catch (CacheIndexMissingException) {
				sets = new Dictionary<long, SetInfo>();
				groups = new Dictionary<long, GroupInfo>();
				resetUserData = true;
			}

			if (resetUserData) {
				mySets = new List<long>();
				recentSets = new List<long>();
				favouriteSets = new List<long>();
				indexDate = DateTime.Now;

				Credentials = credentials;
			}
		}

		public void Dispose() {
			storage.Dispose();
		}

		void LoadIndex() {
			try {
				using (var stream = storage.OpenFile("Index", FileMode.Open)) {
					using (var reader = new BinaryReader(stream)) {
						string accessToken = reader.ReadString();
						string userName = reader.ReadString();
						var tokenExpiry = new DateTime(reader.ReadInt64());
						Credentials = new Credentials(accessToken, userName, tokenExpiry);

						string profileImage = reader.ReadString();
						ProfileImage = string.IsNullOrEmpty(profileImage) ? null : new Uri(profileImage, UriKind.Absolute);

						indexDate = new DateTime(reader.ReadInt64());

						int setCount = reader.ReadInt32();

						sets = new Dictionary<long, SetInfo>();
						while (setCount --> 0) {
							var si = new SetInfo {
								ID = reader.ReadInt64(),
								Title = reader.ReadString(),
								Author = reader.ReadString(),
								Description = reader.ReadString(),
								Uri = new Uri(reader.ReadString()),
								TermCount = reader.ReadInt32(),
								Created = new DateTime(reader.ReadInt64()),
								Modified = new DateTime(reader.ReadInt64()),
								TermLanguageCode = reader.ReadBoolean() ? reader.ReadString() : null,
								DefinitionLanguageCode = reader.ReadBoolean() ? reader.ReadString() : null,
								Subjects = new List<string>()
							};

							int subjectCount = reader.ReadInt32();
							while (subjectCount --> 0)
								si.Subjects.Add(reader.ReadString());

							si.Visibility = reader.ReadString();
							si.Editable = reader.ReadString();
							si.HasAccess = reader.ReadBoolean();

							bool hasTerms = reader.ReadBoolean();

							sets[si.ID] = si;

							/*if (hasTerms)
								LoadSetTerms(si.ID);*/
						}

						int groupCount = reader.ReadInt32();
						groups = new Dictionary<long, GroupInfo>();

						while (groupCount --> 0) {
							var gi = new GroupInfo {
								ID = reader.ReadInt64(),
								Name = reader.ReadString(),
								Description = reader.ReadString(),
								Created = new DateTime(reader.ReadInt64()),
								UserCount = reader.ReadInt32(),
								IsPublic = reader.ReadBoolean(),
								HasAccess = reader.ReadBoolean(),
								HasPassword = reader.ReadBoolean(),
								HasDiscussion = reader.ReadBoolean(),
								MemberAddSets = reader.ReadBoolean(),
								Sets = new List<SetInfo>()
							};

							int groupSetCount = reader.ReadInt32();
							while (groupSetCount --> 0) {
								SetInfo si;
								if(!sets.TryGetValue(reader.ReadInt64(), out si))
									throw new CacheException("Unknown group specified as member of a group");

								// Don't need to make a copy; GroupInfo is a value type
								gi.Sets.Add(si);
							}
							groups[gi.ID] = gi;
						}

						mySets = ReadList(reader, r => r.ReadInt64());
						favouriteSets = ReadList(reader, r => r.ReadInt64());
						recentSets = ReadList(reader, r => r.ReadInt64());
					}
				}
			} catch (IsolatedStorageException e) {
				// The only non-fatal error.
				throw new CacheIndexMissingException("Unable to load application cache because the master cache file is missing", e);
			} catch (IOException) {
				// Might as well delete the cache.
				storage.DeleteFile("Index");

				// We can't have a partially-loaded cache.
				sets = new Dictionary<long, SetInfo>();
				groups = new Dictionary<long, GroupInfo>();
				mySets = new List<long>();
				recentSets = new List<long>();
				favouriteSets = new List<long>();

				// I suppose if we managed to load credentials, we can use them.
				// Thus, no need to reset them.
			}
		}

		static List<T> ReadList<T>(BinaryReader reader, Func<BinaryReader, T> readOne) {
			int n = reader.ReadInt32();
			var list = new List<T>();
			while (n --> 0)
				list.Add(readOne(reader));
			return list;
		}

		public void Write() {
			try {
				if (Credentials == null || sets == null || mySets == null || favouriteSets == null) {
					LogOut();
					return;
				}

				// Don't do this, since we now no longer fully load every group...
				//foreach(string fileName in storage.GetFileNames("Set*"))
				//	storage.DeleteFile(fileName);

				using (var stream = storage.OpenFile("Index", FileMode.Create)) {
					using (var writer = new BinaryWriter(stream)) {
						writer.Write(Credentials.Token);
						writer.Write(Credentials.UserName);
						writer.Write(Credentials.Expiry.Ticks);
						writer.Write(ProfileImage != null ? ProfileImage.ToString() : "");
						writer.Write(indexDate.Ticks);

						writer.Write(sets.Count);
						foreach (var set in sets.Values) {
							if (set.Terms != null) {
								using (var setStream = storage.OpenFile("Set" + set.ID, FileMode.Create)) {
									using (var bw = new BinaryWriter(setStream)) {
										bw.Write(set.Terms.Count);

										foreach (var ti in set.Terms) {
											bw.Write(ti.ID);
											bw.Write(ti.Term);
											bw.Write(ti.Definition);
											bw.Write(ti.Image ?? string.Empty);
										}
									}
								}
							}
							
							writer.Write(set.ID);
							writer.Write(set.Title);
							writer.Write(set.Author);
							writer.Write(set.Description ?? ""); // We might not have it if it was loaded from a search result.
							writer.Write(set.Uri.ToString());
							writer.Write(set.TermCount);
							writer.Write(set.Created.Ticks);
							writer.Write(set.Modified.Ticks);

							writer.Write(set.TermLanguageCode != null);
							if (set.TermLanguageCode != null)
								writer.Write(set.TermLanguageCode);

							writer.Write(set.DefinitionLanguageCode != null);
							if (set.DefinitionLanguageCode != null)
								writer.Write(set.DefinitionLanguageCode);

							writer.Write(set.Subjects.Count);
							foreach (var s in set.Subjects)
								writer.Write(s);

							writer.Write(set.Visibility);
							writer.Write(set.Editable);
							writer.Write(set.HasAccess);

							writer.Write(set.Terms != null);
						}

						writer.Write(groups.Count);
						foreach (var gi in groups.Values) {
							writer.Write(gi.ID);
							writer.Write(gi.Name);
							writer.Write(gi.Description);
							writer.Write(gi.Created.Ticks);
							writer.Write(gi.UserCount);
							writer.Write(gi.IsPublic);
							writer.Write(gi.HasAccess);
							writer.Write(gi.HasPassword);
							writer.Write(gi.HasDiscussion);
							writer.Write(gi.MemberAddSets);

							writer.Write(gi.Sets.Count);
							foreach (var si in gi.Sets)
								writer.Write(si.ID);
						}

						writer.Write(mySets.Count);
						foreach (long id in mySets)
							writer.Write(id);

						writer.Write(favouriteSets.Count);
						foreach (long id in favouriteSets)
							writer.Write(id);

						writer.Write(recentSets.Count);
						foreach (long id in recentSets)
							writer.Write(id);
					}
				}
			} catch (IsolatedStorageException e) {
				throw new CacheException("Unable to write application cache", e);
			} catch (IOException e) {
				throw new CacheException("Unable to write application cache", e);
			}
		}

		public void LogOut() {
			Credentials = null;
			mySets = null;
			favouriteSets = null;

			foreach (string fileName in storage.GetFileNames())
				storage.DeleteFile(fileName);
		}

		public List<TermInfo> LoadSetTerms(long setID) {
			SetInfo set;
			if (!sets.TryGetValue(setID, out set))
				return null;

			try {
				using (var stream = storage.OpenFile("Set" + setID.ToString(CultureInfo.InvariantCulture), FileMode.Open)) {
					using (var reader = new BinaryReader(stream)) {
						int termCount = reader.ReadInt32();

						var terms = new List<TermInfo>();
						while (termCount-- > 0) {
							var term = new TermInfo {
								ID = reader.ReadInt64(),
								Term = reader.ReadString(),
								Definition = reader.ReadString(),
								Image = reader.ReadString()
							};

							if (string.IsNullOrEmpty(term.Image))
								term.Image = null;

							terms.Add(term);
						}

						set.Terms = terms;
						sets[setID] = set;
						return terms;
					}
				}
			} catch (IsolatedStorageException) {
				return null;
			} catch (IOException) {
				return null;
			}
		}

		private IEnumerable<SetInfo> GetSetsOfType(IEnumerable<long> setIDs) {
			foreach (long id in setIDs) {
				var set = sets[id];
				//if (group.Terms == null)
				//    group.Terms = LoadSetTerms(group.ID);
				yield return set;
			}
		}

		public IEnumerable<SetInfo> GetMySets() {
			return GetSetsOfType(mySets);
		}

		public IEnumerable<SetInfo> GetFavourites() {
			return GetSetsOfType(favouriteSets);
		}

		public IEnumerable<SetInfo> GetRecentSets() {
			return GetSetsOfType(recentSets);
		}

		public IEnumerable<GroupInfo> GetGroups() {
			return groups.Values;
		}

		public void Update(SetInfo set) {
			SetInfo oldSet;
			if (!sets.TryGetValue(set.ID, out oldSet))
				sets.Add(set.ID, set);
			else if (oldSet.Modified < set.Modified || oldSet.Terms == null || oldSet.Terms.Count == 0)
				sets[set.ID] = set;
		}

		public void Update(List<SetInfo> newSets) {
			newSets.ForEach(Update);
		}

		public void Update(List<GroupInfo> newGroups) {
			foreach (var gi in newGroups) {
				Update(gi.Sets);

				groups[gi.ID] = gi;
			}
		}

		public void UpdateMySets(List<SetInfo> newSets) {
			Update(newSets);
			mySets = new List<long>(from s in newSets select s.ID);
		}

		public void UpdateFavourites(List<SetInfo> newSets) {
			Update(newSets);
			favouriteSets = new List<long>(from s in newSets select s.ID);
		}

		public void AddSetInfo(SetInfo si) {
			SetInfo existing;
			if (sets.TryGetValue(si.ID, out existing) && existing.Modified > si.Modified)
				return;

			if (!sets.ContainsKey(si.ID))
				sets[si.ID] = si;
		}

		public void MakeSetRecent(long setID) {
			if (recentSets.Contains(setID))
				recentSets.Remove(setID);

			recentSets.Insert(0, setID);
		}

		public void FetchProfileImage(Action<Exception> completion) {
			if (ProfileImage == null) {
				completion(null);
				return;
			}

			var op = System.ComponentModel.AsyncOperationManager.CreateOperation(null);

			// Quizlet now generates non-absolute profile image URIs by default.
			var uri = ProfileImage;
			if (!uri.IsAbsoluteUri)
				uri = new Uri(new Uri("http://www.quizlet.com/"), uri);
			var req = System.Net.WebRequest.Create(uri);

			req.BeginGetResponse(r => {
				Exception err;
				try {
					using (var fileStream = storage.OpenFile("ProfileImage.jpg", FileMode.Create, FileAccess.Write, FileShare.None))
						req.EndGetResponse(r).GetResponseStream().CopyTo(fileStream);

					op.PostOperationCompleted(delegate {
						completion(null);
					}, null);
					return;
				} catch (System.Net.WebException e) {
					err = e;
				} catch (IOException e) {
					err = e;
				}

				op.PostOperationCompleted(delegate {
					completion(err);
				}, null);
			}, null);
		}

		public void DeleteProfileImage() {
			storage.DeleteFile("ProfileImage");
		}

		public SetInfo? GetSet(long id, bool loadTerms) {
			SetInfo set;
			if (sets.TryGetValue(id, out set)) {
				if (set.Terms == null && loadTerms)
					set.Terms = LoadSetTerms(id);
				return set;
			}
			return null;
		}

		public GroupInfo? GetGroup(long id) {
			GroupInfo group;
			if (groups.TryGetValue(id, out @group))
				return @group;
			return null;
		}

		public Uri ProfileImage { get; set; }

		public void SetStarred(long setID, bool starred) {
			if (!sets.ContainsKey(setID))
				throw new ArgumentOutOfRangeException("setID", "Can't star or unstar a group that isn't known");

			if (starred)
				favouriteSets.Add(setID);
			else
				favouriteSets.Remove(setID);
		}

		public void RemoveSet(long setID) {
			sets.Remove(setID);
			mySets.Remove(setID);
			favouriteSets.Remove(setID);
			
			// Use ToArray() to fully enumerate the value list before modifying the dictionary,
			// thus preventing errors during enumeration due to modification.
			foreach(var g in groups.Values.ToArray()) {
				var updated = g;
				updated.Sets = g.Sets.Where(s => s.ID != setID).ToList();
				groups[g.ID] = updated;
			}
			storage.DeleteFile("Set" + setID);
		}
	}

	public class CacheIndexMissingException : CacheException {
		public CacheIndexMissingException() { }
		public CacheIndexMissingException(string message) : base(message) { }
		public CacheIndexMissingException(string message, Exception inner) : base(message, inner) { }
	}

	public class CacheException : Exception {
		public CacheException() { }
		public CacheException(string message) : base(message) { }
		public CacheException(string message, Exception inner) : base(message, inner) { }
	}
}