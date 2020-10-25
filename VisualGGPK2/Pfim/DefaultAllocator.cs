namespace Pfim
{
    class DefaultAllocator : IImageAllocator
    {
        public byte[] Rent(int size)
        {
            return new byte[size];
        }

        public void Return(byte[] data)
        {
        }
    }
}
