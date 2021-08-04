using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LibDat2.Types;

namespace LibDat2 {
	public class DatContainer {
		/// <summary>
		/// Structure definition of dat files
		/// </summary>
		public static JsonElement DatDefinitions;

		/// <summary>
		/// Old format of DatDefinitions before v0.11.4
		/// Used when getting error with <see cref="DatDefinitions"/>
		/// </summary>
		public static JsonElement OldDatDefinitions;

		/// <summary>
		/// Whether to read DatDefinitions_extra.json successfully
		/// </summary>
		public static bool HasOldDatDefinitions;

		/// <summary>
		/// The dat name in this list will force use <see cref="OldDatDefinitions"/>
		/// </summary>
		public static HashSet<string> ForceUseOldDatDefinitionsList = new(0);

		/// <summary>
		/// Whether the data is read with <see cref="OldDatDefinitions"/>
		/// </summary>
		public bool FromOldDefinition;

		/// <summary>
		/// Call <see cref="ReloadDefinitions"/>
		/// </summary>
		static DatContainer() {
			ReloadDefinitions();
		}

		/// <summary>
		/// Name of the dat file
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// Definition of fields in this dat
		/// Left: Name of field, Right: Type of field data
		/// </summary>
		public readonly ReadOnlyCollection<(string, string)> FieldDefinitions;
		/// <summary>
		/// List of record content of the dat file
		/// </summary>
		public List<IFieldData[]> FieldDatas;

		/// <summary>
		/// See <see cref="FirstError"/>
		/// </summary>
		public struct ErrorStruct {
			public Exception Exception;
			public int Row;
			public int Column;
			public string FieldName;
			public long StreamPosition;
			public long LastSucceededPosition;
		}

		/// <summary>
		/// Store the first error in the read process
		/// </summary>
		public readonly ErrorStruct? FirstError;

		/// <summary>
		/// Used to dispose the FileStream when calling <see cref="DatContainer(string)"/>
		/// </summary>
		private static FileStream tmp;

		/// <summary>
		/// Parses the dat file contents from a file
		/// </summary>
		/// <param name="filePath">Path of a dat file</param>
		public DatContainer(string filePath) : this(tmp = File.OpenRead(filePath), filePath) {
			tmp.Close();
			tmp = null;
		}

		/// <summary>
		/// Parses the dat file contents from a binary data
		/// </summary>
		/// <param name="fileData">Binary data of a dat file</param>
		public DatContainer(byte[] fileData, string fileName) : this(new MemoryStream(fileData), fileName) { }

		/// <summary>
		/// Parses the dat file contents from a stream
		/// </summary>
		/// <param name="stream">Contents of a dat file</param>
		/// <param name="fileName">Name of the dat file</param>
		public DatContainer(Stream stream, string fileName) {
			switch (Path.GetExtension(fileName)) {
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
					throw new ArgumentException("The provided file must be a dat file", nameof(fileName));
			}

			Name = Path.GetFileNameWithoutExtension(fileName);
			var Length = stream.Length;

			var reader = new BinaryReader(stream, UTF32 ? Encoding.UTF32 : Encoding.Unicode);

			var Count = reader.ReadInt32();

			var fields = new List<(string, string)>();
			var forceOld = ForceUseOldDatDefinitionsList.Contains(Name);
			if (forceOld) {
				// --- Begin  Old DatDefinitions ---
				if (!HasOldDatDefinitions)
					throw new KeyNotFoundException(Name + " was not defined in Old DatDefinition");
				fields = new();
				try {
					foreach (var field in OldDatDefinitions.GetProperty(Name).EnumerateObject())
						fields.Add((field.Name, ToNewType(field.Value.GetString())));
					FieldDefinitions = new(fields);
				} catch (Exception ex) {
					throw new("Unable to read Old DatDefinition for " + fileName, ex);
				}
				// --- End  Old DatDefinitions ---
			} else
				try {
					var definition = DatDefinitions.GetProperty("tables").EnumerateArray().First(o => o.GetProperty("name").GetString() == Name);
					var unknownCount = 0;
					foreach (var field in definition.GetProperty("columns").EnumerateArray()) {
						var s = field.GetProperty("name").ToString();
						if (string.IsNullOrEmpty(s))
							s = "Unknown" + unknownCount++.ToString();
						fields.Add((s, field.GetProperty("array").GetBoolean() ? "array|" + field.GetProperty("type").GetString() : field.GetProperty("type").GetString()));
					}
				} catch (InvalidOperationException) {
					// --- Begin  Old DatDefinitions ---
					if (!HasOldDatDefinitions)
						throw new KeyNotFoundException(Name + " was not defined");
					fields = new();
					try {
						foreach (var field in OldDatDefinitions.GetProperty(Name).EnumerateObject())
							fields.Add((field.Name, ToNewType(field.Value.GetString())));
						FieldDefinitions = new(fields);
						forceOld = true;
					// --- End  Old DatDefinitions ---
					} catch (KeyNotFoundException) { // Unable to find definition with both two files
						throw new KeyNotFoundException(Name + " was not defined");
					}
				}
			FieldDefinitions = new(fields);

			if (Path.GetFileNameWithoutExtension(fileName) == "Languages")
				UTF32 = true;
			else {
				var actualRecordLength = GetActualRecordLength(reader, Count, Length);
				if (actualRecordLength < 0)
					throw new($"{fileName} : Missing magic number after records");
				DataSectionOffset = Count * actualRecordLength + 4;
				// DataSectionDataLength = Length - DataSectionOffset - 8;

				var recordLength = CalculateRecordLength(FieldDefinitions.Select(t => t.Item2), x64);
				if (recordLength != actualRecordLength) {
					// --- Begin  Old DatDefinitions ---
					if (!HasOldDatDefinitions)
						throw new($"{fileName} : Actual record length: {actualRecordLength} is not equal to that defined in DatDefinitions: {recordLength}");
					fields = new();
					try {
						foreach (var field in OldDatDefinitions.GetProperty(Name).EnumerateObject())
							fields.Add((field.Name, ToNewType(field.Value.GetString())));

						var tmpRecordLength = CalculateRecordLength(fields.Select(t => t.Item2), x64);

						if (tmpRecordLength != actualRecordLength) // Old DatDefinitions can't also match
							throw new($"{fileName} : Actual record length: {actualRecordLength} is not equal to that defined in DatDefinitions: {recordLength}\n\n(Record length from DatDefinitions_extra.json: {tmpRecordLength})");

						FieldDefinitions = new(fields);
						forceOld = true;
						// --- End  Old DatDefinitions ---
					} catch (KeyNotFoundException) { // Not defined in Old DatDefinitions
						throw new($"{fileName} : Actual record length: {actualRecordLength} is not equal to that defined in DatDefinitions: {recordLength}");
					}
				}

				reader.BaseStream.Seek(4, SeekOrigin.Begin);
			}

			List<IFieldData[]> tmpFieldDatas = null;
			var tmpPointedDatas = ReferenceDatas;
			IEnumerable<(string, string)> tmpFieldDefinitions = FieldDefinitions;
		ReadingType:
			FieldDatas = new(Count);
			var error = false;
			var lastPos = reader.BaseStream.Position;
			for (var i = 0; i < Count; ++i) {
				if (error) {
					FieldDatas.Add(null);
					continue;
				}
				var array = new IFieldData[FieldDefinitions.Count];
				var index = 0;
				foreach (var type in FieldDefinitions.Select(t => t.Item2)) {
					try {
						if (type.StartsWith("array|"))
							array[index++] = IArrayData.Read(reader, IFieldData.TypeFromString[type[6..]], this);
						else
							array[index++] = IFieldData.Read(reader, IFieldData.TypeFromString[type], this);
						lastPos = reader.BaseStream.Position;
					} catch (Exception ex) {
						if (!FromOldDefinition)
							FirstError = new ErrorStruct {
								Exception = ex,
								Row = i,
								Column = index - 1,
								FieldName = FieldDefinitions[index - 1].Item1,
								StreamPosition = reader.BaseStream.Position,
								LastSucceededPosition = lastPos
							};
						// --- Begin  Old DatDefinitions ---
						if (!forceOld && !FromOldDefinition && (FromOldDefinition = HasOldDatDefinitions)) {
							fields = new();
							try {
								foreach (var field in OldDatDefinitions.GetProperty(Name).EnumerateObject())
									fields.Add((field.Name, ToNewType(field.Value.GetString())));
								tmpFieldDefinitions = fields;
								FieldDatas.Add(array);
								tmpFieldDatas = FieldDatas; // Save current datas
								ReferenceDatas = new();
								goto ReadingType;
							} catch {
								FromOldDefinition = false;
							}
						}
						if (FromOldDefinition) { // Recovery to the original datas loaded before loading OldDefinition
							FieldDatas = tmpFieldDatas;
							ReferenceDatas = tmpPointedDatas;
						}
						// --- End  Old DatDefinitions ---
						error = true;
						FromOldDefinition = false;
						break;
					}
				}
				if (!error)
					FieldDatas.Add(array);
			}
			if (!error) {
				FirstError = null;
				if (FromOldDefinition)
					FieldDefinitions = new((List<(string, string)>)tmpFieldDefinitions);
			}
			if (forceOld)
				FromOldDefinition = true;

			CurrentOffset = ReferenceDatas.Values.ElementAt(ReferenceDatas.Count - 1).EndOffset;
			reader.BaseStream.Seek(DataSectionOffset + CurrentOffset, SeekOrigin.Begin);
		}

		/// <summary>
		/// Convert type from Old Definitions to the new format
		/// </summary>
		/// <param name="type">Type of old format</param>
		/// <returns>Type of new format</returns>
		public static string ToNewType(string type) {
			if (type == "ref|foreignkey")
				return "foreignrow";
			if (type == "ref|string")
				return "string";
			if (type.StartsWith("ref|list|"))
				return "array|" + ToNewType(type[9..]);
			if (type.StartsWith("ref|"))
				return "row";
			return type switch {
				"bool" => "bool",
				"byte" => "i8",
				"short" => "i16",
				"ushort" => "u16",
				"int" => "i32",
				"uint" => "u32",
				"long" => "i64",
				"ulong" => "u64",
				"float" => "f32",
				"double" => "f64",
				"string" => "valueString",
				_ => type
			};
		}

		// Dependents on the last read/saved dat file
		#region FileDependent
		/// <summary>
		/// Set of IReferenceData of the dat file last read/save
		/// </summary>
		public SortedDictionary<long, IReferenceData> ReferenceDatas = new();
		/// <summary>
		/// Used to find IReferenceData with the actual data in string representation
		/// </summary>
		protected internal Dictionary<string, long> ReferenceDataOffsets = new();
		/// <summary>
		/// Whether the pointer length is 64 bits, otherwise is 32 bits
		/// </summary>
		public bool x64;
		/// <summary>
		/// Whether the string is save as UTF-32, otherwise is UTF-16
		/// </summary>
		public bool UTF32;
		/// <summary>
		/// The begin offset of DataSection(Which contains pointed values and starts with 0xBBBBBBBBBBBBBBBB)
		/// </summary>
		public long DataSectionOffset;
		/// <summary>
		/// Temporary record the offset in DataSection while writing to a dat file
		/// </summary>
		protected internal long CurrentOffset = 8;
		#endregion

		/// <summary>
		/// Create a DatContainer with Datas
		/// </summary>
		/// <param name="stream">Contents of a dat file</param>
		/// <param name="fileName">Name of the dat file</param>
		public DatContainer(List<IFieldData[]> fieldDatas, string fileName) {
			FieldDatas = fieldDatas;
			Name = Path.GetFileNameWithoutExtension(fileName);
		}

		/// <summary>
		/// Save the dat file with the modified <see cref="FieldDatas"/>
		/// </summary>
		public virtual void Save(string filePath, bool x64, bool UTF32) {
			var f = File.Create(filePath);
			Save(f, x64, UTF32);
			f.Close();
		}

		/// <summary>
		/// Save the dat file with the modified <see cref="FieldDatas"/>
		/// </summary>
		public virtual byte[] Save(bool x64, bool UTF32) {
			var f = new MemoryStream();
			Save(f, x64, UTF32);
			var b = f.ToArray();
			f.Close();
			return b;
		}

		/// <summary>
		/// Save the dat file with the modified <see cref="FieldDatas"/>
		/// The position of the stream must be 0
		/// </summary>
		protected virtual void Save(Stream stream, bool x64, bool UTF32) {
			var bw = new BinaryWriter(stream);
			bw.Write(FieldDatas.Count);
			this.x64 = x64;
			this.UTF32 = UTF32;
			CurrentOffset = 8;
			DataSectionOffset = FieldDatas.Count * CalculateRecordLength(FieldDefinitions.Select(t => t.Item2), x64) + 4;
			ReferenceDatas.Clear();
			ReferenceDataOffsets.Clear();
			foreach (var fds in FieldDatas)
				foreach (var fd in fds)
					fd.Write(bw);
			bw.Write(0xBBBBBBBBBBBBBBBB); // Magic number
			bw.Seek((int)CurrentOffset, SeekOrigin.Begin); // Move to end of dat file
		}

		/// <summary>
		/// Convert <see cref="FieldDatas"/> to csv format
		/// </summary>
		/// <returns>Content of the csv file</returns>
		public virtual string ToCsv() {
			var f = new StringBuilder();
			var reg = new Regex("\n|\r|,|\"", RegexOptions.Compiled);
			foreach (var field in FieldDefinitions.Select(t => t.Item1))
				if (reg.IsMatch(field))
					f.Append("\"" + field.Replace("\"", "\"\"") + "\",");
				else
					f.Append(field + ",");
			if (f.Length == 0) {
				for (var i=0; i< FieldDatas.Count; ++i)
					f.AppendLine();
				return f.ToString();
			} else
				f.Length -= 1;
			f.AppendLine();
			foreach (var row in FieldDatas) {
				foreach (var col in row) {
					var s = col.ToString();
					if (reg.IsMatch(s))
						f.Append("\"" + s + "\",");
					else
						f.Append(s + ",");
				}
				f.Length -= 1;
				f.AppendLine();
			}
			f.Length -= 1;
			return f.ToString();
		}

		/// <summary>
		/// Get the length of records in the dat file
		/// </summary>
		protected static long GetActualRecordLength(BinaryReader reader, int numberOfEntries, long datLength) {
			if (numberOfEntries == 0)
				return 0;
			for (long i = 0, offset = reader.BaseStream.Position; reader.BaseStream.Position - offset <= datLength - 8; i++) {
				var ul = reader.ReadUInt64();
				if (ul == 0xBBbbBBbbBBbbBBbb)
					return i;
				reader.BaseStream.Seek(-8 + numberOfEntries, SeekOrigin.Current);
			}
			return -1;
		}

		/// <summary>
		/// Calculate the expected length of records in the dat file
		/// </summary>
		protected static long CalculateRecordLength(IEnumerable<string> fields, bool x64) {
			long result = 0;
			foreach (var type in fields)
				result += FieldTypeLength(type, x64);
			return result;
		}

		/// <summary>
		/// Get the length in dat file of a type of field
		/// </summary>
		public static int FieldTypeLength(string type, bool x64) {
			if (type.StartsWith("array|"))
				return x64 ? 16 : 8;
			else
				return type switch {
					"foreignrow" => x64 ? 16 : 8,
					"row" => x64 ? 8 : 4,
					"string" => x64 ? 8 : 4,
					"bool" => 1,
					"i8" => 1,
					"u8" => 1,
					"i16" => 2,
					"u16" => 2,
					"i32" => 4,
					"u32" => 4,
					"f32" => 4,
					"i64" => 8,
					"u64" => 8,
					"f64" => 8,
					"valueString" => 0,
					"array" => 0,
					_ => throw new InvalidCastException($"Unknown Type: {type}")
				};
		}

		/// <summary>
		/// Reload DatDefinitions from network or file
		/// This won't affect the existing DatContainers
		/// </summary>
		public static void ReloadDefinitions() {
			var h = new HttpClient();
			h.DefaultRequestHeaders.Add("User-Agent", "LibDat2");
			string s;
			try {
				s = h.GetStringAsync("http://github.com/poe-tool-dev/dat-schema/releases/download/latest/schema.min.json").Result;
			} catch (Exception ex) {
				try {
					s = File.ReadAllText(Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName + @"\schema.min.json");
				} catch {
					throw new Exception("Failed to read schema.min.json from network or file", ex);
				}
			}
			h.Dispose();
			DatDefinitions = JsonDocument.Parse(s, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }).RootElement;
			try {
				OldDatDefinitions = JsonDocument.Parse(File.ReadAllText(Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName + @"\DatDefinitions_extra.json"), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }).RootElement;
				HasOldDatDefinitions = true;
				ForceUseOldDatDefinitionsList = new(File.ReadAllLines(Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName + @"\ForceUseOldDatDefinitionsList.txt"));
			} catch { }
		}

		/// <summary>
		/// Reload DatDefinitions from a file
		/// This won't affect the existing DatContainers
		/// </summary>
		public static void ReloadDefinitions(string filePath) {
			DatDefinitions = JsonDocument.Parse(File.ReadAllText(filePath), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }).RootElement;
		}
	}
}