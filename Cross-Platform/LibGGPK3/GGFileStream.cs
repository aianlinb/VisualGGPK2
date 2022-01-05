using LibGGPK3.Records;
using System;
using System.IO;

namespace LibGGPK3 {
	public class GGFileStream : Stream {
		public readonly FileRecord Record;

		protected MemoryStream? _Buffer;
		protected MemoryStream Buffer {
			get {
				if (_Buffer is null) {
					var b = new byte[Record.DataLength];
					Record.Ggpk.FileStream.Seek(Record.DataOffset, SeekOrigin.Begin);
					Record.Ggpk.FileStream.Read(b, 0, b.Length);
					_Buffer = new MemoryStream(b);
				}
				return _Buffer;
			}
		}

		protected bool Modified;

		public GGFileStream(FileRecord record) {
			Record = record;
		}

		public override void Flush() {
			if (_Buffer is null || !Modified)
				return;
			Record.ReplaceContent(new(_Buffer.GetBuffer(), 0, (int)_Buffer.Length));
			Modified = false;
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return Buffer.Read(buffer, offset, count);
		}

		public override int Read(Span<byte> buffer) {
			return Buffer.Read(buffer);
		}

		public override int ReadByte() => Buffer.ReadByte();

		public override long Seek(long offset, SeekOrigin origin) {
			return Buffer.Seek(offset, origin);
		}

		public override void SetLength(long value) {
			if (value == Length)
				return;
			Buffer.SetLength(value);
			Modified = true;
		}

		public override void Write(byte[] buffer, int offset, int count) {
			Buffer.Write(buffer, offset, count);
			Modified = true;
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			Buffer.Write(buffer);
			Modified = true;
		}

		public override void WriteByte(byte value) {
			Buffer.WriteByte(value);
			Modified = true;
		}

		public override bool CanRead => Record.Ggpk.FileStream.CanRead;

		public override bool CanSeek => true;

		public override bool CanWrite => Record.Ggpk.FileStream.CanWrite;

		public override long Length => Buffer.Length;

		public override long Position { get => Buffer.Position; set => Buffer.Position = value; }

		protected override void Dispose(bool disposing) {
			Flush();
			_Buffer?.Close();
		}

		~GGFileStream() {
			Close(); // which will call Dispose(true);
		}
	}
}