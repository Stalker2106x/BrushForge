using Godot;
using Godot.Collections;
using System;
using System.IO;

public partial class MDL : DataPack
{
    public Header header;
    public Array<Texture> textures;
    public Array<MdlBone> bones;
    public Array<MdlSequence> sequences;
    public Array<MdlBodyPart> bodyParts;

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
            eyesPosition = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            min = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            max = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            bbMin = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            bbMax = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
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
        public Vector3 scaleP;
        public Vector3 scaleR;
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
            pos = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            rot = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            scaleP = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            scaleR = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
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

    public partial class MdlFrame : GodotObject
    {
        public Vector3[] pos; // Array of bone positions indexed by boneIdx
        public Vector3[] rot; // Array of bone rotations indexed by boneIdx

        public MdlFrame(int frameCount)
        {
            pos = new Vector3[frameCount];
            rot = new Vector3[frameCount];
        }
    }

    public partial class MdlBlend : GodotObject
    {
        public MdlFrame[] frames;

        public MdlBlend(FileStream fs, BinaryReader reader, Array<MdlBone> bones, UInt32 blendCount, UInt32 frameCount, UInt32 sequenceGroup)
        {
            // Group gives accurate file containing anims, unused here
            int bonesCount = bones.Count;
            // Parse pos and rot of each bone
            var offsetCount = blendCount * (6 * bonesCount);
            UInt16[] blendOffsets = new UInt16[offsetCount];
            for (int i = 0; i < offsetCount; i++)
            {
                blendOffsets[i] = reader.ReadUInt16();
            }

            frames = new MdlFrame[frameCount];
            for (int boneIndex = 0; boneIndex < bonesCount; boneIndex++)
            {
                MdlBone bone = bones[boneIndex];
                UInt16[][] boneFrameData = new UInt16[6][];
                for (int i = 0; i < 6; i++)
                {
                    UInt16[] animData = new UInt16[frameCount];
                    UInt16 offset = blendOffsets[boneIndex * 6 + i];
                    if (offset == 0)
                    {
                        for (int j = 0; j < frameCount; j++)
                        {
                            animData[j] = 0;
                        }
                    }
                    else
                    {
                        for (int j = 0; j < frameCount; j++)
                        {
                            Byte compressedSize = reader.ReadByte();
                            Byte uncompressedSize = reader.ReadByte();
                            UInt16[] compressedData = new UInt16[compressedSize];
                            for (int c = 0; c < compressedSize; c++)
                            {
                                compressedData[c] = reader.ReadUInt16();
                            }
                            int k = 0;
                            while (k < compressedSize && j < frameCount)
                            {
                                animData[j] = compressedData[Math.Min(compressedSize - 1, k)];
                                k++;
                                j++;
                            }
                        }
                    }
                    boneFrameData[i] = (animData);
                }
                for (int i = 0; i < frameCount; i++)
                {
                    MdlFrame frame = new MdlFrame((int)frameCount);
                    frame.pos[i] = new Vector3(boneFrameData[0][i], boneFrameData[1][i], boneFrameData[2][i]);
                    frame.rot[i] = new Vector3(boneFrameData[3][i], boneFrameData[4][i], boneFrameData[5][i]);
                    frames[i] = frame;
                }
            }
        }
    }

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
            linearMovement = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            autoMovePosIndex = reader.ReadUInt32();
            autoMoveAngleIndex = reader.ReadUInt32();
            bbmin = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            bbmax = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
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
            fs.Seek(animationOffset, SeekOrigin.Begin);
            blends = new MdlBlend[blendCount];
            for (int i = 0; i < blendCount; i++)
            {
                blends[i] = new MdlBlend(fs, reader, bones, blendCount, frameCount, sequenceGroup);
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
        public Int16 vertexIndex; // Index in vertices array
        public Int16 normalIndex; // Index in normals array
        public Int16 s;
        public Int16 t;

        public MdlTriVert(FileStream fs, BinaryReader reader)
        {
            vertexIndex = reader.ReadInt16();
            normalIndex = reader.ReadInt16();
            s = reader.ReadInt16();
            t = reader.ReadInt16();
        }
    }
    public partial class MdlTriangle : GodotObject
    {
        public MdlTriVert[] triverts;
        public int type; // -1 for triangle strips, 1 for triangle fans

        public static MdlTriangle Parse(FileStream fs, BinaryReader reader)
        {
            Int16 trivertCount = reader.ReadInt16();
            if (trivertCount == 0)
                return null; // Reached end
            MdlTriangle triangle = new MdlTriangle();
            triangle.triverts = new MdlTriVert[Math.Abs(trivertCount)];
            for (int t = 0; t < Math.Abs(trivertCount); t++)
            {
                triangle.triverts[t] = new MdlTriVert(fs, reader);
                triangle.type = Math.Sign(trivertCount);
            }
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

        public MdlTriangle[] triangles;
        public UInt32 normals;

        public MdlMesh(FileStream fs, BinaryReader reader)
        {
            trianglesCount = reader.ReadUInt32();
            trianglesOffset = reader.ReadUInt32();
            skinRef = reader.ReadUInt32();
            normalsCount = reader.ReadUInt32();
            normalsOffset = reader.ReadUInt32();
        }

        public void Build(FileStream fs, BinaryReader reader)
        {
            fs.Seek(trianglesOffset, SeekOrigin.Begin);
            triangles = new MdlTriangle[trianglesCount];
            // Build tris
            for (int i = 0; i < trianglesCount; i++)
            {
                MdlTriangle triangle = MdlTriangle.Parse(fs, reader);
                if (triangle == null) break;
                triangles[i] = triangle;
            }
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
        public Byte[] boneMap;

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


        public void Build(FileStream fs, BinaryReader reader, Array<MdlBone> bones)
        {
            // Parse vertices
            fs.Seek(verticesOffset, SeekOrigin.Begin);
            vertices = new Vector3[verticesCount];
            for (int i = 0; i < verticesCount; i++)
            {
                vertices[i] = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }
            // Parse normals
            fs.Seek(normalsOffset, SeekOrigin.Begin);
            normals = new Vector3[normalsCount];
            for (int i = 0; i < normalsCount; i++)
            {
                normals[i] = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }
            // Parse transformIndices
            fs.Seek(verticesInfoOffset, SeekOrigin.Begin);
            boneMap = reader.ReadBytes((int)verticesCount);
            // Parse mesh
            fs.Seek(meshOffset, SeekOrigin.Begin);
            meshes = new Array<MdlMesh>();
            for (int i = 0; i < meshCount; i++)
            {
                meshes.Add(new MdlMesh(fs, reader));
            }
            foreach (MdlMesh mesh in meshes)
            {
                mesh.Build(fs, reader);
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

        public MdlBodyPart(FileStream fs, BinaryReader reader, Array<MdlBone> bones)
        {
            name = ExtractString(reader.ReadBytes(64));
            modelsCount = reader.ReadUInt32();
            basee = reader.ReadUInt32();
            modelsOffset = reader.ReadUInt32();
        }

        public void Build(FileStream fs, BinaryReader reader, Array<MdlBone> bones)
        {
            fs.Seek(modelsOffset, SeekOrigin.Begin);
            models = new MdlModel[modelsCount];
            for (int i = 0; i < modelsCount; i++)
            {
                models[i] = new MdlModel(fs, reader);
            }
            // Build models
            for (int i = 0; i < modelsCount; i++)
            {
                models[i].Build(fs, reader, bones);
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
        // Parse bodyParts
        bodyParts = new Array<MdlBodyPart>();
        fs.Seek(header.bodyPartsOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.bodyPartsCount; i++)
        {
            bodyParts.Add(new MdlBodyPart(fs, reader, bones));
        }
        foreach (MdlBodyPart bodyPart in bodyParts)
        {
            bodyPart.Build(fs, reader, bones);
        }
        // Build model
        Skeleton3D skeletonNode = new Skeleton3D();
        skeletonNode.Name = "Model";
        foreach (MdlBone bone in bones)
        {
            skeletonNode.AddBone(bone.name);
        }
        gdModel = skeletonNode;
    }
}
