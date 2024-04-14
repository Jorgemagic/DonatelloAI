using glTFLoader.Schema;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace NetTripoAI.Importers.GLB
{
    /// <summary>
    /// Fast GLB loader methods.
    /// </summary>
    public static class GLBHelpers
    {
        private const uint GLTFVERSION2 = 2;
        private const uint GLTFHEADER = 0x46546C67;
        private const uint CHUNKJSON = 0x4E4F534A;
        private const uint CHUNKBIN = 0x004E4942;

        internal static (Gltf Gltf, byte[] Data) LoadModel(Stream stream)
        {
            //// https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#binary-gltf-layout-overview

            Gltf gltf = null;
            byte[] data = null;
            using (BinaryReader binaryReader = new BinaryReader(stream))
            {
                // Read 12 bytes header
                ReadBinaryHeader(binaryReader);

                // Read json chunk
                var json = ReadBinaryChunk(binaryReader, CHUNKJSON);
                gltf = JsonConvert.DeserializeObject<Gltf>(Encoding.UTF8.GetString(json));

                // Read binary chunk
                data = ReadBinaryChunk(binaryReader, CHUNKBIN);
            }

            return (gltf, data);
        }

        internal static void ReadBinaryHeader(BinaryReader binaryReader)
        {
            uint magic = binaryReader.ReadUInt32();
            if (magic != GLTFHEADER)
            {
                throw new InvalidDataException($"Unexpected magic number: {magic}");
            }

            uint version = binaryReader.ReadUInt32();
            if (version != GLTFVERSION2)
            {
                throw new InvalidDataException($"Unknown version number: {version}");
            }

            uint length = binaryReader.ReadUInt32();
            ////long fileLength = binaryReader.BaseStream.Length;
            ////if (length != fileLength)
            ////{
            ////    throw new InvalidDataException($"The specified length of the file ({length}) is not equal to the actual length of the file ({fileLength}).");
            ////}
        }

        internal static byte[] ReadBinaryChunk(BinaryReader binaryReader, uint format)
        {
            while (true) //// keep reading until EndOfFile exception
            {
                uint chunkLength = binaryReader.ReadUInt32();
                if ((chunkLength & 3) != 0)
                {
                    throw new InvalidDataException($"The chunk must be padded to 4 bytes: {chunkLength}");
                }

                uint chunkFormat = binaryReader.ReadUInt32();

                var data = binaryReader.ReadBytes((int)chunkLength);

                if (chunkFormat == format)
                {
                    return data;
                }
            }
        }
    }
}
