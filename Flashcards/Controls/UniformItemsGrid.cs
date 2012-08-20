using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Flashcards.Controls {
	public class ItemContainer : ContentPresenter { }

	public class UniformItemsGrid : ItemsControl {
		static UniformItemsGrid() {
			RowsProperty = DependencyProperty.Register("Rows", typeof(int), typeof(UniformItemsGrid), new PropertyMetadata(2, OnRowsOrColumnsChanged));
			ColumnsProperty = DependencyProperty.Register("Columns", typeof(int), typeof(UniformItemsGrid), new PropertyMetadata(4, OnRowsOrColumnsChanged));
			OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(UniformItemsGrid), new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));
			ItemContainerStyleProperty = DependencyProperty.Register("ItemContainerStyle", typeof(Style), typeof(UniformItemsGrid), new PropertyMetadata(null, OnItemContainerStyleChanged));
			ExpandDirectionProperty = DependencyProperty.Register("ExpandDirection", typeof(Orientation), typeof(UniformItemsGrid), new PropertyMetadata(Orientation.Vertical, OnExpandChanged));
			ExpandProperty = DependencyProperty.Register("Expand", typeof(bool), typeof(UniformItemsGrid), new PropertyMetadata(false, OnExpandChanged));
		}

		public UniformItemsGrid() {
			DefaultStyleKey = typeof(UniformItemsGrid);
		}

		#region Dependency Properties
		public Orientation ExpandDirection {
			get { return (Orientation)GetValue(ExpandDirectionProperty); }
			set { SetValue(ExpandDirectionProperty, value); }
		}

		public bool Expand {
			get { return (bool) GetValue(ExpandProperty); }
			set { SetValue(ExpandProperty, value); }
		}

		static void OnExpandChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			// Arrange first, because arranging figures out what the right item count is.
			((UniformItemsGrid) o).ArrangePanel();
			((UniformItemsGrid) o).UpdateMeasure();
		}

		public int Rows {
			get { return (int)GetValue(RowsProperty); }
			set { SetValue(RowsProperty, value); }
		}

		public int Columns {
			get { return (int)GetValue(ColumnsProperty); }
			set { SetValue(ColumnsProperty, value); }
		}

		static void OnRowsOrColumnsChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			((UniformItemsGrid)o).UpdateMeasure();
			((UniformItemsGrid) o).ArrangePanel();
		}

		public Orientation Orientation {
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		static void OnOrientationChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			if (e.OldValue != e.NewValue)
				((UniformItemsGrid) o).ArrangePanel();
		}

		public Style ItemContainerStyle {
			get { return (Style)GetValue(ItemContainerStyleProperty); }
			set { SetValue(ItemContainerStyleProperty, value); }
		}

		public static readonly DependencyProperty ColumnsProperty, RowsProperty, OrientationProperty, ItemContainerStyleProperty, ExpandProperty, ExpandDirectionProperty;

		static void OnItemContainerStyleChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			((UniformItemsGrid) o).OnItemContainerStyleChanged(e.OldValue as Style, e.NewValue as Style);
		}

		void OnItemContainerStyleChanged(Style oldStyle, Style newStyle) {
			if (oldStyle == newStyle)
				return;

			foreach (var container in Items.Select(GetItemContainerForObject)) {
				if (container != null && (container.Style != null || container.Style == oldStyle)) {
					container.Style = newStyle;
				}
			}
		}
		#endregion

		#region Arrangement and Measurement
		protected void UpdateMeasure() {
			if (itemsPresenter == null)
				// Probably called UpdateMeasure due to setting Rows/Columns from XAML
				return;

			int ss = VisualTreeHelper.GetChildrenCount(itemsPresenter);

			for (int i = 0; i < ss; i++) {
				var g = VisualTreeHelper.GetChild(itemsPresenter, i) as Grid;
				if (g == null) continue;

				g.RowDefinitions.Clear();
				for (int r = 0; r < Rows; r++)
					g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

				g.ColumnDefinitions.Clear();
				for (int c = 0; c < Columns; c++)
					g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			}
		}

		protected void ArrangePanel() {
			foreach (var container in Items.Select(GetItemContainerForObject))
				if (container != null)
					ArrangeContainer(container);
		}

		protected void ArrangeContainer(ItemContainer container) {
			Debug.Assert(ItemContainerGenerator != null, "ItemContainerGenerator != null");

			int index = ItemContainerGenerator.IndexFromContainer(container);
			int row, column;
			GetRowAndColumnForIndex(index, out row, out column);

			bool visible = row < Rows && column < Columns;

			// TODO: Un-expand when items are removed.
			// (To be done in ClearContainerForItemOverride, probably)
			if (visible) {
				container.Visibility = Visibility.Visible;
			} else {
				if (Expand) {
					container.Visibility = Visibility.Visible;

					if (ExpandDirection == Orientation.Vertical)
						//Rows = Math.Max(Rows, row + 1);
						Rows++;
					else
						//Columns = Math.Max(Columns, column + 1);
						Columns++;
					
					// Return, since increasing the rows/column count arranges the whole
					// panel anyway.
					return;
				} else {
					container.Visibility = Visibility.Collapsed;
				}
			}

			container.SetValue(Grid.ColumnProperty, column);
			container.SetValue(Grid.RowProperty, row);
		}

		protected void GetRowAndColumnForIndex(int index, out int row, out int column) {
			if (Orientation == Orientation.Horizontal) {
				row = index / Columns;
				column = index % Columns;
			} else {
				column = index / Rows;
				row = index % Rows;
			}
		}
		#endregion

		#region Item Containers 
		protected ItemContainer GetItemContainerForObject(object item) {
			Debug.Assert(ItemContainerGenerator != null, "ItemContainerGenerator != null");
			return (ItemContainerGenerator.ContainerFromItem(item) ?? item) as ItemContainer;
		}

		protected override DependencyObject GetContainerForItemOverride() {
			var container = new ItemContainer();

			if (ItemContainerStyle != null)
				container.Style = ItemContainerStyle;

			return container;
		}

		protected override bool IsItemItsOwnContainerOverride(object item) {
			return item is ItemContainer;
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item) {
			base.PrepareContainerForItemOverride(element, item);

			Debug.Assert(ItemContainerGenerator != null, "ItemContainerGenerator != null");

			var container = (ItemContainer) element;
			int index = ItemContainerGenerator.IndexFromContainer(element);

			if (index == Items.Count - 1)
				// This is a new item at the end of the list: just add it normally.
				ArrangeContainer(container);
			else
				// Just re-arrange everything.
				ArrangePanel();

			if (ItemContainerStyle != null && container.Style == null)
				container.Style = ItemContainerStyle;
		}

		protected override void ClearContainerForItemOverride(DependencyObject element, object item) {
			base.ClearContainerForItemOverride(element, item);

			var container = (ItemContainer)element;
			if (container != item) {
				container.Content = null;
				container.Style = null;
				container.ContentTemplate = null;
			}
			
			int removedRow = (int)container.GetValue(Grid.RowProperty),
				removedColumn = (int)container.GetValue(Grid.ColumnProperty);

			int lastRow, lastColumn;
			GetRowAndColumnForIndex(Items.Count, out lastRow, out lastColumn);

			// If we just removed the last item in the grid, so we don't need to re-arrange any other items.
			// But we do need to possibly decrease the size of the grid.
			if (removedRow == lastRow && removedColumn == lastColumn) {
				if (ExpandDirection == Orientation.Vertical && removedRow == Rows - 1 && removedColumn == 0)
					Rows--;
				else if (ExpandDirection == Orientation.Horizontal && removedColumn == Columns - 1 && removedRow == 0)
					Columns--;
				return;
			}

			ArrangePanel();
		}
		#endregion

		#region Template
		ItemsPresenter itemsPresenter;
		
		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			itemsPresenter = (ItemsPresenter) GetTemplateChild("ItemsPresenter");
			itemsPresenter.LayoutUpdated += ItemsPresenterLayoutUpdated;

		}

		void ItemsPresenterLayoutUpdated(object sender, EventArgs e) {
			if (itemsPresenter != null) 
				itemsPresenter.LayoutUpdated -= ItemsPresenterLayoutUpdated;
			itemsPresenter = (ItemsPresenter) GetTemplateChild("ItemsPresenter");
			UpdateMeasure();
		}
		#endregion
	}
}
