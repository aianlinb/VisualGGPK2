using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using LibDat2;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Dynamic;
using System.Linq;
using System.Windows.Data;
using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using LibGGPK2.Records;
using System.IO;

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
		private void OnCellEdit(object sender, DataGridCellEditEndingEventArgs e) {
			if (e.EditAction == DataGridEditAction.Commit) {
				var dat = DatTable.Tag as DatContainer;
				var newText = ((TextBox)e.EditingElement).Text;
				var eo = e.Row.Item as IDictionary<string, object>; // ExpandoObject
				var row = (int)eo["Row"];
				var index = e.Column.DisplayIndex - 1; // -1 For added Row column
				var type = dat.FieldDefinitions[index].Item2;
				try {
					if (type.StartsWith("array|"))
						eo[(string)e.Column.Header] = ArrayToString((object[])(dat.FieldDatas[row][index] = StringToData(newText, type)));
					else
						eo[(string)e.Column.Header] = dat.FieldDatas[row][index] = StringToData(newText, type);
				} catch (InvalidCastException ex) {
					MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					DatTable.CancelEdit();
				}
			}
		}

		/// <summary>
		/// Convert contents of an array to a string
		/// </summary>
		public static string ArrayToString(object[] value) {
			var s = new StringBuilder("[");
			foreach (var f in value) {
				s.Append(f ?? "{null}");
				s.Append(", ");
			}
			if (s.Length > 2)
				s.Remove(s.Length - 2, 2);
			s.Append(']');
			return s.ToString();
		}

		/// <summary>
		/// Convert a string to the corresponding dat type of dat
		/// </summary>
		public static object StringToData(string value, string type) {
			switch (type) {
				case "bool":
					if (bool.TryParse(value, out var b))
						return b;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to Boolean");
				case "i32":
					if (int.TryParse(value, out var i))
						return i;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to Int32");
				case "i8":
					if (sbyte.TryParse(value, out var sby))
						return sby;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to SByte");
				case "i64":
					if (long.TryParse(value, out var l))
						return l;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to Int64");
				case "i16":
					if (short.TryParse(value, out var s))
						return s;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to Int16");
				case "u32":
					if (uint.TryParse(value, out var ui))
						return ui;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to UInt32");
				case "u8":
					if (byte.TryParse(value, out var by))
						return by;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to Byte");
				case "u64":
					if (ulong.TryParse(value, out var ul))
						return ul;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to UInt64");
				case "u16":
					if (ushort.TryParse(value, out var us))
						return us;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to UInt16");
				case "f32":
					if (float.TryParse(value, out var f))
						return f;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to Float");
				case "f64":
					if (double.TryParse(value, out var d))
						return d;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to Double");
				case "foreignrow":
					var k = KeyType.FromString(value);
					if (k != null && k.Foreign)
						return k;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to ForeignKeyType(foreignrow)");
				case "row":
					var k2 = KeyType.FromString(value);
					if (k2 != null && !k2.Foreign)
						return k2;
					else
						throw new InvalidCastException("\"" + value + "\" cannot be convert to ForeignKeyType(foreignrow)");
				case "string":
				case "valueString":
					return value;
				default:
					if (type.StartsWith("array|")) {
						var value2 = Regex.Replace(value, @"\s", "");
						if (!value2.StartsWith('[') || !value2.EndsWith(']'))
							throw new InvalidCastException("\"" + value + "\" cannot be convert to an array");
						type = type[6..];
						if (type == "foreignrow") { // String of foreignrow also has ","
							value2 = value2[1..^1]; // Trim '[' ']'
							if (!value2.StartsWith('<') || !value2.EndsWith('>'))
								throw new InvalidCastException("\"" + value + "\" cannot be convert to an array of ForeignKeyType(foreignrow)");
							var sarray = value2[1..^1].Split(">,<"); // Trim '<' '>'
							var array = new object[sarray.Length];
							for (var n = 0; n < sarray.Length; ++n)
								array[n] = StringToData("<" + sarray[n] + ">", type);
							return array;
						} else {
							var sarray = value2[1..^1].Split(','); // Trim '[' ']'
							var array = new object[sarray.Length];
							for (var n = 0; n < sarray.Length; ++n)
								array[n] = StringToData(sarray[n], type);
							return array;
						}
					} else
						throw new InvalidCastException($"Unknown Type: {type}");
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
			var eos = new List<ExpandoObject>(dat.FieldDefinitions.Count);
			for (var i = 0; i < dat.FieldDatas.Count; i++) {
				var eo = new ExpandoObject() as IDictionary<string, object>;
				eo.Add("Row", i);
				foreach (var (name, value) in (dat.FieldDefinitions.Select(t => t.Item1), dat.FieldDatas[i]))
					if (value is object[] arr)
						eo.Add((string)name, ArrayToString(arr));
					else
						eo.Add((string)name, value);
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
					Binding = new Binding(col) { TargetNullValue = "{null}", Mode = BindingMode.OneTime },
					Width = dataGridLength,
					IsReadOnly = type == "array" // Field of this type must be empty array
				});

			DatTable.ItemsSource = eos;

			var lastEndOffset = 8L;
			var row = 0;
			var pointedList = new List<PointedValue>(dat.PointedDatas.Values);
			for (var i = 0; i < pointedList.Count; ++i) {
				if (pointedList[i].Offset != lastEndOffset)
					toMark.Add(row);
				lastEndOffset = pointedList[i].EndOffset;
				++row;

				if (pointedList[i].Value is object[] arr)
					pointedList[i] = new(pointedList[i].Offset, pointedList[i].Length, ArrayToString(arr));
			}
			DatPointedTable.ItemsSource = pointedList;

			if (dat.FirstError.HasValue)
				MessageBox.Show($"At Row:{dat.FirstError.Value.Row},\r\nColumn:{dat.FirstError.Value.Column} ({dat.FirstError.Value.FieldName}),\r\nStreamPosition:{dat.FirstError.Value.StreamPosition},\r\nLastSucceededPosition:{dat.FirstError.Value.LastSucceededPosition}\r\n\r\n{dat.FirstError.Value.Exception}", "Error While Reading: " + dat.Name, MessageBoxButton.OK, MessageBoxImage.Error);
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
			MessageBox.Show("Saved changes to " + rtn.GetPath(), "Done", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		/// <summary>
		/// On "Reload DatDefinitions" clicked
		/// </summary>
		private void OnReloadClicked(object sender, RoutedEventArgs e) {
			try {
				DatContainer.ReloadDefinitions();
				OnTreeSelectedChanged(null, null);
			} catch (Exception ex) {
				MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// On "Export to .csv" clicked
		/// </summary>
		private void OnCSVClicked(object sender, RoutedEventArgs e) {
			var dat = DatTable.Tag as DatContainer;
			var sfd = new SaveFileDialog() {
				FileName = dat.Name + ".csv",
				DefaultExt = "csv"
			};
			if (sfd.ShowDialog() != true)
				return;
			File.WriteAllText(sfd.FileName, dat.ToCsv());
			MessageBox.Show($"Exported " + sfd.FileName, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}

	/// <summary>
	/// For using foreach on a tuple
	/// </summary>
	public static class GetTupleEnumerator {
		/// <summary>
		/// For using foreach in tuple
		/// </summary>
		public static IEnumerator<(T, T)> GetEnumerator<T>(this (IEnumerable<T>, IEnumerable<T>) TupleEnumerable) => new TupleEnumerator<T>(TupleEnumerable);

		public class TupleEnumerator<T> : ITuple, IEnumerator<(T, T)> {
			public IEnumerator<T> Item1;

			public IEnumerator<T> Item2;

			public TupleEnumerator((IEnumerable<T>, IEnumerable<T>) TupleEnumerable) {
				Item1 = TupleEnumerable.Item1?.GetEnumerator();
				Item2 = TupleEnumerable.Item2?.GetEnumerator();
			}

			public (T, T) Current => (Item1 == null ? default : Item1.Current, Item2 == null ? default : Item2.Current);

			object IEnumerator.Current => Current;

			(T, T) IEnumerator<(T, T)>.Current => Current;

			public int Length => 2;

			public object this[int index] => index switch {
				1 => Item1,
				2 => Item2,
				_ => throw new IndexOutOfRangeException()
			};

			public bool MoveNext() {
				return Item1?.MoveNext() == true | Item2?.MoveNext() == true;
			}

			public void Reset() {
				Item1?.Reset();
				Item2?.Reset();
			}

#pragma warning disable CA1816 // Dispose 方法應該呼叫 SuppressFinalize
			public void Dispose() {
				Item1?.Dispose();
				Item2?.Dispose();
			}
		}
	}
}