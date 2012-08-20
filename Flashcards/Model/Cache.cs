using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Flashcards.Model
{
	[Database(Name="cache")]
	public partial class Cache : DataContext {
		private static MappingSource mappingSource = new AttributeMappingSource();
		
		public Cache(string connection) : base(connection, mappingSource)
		{ }
		
		public Cache(string connection, MappingSource mappingSource) : base(connection, mappingSource)
		{ }

		public Table<Set> Sets {
			get {
				return this.GetTable<Set>();
			}
		}

		public Table<Group> Groups {
			get {
				return this.GetTable<Group>();
			}
		}

		public Table<Term> Terms {
			get {
				return this.GetTable<Term>();
			}
		}

		public IEnumerable<KeyValuePair<string, IEnumerable<Set>>> Subjects {
			get {
				return
					from ss in GetTable<SetSubject>()
					group ss by ss.Subject into grouping
					select new KeyValuePair<string, IEnumerable<Set>> (
						grouping.Key,
						from ss in grouping select ss.Set
					);
			}
		}

		public Set GetSet(long setID) {
			return Sets.SingleOrDefault(set => set.ID == setID);
		}

		public Group GetGroup(long groupID) {
			return Groups.SingleOrDefault(group => group.ID == groupID);
		}

		public Term GetTerm(long termID) {
			return Terms.SingleOrDefault(term => term.ID == termID);
		}

		public void UpdateGroups(IEnumerable<Group> groups) {
			var groupsA = groups.ToArray();
			Groups.AttachAll(groupsA);
			foreach (var g in groupsA)
				UpdateSets(g.Sets);
		}

		private void UpdateSets(IEnumerable<Set> sets) {
			Sets.AttachAll(sets);
		}

		public IQueryable<Set> SetsBy(string author) {
			return Sets.Where(set => set.Author == author);
		}

		public IQueryable<Set> RecentSets {
			get {
				return (from set in Sets
						where set.Accessed != null
						orderby set.Accessed descending
						select set);
			}
		}

		public IQueryable<Set> FavouriteSets {
			get {
				return Sets.Where(set => set.Starred);
			}
		}
	}

	[Table(Name = "SetGroupings")]
	public class SetGrouping {
		public SetGrouping() {
		}

		public SetGrouping(Set set, Group group) {
			Set = set;
			SetID = set.ID;
			Group = group;
			GroupID = group.ID;
		}

		[Column(IsPrimaryKey = true, CanBeNull = false)]
		public long SetID { get; set; }

		[Column(IsPrimaryKey = true, CanBeNull = false)]
		public long GroupID { get; set; }

		EntityRef<Set> set;
		[Association(Storage = "group", ThisKey = "SetID", OtherKey = "ID")]
		public Set Set {
			get { return set.Entity; }
			set { set.Entity = value; }
		}

		EntityRef<Group> group;

		[Association(Storage = "group", ThisKey = "GroupID", OtherKey = "ID")]
		public Group Group {
			get { return group.Entity; }
			set { group.Entity = value; }
		}
	}

	[Table(Name = "Groups")]
	public class Group : IJsonConvertible {
		public Group() {
			setGroupings = new EntitySet<SetGrouping>();
		}

		[Column(IsPrimaryKey = true, CanBeNull = false)]
		public long ID { get; set; }

		EntitySet<SetGrouping> setGroupings;
		[Association(Storage="setGroupings", OtherKey="GroupID")]
		public EntitySet<SetGrouping> SetGroupings {
			get { return setGroupings; }
			set { setGroupings.Assign(value); }
		}

		[Column(CanBeNull = false)] public string Name { get; set; }
		[Column(CanBeNull = false)] public string Description { get; set; }
		[Column(CanBeNull = false)] public int UserCount { get; set; }
		[Column(CanBeNull = false)] public DateTime Created { get; set; }
		[Column(CanBeNull = false)] public bool IsPublic { get; set; }
		[Column(CanBeNull = false)] public bool HasPassword { get; set; }
		[Column(CanBeNull = false)] public bool HasAccess { get; set; }
		[Column(CanBeNull = false)] public bool HasDiscussion { get; set; }
		[Column(CanBeNull = false)] public bool MemberAddSets { get; set; }

		public void AddSet(Set set) {
			setGroupings.Add(new SetGrouping { GroupID = ID, SetID = set.ID, Group = this, Set = set });
		}

		public IEnumerable<Set> Sets {
			get {
				return from g in setGroupings select g.Set;
			}
		}

		public void RemoveSet(Set set) {
			setGroupings.Remove(g => g.SetID == set.ID);
		}

		public Group(JsonValue json, IJsonContext context) : this() {
			var dict = json as JsonDictionary;
			if (dict == null)
				throw new JsonConvertException("Expected a JSON dictionary to be converted to a GroupInfo");

			bool hasID = false, hasName = false;

			foreach (var item in dict.Items) {
				switch (item.Key) {
					case "id":
						hasID = true;
						ID = context.FromJson<long>(item.Value);
						break;
					case "name":
						hasName = true;
						Name = context.FromJson<string>(item.Value);
						break;
					case "description": Description = context.FromJson<string>(item.Value); break;
					case "user_count": UserCount = context.FromJson<int>(item.Value); break;
					case "created_date": Created = new DateTime(context.FromJson<long>(item.Value)); break;
					case "is_public": IsPublic = context.FromJson<bool>(item.Value); break;
					case "has_password": HasPassword = context.FromJson<bool>(item.Value); break;
					case "has_access": HasAccess = context.FromJson<bool>(item.Value); break;
					case "has_discussion": HasDiscussion = context.FromJson<bool>(item.Value); break;
					case "member_add_sets": MemberAddSets = context.FromJson<bool>(item.Value); break;
					case "sets": 
						var sets = context.FromJson<List<Set>>(item.Value);
						AddSets(sets);
						break;
				}
			}

			if (!hasID || !hasName) 
				throw new JsonConvertException("Server did not supply group ID or name");
		}

		public JsonValue ToJson(IJsonContext context) {
			throw new NotImplementedException();
		}

		public void AddSets(IEnumerable<Set> newSets) {
			SetGroupings.AddRange(
				from newSet in newSets
				where SetGroupings.All(sg => sg.SetID != newSet.ID)
				select new SetGrouping(newSet, this));
		}
	}

	[Table(Name = "SetSubjects")]
	public class SetSubject {
		[Column(IsPrimaryKey = true, CanBeNull = false)]
		public string Subject { get; set; }

		[Column(IsPrimaryKey = true, CanBeNull = false)]
		public long SetID { get; set; }
		
		EntityRef<Set> set;
		[Association(IsForeignKey = true, ThisKey = "SetID", OtherKey = "ID")]
		public Set Set {
			get { return set.Entity; }
			set { set.Entity = value; }
		}
	}

	[Table(Name = "Terms")]
	public class Term {
		[Column(IsPrimaryKey = true, CanBeNull = false)]
		public long ID { get; set; }

		EntityRef<Set> set;
		[Association(IsForeignKey = true, OtherKey = "ID")]
		public Set SetID {
			get { return set.Entity; }
			set { set.Entity = value; }
		}

		[Column(CanBeNull = false)]
		public string TermValue { get; set; }

		[Column(CanBeNull = false)]
		public string Definition { get; set; }

		[Column(CanBeNull = false)]
		public string Image { get; set; }
	}

	[Table(Name="Sets")]
	public class Set : IJsonConvertible {
		public Set() {
			setGroupings = new EntitySet<SetGrouping>();
			subjects = new EntitySet<SetSubject>();
			terms = new EntitySet<Term>();
		}
		
		[Column(IsPrimaryKey = true, CanBeNull = false)]
		public long ID { get; set; }

		readonly EntitySet<SetGrouping> setGroupings;
		[Association(Storage = "setGroupings", OtherKey = "SetID")]
		public EntitySet<SetGrouping> SetGroupings {
			get { return setGroupings; }
			set { setGroupings.Assign(value); }
		}

		readonly EntitySet<SetSubject> subjects;
		[Association(Storage = "subjects", OtherKey = "SetID")]
		public EntitySet<SetSubject> Subjects {
			get { return subjects; }
			set { subjects.Assign(value); }
		}

		readonly EntitySet<Term> terms;
		[Association(Storage = "terms", OtherKey = "SetID")]
		public EntitySet<Term> Terms {
			get { return terms; }
			set { terms.Assign(value); }
		}

		[Column(CanBeNull=false)]
		public string Title { get; set; }
		
		[Column(CanBeNull=false)]
		public string Author { get; set; }
		
		[Column(CanBeNull=false)]
		public string Description { get; set; }
		
		[Column(CanBeNull=false)]
		public string Uri { get; set; }

		[Column(CanBeNull = false)]
		public int TermCount { get; set; }

		[Column(CanBeNull = false)]
		public DateTime Created { get; set; }

		[Column(CanBeNull = false)]
		public DateTime Modified { get; set; }

		[Column(Storage = "visibility", CanBeNull = false)]
		byte visibility;
		public SetVisibility Visibility {
			get { 	return (SetVisibility)visibility; }
			set { visibility = (byte)value; }
		}

		[Column(Storage = "editable", CanBeNull = false)]
		byte editable;
		public SetEditPermissions Editable {
			get { return (SetEditPermissions)editable; }
			set { editable = (byte)value; }
		}

		[Column(CanBeNull = false)]
		public bool HasAccess { get; set; }

		[Column(CanBeNull = false)]
		public bool HasDiscussion { get; set; }

		[Column(CanBeNull = false)]
		public string TermLanguageCode { get; set; }

		[Column(CanBeNull = false)]
		public string DefinitionLanguageCode { get; set; }

		[Column(CanBeNull = false)]
		public bool Starred { get; set; }

		[Column]
		public DateTime? Accessed { get; set; }

		public Set(JsonValue json, IJsonContext context) : this () {
			var dict = json as JsonDictionary;
			if (dict == null)
				throw new JsonConvertException("Expected a JSON dictionary to be converted to a SetInfo");

			if (!dict.Items.ContainsKey("id"))
				throw new JsonConvertException("Expected SetInfo JSON to contain an 'id' property");

			foreach (var k in dict.Items) {
				switch (k.Key) {
					case "id": ID = context.FromJson<long>(k.Value); break;
					case "url": Uri = context.FromJson<string>(k.Value); break;
					case "title": Title = context.FromJson<string>(k.Value); break;
					case "created_by": Author = context.FromJson<string>(k.Value); break;
					case "description": Description = context.FromJson<string>(k.Value); break;
					case "term_count": TermCount = context.FromJson<int>(k.Value); break;
					case "created_date": Created = new DateTime(1970, 1, 1).AddSeconds(context.FromJson<long>(k.Value)); break;
					case "modified_date": Modified = new DateTime(1970, 1, 1).AddSeconds(context.FromJson<long>(k.Value)); break;
					case "subjects": AddSubjects(context.FromJson<List<string>>(k.Value)); break;
					case "visibility": Visibility = SetPermissions.ParseVisibility(context.FromJson<string>(k.Value)); break;
					case "editable": Editable = SetPermissions.ParseEditPermissions(context.FromJson<string>(k.Value)); break;
					case "has_access": HasAccess = context.FromJson<bool>(k.Value); break;
					case "has_discussion": HasDiscussion = context.FromJson<bool>(k.Value); break;
					case "lang_terms": TermLanguageCode = context.FromJson<string>(k.Value); break;
					case "lang_definitions": DefinitionLanguageCode = context.FromJson<string>(k.Value); break;
					case "terms":
						var list = new List<Term>();
						if (k.Value is JsonArray) {
							foreach (var termJson in ((JsonArray)k.Value).Items) {
								if (!(termJson is JsonDictionary))
									throw new JsonConvertException("Expected SetInfo.Terms to be an array of JSON dictionaries");
								var term = new Term {
									ID = context.FromJson<long>(((JsonDictionary)termJson).Items["id"]),
									TermValue = context.FromJson<string>(((JsonDictionary)termJson).Items["term"]),
									Definition = context.FromJson<string>(((JsonDictionary)termJson).Items["definition"])
								};
								if (term.TermValue == null || term.Definition == null)
									throw new JsonConvertException("Either term or definition was not group when converting from JSON to Set");
								list.Add(term);
							}
						} else {
							throw new JsonConvertException("Expected SetInfo.Terms to be an array");
						}
						AddTerms(list);
						break;
				}
			}

			// TODO: Validate that important fields are defined
		}

		private void AddSubjects(IEnumerable<string> newSubjects) {
			Subjects.AddRange(
				from newSubject in newSubjects
				where Subjects.All(ss => ss.Subject != newSubject)
				select new SetSubject {
					Set = this,
					Subject = newSubject,
					SetID = ID
				});
		}

		private void AddTerms(IEnumerable<Term> newTerms) {
			Terms.AddRange(
				from newTerm in newTerms
				where Terms.All(t => t.ID != newTerm.ID)
				select newTerm);
		}

		public JsonValue ToJson(IJsonContext context) {
			throw new NotImplementedException();
		}
	}
}
