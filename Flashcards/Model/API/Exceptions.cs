using System.Linq;
using System;

namespace Flashcards.Model.API {
	public class QuizletException : Exception {
		public QuizletException() { }
		public QuizletException(string message) : base(message) { }
		public QuizletException(string message, Exception inner) : base(message, inner) { }
	}

	public class AccessDeniedException : QuizletException {
		public AccessDeniedException() { }
		public AccessDeniedException(string message) : base(message) { }
		public AccessDeniedException(string message, Exception inner) : base(message, inner) { }
	}

	public class ItemNotFoundException : QuizletException {
		public ItemNotFoundException() { }
		public ItemNotFoundException(string message) : base(message) { }
		public ItemNotFoundException(string message, Exception inner) : base(message, inner) { }
	}

	public class ItemDeletedException : QuizletException {
		public ItemDeletedException() { }
		public ItemDeletedException(string message) : base(message) { }
		public ItemDeletedException(string message, Exception inner) : base(message, inner) { }
	}

	public class OperationCanceledException : Exception {
		public OperationCanceledException() { }
		public OperationCanceledException(string message) : base(message) { }
		public OperationCanceledException(string message, Exception inner) : base(message, inner) { }
	}
}