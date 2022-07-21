using LibDat2;
using LibDat2.Types;
using LibGGPK2.Records;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace VisualGGPK2 {
	public partial class MainWindow {
		/// <summary>
		/// Mark the free spaces in data section of dat files
		/// </summary>
		private readonly List<int> toMark = new();
		private void OnCellLoaded(object sender, RoutedEventArgs e) {
			var dc = (DataGridCell)sender;
			var border = (Border)VisualTreeHelper.GetChild(dc, 0);
			var row = DataGridRow.GetRowContainingElement(dc).GetIndex();
			var col = dc.Column.DisplayIndex;
			if (col == 0 && toMark.Contains(row) || col == 2 && toMark.Contains(row + 1)) {
				border.Background = Brushes.Red;
				border.BorderThickness = new Thickness(0);
			}
		}

		/// <summary>
		/// Make changes to DatContainer after editing <see cref="DatTable"/>
		/// </summary>
		private void OnDatTableCellEdit(object sender, DataGridCellEditEndingEventArgs e) {
			if (e.EditAction == DataGridEditAction.Commit) {
				var newText = ((TextBox)e.EditingElement).Text;
				var eo = e.Row.Item as IDictionary<string, object>; // ExpandoObject
				var fd = (IFieldData)eo[(string)e.Column.Header];

				DatTable.CancelEdit();

				if (newText == fd.ToString()) // newText == oldText
					return;

				var dat = DatTable.Tag as DatContainer;
				var row = (int)eo["Row"];
				var index = e.Column.DisplayIndex - 1; // -1 For added Row column

				try {
					if (fd is IReferenceData rd) {
						rd = rd switch {
							StringData => StringData.FromString(newText, dat),
							IArrayData => IArrayData.FromString(newText, ((IArrayData)rd).TypeOfValue, dat),
							_ => throw new InvalidCastException("Unknown data: " + rd)
						};
						eo[(string)e.Column.Header] = dat.FieldDatas[row][index] = rd;
						var oc = (ObservableCollection<IReferenceData>)DatReferenceDataTable.ItemsSource;
						if (!oc.Contains(rd))
							oc.Add(rd);
					} else
						eo[(string)e.Column.Header] = dat.FieldDatas[row][index] = fd.FromString(newText);
				} catch (Exception ex) {
					MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		/// <summary>
		/// Make changes to DatContainer after editing <see cref="DatReferenceDataTable"/>
		/// </summary>
		private void OnDatReferenceDataTableCellEdit(object sender, DataGridCellEditEndingEventArgs e) {
			if (e.EditAction == DataGridEditAction.Commit) {
				var newText = ((TextBox)e.EditingElement).Text;
				var rd = (IReferenceData)e.Row.Item;

				DatReferenceDataTable.CancelEdit();

				if (newText == rd.ToString()) // newText == oldText
					return;

				try {
					rd.FromString(newText);
				} catch (InvalidCastException ex) {
					MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private DataGridLength dataGridLength = new(1.0, DataGridLengthUnitType.Auto);
		/// <summary>
		/// Show dat file on <see cref="DatView"/>
		/// </summary>
		private void ShowDatFile(DatContainer dat) {
			toMark.Clear();
			DatTable.Tag = dat;
			DatTable.Columns.Clear();
			DatTable.ItemsSource = null;
			var eos = new List<ExpandoObject>(dat.FieldDefinitions.Count);
			for (var i = 0; i < dat.FieldDatas.Count; i++) {
				var eo = new ExpandoObject() as IDictionary<string, object>;
				eo.Add("Row", i);
				var names = dat.FieldDefinitions.Select(t => t.Key).GetEnumerator();
				var values = dat.FieldDatas[i];
				if (values != null)
					foreach (var value in values) {
						names.MoveNext();
						eo.Add(names.Current, value);
					}
				names.Dispose();
				eos.Add((ExpandoObject)eo);
			}

			DatTable.Columns.Add(new DataGridTextColumn {
				Header = "Row",
				Binding = new Binding("Row") { Mode = BindingMode.OneTime },
				Width = dataGridLength,
				IsReadOnly = true
			});

			foreach (var (col, type) in dat.FieldDefinitions)
				DatTable.Columns.Add(new DataGridTextColumn {
					Header = col,
					Binding = new Binding(col + ".StringValue") { TargetNullValue = "{null}", Mode = BindingMode.TwoWay },
					Width = dataGridLength,
					IsReadOnly = type == "array" // Field of this type must be empty array
				});

			DatTable.ItemsSource = new ObservableCollection<ExpandoObject>(eos);

			var lastEndOffset = 8L;
			var row = 0;
			var pointedList = new ObservableCollection<IReferenceData>(dat.ReferenceDatas.Values);
			foreach (var rd in pointedList) {
				if (rd.Offset != lastEndOffset)
					toMark.Add(row);
				lastEndOffset = rd.EndOffset;
				++row;
			}
			
			DatReferenceDataTable.Columns.Clear();
			DatReferenceDataTable.ItemsSource = null;
			DatReferenceDataTable.Columns.Add(new DataGridTextColumn {
				Header = "Offset",
				Binding = new Binding("Offset") { Mode = BindingMode.OneWay },
				Width = dataGridLength,
				IsReadOnly = true
			});
			DatReferenceDataTable.Columns.Add(new DataGridTextColumn {
				Header = "Length",
				Binding = new Binding("Length") { Mode = BindingMode.OneWay },
				Width = dataGridLength,
				IsReadOnly = true
			});
			DatReferenceDataTable.Columns.Add(new DataGridTextColumn {
				Header = "EndOffset",
				Binding = new Binding("EndOffset") { Mode = BindingMode.OneWay },
				Width = dataGridLength,
				IsReadOnly = true
			});
			DatReferenceDataTable.Columns.Add(new DataGridTextColumn {
				Header = "Value",
				Binding = new Binding("StringValue") { TargetNullValue = "{null}", Mode = BindingMode.TwoWay },
				Width = dataGridLength
			});

			DatReferenceDataTable.ItemsSource = pointedList;

			if (dat.Exception != null)
				MessageBox.Show(this, dat.Exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		/// <summary>
		/// On "Save" in <see cref="DatView"/> clicked
		/// </summary>
		private void OnSaveDatClicked(object sender, RoutedEventArgs e) {
			var dat = DatTable.Tag as DatContainer;
			var rtn = (RecordTreeNode)((TreeViewItem)Tree.SelectedItem).Tag;
			bool x64, UTF32;
			switch (Path.GetExtension(rtn.Name)) {
				case ".dat":
					x64 = false;
					UTF32 = false;
					break;
				case ".dat64":
					x64 = true;
					UTF32 = false;
					break;
				case ".datl":
					x64 = false;
					UTF32 = true;
					break;
				case ".datl64":
					x64 = true;
					UTF32 = true;
					break;
				default:
					throw new("Unknown file extension for dat file: " + rtn.Name);
			}
			((IFileRecord)rtn).ReplaceContent(dat.Save(x64, UTF32));
			MessageBox.Show(this, "Saved changes to " + rtn.GetPath(), "Done", MessageBoxButton.OK, MessageBoxImage.Information);
			ShowDatFile(dat);
		}

		/// <summary>
		/// On "Reload DatDefinitions" clicked
		/// </summary>
		private void OnReloadClicked(object sender, RoutedEventArgs e) {
			try {
				DatContainer.ReloadDefinitions();
				OnTreeSelectedChanged(null, null);
			} catch (Exception ex) {
				MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// On "Export to .csv" clicked
		/// </summary>
		private void OnCSVClicked(object sender, RoutedEventArgs e) {
			var dat = DatTable.Tag as DatContainer;
			var sfd = new SaveFileDialog() {
				FileName = dat.Name + ".csv",
				DefaultExt = "csv",
				Filter = "*.csv|*.csv"
			};
			if (sfd.ShowDialog() != true)
				return;
			File.WriteAllText(sfd.FileName, dat.ToCsv());
			MessageBox.Show(this, $"Exported " + sfd.FileName, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		/// <summary>
		/// On "Import from .csv" clicked
		/// </summary>
		private void OnImportClicked(object sender, RoutedEventArgs e) {
			var dat = DatTable.Tag as DatContainer;
			var ofd = new OpenFileDialog() {
				FileName = dat.Name + ".csv",
				DefaultExt = "csv",
				Filter = "*.csv|*.csv|*.*|*.*"
			};
			if (ofd.ShowDialog() != true)
				return;
			try {
				dat.FromCsv(File.ReadAllText(ofd.FileName));
				ShowDatFile(dat);
				MessageBox.Show(this, $"Imported from " + ofd.FileName, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
			} catch (Exception ex) {
				MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// On "Use schema.min.json" (un)checked
		/// </summary>
		private void OnSchemaMinChecked(object sender, RoutedEventArgs e) {
			OnTreeSelectedChanged(null, null);
		}
	}
}