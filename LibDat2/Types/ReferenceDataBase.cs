using System.IO;
using static LibDat2.Types.IFieldData;

namespace LibDat2.Types {
	public abstract class ReferenceDataBase<TypeOfValue> : FieldDataBase<TypeOfValue>, IReferenceData {
		public ReferenceDataBase(DatContainer dat) : base(dat) { }

		/// <inheritdoc/>
		public virtual long Offset {
			get => _Offset;
			protected set {
				if (_Offset == value)
					return;
				_Offset = value;
				RaisePropertyChanged(this, new(nameof(Offset)));
				RaisePropertyChanged(this, new(nameof(EndOffset)));
			}
		}
		private long _Offset;

		/// <inheritdoc/>
		public virtual int Length {
			get => _Length;
			protected set {
				if (_Length == value)
					return;
				_Length = value;
				RaisePropertyChanged(this, new(nameof(Length)));
				RaisePropertyChanged(this, new(nameof(EndOffset)));
			}
		}
		private int _Length;

		/// <inheritdoc/>
		public virtual long EndOffset => Offset + Length;

		/// <summary>
		/// Read the pointer and call <see cref="ReadInDataSection(BinaryReader)"/>.
		/// This won't check the <see cref="DatContainer.ReferenceDatas"/>, use <see cref="StringData.Read(BinaryReader, DatContainer)"/> or <see cref="ArrayData{TypeOfValueInArray}.Read(BinaryReader, DatContainer, FieldType)"/> instead.
		/// </summary>
		public override void Read(BinaryReader reader) {
			Offset = Dat.x64 ? reader.ReadInt64() : reader.ReadInt32();
			var previousPos = reader.BaseStream.Position;
			var begin = reader.BaseStream.Seek(Offset + Dat.DataSectionOffset, SeekOrigin.Begin);
			ReadInDataSection(reader);
			Length = (int)(reader.BaseStream.Position - begin);
			reader.BaseStream.Seek(previousPos, SeekOrigin.Begin);

			Dat.ReferenceDatas[Offset] = this;
			Dat.ReferenceDataOffsets[ToString()] = Offset;
		}

		/// <summary>
		/// Read the value from the DataSection
		/// </summary>
		protected abstract void ReadInDataSection(BinaryReader reader);

		/// <summary>
		/// Write the pointer and call <see cref="WriteInDataSection(BinaryWriter)"/>
		/// </summary>
		public override void Write(BinaryWriter writer) {
			var s = ToString();
			if (Dat.ReferenceDataOffsets.TryGetValue(s, out long offset)) {
				if (Dat.x64)
					writer.Write(Offset = offset);
				else
					writer.Write((uint)(Offset = offset));
				return;
			}

			Offset = Dat.CurrentOffset;
			Dat.ReferenceDatas.Add(Offset, this);
			Dat.ReferenceDataOffsets.Add(s, Offset);

			if (Dat.x64)
				writer.Write(Offset);
			else
				writer.Write((uint)Offset);
			var previousPos = writer.BaseStream.Position;
			var begin = writer.BaseStream.Seek(Offset + Dat.DataSectionOffset, SeekOrigin.Begin);
			WriteInDataSection(writer);
			Length = (int)(writer.BaseStream.Position - begin);
			Dat.CurrentOffset += Length;
			writer.BaseStream.Seek(previousPos, SeekOrigin.Begin);
		}

		/// <summary>
		/// Write the value to the DataSection
		/// </summary>
		protected abstract void WriteInDataSection(BinaryWriter writer);
	}
}