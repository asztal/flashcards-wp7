using System;
using System.Collections.Generic;

namespace Flashcards.Model {
	public struct SetInfo : IJsonConvertible {
		public long ID;
		public string Title;
		public string Author;
		public string Description;
		public Uri Uri;
		public int TermCount;

		public DateTime Created;
		public DateTime Modified;

		public List<string> Subjects;
		public string Visibility;
		public string Editable;
		public bool HasAccess;
		public bool HasDiscussion;

		public string TermLanguageCode;
		public string DefinitionLanguageCode;

		public List<TermInfo> Terms;

		public SetInfo(JsonValue json, IJsonContext context) : this () {
			var dict = json as JsonDictionary;
			if (dict == null)
				throw new JsonConvertException("Expected a JSON dictionary to be converted to a SetInfo");

			foreach (var k in dict.Items) {
				switch (k.Key) {
					case "id": ID = context.FromJson<long>(k.Value); break;
					case "url": Uri = new Uri(context.FromJson<string>(k.Value)); break;
					case "title": Title = context.FromJson<string>(k.Value); break;
					case "created_by": Author = context.FromJson<string>(k.Value); break;
					case "description": Description = context.FromJson<string>(k.Value); break;
					case "term_count": TermCount = context.FromJson<int>(k.Value); break;
					case "created_date": Created = new DateTime(1970, 1, 1).AddSeconds(context.FromJson<long>(k.Value)); break;
					case "modified_date": Modified = new DateTime(1970, 1, 1).AddSeconds(context.FromJson<long>(k.Value)); break;
					case "subjects": Subjects = context.FromJson<List<string>>(k.Value); break;
					case "visibility": Visibility = context.FromJson<string>(k.Value); break;
					case "editable": Editable = context.FromJson<string>(k.Value); break;
					case "has_access": HasAccess = context.FromJson<bool>(k.Value); break;
					case "has_discussion": HasDiscussion = context.FromJson<bool>(k.Value); break;
					case "lang_terms": TermLanguageCode = context.FromJson<string>(k.Value); break;
					case "lang_definitions": DefinitionLanguageCode = context.FromJson<string>(k.Value); break;
					case "terms":
						var list = new List<TermInfo>();
						if (k.Value is JsonArray) {
							foreach (var termJson in ((JsonArray)k.Value).Items) {
								if (!(termJson is JsonDictionary))
									throw new JsonConvertException("Expected SetInfo.Terms to be an array of JSON dictionaries");
								var term = new TermInfo {
									ID = context.FromJson<long>(((JsonDictionary)termJson).Items["id"]),
									Term = context.FromJson<string>(((JsonDictionary)termJson).Items["term"]),
									Definition = context.FromJson<string>(((JsonDictionary)termJson).Items["definition"])
								};
								if (term.Term == null || term.Definition == null)
									throw new JsonConvertException("Either term or definition was not group when converting from JSON to SetInfo");
								list.Add(term);
							}
						} else {
							throw new JsonConvertException("Expected SetInfo.Terms to be an array");
						}
						Terms = list;
						break;
				}
			}

			// TODO: Validate that important fields are defined
		}

		public JsonValue ToJson(IJsonContext context) {
			throw new NotImplementedException();
		}
	}

	public struct GroupInfo : IJsonConvertible {
		public long ID;
		public string Name, Description;
		public int UserCount;
		public DateTime Created;
		public bool IsPublic, HasPassword, HasAccess, HasDiscussion, MemberAddSets;

		public List<SetInfo> Sets;

		public GroupInfo(JsonValue json, IJsonContext context) : this() {
			var dict = json as JsonDictionary;
			if (dict == null)
				throw new JsonConvertException("Expected a JSON dictionary to be converted to a GroupInfo");

			bool wasRelaxed = context.RelaxedNumericConversion;
			context.RelaxedNumericConversion = true;
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
					case "sets": Sets = context.FromJson<List<SetInfo>>(item.Value); break;
				}
			}

			if (!hasID || !hasName) 
				throw new JsonConvertException("Server did not supply group ID or name");

			context.RelaxedNumericConversion = wasRelaxed;
		}

		public JsonValue ToJson(IJsonContext context) {
			throw new NotImplementedException();
		}
	}

	public class TermInfo {
		public long ID { get; set; }
		public string Term { get; set; }
		public string Definition { get; set; }
		public string Image { get; set; }
	}

	public struct UserInfo {
		public string UserName { get; set; }
		public string AccountType { get; set; }
		public DateTime SignUpDate { get; set; }
		public Uri ProfileImage { get; set; }

		// Only contains basic set/group info
		public List<SetInfo> Sets { get; set; }
		public List<GroupInfo> Groups { get; set; }

		public int? StudySessionCount;
		public int? MessageCount;
		public int? TotalAnswerCount;
		public int? PublicSetsCreated;
		public int? PublicTermsEntered;

		public UserInfo(JsonValue json, IJsonContext context) : this() {
			var dict = json as JsonDictionary;
			if (dict == null)
				throw new JsonConvertException("Expected a JSON dictionary to be converted to a UserInfo");

			bool wasRelaxed = context.RelaxedNumericConversion;
			context.RelaxedNumericConversion = true;

			UserName = context.FromJson<string>(dict["username"]);
			AccountType = "free";
			SignUpDate = new DateTime(1970, 1, 1).AddSeconds(context.FromJson<long>(dict["sign_up_date"]));

			Sets = context.FromJson<List<SetInfo>>(dict["sets"]);
			Groups = context.FromJson<List<GroupInfo>>(dict["groups"]);

			foreach (var k in dict.Items) {
				switch (k.Key) {
					case "account_type": AccountType = context.FromJson<string>(k.Value); break;
					case "study_session_count": StudySessionCount = context.FromJson<int>(k.Value); break;
					case "message_count": MessageCount = context.FromJson<int>(k.Value); break;
					case "total_answer_count": TotalAnswerCount = context.FromJson<int>(k.Value); break;
					case "public_sets_created": PublicSetsCreated = context.FromJson<int>(k.Value); break;
					case "public_terms_entered": PublicTermsEntered = context.FromJson<int>(k.Value); break;
					case "profile_image": ProfileImage = new Uri(context.FromJson<string>(k.Value), UriKind.Absolute); break;
				}
			}

			context.RelaxedNumericConversion = wasRelaxed;
		}
	}
}