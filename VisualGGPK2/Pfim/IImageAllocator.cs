namespace Pfim
{
    public interface IImageAllocator
    {
        /// <summary>
        /// Allocate a buffer that is used during image decoding. All buffers
        /// allocated with rent will be returned once the image is disposed.
        /// Length of returned data can exceed size requested.
        /// </summary>
        byte[] Rent(int size);

        /// <summary>
        /// Returns a buffer to the pool that was previously obtained by rent.
        /// </summary>
        void Return(byte[] data);
    }
}
