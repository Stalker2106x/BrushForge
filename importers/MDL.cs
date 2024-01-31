using Godot;
using Godot.Collections;
using System;
using System.IO;
using static IDSP;

public partial class MDL : DataPack
{
    public Header header;
    public Array<Texture> textures;
    public Array<MdlBone> bones;
    public Array<MdlSequence> sequences;
    public Array<MdlBodyPart> bodyParts;

    Array<Transform3D> boneLocalTransforms;
    Array<Transform3D> boneLocalTransformsInv;

    public partial class Header : GodotObject
    {
        string name; // Model name, 64 Chars
        UInt32 size;
        Vector3 eyesPosition;
        Vector3 min;
        Vector3 max;
        Vector3 bbMin;
        Vector3 bbMax;
        UInt32 flags;
        public UInt32 bonesCount;
        public UInt32 bonesOffset;
        public UInt32 boneControllersCount;
        public UInt32 boneControllersOffset;
        UInt32 hitboxCount;
        UInt32 hitboxOffset;
        public UInt32 sequencesCount;
        public UInt32 sequencesOffset;
        public UInt32 sequenceGroupsCount;
        public UInt32 sequenceGroupsOffset;
        public UInt32 texturesCount;
        public UInt32 texturesOffset;
        public UInt32 texturesDataOffset;
        UInt32 skinRefCount;
        UInt32 skinFamiliesCount;
        UInt32 skinsOffset;
        public UInt32 bodyPartsCount;
        public UInt32 bodyPartsOffset;
        UInt32 attachmentCount;
        UInt32 attachmentOffset;
        UInt32 soundTable;
        UInt32 soundOffset;
        UInt32 soundGroups;
        UInt32 soundGroupsOffset;
        UInt32 transitionsCount;
        UInt32 transitionsOffset;

        public Header(FileStream fs, BinaryReader reader)
        {
            name = ExtractString(reader.ReadBytes(64));
            size = reader.ReadUInt32();
            eyesPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            min = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            max = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            bbMin = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            bbMax = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            flags = reader.ReadUInt32();
            bonesCount = reader.ReadUInt32();
            bonesOffset = reader.ReadUInt32();
            boneControllersCount = reader.ReadUInt32();
            boneControllersOffset = reader.ReadUInt32();
            hitboxCount = reader.ReadUInt32();
            hitboxOffset = reader.ReadUInt32();
            sequencesCount = reader.ReadUInt32();
            sequencesOffset = reader.ReadUInt32();
            sequenceGroupsCount = reader.ReadUInt32();
            sequenceGroupsOffset = reader.ReadUInt32();
            texturesCount = reader.ReadUInt32();
            texturesOffset = reader.ReadUInt32();
            texturesDataOffset = reader.ReadUInt32();
            skinRefCount = reader.ReadUInt32();
            skinFamiliesCount = reader.ReadUInt32();
            skinsOffset = reader.ReadUInt32();
            bodyPartsCount = reader.ReadUInt32();
            bodyPartsOffset = reader.ReadUInt32();
            attachmentCount = reader.ReadUInt32();
            attachmentOffset = reader.ReadUInt32();
            soundTable = reader.ReadUInt32();
            soundOffset = reader.ReadUInt32();
            soundGroups = reader.ReadUInt32();
            soundGroupsOffset = reader.ReadUInt32();
            transitionsCount = reader.ReadUInt32();
            transitionsOffset = reader.ReadUInt32();
        }
    }

    public partial class MdlBone : GodotObject
    {
        public string name;
        public Int32 parentIndex;
        public UInt32 flags;
        public UInt32[] xyz;
        public UInt32[] rotXYZ;
        public Vector3 pos;
        public Vector3 rot;
        public Vector3 posScale;
        public Vector3 rotScale;
        public int index;

        public Transform3D transform;
        public Transform3D restTransform;

        public MdlBone(FileStream fs, BinaryReader reader, int boneIndex)
        {
            name = ExtractString(reader.ReadBytes(32));
            if (name == "") name = boneIndex.ToString();
            parentIndex = reader.ReadInt32();
            flags = reader.ReadUInt32(); // Looks unused
            xyz = new UInt32[3] {
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32()
            };
            rotXYZ = new UInt32[3] {
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32()
            };
            pos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            rot = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            posScale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            rotScale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            index = boneIndex;
            transform = Transform3D.Identity;
        }
    }

    partial class MdlBoneController : GodotObject
    {
        public UInt32 bone;
        public UInt32 type;
        public UInt32 start;
        public UInt32 end;
        public UInt32 rest;
        public UInt32 index;

        public MdlBoneController(FileStream fs, BinaryReader reader)
        {
            bone = reader.ReadUInt32();
            type = reader.ReadUInt32();
            start = reader.ReadUInt32();
            end = reader.ReadUInt32();
            rest = reader.ReadUInt32();
            index = reader.ReadUInt32();
        }
    }

    public partial class MdlBlend : GodotObject
    {
        public MdlFrame[] frames;

        public MdlBlend(BinaryReader reader, MdlSequence sequence, Array<MdlBone> bones, long startPosition, ushort[] boneOffsets)
        {
            frames = new MdlFrame[sequence.frameCount];
            for (var i = 0; i < frames.Length; i++)
            {
                frames[i] = new MdlFrame();
                frames[i].positions = new Vector3[bones.Count];
                frames[i].rotations = new Vector3[bones.Count];
            }

            for (var bone = 0; bone < bones.Count; bone++)
            {
                var boneValues = new short[6][];
                for (var j = 0; j < 6; j++)
                {
                    var offset = boneOffsets[bone * 6 + j];
                    if (offset <= 0)
                    {
                        boneValues[j] = new short[sequence.frameCount];
                        continue;
                    }

                    reader.BaseStream.Seek(startPosition + bone * 6 * 2 + offset, SeekOrigin.Begin);
                    boneValues[j] = MdlFrame.ReadAnimationFrameValues(reader, (int)sequence.frameCount);
                }

                for (var j = 0; j < sequence.frameCount; j++)
                {
                    frames[j].positions[bone] = new Vector3(boneValues[0][j], boneValues[1][j], boneValues[2][j]) * bones[bone].posScale;
                    frames[j].rotations[bone] = new Vector3(boneValues[3][j], boneValues[4][j], boneValues[5][j]) * bones[bone].rotScale;
                }
            }
        }
    }

    public partial class MdlFrame : GodotObject
    {
        public Vector3[] positions;
        public Vector3[] rotations;

        public static short[] ReadAnimationFrameValues(BinaryReader reader, int count)
        {
            /*
             * RLE data:
             * byte compressed_length - compressed number of values in the data
             * byte uncompressed_length - uncompressed number of values in run
             * short values[compressed_length] - values in the run, the last value is repeated to reach the uncompressed length
             */
            var values = new short[count];

            for (var i = 0; i < count; /* i = i */)
            {
                var run = reader.ReadBytes(2); // read the compressed and uncompressed lengths

                Int16[] vals = new Int16[run[0]];
                for (int v = 0; v < run[0]; v++)
                    vals[v] = reader.ReadInt16();
                for (var j = 0; j < run[1] && i < count; i++, j++)
                {
                    var idx = Math.Min(run[0] - 1, j); // value in the data or the last value if we're past the end
                    values[i] = vals[idx];
                }
            }

            return values;
        }

    }

    /*
     * 
     */

    public partial class MdlSequence : GodotObject
    {
        public string name;
        public float fps;
        public UInt32 flags;
        public UInt32 activity;
        public UInt32 actWeight;
        public UInt32 eventCount;
        public UInt32 eventOffset;
        public UInt32 frameCount;
        public UInt32 pivotCount;
        public UInt32 pivotOffset;
        public UInt32 motionType;
        public UInt32 motionBone;
        public Vector3 linearMovement;
        public UInt32 autoMovePosIndex;
        public UInt32 autoMoveAngleIndex;
        public Vector3 bbmin;
        public Vector3 bbmax;
        public UInt32 blendCount;
        public UInt32 animationOffset;
        public UInt32[] blendType;
        public float[] blendStart;
        public float[] blendEnd;
        public float blendParent;
        public UInt32 sequenceGroup;
        public UInt32 entryNode;
        public UInt32 exitNode;
        public UInt32 nodeFlags;
        public UInt32 nextSequence;

        public MdlBlend[] blends;

        public MdlSequence(FileStream fs, BinaryReader reader)
        {
            name = ExtractString(reader.ReadBytes(32));
            fps = reader.ReadSingle();
            flags = reader.ReadUInt32();
            activity = reader.ReadUInt32();
            actWeight = reader.ReadUInt32();
            eventCount = reader.ReadUInt32();
            eventOffset = reader.ReadUInt32();
            frameCount = reader.ReadUInt32();
            pivotCount = reader.ReadUInt32();
            pivotOffset = reader.ReadUInt32();
            motionType = reader.ReadUInt32();
            motionBone = reader.ReadUInt32();
            linearMovement = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            autoMovePosIndex = reader.ReadUInt32();
            autoMoveAngleIndex = reader.ReadUInt32();
            bbmin = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            bbmax = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            blendCount = reader.ReadUInt32();
            animationOffset = reader.ReadUInt32();
            blendType = new UInt32[2] { reader.ReadUInt32(), reader.ReadUInt32() };
            blendStart = new float[2] { reader.ReadSingle(), reader.ReadSingle() };
            blendEnd = new float[2] { reader.ReadSingle(), reader.ReadSingle() };
            blendParent = reader.ReadSingle();
            sequenceGroup = reader.ReadUInt32();
            entryNode = reader.ReadUInt32();
            exitNode = reader.ReadUInt32();
            nodeFlags = reader.ReadUInt32();
            nextSequence = reader.ReadUInt32();
        }

        public void Build(FileStream fs, BinaryReader reader, Array<MdlBone> bones)
        {

            blends = new MdlBlend[blendCount];
            var blendLength = 6 * bones.Count;

            reader.BaseStream.Seek(animationOffset, SeekOrigin.Begin);

            var animPosition = reader.BaseStream.Position;
            UInt16[] offsets = new UInt16[blendLength * blendCount];
            for (int i = 0; i < blendLength * blendCount; i++)
                offsets[i] = reader.ReadUInt16();
            for (var i = 0; i < blendCount; i++)
            {
                var blendOffsets = new ushort[blendLength];
                System.Array.Copy(offsets, blendLength * i, blendOffsets, 0, blendLength);

                var startPosition = animPosition + i * blendLength * 2;
                blends[i] = new MdlBlend(reader, this, bones, startPosition, blendOffsets);
            }
        }
    }

    partial class MdlSequenceGroup : GodotObject
    {
        public string label;
        public string name;
        public UInt32 data;

        public MdlSequenceGroup(FileStream fs, BinaryReader reader)
        {
            label = ExtractString(reader.ReadBytes(32));
            name = ExtractString(reader.ReadBytes(32));
            reader.ReadBytes(4); // Dummy
            data = reader.ReadUInt32();
        }
    }

    public partial class MdlTriVert : GodotObject
    {
        public Int16 vertexIndex; 
        public Int16 normalIndex; // Index in normals array
        public Int16 s;
        public Int16 t;

        public MdlTriVert(FileStream fs, BinaryReader reader)
        {
        }
    }
    /*
     * This class represents an array of GL triangle strips / fan, that we need to unpack
     */
    public partial class MdlTrianglePack : GodotObject
    {
        public Int16 trivertCount;
        public UInt16[] verticeIndexes; // Index in mesh vertices array
        public UInt16[] normalIndexes; // Index in mesh vertices array
        public UInt16[] s; // UV first coordinate
        public UInt16[] t; // UV second coordinate
        public int type; // -1 for triangle fans, 1 for triangle strips

        public static MdlTrianglePack Parse(FileStream fs, BinaryReader reader)
        {
            Int16 trivertCount = reader.ReadInt16();
            if (trivertCount == 0)
                return null; // Reached end
            MdlTrianglePack triangle = new MdlTrianglePack();
            triangle.trivertCount = Math.Abs(trivertCount);
            triangle.verticeIndexes = new UInt16[triangle.trivertCount];
            triangle.normalIndexes = new UInt16[triangle.trivertCount];
            triangle.s = new UInt16[triangle.trivertCount];
            triangle.t = new UInt16[triangle.trivertCount];
            for (int t = 0; t < triangle.trivertCount; t++)
            {
                triangle.verticeIndexes[t] = reader.ReadUInt16();
                triangle.normalIndexes[t] = reader.ReadUInt16();
                triangle.s[t] = reader.ReadUInt16();
                triangle.t[t] = reader.ReadUInt16();
            }
            triangle.type = Math.Sign(trivertCount);
            return triangle;
        }
    }

    public partial class MdlMesh : GodotObject
    {
        public UInt32 trianglesCount;
        public UInt32 trianglesOffset;
        public UInt32 skinRef;
        public UInt32 normalsCount;
        public UInt32 normalsOffset;

        public Array<MdlTrianglePack> trianglePacks;

        public Array<Vector3> sVerticesIndex; // Index connecting the final array in surfaceTool

        public MdlMesh(FileStream fs, BinaryReader reader)
        {
            trianglesCount = reader.ReadUInt32();
            trianglesOffset = reader.ReadUInt32();
            skinRef = reader.ReadUInt32();
            normalsCount = reader.ReadUInt32();
            normalsOffset = reader.ReadUInt32();
        }

        public void Parse(FileStream fs, BinaryReader reader)
        {
            // Parse tris
            fs.Seek(trianglesOffset, SeekOrigin.Begin);
            trianglePacks = new Array<MdlTrianglePack>();
            for (int i = 0; i < trianglesCount; i++)
            {
                MdlTrianglePack triangle = MdlTrianglePack.Parse(fs, reader);
                if (triangle == null) break; // Reached end of trianglePacks
                trianglePacks.Add(triangle);
            }
        }

        public ArrayMesh Build(Array<MdlBone> bones, Dictionary<string, Texture2D> gdTextures, Vector3[] allVertices, Vector3[] allNormals, Byte[] boneIndexes)
        {
            Array<SurfaceTool> sfTools = new Array<SurfaceTool>();
            // Build final vertices & normal arrays
            foreach (MdlTrianglePack trianglePack in trianglePacks)
            {
                SurfaceTool sfTool = new SurfaceTool();
                sfTool.Begin(Mesh.PrimitiveType.Triangles);
                for (int i = 0; i < trianglePack.trivertCount - 2; i++)
                {
                    for (int summit = 0; summit < 3; summit++)
                    {
                        // We have to populate surfacetool per triangle (3 vertex for each),
                        // (1)  In triangle strips (/\/\/\), we iterate on current summit + the next two verts, because they are also part of the triangle.
                        // (-1) In triangle fans (_\|/_), we iterate on first summit + the next two verts, because all triangles have the first summit of
                        //      the whole array in common.
                        // In both cases, we end with an offset of 2 to compensate.
                        // We store the index of current summit in triangle fan vertex array in the "trivertIndex" variable
                        int trivertIndex = -1;
                        if (trianglePack.type == 1)
                        {
                            switch (summit)
                            {
                                case 0:
                                    trivertIndex = i + 0;
                                    break;
                                case 1:
                                    trivertIndex = i % 2 == 1 ? i + 2 : i + 1;
                                    break;
                                case 2:
                                    trivertIndex = i % 2 == 1 ? i + 1 : i + 2;
                                    break;
                            }
                        }
                        else
                        {
                            trivertIndex = (summit == 0 ? 0 : i + summit);
                        }

                        int vertexIndex = trianglePack.verticeIndexes[trivertIndex];
                        Vector3 tVertex = allVertices[vertexIndex] * bones[boneIndexes[vertexIndex]].transform;
                        sfTool.SetNormal(allNormals[trianglePack.normalIndexes[trivertIndex]]);
                        sfTool.SetUV(new Vector2(trianglePack.s[trivertIndex], trianglePack.t[trivertIndex]));
                        sfTool.SetWeights(new float[] { 1.0f, 0.0f, 0.0f, 0.0f });
                        sfTool.SetBones(new int[] { boneIndexes[vertexIndex], 0, 0, 0 });
                        sfTool.AddVertex(tVertex);
                    }
                }
                sfTools.Add(sfTool);
            }
            ArrayMesh mesh = null;
            Array<string> keys = gdTextures.Keys as Array<string>;
            Texture2D texture = gdTextures[keys[(int)skinRef]];
            foreach (SurfaceTool sfTool in sfTools)
            {
                StandardMaterial3D mat = new StandardMaterial3D();
                mat.Uv1Scale /= new Vector3(texture.GetWidth(), texture.GetHeight(), 1);
                mat.AlbedoTexture = texture;
                sfTool.SetMaterial(mat);
                mesh = sfTool.Commit(mesh);
            }
            return mesh;
        }
    }

    public partial class MdlModel : GodotObject
    {
        public string name;
        public UInt32 type; // 0 for GoldSrc
        public float boundingRadius;
        public UInt32 meshCount;
        public UInt32 meshOffset;
        public UInt32 verticesCount;
        public UInt32 verticesInfoOffset;
        public UInt32 verticesOffset;
        public UInt32 normalsCount;
        public UInt32 normalsInfoOffset;
        public UInt32 normalsOffset;
        public UInt32 groupsCount;
        public UInt32 groupsOffset;

        public Vector3[] vertices;
        public Vector3[] normals;
        public Byte[] boneIndexes;

        public Array<MdlMesh> meshes;

        public MdlModel(FileStream fs, BinaryReader reader)
        {
            name = ExtractString(reader.ReadBytes(64));
            type = reader.ReadUInt32();
            boundingRadius = reader.ReadSingle();
            meshCount = reader.ReadUInt32();
            meshOffset = reader.ReadUInt32();
            verticesCount = reader.ReadUInt32();
            verticesInfoOffset = reader.ReadUInt32();
            verticesOffset = reader.ReadUInt32();
            normalsCount = reader.ReadUInt32();
            normalsInfoOffset = reader.ReadUInt32();
            normalsOffset = reader.ReadUInt32();
            groupsCount = reader.ReadUInt32();
            groupsOffset = reader.ReadUInt32();
        }


        public void Parse(FileStream fs, BinaryReader reader)
        {
            // Parse vertices
            fs.Seek(verticesOffset, SeekOrigin.Begin);
            vertices = new Vector3[verticesCount];
            for (int i = 0; i < verticesCount; i++)
            {
                vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            // Parse normals
            fs.Seek(normalsOffset, SeekOrigin.Begin);
            normals = new Vector3[normalsCount];
            for (int i = 0; i < normalsCount; i++)
            {
                normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            // Parse boneIndexes
            fs.Seek(verticesInfoOffset, SeekOrigin.Begin);
            boneIndexes = reader.ReadBytes((int)verticesCount);
            // Parse meshes
            fs.Seek(meshOffset, SeekOrigin.Begin);
            meshes = new Array<MdlMesh>();
            for (int i = 0; i < meshCount; i++)
            {
                meshes.Add(new MdlMesh(fs, reader));
            }
            foreach (MdlMesh mesh in meshes)
            {
                mesh.Parse(fs, reader);
            }
        }

        public void Build(Node3D modelNode, Array<MdlBone> bones, Dictionary<string, Texture2D> gdTextures)
        {
            foreach (MdlMesh mesh in meshes)
            {
                MeshInstance3D meshInstance = new MeshInstance3D();
                meshInstance.Name = name;
                meshInstance.Mesh = mesh.Build(bones, gdTextures, vertices, normals, boneIndexes);
                modelNode.AddChild(meshInstance);
            }
        }
    }

    public partial class MdlBodyPart : GodotObject
    {
        public string name;
        public UInt32 modelsCount;
        public UInt32 basee;
        public UInt32 modelsOffset;

        public MdlModel[] models;

        public MdlBodyPart(FileStream fs, BinaryReader reader)
        {
            name = ExtractString(reader.ReadBytes(64));
            modelsCount = reader.ReadUInt32();
            basee = reader.ReadUInt32();
            modelsOffset = reader.ReadUInt32();
        }

        public void Parse(FileStream fs, BinaryReader reader, Array<MdlBone> bones)
        {
            // Parse models
            fs.Seek(modelsOffset, SeekOrigin.Begin);
            models = new MdlModel[modelsCount];
            for (int i = 0; i < modelsCount; i++)
            {
                models[i] = new MdlModel(fs, reader);
            }
            foreach (MdlModel model in models)
            {
                model.Parse(fs, reader);
            }
        }

        public void Build(Node3D modelNode, Array<MdlBone> bones, Dictionary<string, Texture2D> gdTextures)
        {
            foreach (MdlModel model in models)
            {
                model.Build(modelNode, bones, gdTextures);
            }
        }
    }

    public partial class Texture : GodotObject
    {
        public string name; // Texture name, 64 Chars
        public UInt32 flags;
        public UInt32 width;
        public UInt32 height;
        public UInt32 textureOffset;

        public Byte[] data;
        public Color[] colorPalette;

        public Texture(FileStream fs, BinaryReader reader)
        {
            name = ExtractString(reader.ReadBytes(64));
            flags = reader.ReadUInt32();
            width = reader.ReadUInt32();
            height = reader.ReadUInt32();
            textureOffset = reader.ReadUInt32();
        }

        public void LoadData(FileStream fs, BinaryReader reader)
        {
            fs.Seek(textureOffset, SeekOrigin.Begin);
            data = reader.ReadBytes((int)width * (int)height);
            colorPalette = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                colorPalette[i] = new Color(reader.ReadByte() / 255.0f, reader.ReadByte() / 255.0f, reader.ReadByte() / 255.0f);
            }
        }
    }

    public Transform3D ConvertTransform(Vector3 pos, Vector3 rot)
    {
        Transform3D t = Transform3D.Identity;
        t.Origin = pos;
        t.Basis = t.Basis.Rotated(new Vector3(1, 0, 0), rot.X);
        t.Basis = t.Basis.Rotated(new Vector3(0, 1, 0), rot.Y);
        t.Basis = t.Basis.Rotated(new Vector3(0, 0, 1), rot.Z);
        return t;
    }

    public void boneItt3(MdlFrame frame, int bonesCount)
    {
        for (int boneIdx = 0; boneIdx < bonesCount; boneIdx++)
        {
            Vector3 boneRot = bones[boneIdx].rot;
            Vector3 bonePos = bones[boneIdx].pos;
            Vector3 framePos = frame.positions[boneIdx];
            Vector3 frameRot = frame.rotations[boneIdx];

            bones[boneIdx].restTransform = ConvertTransform(bonePos, boneRot);

            Transform3D animatedTransform = ConvertTransform(bonePos + framePos, boneRot + frameRot);
            boneLocalTransforms.Add(animatedTransform);
            boneLocalTransformsInv.Add(animatedTransform.Inverse());
        }

        for (int boneIdx = 0; boneIdx < bonesCount; boneIdx++)
        {
            Transform3D transform = boneLocalTransforms[boneIdx];

            int parentIdx = (int)bones[boneIdx].parentIndex;

            while (parentIdx >= 0)
            {
                transform *= boneLocalTransforms[parentIdx];
                parentIdx = (int)bones[parentIdx].parentIndex;
            }
            bones[boneIdx].transform = transform;
        }
    }

    override public void Import(FileStream fs, BinaryReader reader)
    {
        // Parse header
        header = new Header(fs, reader);
        // Parse textures
        textures = new Array<Texture>();
        fs.Seek(header.texturesOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.texturesCount; i++)
        {
            textures.Add(new Texture(fs, reader));
        }
        // Insert in GDTextures
        foreach (Texture texture in textures)
        {
            texture.LoadData(fs, reader);
            Image img = Image.Create((int)texture.width, (int)texture.height, false, Image.Format.Rgb8);
            for (int i = 0; i < texture.data.Length; i++)
            {
                img.SetPixel((int)(i % texture.width), (int)(i / texture.width), texture.colorPalette[texture.data[i]]);
            }
            gdTextures[texture.name] = ImageTexture.CreateFromImage(img);
        }
        // Parse bones
        bones = new Array<MdlBone>();
        fs.Seek(header.bonesOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.bonesCount; i++)
        {
            bones.Add(new MdlBone(fs, reader, i));
        }
        // Parse sequences
        sequences = new Array<MdlSequence>();
        fs.Seek(header.sequencesOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.sequencesCount; i++)
        {
            sequences.Add(new MdlSequence(fs, reader));
        }
        foreach (MdlSequence sequence in sequences)
        {
            sequence.Build(fs, reader, bones);
        }
        // Create transform arrays
        boneLocalTransforms = new Array<Transform3D>();
        boneLocalTransformsInv = new Array<Transform3D>();
        boneItt3(sequences[0].blends[0].frames[0], bones.Count);
        // Parse bodyParts
        bodyParts = new Array<MdlBodyPart>();
        fs.Seek(header.bodyPartsOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.bodyPartsCount; i++)
        {
            bodyParts.Add(new MdlBodyPart(fs, reader));
        }
        // Build Skel
        Skeleton3D skeletonNode = new Skeleton3D();
        skeletonNode.Name = "Model";
        foreach (MdlBone bone in bones)
        {
            skeletonNode.AddBone(bone.name);
        }
        for (int boneIdx = 0; boneIdx < bones.Count; boneIdx++)
        {
            MdlBone bone = bones[boneIdx];
            int sBoneIdx = skeletonNode.FindBone(bone.name);
            skeletonNode.SetBoneParent(sBoneIdx, (int)bone.parentIndex);
            skeletonNode.SetBoneRest(sBoneIdx, boneLocalTransforms[boneIdx]);
            skeletonNode.SetBonePosePosition(sBoneIdx, boneLocalTransforms[boneIdx].Origin);
            skeletonNode.SetBonePoseRotation(sBoneIdx, boneLocalTransforms[boneIdx].Basis.GetRotationQuaternion());
        }
        // Add animation
        AnimationPlayer animPlayer = new AnimationPlayer();
        animPlayer.Name = "anims";
        var firstAnim = true;
        var animLibrary = new AnimationLibrary();
        foreach (MdlSequence seq in sequences)
        {
            Animation anim = new Animation();
            var delta = 1 / seq.fps;

            var animationName = seq.name.ToLower();
            animLibrary.AddAnimation(animationName, anim);
            anim.Length = delta * seq.frameCount;

            if (firstAnim == true)
            {
                animPlayer.Autoplay = seq.name.ToLower();
                firstAnim = false;
            }

            if (seq.flags == 1)
                anim.LoopMode = Animation.LoopModeEnum.Linear;

            for (int boneIdx = 0; boneIdx < header.bonesCount; boneIdx++)
            {
                var bone = bones[boneIdx];
                var animParentPath = "../" + skeletonNode.Name + ":" + bone.name;
                var trackPosIdx = anim.AddTrack(Animation.TrackType.Position3D);
                var trackRotIdx = anim.AddTrack(Animation.TrackType.Rotation3D);
                var trackScaleIdx = anim.AddTrack(Animation.TrackType.Scale3D);
                anim.TrackSetPath(trackPosIdx, animParentPath);
                anim.TrackSetPath(trackRotIdx, animParentPath);
                anim.TrackSetPath(trackScaleIdx, animParentPath);

                for (int frame = 0; frame < seq.frameCount; frame++)
                {
                    var frameData = seq.blends[0].frames[frame];
                    var pos = bone.pos + frameData.positions[boneIdx];
                    var rot = bone.rot + frameData.rotations[boneIdx];

                    Transform3D transform = boneLocalTransforms[boneIdx] * (boneLocalTransformsInv[boneIdx] * ConvertTransform(pos, rot));

                    transform.Translated(pos);
                    var rotQuat = transform.Basis.GetRotationQuaternion();

                    var keyLocation = Vector3.Zero;
                    var keyRotation = Quaternion.Identity;
                    var keyScale = new Vector3(1, 1, 1);
                    if (transform.Origin != keyLocation || keyRotation != rotQuat)
                    {
                        keyLocation = transform.Origin;
                        keyRotation = rotQuat;
                        anim.PositionTrackInsertKey(trackPosIdx, frame * delta, keyLocation);
                        anim.RotationTrackInsertKey(trackRotIdx, frame * delta, keyRotation);
                        anim.ScaleTrackInsertKey(trackScaleIdx, frame * delta, keyScale);
                    }
                }
            }
        }
        // Build Mesh
        foreach (MdlBodyPart bodyPart in bodyParts)
        {
            bodyPart.Parse(fs, reader, bones);
        }
        foreach (MdlBodyPart bodyPart in bodyParts)
        {
            bodyPart.Build(skeletonNode, bones, gdTextures);
        }
        animPlayer.AddAnimationLibrary("", animLibrary);
        skeletonNode.AddChild(animPlayer);

        gdModel = skeletonNode;
    }
}
