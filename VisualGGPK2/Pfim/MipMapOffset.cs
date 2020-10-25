namespace Pfim
{
    public class MipMapOffset
    {
        public MipMapOffset(int width, int height, int stride, int dataOffset, int dataLen)
        {
            Stride = stride;
            Width = width;
            Height = height;
            DataOffset = dataOffset;
            DataLen = dataLen;
        }

        public int Stride { get; }

        public int Width { get; }

        public int Height { get; }

        public int DataOffset { get; }

        public int DataLen { get; }

        protected bool Equals(MipMapOffset other)
        {
            return Stride == other.Stride && Width == other.Width && Height == other.Height && DataOffset == other.DataOffset && DataLen == other.DataLen;
        }

        public override string ToString()
        {
            return $"{nameof(Stride)}: {Stride}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height}, {nameof(DataOffset)}: {DataOffset}, {nameof(DataLen)}: {DataLen}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MipMapOffset) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Stride;
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;
                hashCode = (hashCode * 397) ^ DataOffset;
                hashCode = (hashCode * 397) ^ DataLen;
                return hashCode;
            }
        }
    }
}
