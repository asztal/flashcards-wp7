using System;
using System.Diagnostics;

namespace Flashcards {
	public class Metrics {
		public static void Measure(string description, Action action) {
			var timer = new Stopwatch();
			timer.Start();
			action();
			Debug.WriteLine("Finished action {0} in {1}ms", description, timer.ElapsedMilliseconds);
		}
	}
}