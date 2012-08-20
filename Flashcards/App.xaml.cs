using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using Flashcards.Model;
using Flashcards.Model.API;
using Flashcards.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Flashcards
{
	public partial class App {
		private static MainViewModel viewModel;

		/// <summary>
		/// A static ViewModel used by the views to bind against.
		/// </summary>
		/// <returns>The MainViewModel object.</returns>
		public static MainViewModel ViewModel
		{
			get {
				// Delay creation of the view model until necessary
				return viewModel ?? (viewModel = new MainViewModel());
			}
		}

		public new static App Current {
			get {
				return Application.Current as App;
			}
		}

		/// <summary>
		/// Provides easy access to the root frame of the Phone Application.
		/// </summary>
		/// <returns>The root frame of the Phone Application.</returns>
		public PhoneApplicationFrame RootFrame { get; private set; }

		/// <summary>
		/// Constructor for the Application object.
		/// </summary>
		public App()
		{
			// Global handler for uncaught exceptions. 
			UnhandledException += OnUnhandledException;

			// Show graphics profiling information while debugging.
			if (Debugger.IsAttached)
			{
				// Display the current frame rate counters
				//Application.Current.Host.Settings.EnableFrameRateCounter = true;

				// Show the areas of the app that are being redrawn in each frame.
				//Application.Current.Host.Settings.EnableRedrawRegions = true;

				// Enable non-production analysis visualization mode, 
				// which shows areas of a page that are being GPU accelerated with a colored overlay.
				//Application.Current.Host.Settings.EnableCacheVisualization = true;
			}

			// Standard Silverlight initialization
			InitializeComponent();

			// Phone-specific initialization
			InitializePhoneApplication();

			TiltEffect.TiltableItems.Add(typeof(Controls.TiltableGrid));
		}

		// Code to execute when the application is launching (eg, from Start)
		// This code will not execute when the application is reactivated
		private void ApplicationLaunching(object sender, LaunchingEventArgs e) {
			Debug.WriteLine("Application Launching");

			LoadSavedAuthentication();
		}

		void LoadSavedAuthentication() {
			if (QuizletAPI.Default.Credentials == null) {
				// Try to load from transient storage.
				object accessToken, userName, expiry;
				PhoneApplicationService.Current.State.TryGetValue("quizletAccessToken", out accessToken);
				PhoneApplicationService.Current.State.TryGetValue("quizletUserName", out userName);
				PhoneApplicationService.Current.State.TryGetValue("quizletExpiry", out expiry);

				var q = new JsonConfiguration();

				if (accessToken is string && userName is string && expiry is DateTime) {
					QuizletAPI.Default.Authenticate(new Credentials((string)accessToken, (string)userName, (DateTime)expiry));
				} else {
					// Not found in transient storage, load from JSON configuration.
					if (Configuration.AccessToken != null && Configuration.UserName != null)
						QuizletAPI.Default.Authenticate(new Credentials(Configuration.AccessToken, Configuration.UserName, Configuration.AccessTokenExpiry));
				}
			}
		}

		// Code to execute when the application is activated (brought to foreground)
		// This code will not execute when the application is first launched
		private void ApplicationActivated(object sender, ActivatedEventArgs e) {
			Debug.WriteLine("Application Activated: IsApplicationInstancePreserved = " + e.IsApplicationInstancePreserved.ToString(CultureInfo.InvariantCulture));	

			// Ensure that application state is restored appropriately
			if (!e.IsApplicationInstancePreserved) {
				LoadSavedAuthentication();

				if (!ViewModel.IsDataLoaded)
					ViewModel.LoadData();
			}
		}

		// Code to execute when the application is deactivated (sent to background)
		// This code will not execute when the application is closing
		private void ApplicationDeactivated(object sender, DeactivatedEventArgs e) {
			Debug.WriteLine("Application Deactivating");

			Configuration.Save();
			ViewModel.SaveData();
		}

		// Code to execute when the application is closing (eg, user hit Back)
		// This code will not execute when the application is deactivated
		private void ApplicationClosing(object sender, ClosingEventArgs e) {
			Debug.WriteLine("Application Closing");

			Configuration.Save();

			ViewModel.SaveData();
			// Ensure that required application state is persisted here.
		}

		// Code to execute if a navigation fails
		private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e) {
			if (Debugger.IsAttached)
				// A navigation has failed; break into the debugger
				Debugger.Break();
		}

		// Code to execute on Unhandled Exceptions
		private static void OnUnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e) {
			if (Debugger.IsAttached)
				// An unhandled exception has occurred; break into the debugger
				Debugger.Break();
		}

		#region Phone application initialization
		// Avoid double-initialization
		private bool phoneApplicationInitialized;

		// Do not add any additional code to this method
		private void InitializePhoneApplication() {
			if (phoneApplicationInitialized)
				return;

			// Create the frame but don't group it as RootVisual yet; this allows the splash
			// screen to remain active until the application is ready to render.
			RootFrame = new TransitionFrame();
			RootFrame.Navigated += CompleteInitializePhoneApplication;

			// Handle navigation failures
			RootFrame.NavigationFailed += OnNavigationFailed;

			// Ensure we don't initialize again
			phoneApplicationInitialized = true;
		}

		// Do not add any additional code to this method
		private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e) {
			// Set the root visual to allow the application to render
			if (RootVisual != RootFrame)
				RootVisual = RootFrame;

			// Remove this handler since it is no longer needed
			RootFrame.Navigated -= CompleteInitializePhoneApplication;
		}
		#endregion
	}
}