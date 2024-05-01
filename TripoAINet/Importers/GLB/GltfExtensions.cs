using Evergine.Common.Graphics;
using Evergine.Mathematics;
using glTFLoader.Schema;
using System.Collections.Generic;
using System.Linq;
using static glTFLoader.Schema.Accessor;
using static glTFLoader.Schema.MeshPrimitive;
using static glTFLoader.Schema.Sampler;

namespace TripoAINet.Importers.GLB
{
    /// <summary>
    /// AssImp extensions.
    /// </summary>
    internal static class GltfExtensions
    {
        /// <summary>
        /// Component counts by tipe.
        /// </summary>
        private static readonly int[] componentCountForType = { 1, 2, 3, 4, 4, 9, 16 };

        /// <summary>
        /// To Evergine matrix.
        /// </summary>
        /// <param name="m">The m.</param>
        /// <returns>The Evergine matrix.</returns>
        public static Matrix4x4 ToEvergineMatrix(this float[] m)
        {
            Matrix4x4 newMatrix = new Matrix4x4();

            newMatrix.M11 = m[0];
            newMatrix.M12 = m[1];
            newMatrix.M13 = m[2];
            newMatrix.M14 = m[3];

            newMatrix.M21 = m[4];
            newMatrix.M22 = m[5];
            newMatrix.M23 = m[6];
            newMatrix.M24 = m[7];

            newMatrix.M31 = m[8];
            newMatrix.M32 = m[9];
            newMatrix.M33 = m[10];
            newMatrix.M34 = m[11];

            newMatrix.M41 = m[12];
            newMatrix.M42 = m[13];
            newMatrix.M43 = m[14];
            newMatrix.M44 = m[15];

            return newMatrix;
        }

        /// <summary>
        /// To Evergine vector2.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns>The Evergine Vector2.</returns>
        public static Vector2 ToEvergineVector2(this float[] v)
        {
            Vector2 newVector = new Vector2();

            newVector.X = v[0];
            newVector.Y = v[1];

            return newVector;
        }

        /// <summary>
        /// To Evergine vector3.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns>To vector3.</returns>
        public static Vector3 ToEvergineVector3(this float[] v)
        {
            Vector3 newVector = new Vector3();

            newVector.X = v[0];
            newVector.Y = v[1];
            newVector.Z = v[2];

            return newVector;
        }

        /// <summary>
        /// To Evergine Color.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns>The Evergine Color.</returns>
        public static Vector4 ToEvergineVector4(this float[] v)
        {
            Vector4 newVector = new Vector4();

            newVector.X = v[0];
            newVector.Y = v[1];
            newVector.Z = v[2];
            newVector.W = v[3];

            return newVector;
        }

        /// <summary>
        /// To Evergine Color.
        /// </summary>
        /// <param name="q">The q.</param>
        /// <returns>The Evergine Quaternion.</returns>
        public static Quaternion ToEvergineQuaternion(this float[] q)
        {
            Quaternion newQuaternion = new Quaternion();

            newQuaternion.X = q[0];
            newQuaternion.Y = q[1];
            newQuaternion.Z = q[2];
            newQuaternion.W = q[3];

            return newQuaternion;
        }

        /// <summary>
        /// To Evergine Color with alpha.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>The Evergine color.</returns>
        public static Color ToEvergineColorAlpha(this IEnumerable<float> enumerable)
        {
            var colorValues = enumerable.ToArray();
            return new Color(
                colorValues[0],
                colorValues[1],
                colorValues[2],
                colorValues[3]);
        }

        /// <summary>
        /// To Evergine Color with alpha.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>The Evergine color.</returns>
        public static float ToEvergineAlpha(this IEnumerable<float> enumerable)
        {
            var colorValues = enumerable.ToArray();
            return colorValues[3];
        }

        /// <summary>
        /// To Evergine Color.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>The Evergine color.</returns>
        public static Color ToEvergineColor(this IEnumerable<float> enumerable)
        {
            var colorValues = enumerable.ToArray();
            return new Color(
                colorValues[0],
                colorValues[1],
                colorValues[2],
                1);
        }

        /// <summary>
        /// To Evergine Linear color.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>The Evergine linear space color.</returns>
        public static LinearColor ToLinearColor(this IEnumerable<float> enumerable)
        {
            var colorValues = enumerable.ToArray();
            var r = colorValues[0];
            var g = colorValues[1];
            var b = colorValues[2];
            var a = (colorValues.Length > 3) ? colorValues[3] : 1;

            var linearColor = new LinearColor(r, g, b, a);

            return linearColor;
        }

        /// <summary>
        /// To Evergine primitive type.
        /// </summary>
        /// <param name="mode">The gltf mode.</param>
        /// <param name="primitiveType">The primitive type.</param>
        /// <returns>If this primitive is supported.</returns>
        public static bool ToEverginePrimitive(this ModeEnum mode, out PrimitiveTopology primitiveType)
        {
            bool supported = true;
            primitiveType = PrimitiveTopology.TriangleList;
            switch (mode)
            {
                case ModeEnum.TRIANGLES:
                    primitiveType = PrimitiveTopology.TriangleList;
                    break;
                case ModeEnum.TRIANGLE_STRIP:
                    primitiveType = PrimitiveTopology.TriangleStrip;
                    break;
                case ModeEnum.LINES:
                    primitiveType = PrimitiveTopology.LineList;
                    break;
                case ModeEnum.LINE_STRIP:
                    primitiveType = PrimitiveTopology.LineStrip;
                    break;
                default:
                    supported = false;
                    break;
            }

            return supported;
        }

        /// <summary>
        /// Gets the vertex usafe from string.
        /// </summary>
        /// <param name="semantic">The semantic.</param>
        /// <param name="usage">The usage.</param>
        /// <param name="usageIndex">The usage index.</param>
        public static void VertexUsageFromString(string semantic, out VertexElementUsage usage, out int usageIndex)
        {
            var semanticSplit = semantic.Split('_');

            // Gets the usage semantics
            var usageStr = semanticSplit[0];

            switch (usageStr)
            {
                default:
                case "POSITION":
                    usage = VertexElementUsage.Position;
                    break;
                case "NORMAL":
                    usage = VertexElementUsage.Normal;
                    break;
                case "TANGENT":
                    usage = VertexElementUsage.Tangent;
                    break;
                case "TEXCOORD":
                    usage = VertexElementUsage.TextureCoordinate;
                    break;
                case "COLOR":
                    usage = VertexElementUsage.Color;
                    break;
                case "JOINTS":
                    usage = VertexElementUsage.BlendIndices;
                    break;
                case "WEIGHTS":
                    usage = VertexElementUsage.BlendWeight;
                    break;
            }

            // Usage index
            if (semanticSplit.Length > 1)
            {
                usageIndex = int.Parse(semanticSplit[1]);
            }
            else
            {
                usageIndex = 0;
            }
        }

        /// <summary>
        /// Get byte size of the component type.
        /// </summary>
        /// <param name="componentType">The component type.</param>
        /// <returns>The byte size.</returns>
        public static int GetByteSize(this ComponentTypeEnum componentType)
        {
            switch (componentType)
            {
                case ComponentTypeEnum.BYTE: return 1;
                case ComponentTypeEnum.UNSIGNED_BYTE: return 1;
                case ComponentTypeEnum.SHORT: return 2;
                case ComponentTypeEnum.UNSIGNED_SHORT: return 2;
                case ComponentTypeEnum.UNSIGNED_INT: return 4;
                case ComponentTypeEnum.FLOAT: return 4;
            }

            return 0;
        }

        /// <summary>
        /// Convert wrap enum to Evergine.
        /// </summary>
        /// <param name="wrapS">The wrap S mode.</param>
        /// <returns>The address mode.</returns>
        public static TextureAddressMode ToEvergine(this WrapSEnum wrapS)
        {
            switch (wrapS)
            {
                case WrapSEnum.CLAMP_TO_EDGE:
                    return TextureAddressMode.Clamp;
                case WrapSEnum.MIRRORED_REPEAT:
                    return TextureAddressMode.Mirror;
                case WrapSEnum.REPEAT:
                default:
                    return TextureAddressMode.Wrap;
            }
        }

        /// <summary>
        /// Convert wrap enum to Evergine.
        /// </summary>
        /// <param name="wrapT">The wrap T mode.</param>
        /// <returns>The address mode.</returns>
        public static TextureAddressMode ToEvergine(this WrapTEnum wrapT)
        {
            switch (wrapT)
            {
                case WrapTEnum.CLAMP_TO_EDGE:
                    return TextureAddressMode.Clamp;
                case WrapTEnum.MIRRORED_REPEAT:
                    return TextureAddressMode.Mirror;
                case WrapTEnum.REPEAT:
                default:
                    return TextureAddressMode.Wrap;
            }
        }

        /// <summary>
        /// Convert to file extension.
        /// </summary>
        /// <param name="mimeType">The mime type.</param>
        /// <returns>The address mode.</returns>
        public static string ToFileExtension(this Image.MimeTypeEnum? mimeType)
        {
            if (mimeType.HasValue)
            {
                switch (mimeType)
                {
                    case Image.MimeTypeEnum.image_jpeg:
                        return ".jpg";
                    case Image.MimeTypeEnum.image_png:
                        return ".png";
                }
            }

            return ".jpg";
        }

        /// <summary>
        /// Get byte size of the component type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The byte size.</returns>
        public static int GetComponentCount(this TypeEnum type)
        {
            return componentCountForType[(int)type];
        }

        /// <summary>
        /// Get skip info.
        /// </summary>
        /// <param name="componentType">The component type.</param>
        /// <param name="type">The type.</param>
        /// <param name="skipEvery">Skip every components.</param>
        /// <param name="skipBytes">Skip bytes.</param>
        /// <param name="elementSize">The element size.</param>
        public static void GetFormatInfo(ComponentTypeEnum componentType, TypeEnum type, out int skipEvery, out int skipBytes, out int elementSize)
        {
            int componentCount = type.GetComponentCount();
            int componentSize = componentType.GetByteSize();
            elementSize = componentCount * componentSize;

            skipEvery = 0;
            skipBytes = 0;

            // Special case of alignments, as described in spec
            switch (componentType)
            {
                case ComponentTypeEnum.BYTE:
                case ComponentTypeEnum.UNSIGNED_BYTE:
                    {
                        if (type == TypeEnum.MAT2)
                        {
                            skipEvery = 2;
                            skipBytes = 2;
                            elementSize = 8; // Override for this case
                        }
                        else if (type == TypeEnum.MAT3)
                        {
                            skipEvery = 3;
                            skipBytes = 1;
                            elementSize = 12; // Override for this case
                        }
                    }

                    break;
                case ComponentTypeEnum.SHORT:
                case ComponentTypeEnum.UNSIGNED_SHORT:
                    {
                        if (type == TypeEnum.MAT3)
                        {
                            skipEvery = 6;
                            skipBytes = 4;
                            elementSize = 16; // Override for this case
                        }
                    }

                    break;
            }
        }
    }
}
