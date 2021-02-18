using LibDat2.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LibDat2 {
    public class DatContainer {

        /// <summary>
        /// Structure definition of dat files
        /// </summary>
        protected static JsonElement DatDefinitions = JsonDocument.Parse(File.ReadAllText("DatDefinitions.json"), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }).RootElement;

        /// <summary>
        /// Whether the file extension is .dat64
        /// </summary>
        public readonly bool x64;
        /// <summary>
        /// Name of the dat file without extension
        /// </summary>
        public readonly string DatName;
        /// <summary>
        /// Length of the dat file
        /// </summary>
        public long Length { get; private set; }
        /// <summary>
        /// Number of entries, got from first 4 bytes of dat file
        /// </summary>
        public int Count { get; private set; }
        /// <summary>
        /// Definition of fields in dat file.
        /// Key: Name of field, Value: Type of field data.
        /// </summary>
        public readonly ReadOnlyDictionary<string, string> FieldDefinitions;
        /// <summary>
        /// List of record content of the dat file
        /// </summary>
        public List<List<FieldType>> FieldDatas;
        /// <summary>
        /// Set of pointed value of the dat file
        /// </summary>
        public SortedDictionary<long, PointedValue> PointedDatas;
        /// <summary>
        /// Offset of the data section in the dat file (Starts with 0xBBBBBBBBBBBBBBBB)
        /// </summary>
        public long DataSectionOffset { get; protected set; }

        public struct ErrorStruct {
            public Exception Exception;
            public int Row;
            public int Column;
            public string FieldName;
            public long StreamPosition;
            public long LastSucceededPosition;
        }

        public readonly ErrorStruct? FirstError;

        protected static FileStream tmp;
        /// <summary>
        /// Parses the dat file contents from a file
        /// </summary>
        /// <param name="filePath">Path of a dat file</param>
        public DatContainer(string filePath) : this(tmp = File.OpenRead(filePath), tmp.Length, filePath) => tmp.Close();

        /// <summary>
        /// Parses the dat file contents from a binary data
        /// </summary>
        /// <param name="data">Binary data of a dat file</param>
        public DatContainer(byte[] data, string fileName) : this(new MemoryStream(data), data.Length, fileName) { }

        /// <summary>
        /// Parses the dat file contents from a stream.
        /// </summary>
        /// <param name="stream">Contents of a dat file</param>
        /// <param name="fileName">Name of the dat file</param>
        public DatContainer(Stream stream, long length, string fileName) : this(new BinaryReader(stream, Encoding.Unicode), length, fileName) { }
        
        /// <summary>
        /// Parses the dat file contents.
        /// </summary>
        /// <param name="reader">BinaryReader in Unicode to read the contents of a dat file</param>
        /// <param name="fileName">Name of the dat file</param>
        public DatContainer(BinaryReader reader, long length, string fileName) {
            x64 = Path.GetExtension(fileName) == ".dat64";
            DatName = Path.GetFileNameWithoutExtension(fileName);
            Length = length;
            
            Count = reader.ReadInt32();
            var actualRecordLength = GetActualRecordLength(reader, Count, Length);
            if (actualRecordLength < 0)
                throw new($"{DatName} : Missing magic number after records");
            DataSectionOffset = Count * actualRecordLength + 4;
            // DataSectionDataLength = Length - DataSectionOffset - 8;

            try {
                var fields = new Dictionary<string, string>();
                foreach (var field in DatDefinitions.GetProperty(DatName).EnumerateObject())
                    fields.Add(field.Name, field.Value.GetString());
                FieldDefinitions = new(fields);
            } catch (KeyNotFoundException) {
                throw new Exception(DatName + " was not defined");
			}

            var recordLength = CalculateRecordLength(FieldDefinitions.Values, x64);
			if (recordLength != actualRecordLength)
				throw new($"{Path.GetFileName(fileName)} : Actual record length: {actualRecordLength} is not equal to that defined in DatDefinitions: {recordLength}");

			// Read Data
			reader.BaseStream.Seek(4, SeekOrigin.Begin);
            FieldDatas = new(Count);
            PointedDatas = new();
            var error = false;
            var lastPos = reader.BaseStream.Position;
            for (var i = 0; i < Count; i++) {
                var list = new List<FieldType>(FieldDefinitions.Count);
                foreach (var type in FieldDefinitions.Values) {
                    if (error) {
                        list.Add(FieldType.Null);
                        continue;
                    }
                    try {
                        list.Add(FieldType.Create(type, reader, this));
                        lastPos = reader.BaseStream.Position;
                    } catch (Exception ex) {
                        error = true;
                        FirstError = new ErrorStruct {
                            Exception = ex,
                            Row = i + 1,
                            Column = list.Count + 1,
                            FieldName = FieldDefinitions.Keys.ElementAt(list.Count),
                            StreamPosition = reader.BaseStream.Position,
                            LastSucceededPosition = lastPos
                        };
                        list.Add(FieldType.Null);
                    }
                }
                FieldDatas.Add(list);
            }
        }

        /// <summary>
        /// Save the dat file with the modified <see cref="FieldDatas"/>.
        /// Haven't implemented yet.
        /// </summary>
        /// <exception cref="NotImplementedException">Always thrown</exception>
        public virtual byte[] Save() {
            throw new NotImplementedException();
		}

        public virtual string ToCsv() {
            var f = new StringBuilder();
            var reg = new Regex("\n|\r|,", RegexOptions.Compiled);
            foreach (var field in FieldDefinitions.Keys)
                if (reg.IsMatch(field))
                    f.Append("\"" + field + "\",");
                else
                    f.Append(field + ",");
            f.Remove(f.Length - 1, 1);
            f.AppendLine();
            foreach (var row in FieldDatas) {
                foreach (var col in row) {
                    var s = col.Value.ToString();
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
        private static long GetActualRecordLength(BinaryReader reader, int numberOfEntries, long datLength) {
            if (numberOfEntries == 0) return 0;
            for (long i = 0, offset = reader.BaseStream.Position; reader.BaseStream.Position - offset <= datLength - 8; i++) {
                var ul = reader.ReadUInt64();
                if (ul == 0xBBbbBBbbBBbbBBbb) return i;
                reader.BaseStream.Seek(-8 + numberOfEntries, SeekOrigin.Current);
            }
            return -1;
        }

        /// <summary>
        /// Calculate the expected length of records in the dat file
        /// </summary>
        private static long CalculateRecordLength(IEnumerable<string> Fields, bool x64) {
            long result = 0;
            foreach (var type in Fields)
                result += FieldType.TypeLength(type, x64).Value;
            return result;
        }

        /// <summary>
        /// Reload the DatDefinitions.json
        /// </summary>
        public static void ReloadDefinitions() {
            DatDefinitions = JsonDocument.Parse(File.ReadAllText("DatDefinitions.json"), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }).RootElement;
        }
    }
}