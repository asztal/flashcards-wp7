using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text;
using Flashcards.Model.API.Https;

namespace Flashcards.Model.API {
	/// <summary>
	/// Defines methods which call the Quizlet API over HTTPS. Some endpoints
	/// require the user to be logged in, in which case the Credentials
	/// property must be set.
	/// </summary>
	/// <remarks>
	/// The Client ID and the Secret API Key are in defined in APIKey.cs, which
	/// is not included in source control. Quizlet forbids giving away API keys.
	/// That's why this is a partial class.
	/// </remarks>
	public sealed partial class QuizletAPI {
		public static readonly QuizletAPI Default = new QuizletAPI();

		public Uri Host { get; private set; }
		public Credentials Credentials { get; private set; }
		
		/// <summary>
		/// Initializes the API using the default Quizlet server.
		/// </summary>
		public QuizletAPI() {
			Host = new Uri("https://api.quizlet.com:443/");
			Credentials = null;
		}

		public bool Authenticate(Credentials credentials) {
			if (credentials.Expiry < DateTime.Now)
				return false;

			Credentials = credentials;			
			return true;
		}

		/// <summary>
		/// Takes an OAuth code and turns it into an API token.
		/// </summary>
		/// <param name="code">A code returned from the OAuth page (https://quizlet.com/authorize/)</param>
		/// <param name="success">A delegate to be called when the authentication succeeds.</param>
		/// <param name="failure">What to do when the authentication fails.</param>
		/// <param name="token">A CancellationToken, which is currently useless.</param>
		public void Authenticate(string code, Action success, Action<Exception> failure, CancellationToken token) {
			var fields = "grant_type=authorization_code&code=" + code + "&redirect_uri=https://q.asztal.net/";

			var req = new HttpsRequest("POST", "/oauth/token");
			req.BasicAuthorization(ClientID, SecretKey);

			req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			req.PostData = Encoding.UTF8.GetBytes(fields);

			FetchJSON(req,
			json => {
				try {
					if (json is JsonDictionary) {
						var ctx = new JsonContext();

						var accessToken = ctx.FromJson<string>((json as JsonDictionary).Items["access_token"]);
						var userName = ctx.FromJson<string>((json as JsonDictionary).Items["user_id"]);
						var tokenExpiry = DateTime.Now.AddSeconds(ctx.FromJson<double>((json as JsonDictionary).Items["expires_in"]));
						Credentials = new Credentials(accessToken, userName, tokenExpiry);

						success();
					} else {
						failure(new FormatException("Quizlet server returned an invalid response."));
					}
				} catch (KeyNotFoundException) {
					failure(new FormatException("Quizlet server returned an invalid response."));
				} catch (JsonConvertException) {
					failure(new FormatException("Quizlet server returned an invalid response."));
				}
			},
			failure,
			token);
		}

		void AuthorizeRequest(HttpsRequest request, bool needsToken) {
			if (Credentials != null) {
				request.Authorization = "Bearer " + Credentials.Token;
			} else {
				if (needsToken)
					throw new QuizletException("A quizlet API endpoint was called which needs authentication");

				// TODO: check if GET or POST or PUT etc.
				char delim = '?';
				if (request.Path.Contains("?"))
					delim = '&';

				request.Path = request.Path + delim + "client_id=" + ClientID;
			}
		}

		void FetchJSON(HttpsRequest request, Action<JsonValue> completion, Action<Exception> errorHandler, CancellationToken token) {
			var op = System.ComponentModel.AsyncOperationManager.CreateOperation(null);

			request.UserAgent = "FlashcardsWP7/0.1 (" + Environment.OSVersion + "; .NET " + Environment.Version + ")";

			try {
				token.Register(delegate { throw new OperationCanceledException(); });

				ThreadPool.QueueUserWorkItem(
					_ => {
						Exception error;
						try {
							var client = new HttpsClient(Host.Host, Host.Port, Configuration.VerifyCertificates);
							var response = client.MakeRequest(request);

							var text = response.DecodeTextBody();

							if (response.StatusCode == 204) {
								op.PostOperationCompleted(delegate { completion(null); }, null);
								return;
							}

							var json = JsonValue.Parse(new StringReader(text));

							if (response.StatusCode / 100 != 2) {
								var dict = (JsonDictionary) json;
								var errorCode = new JsonContext().FromJson<string>(dict.Items["error"]);
								var errorText = new JsonContext().FromJson<string>(dict.Items["error_description"]);
								if (errorCode != null && errorText != null) {
									// TODO: find error code for needing password
									if (errorCode == "invalid_access")
										throw new AccessDeniedException(errorText);
									if (errorCode == "item_not_found")
										throw new ItemNotFoundException(errorText);
									if (errorCode == "item_deleted")
										throw new ItemDeletedException(errorText);
									throw new QuizletException(errorText);
								}
								throw new QuizletException("The Quizlet request failed (HTTP error " + response.StatusCode.ToString(CultureInfo.InvariantCulture) + ").");
							}

							op.PostOperationCompleted(delegate { completion(json); }, null);
							return;
						}
						catch (QuizletException e) {
							error = e;
						}
						catch (OperationCanceledException e) {
							error = e;
						}
						catch (HttpException e) {
							error = e;
						}
						catch (System.Net.Sockets.SocketException e) {
							error = e;
						}
						catch (IOException e) {
							error = e;
						}
						catch (ArgumentException e) {
							error = e;
						}
						catch (InvalidOperationException e) {
							error = e;
						}
						catch (NotSupportedException e) {
							error = e;
						}
						catch (FormatException e) {
							error = new QuizletException("The Quizlet server returned an invalid document.", e);
						}
						catch (JsonConvertException e) {
							error = new QuizletException("The Quizlet server returned an invalid document.", e);
						}
						catch (InvalidCastException e) {
							error = new QuizletException("The Quizlet server returned an invalid document.", e);
						}
						catch (KeyNotFoundException e) {
							error = new QuizletException("The Quizlet server returned an invalid document.", e);
						}

						op.PostOperationCompleted(delegate { errorHandler(error); }, null);
					});
			} catch (OperationCanceledException e) {
				errorHandler(e);
			}
		}

		public void GetSetInfo(long setID, Action<SetInfo> completion, Action<Exception> errorHandler, CancellationToken token) {
			FetchJSON(
				new HttpsRequest("GET", "/2.0/sets/" + setID.ToString(CultureInfo.InvariantCulture) + "?client_id=" + Uri.EscapeDataString(ClientID)),
				json => {
					try {
						var set = new JsonContext().FromJson<SetInfo>(json);
						completion(set);
					}
					catch (JsonConvertException e) {
						errorHandler(e);
					}
				},
				errorHandler,
				token);
		}

		public void SearchSets(string query, bool searchTerms, Action<List<SetInfo>> completion, Action<Exception> errorHandler, CancellationToken token) {
			var req = new HttpsRequest("GET", "/2.0/search/sets?sort=most_studied&" + (searchTerms ? "term=" : "q=") + Uri.EscapeDataString(query));

			AuthorizeRequest(req, false);

			FetchJSON(
				req,
				json => {
					try {
						var sets = new JsonContext().FromJson<List<SetInfo>>(((JsonDictionary)json).Items["sets"]);
						completion(sets);
					} catch (JsonConvertException e) {
						errorHandler(e);
					}
				},
				errorHandler,
				token);
		}

		public void FetchUserData(string userName, Action<Uri> completion, Action<Exception> errorHandler, CancellationToken token) {
			var req = new HttpsRequest("GET", "/2.0/users/" + Uri.EscapeUriString(userName));

			AuthorizeRequest(req, false);

			FetchJSON(
				req,
				json => {
					try {
						var dict = (JsonDictionary) json;
						completion(dict.Items.ContainsKey("profile_image") && dict.Items["profile_image"] != null
						           	? new Uri(new JsonContext().FromJson<string>(dict.Items["profile_image"]), UriKind.RelativeOrAbsolute)
						           	: null);
					}
					catch (JsonConvertException e) {
						errorHandler(e);
					}
				},
				errorHandler,
				token);
		}

		public void FetchUserSets(string userName, Action<List<SetInfo>> completion, Action<Exception> errorHandler, CancellationToken token) {
			var req = new HttpsRequest("GET", "/2.0/users/" + Uri.EscapeUriString(userName) + "/sets");

			AuthorizeRequest(req, true);

			FetchJSON(
				req,
				json => {
					try {
						var sets = new JsonContext().FromJson<List<SetInfo>>(json);
						completion(sets);
					} catch (JsonConvertException e) {
						errorHandler(e);
					}
				},
				errorHandler,
				token);
		}

		public void FetchUserFavourites(string userName, Action<List<SetInfo>> completion, Action<Exception> errorHandler, CancellationToken token) {
			var req = new HttpsRequest("GET", "/2.0/users/" + Uri.EscapeUriString(userName) + "/favorites");

			AuthorizeRequest(req, true);

			FetchJSON(
				req,
				json => {
					try {
						var sets = new JsonContext().FromJson<List<SetInfo>>(json);
						completion(sets);
					} catch (JsonConvertException e) {
						errorHandler(e);
					}
				},
				errorHandler,
				token);
		}

		public void FetchUserGroups(string userName, Action<List<GroupInfo>> completion, Action<Exception> errorHandler, CancellationToken token) {
			var req = new HttpsRequest("GET", string.Format(CultureInfo.InvariantCulture, "/2.0/users/{0}/groups", Uri.EscapeUriString(userName)));

			AuthorizeRequest(req, false);

			FetchJSON(
				req,
				json => {
					try {
						var groups = new JsonContext { RelaxedNumericConversion = true }.FromJson<List<GroupInfo>>(json);
						completion(groups);
					} catch (JsonConvertException e) {
						errorHandler(e);
					}
				},
				errorHandler,
				token);
		}

		public void FetchSet(long id, Action<SetInfo> completion, Action<Exception> errorHandler, CancellationToken token) {
			var req = new HttpsRequest("GET", "/2.0/sets/" + id.ToString(CultureInfo.InvariantCulture));

			AuthorizeRequest(req, false);

			FetchJSON(
				req,
				json => {
					try {
						completion(new JsonContext().FromJson<SetInfo>(json));
					} catch (JsonConvertException e) {
						errorHandler(e);
					}
				},
				errorHandler,
				token);
		}

		public void FetchSets(IEnumerable<long> ids, Action<List<SetInfo>> completion, Action<Exception> errorHandler, CancellationToken token) {
			var req = new HttpsRequest("GET", "/2.0/sets?set_ids=" + string.Join(",", from id in ids select id.ToString(CultureInfo.InvariantCulture)));

			AuthorizeRequest(req, false);

			FetchJSON(
				req,
				json => {
					try {
						completion(new JsonContext().FromJson<List<SetInfo>>(json));
					} catch (JsonConvertException e) {
						errorHandler(e);
					}
				},
				errorHandler,
				token);
		}

		public void SetStarred(long setID, bool starred, Action success, Action<Exception> errorHandler, CancellationToken token) {
			if (Credentials == null)
				throw new InvalidOperationException("Must be logged in to mark or unmark a group as favourite");

			var req = new HttpsRequest(starred ? "PUT" : "DELETE", "/2.0/users/" + Credentials.UserName + "/favorites/" + setID.ToString(CultureInfo.InvariantCulture));

			AuthorizeRequest(req, true);

			FetchJSON(
				req,
				_ => success(), // No content is returned (HTTP 204)
				errorHandler,
				token);
		}

		public void DeleteSet(long setID, Action success, Action<Exception> errorHandler, CancellationToken token) {
			var req = new HttpsRequest("DELETE", string.Format(CultureInfo.InvariantCulture, "/2.0/sets/{0}", setID));

			AuthorizeRequest(req, true);

			FetchJSON(
				req,
				_ => success(),
				errorHandler,
				token);
		}

		public void SubmitPassword(long setID, string password, Action success, Action<Exception> failure, CancellationToken token) {
			var req = new HttpsRequest("POST", string.Format(CultureInfo.InvariantCulture, "/2.0/sets/{0}/password", setID));

			AuthorizeRequest(req, true);

			var postData = new MemoryStream();
			using (var writer = new StreamWriter(postData))
				WriteQueryString(writer, "password", password);

			req.ContentType = "application/x-www-form-urlencoded";
			req.PostData = postData.ToArray();
			
			FetchJSON(
				req,
				json => success(),
				failure,
				token);
		}

		public void EditSet(SetInfo si, string newPassword, string newVisibility, string newPermissions, IEnumerable<long> groupIDs, Action success, Action<Exception> errorHandler, CancellationToken token) {
			var req = new HttpsRequest("PUT", string.Format(CultureInfo.InvariantCulture, "/2.0/sets/{0}", si.ID));

			var postData = new MemoryStream();
			using (var writer = new StreamWriter(postData))
				WriteSetInfo(si, writer, newPassword, newVisibility, newPermissions, groupIDs);

			req.ContentType = "application/x-www-form-urlencoded";
			req.PostData = postData.ToArray();
			AuthorizeRequest(req, true);

			FetchJSON(
				req,
				json => success(),
				errorHandler,
				token);
		}

		public void CreateSet(SetInfo si, string password, IEnumerable<long> groupIDs, Action<long, Uri> completion, Action<Exception> errorHandler, CancellationToken token) {
			var req = new HttpsRequest("POST", "/2.0/sets");
			
			var postData = new MemoryStream();
			using (var writer = new StreamWriter(postData))
				WriteSetInfo(si, writer, password, si.Visibility, si.Editable, groupIDs);

			req.ContentType = "application/x-www-form-urlencoded";
			req.PostData = postData.ToArray();
			AuthorizeRequest(req, true);

			FetchJSON(
				req,
				json => {
					try {
						if (!(json is JsonDictionary))
							throw new JsonConvertException("Expected a JSON dictionary");

						var ctx = new JsonContext();
						var dict = (json as JsonDictionary).Items;
						var uri = new Uri(ctx.FromJson<string>(dict["url"]));
						var id = ctx.FromJson<long>(dict["set_id"]);

						completion(id, uri);
					} catch (JsonConvertException err) {
						errorHandler(err);
					} catch (KeyNotFoundException err) {
						errorHandler(new QuizletException("Invalid response from server", err));
					}
				},
				errorHandler,
				token);
		}

		private static void WriteQueryString(TextWriter writer, string name, string value, bool first = false) {
			if (value == null)
				return;
			if (!first)
				writer.Write("&");
			writer.Write(name);
			writer.Write("=");
			writer.Write(Uri.EscapeUriString(value));
		}

		static void WriteSetInfo(SetInfo si, TextWriter writer, string newPassword = null, string newVisibility = null, string newPermissions = null, IEnumerable<long> groupIDs = null) {
			WriteQueryString(writer, "title", si.Title, true);
			WriteQueryString(writer, "description", si.Description);
			foreach (var s in si.Subjects)
				WriteQueryString(writer, "subjects[]", s);
			foreach (var term in si.Terms)
				WriteQueryString(writer, "terms[]", term.Term);
			foreach (var term in si.Terms)
				WriteQueryString(writer, "definitions[]", term.Definition);
			WriteQueryString(writer, "lang_terms", si.TermLanguageCode);
			WriteQueryString(writer, "lang_definitions", si.DefinitionLanguageCode);

			WriteQueryString(writer, "password", newPassword);
			WriteQueryString(writer, "visibility", newVisibility);
			WriteQueryString(writer, "editable", newPermissions);

			if (groupIDs != null)
				foreach(var id in groupIDs)
					WriteQueryString(writer, "groups[]", id.ToString(CultureInfo.InvariantCulture));
		}

		public void LogOut() {
			Credentials = null;
		}
	}
}