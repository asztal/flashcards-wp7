using System.Collections.Generic;
using System.Linq;
using System;

namespace Flashcards.Model.API {
	public class CancellationToken {
		bool cancelled;
		readonly List<Action> handlers;
		readonly object syncRoot;

		public CancellationToken() {
			cancelled = false;
			handlers = new List<Action>();
			syncRoot = new object();
		}

		public bool CancellationRequested { get { lock (syncRoot) return cancelled; } }

		public void Register(Action onCancel) {
			lock (syncRoot)
				handlers.Add(onCancel);
		}

		public void Cancel() {
			List<Action> handlersCopy;
			lock (syncRoot) {
				if (cancelled)
					return;
				handlersCopy = new List<Action>(handlers);
			}

			// Won't this just throw the exception on the the calling thread... ?
			foreach (var handler in handlersCopy)
				handler.Invoke();
		}
	}
}