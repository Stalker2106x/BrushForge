using Godot;
using Godot.Collections;
using System;
using System.Data.Common;
using System.IO;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;

public partial class MDL : DataPack
{
    MdlHeader header;
    Array<MdlBone> bones;
    Array<MdlBoneController> boneControllers;
    Array<MdlSequence> sequences;
    Array<MdlSequenceGroup> sequenceGroups;
    Array<MdlTexture> textures;
    Array<MdlBodyPart> bodyParts;
    Array<MdlModel> models;

    partial class MdlHeader : GodotObject {
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

        public MdlHeader(FileStream fs, BinaryReader reader) {
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

    partial class MdlBone : GodotObject
    {
        public string name;
        public UInt32 parent;
        public UInt32 flags;
        public UInt32[] boneController;
        public UInt32[] value;
        public UInt32[] scale;

        public MdlBone(FileStream fs, BinaryReader reader)
        {
            name = ExtractString(reader.ReadBytes(32));
            parent = reader.ReadUInt32();
            flags = reader.ReadUInt32();
            boneController = new UInt32[6] {
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32()
            };
            value = new UInt32[6] {
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32()
            };
            scale = new UInt32[6] {
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32()
            };
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

    partial class MdlSequence : GodotObject
    {
        public string label;
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
        public UInt32 blendParent;
        public UInt32 sequenceGroup;
        public UInt32 entryNode;
        public UInt32 exitNode;
        public UInt32 nodeFlags;
        public UInt32 nextSequence;

        public MdlSequence(FileStream fs, BinaryReader reader)
        {
            label = ExtractString(reader.ReadBytes(32));
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
            blendParent = reader.ReadUInt32();
            sequenceGroup = reader.ReadUInt32();
            entryNode = reader.ReadUInt32();
            exitNode = reader.ReadUInt32();
            nodeFlags = reader.ReadUInt32();
            nextSequence = reader.ReadUInt32();
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

    partial class MdlTexture : GodotObject
    {
        public string name;
        public UInt32 flags;
        public UInt32 width;
        public UInt32 height;
        public UInt32 index;
        public UInt32 id;

        public MdlTexture(FileStream fs, BinaryReader reader)
        {
            name = ExtractString(reader.ReadBytes(64));
            flags = reader.ReadUInt32();
            width = reader.ReadUInt32();
            height = reader.ReadUInt32();
            index = reader.ReadUInt32();
            id = reader.ReadUInt32();
        }
    }

    partial class MdlMesh : GodotObject
    {
        public UInt32 trianglesCount;
        public UInt32 trianglesOffset;
        public UInt32 skinRef;
        public UInt32 normalsCount;
        public UInt32 normalsOffset;

        public UInt32[] triangles;
        public UInt32 normals;

        public MdlMesh(FileStream fs, BinaryReader reader)
        {
            trianglesCount = reader.ReadUInt32();
            trianglesOffset = reader.ReadUInt32();
            skinRef = reader.ReadUInt32();
            normalsCount = reader.ReadUInt32();
            normalsOffset = reader.ReadUInt32();
        }

        public void build(FileStream fs, BinaryReader reader)
        {
            // Build tris
            fs.Seek(trianglesOffset, SeekOrigin.Begin);
            triangles = new UInt32[trianglesCount];
            for (int i = 0; i < trianglesCount; i++)
            {
                triangles[i] = 0; // Wtf?
            }
        }
    }

    partial class MdlModel : GodotObject
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

        public Array<Vector3> vertices;
        public Array<Vector3> normals;
        public Byte[] transformIndices;
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

        public void build(FileStream fs, BinaryReader reader)
        {
            // Parse vertices
            fs.Seek(verticesOffset, SeekOrigin.Begin);
            for (int i = 0; i < verticesCount; i++)
            {
                vertices.Add(DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())));
            }
            // Parse normals
            fs.Seek(normalsOffset, SeekOrigin.Begin);
            for (int i = 0; i < normalsCount; i++)
            {
                normals.Add(DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())));
            }
            // Parse transformIndices
            fs.Seek(verticesInfoOffset, SeekOrigin.Begin);
            transformIndices = reader.ReadBytes((int)verticesCount);
            // Parse meshes
            meshes = new Array<MdlMesh>();
            fs.Seek(meshOffset, SeekOrigin.Begin);
            for (int i = 0; i < meshCount; i++)
            {
                meshes.Add(new MdlMesh(fs, reader));
            }
            // Build meshes
            for (int i = 0; i < meshCount; i++)
            {
                meshes[i].build(fs, reader);
            }
        }
    }

    partial class MdlBodyPart : GodotObject
    {
        public string name;
        public UInt32 modelsCount;
        public UInt32 basee;
        public UInt32 modelsOffset;

        public Array<MdlModel> models;

        public MdlBodyPart(FileStream fs, BinaryReader reader)
        {
            name = ExtractString(reader.ReadBytes(64));
            modelsCount = reader.ReadUInt32();
            basee = reader.ReadUInt32();
            modelsOffset = reader.ReadUInt32();
            fs.Seek(modelsOffset, SeekOrigin.Begin);
            for (int i = 0; i < modelsCount; i++)
            {
                models.Add(new MdlModel(fs, reader));
            }
        }
    }

    override public void Import(FileStream fs, BinaryReader reader)
    {
        // Parse header
        header = new MdlHeader(fs, reader);
        // Parse bones
        bones = new Array<MdlBone>();
        fs.Seek(header.bonesOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.bonesCount; i++)
        {
            bones.Add(new MdlBone(fs, reader));
        }
        // Parse bones controllers
        boneControllers = new Array<MdlBoneController>();
        fs.Seek(header.boneControllersOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.boneControllersCount; i++)
        {
            boneControllers.Add(new MdlBoneController(fs, reader));
        }
        // Parse sequences
        sequences = new Array<MdlSequence>();
        fs.Seek(header.sequencesOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.sequencesCount; i++)
        {
            sequences.Add(new MdlSequence(fs, reader));
        }
        // Parse sequences groups
        sequenceGroups = new Array<MdlSequenceGroup>();
        fs.Seek(header.sequenceGroupsOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.sequenceGroupsCount; i++)
        {
            sequenceGroups.Add(new MdlSequenceGroup(fs, reader));
        }
        // Parse textures
        textures = new Array<MdlTexture>();
        fs.Seek(header.texturesOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.texturesCount; i++)
        {
            textures.Add(new MdlTexture(fs, reader));
        }
        // Parse bodyParts
        bodyParts = new Array<MdlBodyPart>();
        fs.Seek(header.bodyPartsOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.bodyPartsCount; i++)
        {
            bodyParts.Add(new MdlBodyPart(fs, reader));
        }
    }

    void build()
    {
        foreach (MdlBodyPart part in bodyParts)
        {
            foreach (MdlModel model in part.models) {
                Dictionary<string, SurfaceTool> surfaceTools = new Dictionary<string, SurfaceTool>();
                foreach (MdlMesh mesh in model.meshes) {
                    SurfaceTool surfaceTool = null;
                    if (surfaceTools.ContainsKey(mesh.skinRef.ToString()))
                        surfaceTool = surfaceTools[mesh.skinRef.ToString()];
                    else
                        surfaceTool = new SurfaceTool();
                    surfaceTools[mesh.skinRef.ToString()] = surfaceTool;
                    var material = new StandardMaterial3D();
                    material.AlbedoColor = new Color((float)GD.RandRange(0.0f, 1.0f), (float)GD.RandRange(0.0f, 1.0f), (float)GD.RandRange(0.0f, 1.0f));
                    surfaceTool.SetMaterial(material);
                    surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
                }
                //Here we should inject all meshes in surfaceTool...
                //But i'm too lazy to rewrite MDL creation
                ArrayMesh arrayMesh = new ArrayMesh();
                var keys = surfaceTools.Keys;
                foreach (string surfName in keys)
                {
                    arrayMesh = surfaceTools[surfName].Commit(arrayMesh);
                }
            }
        }
    }
}
