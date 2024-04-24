using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Animation;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Materials;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.Platform;
using glTFLoader;
using glTFLoader.Schema;
using NetTripoAI.Importers.Images;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static glTFLoader.Schema.Material;
using Buffer = Evergine.Common.Graphics.Buffer;
using Material = Evergine.Framework.Graphics.Material;
using Mesh = Evergine.Framework.Graphics.Mesh;
using Texture = Evergine.Common.Graphics.Texture;

namespace NetTripoAI.Importers.GLB
{
    /// <summary>
    /// GLB files loader in runtime.
    /// </summary>
    public class GLBRuntime : ModelRuntime
    {
        /// <summary>
        /// Single instance (Singleton).
        /// </summary>
        public readonly static GLBRuntime Instance = new GLBRuntime();

        private GraphicsContext graphicsContext;
        private AssetsService assetsService;
        private AssetsDirectory assetsDirectory;

        private RenderLayerDescription opaqueLayer;
        private RenderLayerDescription alphaLayer;
        private SamplerState linearClampSampler;
        private SamplerState linearWrapSampler;
        private List<char> invalidNameCharacters;

        private Dictionary<int, (string name, Material material)> materials = new Dictionary<int, (string, Material)>();
        private Dictionary<int, Texture> images = new Dictionary<int, Texture>();
        private Dictionary<int, List<Mesh>> meshes = new Dictionary<int, List<Mesh>>();
        private List<MeshContainer> meshContainers = new List<MeshContainer>();
        private NodeContent[] allNodes;
        private List<int> rootIndices = new List<int>();
        private Dictionary<string, AnimationClip> animations = new Dictionary<string, AnimationClip>();
        private SkinContent[] skins;

        private Gltf glbModel;
        private byte[] binaryChunk;
        private BufferInfo[] bufferInfos;
        private Func<Color, Texture, SamplerState, AlphaModeEnum, float, float, bool, Material> materialAssigner;

        private GLBRuntime()
        {
        }

        /// <inheritdoc/>
        public override string Extentsion => ".glb";

        /// <summary>
        /// Read a glb file and return a model asset.
        /// </summary>
        /// <param name="filePath">Glb filepath.</param>
        /// <param name="materialAssigner">Material assigner.</param>
        /// <returns>Model asset.</returns>
        public async Task<Model> Read(string filePath, Func<Color, Texture, SamplerState, AlphaModeEnum, float, float, bool, Material> materialAssigner = null)
        {
            Model model = null;

            if (this.assetsDirectory == null)
            {
                this.assetsDirectory = Application.Current.Container.Resolve<AssetsDirectory>();
            }

            using (var stream = this.assetsDirectory.Open(filePath))
            {
                if (stream == null || !stream.CanRead)
                {
                    throw new ArgumentException("Invalid parameter. Stream must be readable", "imageStream");
                }

                model = await this.Read(stream, materialAssigner);
            }

            return model;
        }

        /// <summary>
        /// Read a glb file from stream and return a model asset.
        /// </summary>
        /// <param name="stream">Seeked stream.</param>
        /// <param name="materialAssigner">Material assigner.</param>
        /// <returns>Model asset.</returns>
        public override async Task<Model> Read(Stream stream, Func<Color, Texture, SamplerState, AlphaModeEnum, float, float, bool, Material> materialAssigner = null)
        {
            this.materialAssigner = materialAssigner;

            this.LoadStaticResources();

            var model = await this.ReadGLB(stream);

            this.FreeResources();

            return model;
        }

        private void LoadStaticResources()
        {
            if (this.graphicsContext == null)
            {
                // Get Services
                this.graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
                this.assetsService = Application.Current.Container.Resolve<AssetsService>();

                // Get invalid character used in node names
                this.invalidNameCharacters = Path.GetInvalidFileNameChars().ToList();
                this.invalidNameCharacters.Add('.');
                this.invalidNameCharacters.Add('[');
                this.invalidNameCharacters.Add(']');

                // Get static resources
                this.opaqueLayer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.OpaqueRenderLayerID);
                this.alphaLayer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.AlphaRenderLayerID);
                this.linearClampSampler = this.assetsService.Load<SamplerState>(DefaultResourcesIDs.LinearClampSamplerID);
                this.linearWrapSampler = this.assetsService.Load<SamplerState>(DefaultResourcesIDs.LinearWrapSamplerID);
            }
        }

        private void FreeResources()
        {
            for (int i = 0; i < this.bufferInfos.Length; i++)
            {
                this.bufferInfos[i].Dispose();
            }

            this.glbModel = null;
            this.materials.Clear();
            this.images.Clear();
            this.meshes.Clear();
            this.meshContainers.Clear();
            this.rootIndices.Clear();
            this.binaryChunk = null;
        }

        private async Task<Model> ReadGLB(Stream stream)
        {
            Model model = null;

            if (stream == null || !stream.CanRead)
            {
                throw new ArgumentException("Invalid parameter. Stream must be readable", "imageStream");
            }

            var result = GLBHelpers.LoadModel(stream);
            this.glbModel = result.Gltf;
            this.binaryChunk = result.Data;

            this.ReadBuffers();
            this.ReadSkins();
            await this.ReadDefaultScene();
            this.ReadAnimations();

            var materialCollection = new List<(string, Guid)>();
            foreach (var materialInfo in this.materials.Values)
            {
                this.assetsService.RegisterInstance<Material>(materialInfo.material);
                materialCollection.Add((materialInfo.name, materialInfo.material.Id));
            }

            model = new Model()
            {
                MeshContainers = this.meshContainers.ToArray(),
                AllNodes = this.allNodes.ToArray(),
                Materials = materialCollection,
                RootNodes = this.rootIndices.ToArray(),
                Animations = this.animations,
                Skins = this.skins,
            };

            // Compute global bounding box
            model.RefreshBoundingBox();

            return model;
        }

        private void ReadBuffers()
        {
            // read all buffers
            int numBuffers = this.glbModel.Buffers.Length;
            this.bufferInfos = new BufferInfo[numBuffers];
            for (int i = 0; i < numBuffers; ++i)
            {
                this.bufferInfos[i] = new BufferInfo(this.glbModel.LoadBinaryBuffer(i, this.GetExternalFileSolver));
            }
        }

        private async Task ReadDefaultScene()
        {
            if (this.glbModel.Scene.HasValue)
            {
                var defaultSceneId = this.glbModel.Scene.Value;
                var scene = this.glbModel.Scenes[defaultSceneId];
                var nodeCount = scene.Nodes.Length;

                this.allNodes = new NodeContent[this.glbModel.Nodes.Length];
                for (int n = 0; n < nodeCount; n++)
                {
                    int nodeId = scene.Nodes[n];
                    var rootNode = await this.ReadNode(nodeId);
                    this.rootIndices.Add(rootNode.index);
                }
            }
            else
            {
                throw new Exception("GLB not defines any scene");
            }
        }

        private async Task<(NodeContent node, int index)> ReadNode(int nodeId)
        {
            var node = this.glbModel.Nodes[nodeId];

            // Process the children
            NodeContent[] children = null;
            int[] childIndices = null;
            if (node.Children != null)
            {
                children = new NodeContent[node.Children.Length];
                childIndices = new int[node.Children.Length];
                for (int c = 0; c < node.Children.Length; c++)
                {
                    int childNodeId = node.Children[c];
                    var childNode = await this.ReadNode(childNodeId);

                    children[c] = childNode.node;
                    childIndices[c] = childNode.index;
                }
            }

            // Get Matrices
            Vector3 position;
            Quaternion orientation;
            Vector3 scale;

            Matrix4x4 transform = node.Matrix.ToEvergineMatrix();
            if (transform != Matrix4x4.Identity)
            {
                position = transform.Translation;
                orientation = transform.Orientation;
                scale = transform.Scale;
            }
            else
            {
                position = node.Translation.ToEvergineVector3();
                orientation = node.Rotation.ToEvergineQuaternion();
                scale = node.Scale.ToEvergineVector3();
            }

            // Read mesh
            List<Mesh> nodePrimitives = null;
            MeshContainer meshContainer = null;
            if (node.Mesh.HasValue)
            {
                int meshId = node.Mesh.Value;
                var glbMesh = this.glbModel.Meshes[meshId];

                if (!this.meshes.TryGetValue(meshId, out nodePrimitives))
                {
                    nodePrimitives = new List<Mesh>(glbMesh.Primitives.Length);
                    for (int p = 0; p < glbMesh.Primitives.Length; p++)
                    {
                        var primitiveInfo = glbMesh.Primitives[p];
                        var primitive = await this.ReadPrimitive(primitiveInfo);
                        nodePrimitives.Add(primitive);
                    }

                    this.meshes[meshId] = nodePrimitives;
                }

                meshContainer = new MeshContainer()
                {
                    Name = string.IsNullOrEmpty(glbMesh.Name) ? $"_Mesh_{meshId}" : this.MakeSafeName(glbMesh.Name),
                    Meshes = nodePrimitives,
                    Skin = node.Skin != null ? this.skins[node.Skin.Value] : null,
                    MorphTargetCount = 0,
                    MorphTargets = null,
                    MorphTargetWeights = new float[0],
                };
                meshContainer.RefreshBoundingBox();

                this.meshContainers.Add(meshContainer);
            }

            // Create node content
            var nodeContent = new NodeContent()
            {
                Name = string.IsNullOrEmpty(node.Name) ? $"_Node_{nodeId}" : this.MakeSafeName(node.Name),
                Translation = position,
                Orientation = orientation,
                Scale = scale,
                Children = children,
                ChildIndices = childIndices,
                Mesh = meshContainer,
                Skin = node.Skin.HasValue ? this.skins[node.Skin.Value] : null,
            };

            this.allNodes[nodeId] = nodeContent;

            // Set parent to the children
            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    children[i].Parent = nodeContent;
                }
            }

            return (nodeContent, nodeId);
        }

        private async Task<Mesh> ReadPrimitive(MeshPrimitive primitive)
        {
            // Create Vertex Buffers
            var attributes = primitive.Attributes.ToArray();

            List<VertexBuffer> vertexBuffersList = new List<VertexBuffer>();

            var sortedAttributes = this.SortAttributes(attributes);

            BoundingBox meshBounding = default;
            bool vertexColorEnabled = false;
            int lastBufferViewId = -1;
            LayoutDescription currentLayout = null;
            uint lastAttributeSizeInBytes = 0;
            for (int i = 0; i < sortedAttributes.Length; i++)
            {
                var attributeName = sortedAttributes[i].name;

                if (attributeName.Contains("COLOR"))
                {
                    vertexColorEnabled = true;
                }

                var accessor = sortedAttributes[i].accessor;
                int bufferViewId = accessor.BufferView.Value;
                var bufferView = this.glbModel.BufferViews[bufferViewId];

                ElementDescription elementDesc = this.GetElementFromAttribute(attributeName, accessor);

                if (bufferViewId != lastBufferViewId ||
                    accessor.ByteOffset >= lastAttributeSizeInBytes)
                {
                    lastBufferViewId = bufferViewId;
                    IntPtr attributePointer = this.bufferInfos[bufferView.Buffer].bufferPointer + bufferView.ByteOffset + accessor.ByteOffset;
                    int dataSize = this.SizeInBytes(accessor);
                    int strideInBytes = bufferView.ByteStride.HasValue ? bufferView.ByteStride.Value : dataSize;

                    uint attributeSizeInBytes = (uint)(strideInBytes * accessor.Count);
                    lastAttributeSizeInBytes = attributeSizeInBytes;

                    Buffer buffer = null;
                    await EvergineForegroundTask.Run(() =>
                    {
                        BufferDescription bufferDesc = new BufferDescription(
                                                                            attributeSizeInBytes,
                                                                            BufferFlags.ShaderResource | BufferFlags.VertexBuffer,
                                                                            ResourceUsage.Default,
                                                                            ResourceCpuAccess.None,
                                                                            strideInBytes);

                        buffer = this.graphicsContext.Factory.CreateBuffer(attributePointer, ref bufferDesc);
                    });
                    
                    currentLayout = new LayoutDescription()
                                                  .Add(elementDesc);
                    vertexBuffersList.Add(new VertexBuffer(buffer, currentLayout));
                }
                else
                {
                    currentLayout.Add(elementDesc);
                }

                // Create bounding box
                if (elementDesc.Semantic == ElementSemanticType.Position && elementDesc.SemanticIndex == 0)
                {
                    meshBounding = new BoundingBox(
                        new Vector3(accessor.Min[0], accessor.Min[1], accessor.Min[2]),
                        new Vector3(accessor.Max[0], accessor.Max[1], accessor.Max[2]));
                }
            }

            VertexBuffer[] meshVertexBuffers = vertexBuffersList.ToArray();

            // Create Index buffer
            var indicesAccessor = this.glbModel.Accessors[primitive.Indices.Value];
            var indicesbufferView = this.glbModel.BufferViews[indicesAccessor.BufferView.Value];

            IntPtr indicesPointer = this.bufferInfos[indicesbufferView.Buffer].bufferPointer + indicesbufferView.ByteOffset + indicesAccessor.ByteOffset;
            var indexFormatInfo = this.GetIndexFormat(indicesAccessor.ComponentType);
            uint indexSizeInBytes = (uint)(indexFormatInfo.size * indicesAccessor.Count);
            int indexStrideInBytes = indicesbufferView.ByteStride.HasValue ? indicesbufferView.ByteStride.Value : 0;

            IndexBuffer indexBuffer = null;
            await EvergineForegroundTask.Run(() =>
            {
                var iBufferDesc = new BufferDescription(indexSizeInBytes, BufferFlags.IndexBuffer, ResourceUsage.Default, ResourceCpuAccess.None, indexStrideInBytes);
                Buffer iBuffer = this.graphicsContext.Factory.CreateBuffer(indicesPointer, ref iBufferDesc);
                indexBuffer = new IndexBuffer(iBuffer, indexFormatInfo.format, flipWinding: true);
            });

            // Get Topology
            primitive.Mode.ToEverginePrimitive(out var primitiveTopology);

            // Get material
            int materialIndex = 0;
            if (primitive.Material.HasValue)
            {
                int materialId = primitive.Material.Value;
                materialIndex = await this.ReadMaterial(materialId, vertexColorEnabled);
            }

            // Create Mesh
            return new Mesh(meshVertexBuffers, indexBuffer, primitiveTopology)
            {
                BoundingBox = meshBounding,
                MaterialIndex = materialIndex,
            };
        }

        private (string name, Accessor accessor)[] SortAttributes(KeyValuePair<string, int>[] attributes)
        {
            List<(string name, Accessor accessor)> sortedAttriburtes = new List<(string name, Accessor accessor)>(attributes.Length);
            for (int i = 0; i < attributes.Length; i++)
            {
                var attribute = attributes[i];
                sortedAttriburtes.Add((attribute.Key, this.glbModel.Accessors[attribute.Value]));
            }

            sortedAttriburtes.Sort((a, b) =>
            {
                var res = a.accessor.BufferView.Value.CompareTo(b.accessor.BufferView.Value);
                if (res == 0)
                {
                    res = a.accessor.ByteOffset.CompareTo(b.accessor.ByteOffset);
                }

                return res;
            });

            return sortedAttriburtes.ToArray();
        }

        private ElementDescription GetElementFromAttribute(string name, Accessor accessor)
        {
            var semanticSplit = name.Split('_');
            var usageStr = semanticSplit[0];
            ElementSemanticType semantic;
            uint semanticIndex;

            switch (usageStr)
            {
                default:
                case "POSITION":
                    semantic = ElementSemanticType.Position;
                    break;
                case "NORMAL":
                    semantic = ElementSemanticType.Normal;
                    break;
                case "TANGENT":
                    semantic = ElementSemanticType.Tangent;
                    break;
                case "TEXCOORD":
                    semantic = ElementSemanticType.TexCoord;
                    break;
                case "COLOR":
                    semantic = ElementSemanticType.Color;
                    break;
                case "JOINTS":
                    semantic = ElementSemanticType.BlendIndices;
                    break;
                case "WEIGHTS":
                    semantic = ElementSemanticType.BlendWeight;
                    break;
            }

            // Usage index
            if (semanticSplit.Length > 1)
            {
                semanticIndex = uint.Parse(semanticSplit[1]);
            }
            else
            {
                semanticIndex = 0;
            }

            // Element Format
            ElementFormat format;
            switch (accessor.ComponentType)
            {
                case Accessor.ComponentTypeEnum.BYTE:
                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = accessor.Normalized ? ElementFormat.ByteNormalized : ElementFormat.Byte;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = accessor.Normalized ? ElementFormat.Byte2Normalized : ElementFormat.Byte2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = accessor.Normalized ? ElementFormat.Byte3Normalized : ElementFormat.Byte3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = accessor.Normalized ? ElementFormat.Byte4Normalized : ElementFormat.Byte4;
                            break;
                    }

                    break;
                case Accessor.ComponentTypeEnum.UNSIGNED_BYTE:
                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = accessor.Normalized ? ElementFormat.UByteNormalized : ElementFormat.UByte;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = accessor.Normalized ? ElementFormat.UByte2Normalized : ElementFormat.UByte2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = accessor.Normalized ? ElementFormat.UByte3Normalized : ElementFormat.UByte3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = accessor.Normalized ? ElementFormat.UByte4Normalized : ElementFormat.UByte4;
                            break;
                    }

                    break;

                case Accessor.ComponentTypeEnum.SHORT:
                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = accessor.Normalized ? ElementFormat.ShortNormalized : ElementFormat.Short;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = accessor.Normalized ? ElementFormat.Short2Normalized : ElementFormat.Short2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = accessor.Normalized ? ElementFormat.Short3Normalized : ElementFormat.Short3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = accessor.Normalized ? ElementFormat.Short4Normalized : ElementFormat.Short4;
                            break;
                    }

                    break;

                case Accessor.ComponentTypeEnum.UNSIGNED_SHORT:
                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = accessor.Normalized ? ElementFormat.UShortNormalized : ElementFormat.UShort;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = accessor.Normalized ? ElementFormat.UShort2Normalized : ElementFormat.UShort2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = accessor.Normalized ? ElementFormat.UShort3Normalized : ElementFormat.UShort3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = accessor.Normalized ? ElementFormat.UShort4Normalized : ElementFormat.UShort4;
                            break;
                    }

                    break;

                case Accessor.ComponentTypeEnum.UNSIGNED_INT:
                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = ElementFormat.UInt;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = ElementFormat.UInt2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = ElementFormat.UInt3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = ElementFormat.UInt4;
                            break;
                    }

                    break;

                default:
                case Accessor.ComponentTypeEnum.FLOAT:

                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = ElementFormat.Float;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = ElementFormat.Float2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = ElementFormat.Float3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = ElementFormat.Float4;
                            break;
                    }

                    break;
            }

            return new ElementDescription(format, semantic, semanticIndex);
        }

        private int SizeInBytes(Accessor accessor)
        {
            switch (accessor.ComponentType)
            {
                case Accessor.ComponentTypeEnum.BYTE:
                case Accessor.ComponentTypeEnum.UNSIGNED_BYTE:
                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            return 1;

                        case Accessor.TypeEnum.VEC2:
                            return 2;

                        case Accessor.TypeEnum.VEC3:
                            return 3;

                        case Accessor.TypeEnum.VEC4:
                            return 4;
                    }

                case Accessor.ComponentTypeEnum.SHORT:
                case Accessor.ComponentTypeEnum.UNSIGNED_SHORT:

                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            return 2;

                        case Accessor.TypeEnum.VEC2:
                            return 4;

                        case Accessor.TypeEnum.VEC3:
                            return 6;

                        case Accessor.TypeEnum.VEC4:
                            return 8;
                    }

                case Accessor.ComponentTypeEnum.UNSIGNED_INT:
                case Accessor.ComponentTypeEnum.FLOAT:

                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            return 4;

                        case Accessor.TypeEnum.VEC2:
                            return 8;

                        case Accessor.TypeEnum.VEC3:
                            return 12;

                        case Accessor.TypeEnum.VEC4:
                            return 16;

                        case Accessor.TypeEnum.MAT4:
                            return 64;
                    }
            }

            throw new Exception($"Accessor type {accessor.ComponentType} not supported");
        }

        private (IndexFormat format, int size) GetIndexFormat(Accessor.ComponentTypeEnum componentType)
        {
            switch (componentType)
            {
                default:
                case Accessor.ComponentTypeEnum.SHORT:
                case Accessor.ComponentTypeEnum.UNSIGNED_SHORT:
                    return (IndexFormat.UInt16, 2);
                case Accessor.ComponentTypeEnum.UNSIGNED_INT:
                    return (IndexFormat.UInt32, 4);
            }
        }

        private async Task<int> ReadMaterial(int materialId, bool vertexColorEnabled)
        {
            var glbMaterial = this.glbModel.Materials[materialId];
            if (!this.materials.ContainsKey(materialId))
            {
                // Get the base color
                LinearColor baseColor = new LinearColor(1, 1, 1, 1);
                Texture baseColorTexture = null;
                SamplerState baseColorSampler = null;
                if (glbMaterial.PbrMetallicRoughness != null)
                {
                    baseColor = glbMaterial.PbrMetallicRoughness.BaseColorFactor.ToLinearColor();

                    // Get the baseColor texture
                    if (this.glbModel.Images != null && glbMaterial.PbrMetallicRoughness.BaseColorTexture != null)
                    {
                        var textureId = glbMaterial.PbrMetallicRoughness.BaseColorTexture.Index;
                        var result = await this.ReadTexture(textureId);

                        baseColorTexture = result.texture;
                        baseColorSampler = result.sampler;
                    }
                }
                else if (glbMaterial.Extensions != null && glbMaterial.Extensions.TryGetValue("KHR_materials_pbrSpecularGlossiness", out var pbrSpecularGlossiness))
                {
                    var jobject = pbrSpecularGlossiness as JObject;
                    var diffuseFactorToken = jobject.SelectToken("diffuseFactor");
                    if (diffuseFactorToken != null && diffuseFactorToken.HasValues)
                    {
                        baseColor = new LinearColor(
                                                    (float)diffuseFactorToken[0],
                                                    (float)diffuseFactorToken[1],
                                                    (float)diffuseFactorToken[2],
                                                    (float)diffuseFactorToken[3]);
                    }

                    var diffuseTextureToken = jobject.SelectToken("diffuseTexture");
                    if (diffuseTextureToken != null && diffuseTextureToken.HasValues)
                    {
                        int textureId = (int)diffuseTextureToken["index"];
                        var result = await this.ReadTexture(textureId);

                        baseColorTexture = result.texture;
                        baseColorSampler = result.sampler;
                    }
                }

                Material material = null;
                if (this.materialAssigner == null)
                {
                    material = this.CreateEngineMaterial(baseColor.ToColor(), baseColorTexture, baseColorSampler, glbMaterial.AlphaMode, baseColor.A, glbMaterial.AlphaCutoff, vertexColorEnabled);
                }
                else
                {
                    material = this.materialAssigner(baseColor.ToColor(), baseColorTexture, baseColorSampler, glbMaterial.AlphaMode, baseColor.A, glbMaterial.AlphaCutoff, vertexColorEnabled);
                }

                this.materials.Add(materialId, (glbMaterial.Name, material));

                return this.materials.Count - 1;
            }

            return this.materials.Keys.ToList().IndexOf(materialId);
        }

        private Material CreateEngineMaterial(Color baseColor, Texture baseColorTexture, SamplerState baseColorSampler, AlphaModeEnum alphaMode, float alpha, float alphaCutOff, bool vertexColorEnabled)
        {
            RenderLayerDescription layer;
            switch (alphaMode)
            {
                default:
                case AlphaModeEnum.MASK:
                case AlphaModeEnum.OPAQUE:
                    layer = this.opaqueLayer;
                    break;
                case AlphaModeEnum.BLEND:
                    layer = alpha < 1.0f ? this.alphaLayer : this.opaqueLayer;
                    break;
            }

            var effect = this.assetsService.Load<Effect>(DefaultResourcesIDs.StandardEffectID);
            StandardMaterial material = new StandardMaterial(effect)
            {
                LightingEnabled = true,
                IBLEnabled = true,
                BaseColor = baseColor,
                Alpha = alpha,
                BaseColorTexture = baseColorTexture,
                BaseColorSampler = baseColorSampler,
                LayerDescription = layer,
                AlphaCutout = alphaCutOff,
            };

            if (vertexColorEnabled)
            {
                if (material.ActiveDirectivesNames.Contains("VCOLOR"))
                {
                    var directivesArray = material.ActiveDirectivesNames;
                    Array.Resize(ref directivesArray, directivesArray.Length + 1);
                    directivesArray[directivesArray.Length - 1] = "VCOLOR";
                    material.ActiveDirectivesNames = directivesArray;
                }
            }

            return material.Material;
        }

        private async Task<(Texture texture, SamplerState sampler)> ReadTexture(int textureId)
        {
            // Get texture info
            if (textureId > this.glbModel.Textures.Length)
            {
                return (null, null);
            }

            var glbTexture = this.glbModel.Textures[textureId];
            Texture texture = null;
            SamplerState sampler = null;

            // Get image info
            int imageId = -1;
            if (glbTexture.Source.HasValue)
            {
                imageId = glbTexture.Source.Value;
            }

            if (imageId >= 0 && imageId < this.glbModel.Images.Length)
            {
                var glbImage = this.glbModel.Images[imageId];
                if (glbImage.BufferView.HasValue)
                {
                    if (this.images.TryGetValue(imageId, out var textureCached))
                    {
                        texture = textureCached;
                    }
                    else
                    {
                        texture = await this.ReadImage(imageId);
                    }
                }
            }

            // Get sampler info
            int samplerId = -1;
            if (glbTexture.Sampler.HasValue)
            {
                samplerId = glbTexture.Sampler.Value;
            }

            if (samplerId >= 0 && samplerId < this.glbModel.Samplers.Length)
            {
                var glbSampler = this.glbModel.Samplers[samplerId];
                sampler = glbSampler.WrapS == Sampler.WrapSEnum.CLAMP_TO_EDGE ? this.linearClampSampler : this.linearWrapSampler;
            }

            return (texture, sampler);
        }

        private async Task<Texture> ReadImage(int imageId)
        {
            Texture result = null;

            using (Stream fileStream = this.glbModel.OpenImageFile(imageId, this.GetExternalFileSolver))
            {                
                using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fileStream))
                {
                    RawImageLoader.CopyImageToArrayPool(image, out _, out byte[] data);
                    await EvergineForegroundTask.Run(() =>
                    {
                        TextureDescription desc = new TextureDescription()
                        {
                            Type = TextureType.Texture2D,
                            Width = (uint)image.Width,
                            Height = (uint)image.Height,
                            Depth = 1,
                            ArraySize = 1,
                            Faces = 1,
                            Usage = ResourceUsage.Default,
                            CpuAccess = ResourceCpuAccess.None,
                            Flags = TextureFlags.ShaderResource,
                            Format = PixelFormat.R8G8B8A8_UNorm,
                            MipLevels = 1,
                            SampleCount = TextureSampleCount.None,
                        };
                        result = this.graphicsContext.Factory.CreateTexture(ref desc);

                        this.graphicsContext.UpdateTextureData(result, data);
                    });
                }

                // Read
                fileStream.Flush();
            }

            return result;
        }

        private byte[] GetExternalFileSolver(string empty)
        {
            return this.binaryChunk;
        }

        private string MakeSafeName(string name)
        {
            foreach (char c in this.invalidNameCharacters)
            {
                name = name.Replace(c, '_');
            }

            return name;
        }

        private void ReadAnimations()
        {
            if (this.glbModel.Animations == null) return;

            var animationCount = this.glbModel.Animations?.Length;
            for (int i = 0; i < animationCount; i++)
            {
                var gltfAnimation = this.glbModel.Animations[i];
                string name = string.IsNullOrEmpty(gltfAnimation.Name) ? $"Track{i}" : gltfAnimation.Name;
                this.animations.Add(name, this.ReadAnimation(name, gltfAnimation));
            }
        }

        private AnimationClip ReadAnimation(string name, Animation gltfAnimation)
        {
            // Asserts
            if (gltfAnimation.Channels == null) return null;

            var clip = new AnimationClip()
            {
                Name = name,
                Duration = 0,
            };

            // Read Channels
            var channelCount = gltfAnimation.Channels.Length;
            for (int i = 0; i < channelCount; i++)
            {
                var gltfChannel = gltfAnimation.Channels[i];
                var gltfSampler = gltfAnimation.Samplers[gltfChannel.Sampler];
                var animationChannel = this.ReadAnimationChannel(clip, gltfChannel, gltfSampler);

                clip.Duration = Math.Max(clip.Duration, animationChannel.Duration);
                clip.AddChannel(animationChannel);
            }

            return clip;
        }

        private Evergine.Framework.Animation.AnimationChannel ReadAnimationChannel(AnimationClip clip, glTFLoader.Schema.AnimationChannel gltfChannel, AnimationSampler gltfSampler)
        {
            // Input Accessor
            var inputAccessor = this.glbModel.Accessors[gltfSampler.Input];
            var inputBufferView = this.glbModel.BufferViews[inputAccessor.BufferView.Value];
            var inputOffset = inputBufferView.ByteOffset + inputAccessor.ByteOffset;
            var inputStride = inputBufferView.ByteStride.HasValue ? inputBufferView.ByteStride.Value : this.SizeInBytes(inputAccessor);
            var inputBuffer = this.bufferInfos[inputBufferView.Buffer];

            // Output Accessor
            var outputAccessor = this.glbModel.Accessors[gltfSampler.Output];
            var outputBufferView = this.glbModel.BufferViews[outputAccessor.BufferView.Value];
            var outputOffset = outputBufferView.ByteOffset + outputAccessor.ByteOffset;
            var outputStride = outputBufferView.ByteStride.HasValue ? outputBufferView.ByteStride.Value : this.SizeInBytes(outputAccessor);
            var outputBuffer = this.bufferInfos[outputBufferView.Buffer];

            // Create animationChannel object
            var animationChannel = new Evergine.Framework.Animation.AnimationChannel()
            {
                NodeIndex = gltfChannel.Target.Node.Value,
                Duration = inputAccessor.Max.Length > 0 ? inputAccessor.Max[0] : 0,
                Track = clip,
            };

            float curveDuration = 0;
            switch (gltfChannel.Target.Path)
            {
                case AnimationChannelTarget.PathEnum.translation:
                    var positionCurve = new AnimationCurveVector3();

                    positionCurve.KeyCount = inputAccessor.Count;
                    positionCurve.Keyframes = new AnimationKeyframe<Vector3>[positionCurve.KeyCount];
                    for (int i = 0; i < positionCurve.KeyCount; i++)
                    {
                        positionCurve.Keyframes[i].Time = GLBHelpers.GetFloatFromBuffer(inputBuffer, inputOffset, inputStride, i);
                        positionCurve.Keyframes[i].Value = GLBHelpers.GetVector3FromBuffer(outputBuffer, outputOffset, outputStride, i);
                    }

                    positionCurve.StartTime = positionCurve.Keyframes[0].Time;
                    positionCurve.EndTime = positionCurve.Keyframes[positionCurve.Keyframes.Length - 1].Time;
                    curveDuration = positionCurve.EndTime - positionCurve.StartTime;

                    animationChannel.ComponentType = typeof(Transform3D);
                    animationChannel.PropertyName = nameof(Transform3D.LocalPosition);
                    animationChannel.Curve = positionCurve;

                    break;
                case AnimationChannelTarget.PathEnum.rotation:
                    var quaternionCurve = new AnimationCurveQuaternion();

                    quaternionCurve.KeyCount = inputAccessor.Count;
                    quaternionCurve.Keyframes = new AnimationKeyframe<Quaternion>[quaternionCurve.KeyCount];

                    for (int i = 0; i < quaternionCurve.KeyCount; i++)
                    {
                        quaternionCurve.Keyframes[i].Time = GLBHelpers.GetFloatFromBuffer(inputBuffer, inputOffset, inputStride, i);
                        quaternionCurve.Keyframes[i].Value = GLBHelpers.GetQuaternionFromBuffer(outputBuffer, outputOffset, outputStride, i);
                    }

                    quaternionCurve.StartTime = quaternionCurve.Keyframes[0].Time;
                    quaternionCurve.EndTime = quaternionCurve.Keyframes[quaternionCurve.Keyframes.Length - 1].Time;
                    curveDuration = quaternionCurve.EndTime - quaternionCurve.StartTime;

                    animationChannel.ComponentType = typeof(Transform3D);
                    animationChannel.PropertyName = nameof(Transform3D.LocalOrientation);
                    animationChannel.Curve = quaternionCurve;

                    break;
                case AnimationChannelTarget.PathEnum.scale:
                    var scaleCurve = new AnimationCurveVector3();

                    scaleCurve.KeyCount = inputAccessor.Count;
                    scaleCurve.Keyframes = new AnimationKeyframe<Vector3>[scaleCurve.KeyCount];

                    for (int i = 0; i < scaleCurve.KeyCount; i++)
                    {
                        scaleCurve.Keyframes[i].Time = GLBHelpers.GetFloatFromBuffer(inputBuffer, inputOffset, inputStride, i);
                        scaleCurve.Keyframes[i].Value = GLBHelpers.GetVector3FromBuffer(outputBuffer, outputOffset, outputStride, i);
                    }

                    scaleCurve.StartTime = scaleCurve.Keyframes[0].Time;
                    scaleCurve.EndTime = scaleCurve.Keyframes[scaleCurve.KeyCount - 1].Time;
                    curveDuration = scaleCurve.EndTime - scaleCurve.StartTime;

                    animationChannel.ComponentType = typeof(Transform3D);
                    animationChannel.PropertyName = nameof(Transform3D.LocalScale);
                    animationChannel.Curve = scaleCurve;

                    break;
                case AnimationChannelTarget.PathEnum.weights:

                    int weightsCount = outputAccessor.Count / inputAccessor.Count;
                    var weightsCurve = new AnimationCurveFloatArray(weightsCount);

                    weightsCurve.KeyCount = inputAccessor.Count;
                    weightsCurve.Keyframes = new AnimationKeyframe<float[]>[weightsCurve.KeyCount];

                    for (int i = 0; i < weightsCurve.KeyCount; i++)
                    {
                        weightsCurve.Keyframes[i].Time = GLBHelpers.GetFloatFromBuffer(inputBuffer, inputOffset, inputStride, i);
                        weightsCurve.Keyframes[i].Value = GLBHelpers.GetFloatArrayFromBuffer(outputBuffer, outputOffset, outputStride, i, weightsCount);
                    }

                    weightsCurve.StartTime = weightsCurve.Keyframes[0].Time;
                    weightsCurve.EndTime = weightsCurve.Keyframes[weightsCurve.KeyCount - 1].Time;
                    curveDuration = weightsCurve.EndTime - weightsCurve.StartTime;

                    animationChannel.ComponentType = typeof(SkinnedMeshRenderer);
                    animationChannel.PropertyName = nameof(SkinnedMeshRenderer.MorphTargetWeights);
                    animationChannel.Curve = weightsCurve;

                    break;
                default:
                    break;
            }

            animationChannel.Curve.Duration = curveDuration;

            return animationChannel;
        }

        private void ReadSkins()
        {
            if (this.glbModel.Skins == null) return;

            this.skins = new SkinContent[this.glbModel.Skins.Length];

            for (int i = 0; i < this.skins.Length; i++)
            {
                var gltfSkin = this.glbModel.Skins[i];

                Matrix4x4[] matrices = null;
                if (gltfSkin.InverseBindMatrices.HasValue)
                {
                    var inverseBindMatrices = gltfSkin.InverseBindMatrices.Value;

                    var matricesAccessor = this.glbModel.Accessors[inverseBindMatrices];
                    var matricesBufferView = this.glbModel.BufferViews[matricesAccessor.BufferView.Value];
                    var matricesStride = matricesBufferView.ByteStride.HasValue ? matricesBufferView.ByteStride.Value : this.SizeInBytes(matricesAccessor);
                    var matricesBuffer = this.bufferInfos[matricesBufferView.Buffer];

                    matrices = new Matrix4x4[matricesAccessor.Count];
                    for (int m = 0; m < matricesAccessor.Count; m++)
                    {
                        matrices[m] = GLBHelpers.GetMatrix4x4(matricesBuffer, matricesBufferView.ByteOffset, matricesStride, m);
                    }
                }

                var skinContent = new SkinContent()
                {
                    Name = gltfSkin.Name ?? string.Empty,
                    RootJoint = gltfSkin.Skeleton ?? 0,
                    Nodes = gltfSkin.Joints ?? null,
                    InverseBindPoses = matrices,
                };
                this.skins[i] = skinContent;
            }
        }
    }
}
