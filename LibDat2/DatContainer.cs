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
		public List<object[]> FieldDatas;
		/// <summary>
		/// Set of pointed value of the dat file last read/save
		/// </summary>
		public SortedDictionary<long, PointedValue> PointedDatas = new();
		/// <summary>
		/// Used to find PointedValue with the actual data
		/// </summary>
		protected Dictionary<object, PointedValue> PointedDatas2 = new();

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
					Args.x64 = false;
					Args.UTF32 = false;
					break;
				case ".dat64":
					Args.x64 = true;
					Args.UTF32 = false;
					break;
				case ".datl":
					Args.x64 = false;
					Args.UTF32 = true;
					break;
				case ".datl64":
					Args.x64 = true;
					Args.UTF32 = true;
					break;
				default:
					throw new ArgumentException("The provided file must be a dat file", nameof(fileName));
			}

			Name = fileName;
			var name = Path.GetFileNameWithoutExtension(fileName);
			var Length = stream.Length;

			var reader = new BinaryReader(stream, Args.UTF32 ? Encoding.UTF32 : Encoding.Unicode);

			var Count = reader.ReadInt32();

			var fields = new List<(string, string)>();
			var forceOld = ForceUseOldDatDefinitionsList.Contains(name);
			if (forceOld) {
				// --- Begin  Old DatDefinitions ---
				if (!HasOldDatDefinitions)
					throw new KeyNotFoundException(name + " was not defined in Old DatDefinition");
				fields = new();
				try {
					foreach (var field in OldDatDefinitions.GetProperty(name).EnumerateObject())
						fields.Add((field.Name, ToNewType(field.Value.GetString())));
					FieldDefinitions = new(fields);
				} catch (Exception ex) {
					throw new("Unable to read Old DatDefinition for " + Name, ex);
				}
				// --- End  Old DatDefinitions ---
			} else
				try {
					var definition = DatDefinitions.GetProperty("tables").EnumerateArray().First(o => o.GetProperty("name").GetString() == name);
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
						throw new KeyNotFoundException(name + " was not defined");
					fields = new();
					try {
						foreach (var field in OldDatDefinitions.GetProperty(name).EnumerateObject())
							fields.Add((field.Name, ToNewType(field.Value.GetString())));
						FieldDefinitions = new(fields);
						forceOld = true;
					// --- End  Old DatDefinitions ---
					} catch (KeyNotFoundException) { // Unable to find definition with both two files
						throw new KeyNotFoundException(name + " was not defined");
					}
				}
			FieldDefinitions = new(fields);

			if (Path.GetFileNameWithoutExtension(fileName) == "Languages")
				Args.UTF32 = true;
			else {
				var actualRecordLength = GetActualRecordLength(reader, Count, Length);
				if (actualRecordLength < 0)
					throw new($"{fileName} : Missing magic number after records");
				Args.DataSectionOffset = Count * actualRecordLength + 4;
				// DataSectionDataLength = Length - DataSectionOffset - 8;

				var recordLength = CalculateRecordLength(FieldDefinitions.Select(t => t.Item2), Args.x64);
				if (recordLength != actualRecordLength) {
					// --- Begin  Old DatDefinitions ---
					if (!HasOldDatDefinitions)
						throw new($"{fileName} : Actual record length: {actualRecordLength} is not equal to that defined in DatDefinitions: {recordLength}");
					fields = new();
					try {
						foreach (var field in OldDatDefinitions.GetProperty(name).EnumerateObject())
							fields.Add((field.Name, ToNewType(field.Value.GetString())));

						var tmpRecordLength = CalculateRecordLength(fields.Select(t => t.Item2), Args.x64);

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

			List<object[]> tmpFieldDatas = null;
			var tmpPointedDatas = PointedDatas;
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
				var list = new object[FieldDefinitions.Count];
				var index = 0;
				foreach (var type in FieldDefinitions.Select(t => t.Item2)) {
					try {
						list[index++] = ReadType(reader, type);
						lastPos = reader.BaseStream.Position;
					} catch (Exception ex) {
						if (!FromOldDefinition)
							FirstError = new ErrorStruct {
								Exception = ex,
								Row = i + 1,
								Column = index,
								FieldName = FieldDefinitions[index - 1].Item1,
								StreamPosition = reader.BaseStream.Position,
								LastSucceededPosition = lastPos
							};
						// --- Begin  Old DatDefinitions ---
						if (!forceOld && !FromOldDefinition && (FromOldDefinition = HasOldDatDefinitions)) {
							fields = new();
							try {
								foreach (var field in OldDatDefinitions.GetProperty(name).EnumerateObject())
									fields.Add((field.Name, ToNewType(field.Value.GetString())));
								tmpFieldDefinitions = fields;
								FieldDatas.Add(list);
								tmpFieldDatas = FieldDatas; // Save current datas
								PointedDatas = new();
								goto ReadingType;
							} catch {
								FromOldDefinition = false;
							}
						}
						if (FromOldDefinition) { // Recovery to original datas before loading OldDefinition
							FieldDatas = tmpFieldDatas;
							PointedDatas = tmpPointedDatas;
						}
						// --- End  Old DatDefinitions ---
						error = true;
						FromOldDefinition = false;
						break;
					}
				}
				FieldDatas.Add(list);
			}
			reader = null;
			if (!error) {
				FirstError = null;
				if (FromOldDefinition)
					FieldDefinitions = new((List<(string, string)>)tmpFieldDefinitions);
			}
			if (forceOld)
				FromOldDefinition = true;
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
				_ => throw new InvalidCastException($"Unknown Type: {type}")
			};
		}

		/// <summary>
		/// See <see cref="Args"/>
		/// </summary>
		protected struct Arguments {
			/// <summary>
			/// Whether the pointer length is 64 bits, otherwise is 32 bits
			/// </summary>
			public bool x64;
			/// <summary>
			/// Whether the string is save as UTF-32, otherwise is UTF-16
			/// </summary>
			public bool UTF32;
			/// <summary>
			/// The begin offset of pointed values
			/// </summary>
			public long DataSectionOffset;
		}

		/// <summary>
		/// Contains information of last read/save dat file
		/// </summary>
		protected Arguments Args = new();

		protected object ReadType(BinaryReader reader, string type) {
			if (type.StartsWith('i'))
				switch (type[1..]) {
					case "32":
						return reader.ReadInt32();
					case "8":
						return reader.ReadSByte();
					case "64":
						return reader.ReadInt64();
					case "16":
						return reader.ReadInt16();
					default:
						throw new InvalidCastException("Unknown Type: " + type);
				}
			else if (type.StartsWith('u'))
				switch (type[1..]) {
					case "32":
						return reader.ReadUInt32();
					case "64":
						return reader.ReadUInt64();
					case "8":
						return reader.ReadByte();
					case "16":
						return reader.ReadUInt16();
					default:
						throw new InvalidCastException("Unknown Type: " + type);
				}
			else if (type == "bool") {
				return reader.ReadBoolean();
			} else if (type.StartsWith("array|")) {
				long length;
				long pos;
				if (Args.x64) {
					length = reader.ReadInt64();
					pos = reader.ReadInt64();
				} else {
					length = reader.ReadInt32();
					pos = reader.ReadInt32();
				}

				if (length == 0)
					return PointedValue.NullArray;
				if (PointedDatas.TryGetValue(pos, out PointedValue p))
					return p.Value;

				var array = new object[length];
				var previousPos = reader.BaseStream.Position;
				reader.BaseStream.Seek(pos + Args.DataSectionOffset, SeekOrigin.Begin);
				var type2 = type[6..];
				for (var i = 0L; i < length; ++i)
					array[i] = ReadType(reader, type2);
				reader.BaseStream.Seek(previousPos, SeekOrigin.Begin);
				var pv = new PointedValue(pos, array, type, Args.x64, Args.UTF32);
				PointedDatas.Add(pos, new PointedValue(pos, array, type, Args.x64, Args.UTF32));
				PointedDatas2.Add(array, pv);
				return array;
			} else if (type == "string") {
				var pos = Args.x64 ? reader.ReadInt64() : reader.ReadInt32();

				if (PointedDatas.TryGetValue(pos, out PointedValue p))
					return p.Value;

				if (pos + Args.DataSectionOffset == reader.BaseStream.Length)
					return null;
				var previousPos = reader.BaseStream.Position;
				reader.BaseStream.Seek(pos + Args.DataSectionOffset, SeekOrigin.Begin);
				var sb = new StringBuilder();

				if (Args.UTF32) {
					int ch;
					while ((ch = reader.ReadInt32()) != 0)
						sb.Append(ch);
				} else {
					char ch;
					while ((ch = reader.ReadChar()) != 0)
						sb.Append(ch);
					if (reader.ReadChar() != 0)  // string should end with 4 bytes of zero
						throw new("Not found \\0 at the end of the string");
				}

				reader.BaseStream.Seek(previousPos, SeekOrigin.Begin);
				var s = sb.ToString();
				var pv = new PointedValue(pos, s, type, Args.x64, Args.UTF32);
				PointedDatas.Add(pos, pv);
				PointedDatas2.Add(s, pv);
				return sb.ToString();
			} else if (type == "foreignrow") {
				ulong? key1, key2;
				if (Args.x64) {
					key1 = reader.ReadUInt64();
					if (key1 == KeyType.Nullx64Key)
						key1 = null;
					key2 = reader.ReadUInt64();
					if (key2 == KeyType.Nullx64Key)
						key2 = null;
				} else {
					key1 = reader.ReadUInt32();
					if (key1 == KeyType.Nullx32Key)
						key1 = null;
					key2 = reader.ReadUInt32();
					if (key2 == KeyType.Nullx32Key)
						key2 = null;
				}
				return new KeyType(true, key1, key2);
			} else if (type == "row") {
				ulong? key;
				if (Args.x64) {
					key = reader.ReadUInt64();
					if (key == KeyType.Nullx64Key)
						key = null;
				} else {
					key = reader.ReadUInt32();
					if (key == KeyType.Nullx32Key)
						key = null;
				}
				return new KeyType(false, key, 0UL);
			} else if (type.StartsWith('f'))
				switch (type[1..]) {
					case "32":
						return reader.ReadSingle();
					case "64":
						return reader.ReadDouble();
					default:
						throw new InvalidCastException("Unknown Type: " + type);
				}
			else if (type == "valueString") {
				if (reader.BaseStream.Position == reader.BaseStream.Length)
					return null;
				var sb = new StringBuilder();
				char ch;
				while ((ch = reader.ReadChar()) != 0)
					sb.Append(ch);
				if (!Args.UTF32 && reader.ReadChar() != 0)  // string should end with 4 bytes of zero
					throw new("Not found \\0 at the end of the string");
				return sb.ToString();
			} else if (type == "array")
				return "error";
			else
				throw new InvalidCastException("Unknown Type: " + type);
		}

		/// <summary>
		/// Create a DatContainer with Datas
		/// </summary>
		/// <param name="stream">Contents of a dat file</param>
		/// <param name="fileName">Name of the dat file</param>
		public DatContainer(List<object[]> fieldDatas, string fileName) {
			FieldDatas = fieldDatas;
			Name = fileName;
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
		/// </summary>
		public virtual void Save(Stream stream, bool x64, bool UTF32) {
			var bw = new BinaryWriter(stream);
			bw.Write(FieldDatas.Count);
			Args.x64 = x64;
			Args.UTF32 = UTF32;
			var pointer = (Args.DataSectionOffset = FieldDatas.Count * CalculateRecordLength(FieldDefinitions.Select(t => t.Item2), x64) + 4) + 8;
			PointedDatas.Clear();
			PointedDatas2.Clear();
			var def = FieldDefinitions.GetEnumerator();
			foreach (var os in FieldDatas) {
				foreach (var o in os) {
					def.MoveNext();
					WriteType(bw, o, ref pointer, def.Current.Item2);
				}
				def.Reset();
			}
			bw.Write(0xBBBBBBBBBBBBBBBB); // Magic number
			bw.Seek((int)(pointer - bw.BaseStream.Position), SeekOrigin.Current); // Move to end of dat file
		}

		protected unsafe void WriteType(BinaryWriter writer, object o, ref long pointer, string type) {
			if (type == "i32")
				writer.Write((int)o);
			else if (type == "u8")
				writer.Write((byte)o);
			else if (type == "bool")
				writer.Write((bool)o);
			else if (type == "foreignrow" || type == "row")
				((KeyType)o).Write(writer, Args.x64);
			else if (type == "u32")
				writer.Write((uint)o);
			else if (type == "u64")
				writer.Write((ulong)o);
			else if (type == "i64")
				writer.Write((long)o);
			else if (type == "f32")
				writer.Write((float)o);
			else if (type == "i16")
				writer.Write((short)o);
			else if (type == "f64")
				writer.Write((double)o);
			else if (type == "i8")
				writer.Write((sbyte)o);
			else if (type == "u16")
				writer.Write((ushort)o);
			else if (type.StartsWith("array|")) {
				var os = o as object[];
				if (PointedDatas2.TryGetValue(o, out PointedValue pv)) {
					if (Args.x64) {
						writer.Write((long)os.Length);
						writer.Write(pv.Offset);
					} else {
						writer.Write(os.Length);
						writer.Write((uint)pv.Offset);
					}
				} else {
					var offset = pointer - Args.DataSectionOffset;
					if (Args.x64) {
						writer.Write((long)os.Length);
						writer.Write(offset);
					} else {
						writer.Write(os.Length);
						writer.Write((uint)offset);
					}
					if (os.Length == 0)
						return;

					var pv2 = new PointedValue(offset, o, type, Args.x64, Args.UTF32);
					PointedDatas.Add(offset, pv2);
					PointedDatas2.Add(o, pv2);

					var previousPointer = writer.BaseStream.Position;
					writer.BaseStream.Seek(pointer, SeekOrigin.Begin);
					pointer += pv2.Length;
					foreach (var o2 in os)
						WriteType(writer, o2, ref pointer, type[6..]);
					if (pointer < writer.BaseStream.Position)
						pointer = writer.BaseStream.Position;
					writer.BaseStream.Seek(previousPointer, SeekOrigin.Begin);
				}
			} else if (type == "string") {
				var s = o as string;
				if (PointedDatas2.TryGetValue(o, out PointedValue pv)) {
					if (Args.x64)
						writer.Write(pv.Offset);
					else
						writer.Write((uint)pv.Offset);
				} else {
					var offset = pointer - Args.DataSectionOffset;
					if (Args.x64)
						writer.Write(offset);
					else
						writer.Write((uint)offset);

					var pv2 = new PointedValue(offset, o, type, Args.x64, Args.UTF32);
					PointedDatas.Add(offset, pv2);
					PointedDatas2.Add(o, pv2);

					var previousPointer = writer.BaseStream.Position;
					writer.BaseStream.Seek(pointer, SeekOrigin.Begin);
					pointer += pv2.Length;
					if (Args.UTF32)
						writer.Write(Encoding.UTF32.GetBytes(s));
					else
						fixed (char* c = s) {
							var p = (byte*)c;
							writer.BaseStream.Write(new ReadOnlySpan<byte>(p, Encoding.Unicode.GetByteCount(s)));
						}
					writer.Write(0); // \0 at the end of string
					if (pointer < writer.BaseStream.Position)
						pointer = writer.BaseStream.Position;
					writer.BaseStream.Seek(previousPointer, SeekOrigin.Begin);
				}
			} else if (type == "valueString") {
				var s = o as string;
				fixed (char* c = s) {
					var p = (byte*)c;
					writer.BaseStream.Write(new ReadOnlySpan<byte>(p, Args.UTF32 ? Encoding.UTF32.GetByteCount(s) : Encoding.Unicode.GetByteCount(s)));
				}
			} else if (type == "array") {
				return;
			} else
				throw new InvalidCastException($"Unknown Type: {type}");
		}

		/// <summary>
		/// Convert <see cref="FieldDatas"/> to csv format
		/// </summary>
		/// <returns>Content of the csv file</returns>
		public virtual string ToCsv() {
			var f = new StringBuilder();
			var reg = new Regex("\n|\r|,", RegexOptions.Compiled);
			foreach (var field in FieldDefinitions.Select(t => t.Item1))
				if (reg.IsMatch(field))
					f.Append("\"" + field + "\",");
				else
					f.Append(field + ",");
			f.Remove(f.Length - 1, 1);
			f.AppendLine();
			foreach (var row in FieldDatas) {
				foreach (var col in row) {
					var s = col.ToString();
					if (reg.IsMatch(s))
						f.Append("\"" + s + "\",");
					else
						f.Append(s + ",");
				}
				f.Remove(f.Length - 1, 1);
				f.AppendLine();
			}
			f.Remove(f.Length - 2, 2);
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
			h.DefaultRequestHeaders.Remove("UserAgent");
			h.DefaultRequestHeaders.Add("UserAgent", "LibDat2");
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