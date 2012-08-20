using System.Linq;
using System;

namespace Flashcards.Model {
	public class Credentials {
		public string Token { get; private set; }
		public string UserName { get; private set; }
		public DateTime Expiry { get; private set; }

		public Credentials(string token, string userName, DateTime expiry) {
			Token = token;
			UserName = userName;
			Expiry = expiry;
		}
	}
}