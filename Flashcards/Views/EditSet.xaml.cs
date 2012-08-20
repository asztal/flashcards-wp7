using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Flashcards.Model.API;
using Flashcards.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Flashcards.Views {
	[NavigationUri("/Views/EditSet.xaml")]
	public partial class EditSet {
		SetViewModel set;
		bool isNew, passwordChanged, visibilityChanged, editableChanged, groupPermissionsChanged;

		public EditSet() {
			InitializeComponent();

			Loaded += OnLoaded;
		}

		void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
			groupsForPermissions.DataContext = App.ViewModel;
		}

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
			string idString;
			long setID;
			if (!NavigationContext.QueryString.TryGetValue("id", out idString)) {
				isNew = true;
				DataContext = set = new SetViewModel (App.ViewModel) {
					Title = "",
					Author = App.ViewModel.UserName,
					Description = "",
					TermsLoaded = true,
					TermLanguageCode = Configuration.DefaultTermLanguageCode,
					DefinitionLanguageCode = Configuration.DefaultDefinitionLanguageCode
				};
				return;
			}
			
			if (!long.TryParse(idString, out setID)) {
				MessageBox.Show("Error editing group: invalid group ID");
				NavigationService.GoBack();
				return;
			}

			DataContext = set = App.ViewModel.GetSet(setID, true);

			if (set == null) {
				MessageBox.Show("Cannot edit this group: it is not known to this application.");
				NavigationService.GoBack();
				return;
			}

			InitializePermissionsTab();
		}

		private void AddNew(object sender, EventArgs e) {
			if (Pivot.SelectedItem == TermsTab) {
				var term = new TermViewModel();
				set.Terms.Add(term);

				Dispatcher.BeginInvoke(delegate {
					Debug.Assert(TermList.ItemContainerGenerator != null, "TermList.ItemContainerGenerator != null");

					var container = TermList.ItemContainerGenerator.ContainerFromItem(term) as FrameworkElement;
					FocusTextBox(container);
				});
			} else if (Pivot.SelectedItem == SubjectsTab) {
				var subject = new SubjectViewModel();
				set.Subjects.Insert(0, subject);

				Dispatcher.BeginInvoke(delegate {
					Debug.Assert(SubjectList.ItemContainerGenerator != null, "SubjectList.ItemContainerGenerator != null");

					var container = SubjectList.ItemContainerGenerator.ContainerFromItem(subject) as FrameworkElement;
					FocusTextBox(container);
				});
			}
		}

		static void FocusTextBox(DependencyObject container) {
			if (container == null)
				return;

			var tb = GetDescendantByType<TextBox>(container) as TextBox;
			if (tb != null) 
				tb.Focus();
		}

		private void Save(object sender, EventArgs e) {
			if (set.TermLanguageCode == null || set.DefinitionLanguageCode == null) {
				MessageBox.Show("You must select a term and definition language before saving the group.");
				return;
			}

			if (!isNew && set.EditPermissions == SetEditPermissions.Password && !set.HasAccess) {
				AskForPassword();
				return;
			}

			// We must do this otherwise a field that is being edited might not be committed to the view model...
			var focusedTextBox = FocusManager.GetFocusedElement() as TextBox;
			if (focusedTextBox != null) {
				var binding = focusedTextBox.GetBindingExpression(TextBox.TextProperty);
				if (binding != null)
					binding.UpdateSource();
			}

			SystemTray.SetProgressIndicator(this,
				new ProgressIndicator {
					IsIndeterminate = true,
					IsVisible = true,
					Text = isNew ? "Creating group..." : "Saving group..."
				});

			SetVisibility? newVisibility = null;
			if (visibilityChanged || isNew) {
				var item = visibilityPicker.SelectedItem;
				if (item == visibilityEveryone)
					newVisibility = SetVisibility.Public;
				else if (item == visibilityOnlyMe)
					newVisibility = SetVisibility.OnlyMe;
				else if (item == visibilityGroups)
					newVisibility = SetVisibility.Groups;
				else if (item == visibilityPassword)
					newVisibility = SetVisibility.Password;
			}

			// Keep the old password.
			if (!isNew && newVisibility == SetVisibility.Password && set.Visibility == SetVisibility.Password && !passwordChanged)
				newVisibility = null;

			SetEditPermissions? newPermissions = null;
			if (editableChanged || isNew) {
				var item = editablePicker.SelectedItem;
				if (item == editableOnlyMe)
					newPermissions = SetEditPermissions.OnlyMe;
				else if (item == editableGroups)
					newPermissions = SetEditPermissions.Groups;
				else if (item == editablePassword)
					newPermissions = SetEditPermissions.Password;
			}

			if (!isNew && newPermissions == SetEditPermissions.Password && set.EditPermissions == SetEditPermissions.Password && !passwordChanged)
				newVisibility = null;

			string newPassword = newPermissions == SetEditPermissions.Password || newVisibility == SetVisibility.Password
				? password.Password
				: null;

			List<long> groupIDs = null;
			if (newPermissions == SetEditPermissions.Groups || newVisibility == SetVisibility.Groups)
				groupIDs = groupsForPermissions.SelectedItems.Cast<GroupViewModel>().Select(g => g.ID).ToList();

			if (isNew) {
				set.Visibility = newVisibility ?? set.Visibility;
				set.EditPermissions = newPermissions ?? set.EditPermissions;
				App.ViewModel.CreateSet(
					set,
					newPassword,
					groupIDs,
					delegate {
						SystemTray.SetProgressIndicator(this, null);
						NavigationService.Navigate(new Uri("/Views/Set.xaml?id=" + set.ID, UriKind.Relative));
					},
					err => {
						SystemTray.SetProgressIndicator(this, null);
						MessageBox.Show(err.Message);
					});
			} else {
				set.CommitEdits(
					passwordChanged ? password.Password : null,
					newVisibility,
					newPermissions,
					groupIDs,
					() => {
						SystemTray.SetProgressIndicator(this, null);
						NavigationService.GoBack();
					},
					err => {
						SystemTray.SetProgressIndicator(this, null);

						if (err is AccessDeniedException) {
							set.HasAccess = false;

							switch (set.EditPermissions) {
								case SetEditPermissions.OnlyMe:
									MessageBox.Show("You do not have write access to this group. Only the owner of this group can edit it.");
									break;
								case SetEditPermissions.Groups:
									MessageBox.Show("You do not have write access to this group. Only members of certain groups can edit it.");
									break;
								default:
									AskForPassword();
									break;
							}
						} else {
							MessageBox.Show(err.Message);
						}
					});
			}
		}

		void AskForPassword() {
			NavigationService.Navigate(new Uri(string.Format(CultureInfo.InvariantCulture, "/Views/SetPassword.xaml?id={0}", set.ID), UriKind.Relative));
		}

		private void SubjectDeleted(object sender, RoutedEventArgs e) {
			set.Subjects.Remove(((FrameworkElement) sender).DataContext as SubjectViewModel);
		}

		static DependencyObject GetDescendantByType<T>(DependencyObject element) where T : class {
			if (element == null)
				return null;

			if (element is T)
				return element;

			DependencyObject foundElement = null;

			if (element is Control)
				(element as Control).ApplyTemplate();

			for (int i = 0;
				i < VisualTreeHelper.GetChildrenCount(element); i++) {

				var visual = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
				foundElement = GetDescendantByType<T>(visual);

				if (foundElement != null)
					break;
			}

			return foundElement;
		}

		private void DeleteTerm(object sender, RoutedEventArgs e) {
			set.Terms.Remove(((FrameworkElement) sender).DataContext as TermViewModel);
		}

		private void TabChanged(object sender, SelectionChangedEventArgs e) {
			var addButton = ApplicationBar.Buttons[1] as ApplicationBarIconButton;
			if (addButton == null)
				return;

			var selectedItem = ((Pivot) sender).SelectedItem as PivotItem;
			if (selectedItem == TermsTab) {
				addButton.IsEnabled = true;
				addButton.Text = "add term";
			} else if (selectedItem == SubjectsTab) {
				addButton.IsEnabled = true;
				addButton.Text = "add ss";
			} else {
				addButton.IsEnabled = false;
				addButton.Text = "add term/ss";
			}
		}

		private void TabSubjectOnReturn(object sender, KeyEventArgs e) {
			if (e.Key != Key.Enter)
				return;

			var subject = ((FrameworkElement) sender).DataContext as SubjectViewModel;
			if (subject == null)
				return;

			e.Handled = true;
			int row = set.Subjects.IndexOf(subject);
			if (row == set.Subjects.Count - 1) {
				AddNew(sender, e);
			} else {
				TermList.ScrollIntoView(set.Subjects[row]);
				Debug.Assert(SubjectList.ItemContainerGenerator != null, "SubjectList.ItemContainerGenerator != null");
				FocusTextBox(SubjectList.ItemContainerGenerator.ContainerFromIndex(row + 1) as FrameworkElement);
			}
		}

		private void TabTermOnReturn(object sender, KeyEventArgs e) {
			if (e.Key != Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
				return;

			var textBox = (TextBox) sender;
			var term = (TermViewModel) textBox.DataContext;

			e.Handled = true;
			int row = set.Terms.IndexOf(term);
			bool onDefinition = (textBox).Name == "TermDefinition";

			if (row == set.Terms.Count - 1 && onDefinition) {
				AddNew(sender, e);
			} else {
				if (onDefinition) {
					TermList.ScrollIntoView(set.Terms[row]);
					Debug.Assert(TermList.ItemContainerGenerator != null, "TermList.ItemContainerGenerator != null");
					FocusTextBox(TermList.ItemContainerGenerator.ContainerFromIndex(row + 1) as FrameworkElement);
				} else {
					var next = ((FrameworkElement) textBox.Parent).FindName("TermDefinition") as TextBox;
					if (next != null)
						next.Focus();
				}
			}
		}

		void InitializePermissionsTab() {
			switch (set.Visibility) {
				case SetVisibility.Public: visibilityPicker.SelectedItem = visibilityEveryone; break;
				case SetVisibility.OnlyMe: visibilityPicker.SelectedItem = visibilityOnlyMe; break;
				case SetVisibility.Groups: visibilityPicker.SelectedItem = visibilityGroups; break;
				case SetVisibility.Password: visibilityPicker.SelectedItem = visibilityPassword; break;
			}

			switch (set.EditPermissions) {
				case SetEditPermissions.OnlyMe: editablePicker.SelectedItem = editableOnlyMe; break;
				case SetEditPermissions.Groups: editablePicker.SelectedItem = editableGroups; break;
				case SetEditPermissions.Password: editablePicker.SelectedItem = editablePassword; break;
			}

			UpdatePermissionsTab();
		}

		void UpdatePermissionsTab() {
			if (visibilityPicker == null)
				return;

			bool showPasswordField = visibilityPicker.SelectedItem == visibilityPassword || editablePicker.SelectedItem == editablePassword;
			bool showGroups = visibilityPicker.SelectedItem == visibilityGroups || editablePicker.SelectedItem == editableGroups;

			passwordRow.Height = showPasswordField ? GridLength.Auto : new GridLength(0);
			groupsHeaderRow.Height = showGroups ? GridLength.Auto : new GridLength(0);
			groupsForPermissions.Visibility = showGroups ? Visibility.Visible : Visibility.Collapsed;
		}

		private void PermissionSelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (sender == visibilityPicker)
				visibilityChanged = true;
			else 
				editableChanged = true;
			UpdatePermissionsTab();
		}

		private void PasswordChanged(object sender, RoutedEventArgs e) {
			passwordChanged = true;
		}

		private void GroupsPermissionsChanged(object sender, SelectionChangedEventArgs e) {
			groupPermissionsChanged = true;
		}
	}
}