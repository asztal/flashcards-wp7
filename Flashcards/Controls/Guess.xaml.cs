using System;
using System.Windows;
using System.Windows.Input;
using Flashcards.ViewModels;

namespace Flashcards.Controls {
	public partial class Guess {
		PracticeViewModel session;

		public Guess() {
			InitializeComponent();
		}

		public PracticeViewModel Session {
			get { return (PracticeViewModel)GetValue(SessionProperty); }
			set { SetValue(SessionProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Session.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SessionProperty =
			DependencyProperty.Register("Session", typeof(PracticeViewModel), typeof(Guess), new PropertyMetadata(SessionChanged));

		static void SessionChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			((Guess) o).SessionChanged(e.OldValue as PracticeViewModel);
		}

		void SessionChanged(PracticeViewModel oldValue) {
			if (oldValue != null) {
				oldValue.Finished -= PracticeFinished;
				oldValue.FinishedRound -= PracticeFinishedRound;
			}

			session = Session;

			session.Finished += PracticeFinished;
			session.FinishedRound += PracticeFinishedRound;

			if (session.IsFinished)
				GoToState(GameState.GameOverview);
			else if (session.CurrentItem == null)
				GoToState(GameState.RoundOverview);
			else 
				GoToState(GameState.Guessing);
		}

		private void PracticeFinishedRound(object sender, RoutedEventArgs e) {
			GoToState(GameState.RoundOverview);
		}

		private void PracticeFinished(object sender, RoutedEventArgs e) {
			GoToState(GameState.GameOverview);
		}

		private void AnswerKeyDown(object sender, KeyEventArgs e) {
			if (e.Key != Key.Enter)
				return;

			e.Handled = true;
			bool correct = session.SubmitAnswer(answer.Text);
			answer.Text = "";
			if (state == GameState.Guessing || state == GameState.IncorrectAnswer)
				GoToState(correct ? GameState.Guessing : GameState.IncorrectAnswer);
		}

		void Override(object sender, RoutedEventArgs e) {
			session.OverrideWrongAnswer();
			if (state == GameState.IncorrectAnswer)
				GoToState(GameState.Guessing);
		}

		GameState state = GameState.PreGame;
		void GoToState(GameState newState) {
			switch(newState) {
				case GameState.Guessing:
					if (state != GameState.Guessing && state != GameState.IncorrectAnswer)
						VisualStateManager.GoToState(this, "Guessing", true);
					VisualStateManager.GoToState(this, "FirstGuess", true);
					break;
				case GameState.IncorrectAnswer:
					if (state != GameState.Guessing && state != GameState.IncorrectAnswer)
						VisualStateManager.GoToState(this, "Guessing", true);
					VisualStateManager.GoToState(this, "SecondGuess", true);
					break;
				case GameState.RoundOverview:
					VisualStateManager.GoToState(this, "RoundOverview", true);
					break;
				case GameState.GameOverview:
					VisualStateManager.GoToState(this, "GameOverview", true);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			state = newState;

			VisualStateManager.GoToState(this, state.ToString(), true);
		}

		void StartNewRound(object sender, RoutedEventArgs e) {
			session.StartRound();
			GoToState(GameState.Guessing);
		}

		void GuessStateChangeFinished(object sender, EventArgs e) {
			if (GameStates.CurrentState == Guessing)
				Dispatcher.BeginInvoke(() => answer.Focus());
		}
	}

	public enum GameState {
		PreGame,
		Guessing,
		IncorrectAnswer,
		RoundOverview,
		GameOverview
	}
}
