using System;
using System.Runtime.InteropServices;

namespace DonatelloAI.Importers.GLB
{
    /// <summary>
    /// Helper to buffer access.
    /// </summary>
    public class BufferInfo : IDisposable
    {
        /// <summary>
        /// Buffer content.
        /// </summary>
        public byte[] bufferBytes;

        /// <summary>
        /// Buffer pointer.
        /// </summary>
        public IntPtr bufferPointer;

        /// <summary>
        /// Buffer cg handler.
        /// </summary>
        public GCHandle bufferHandle;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferInfo"/> class.
        /// </summary>
        /// <param name="bufferBytes">Buffer byte array.</param>
        public BufferInfo(byte[] bufferBytes)
        {
            this.bufferBytes = bufferBytes;
            this.bufferHandle = GCHandle.Alloc(this.bufferBytes, GCHandleType.Pinned);
            this.bufferPointer = Marshal.UnsafeAddrOfPinnedArrayElement(this.bufferBytes, 0);
        }

        /// <summary>
        /// Free resources.
        /// </summary>
        public void Dispose()
        {
            this.bufferHandle.Free();
        }
    }
}
