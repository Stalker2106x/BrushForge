using Godot;
using Godot.Collections;
using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using static IDSP;
using static System.Net.Mime.MediaTypeNames;

public partial class MDL_Legacy : DataPack
{
    MdlHeader header;
    Array<MdlBone> bones;
    Array<MdlBoneController> boneControllers;
    Array<MdlSequence> sequences;
    Array<MdlSequenceGroup> sequenceGroups;
    Array<MdlTexture> textures;
    Array<MdlBodyPart> bodyParts;
    Array<MdlModel> models;

    Array<Transform3D> boneLocalTransforms;
    Array<Transform3D> boneLocalTransformsInv;

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

    partial class MdlSequence : GodotObject
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

    partial class MdlTexture : GodotObject
    {
        public string name;
        public UInt32 flags;
        public UInt32 width;
        public UInt32 height;
        public UInt32 textureOffset;
        public UInt32 id;

        public Byte[] textureData; // Contains texture data as palette indexes
        public Color[] palette; // Contains colors indexed

        public MdlTexture(FileStream fs, BinaryReader reader)
        {
            name = ExtractString(reader.ReadBytes(64));
            flags = reader.ReadUInt32();
            width = reader.ReadUInt32();
            height = reader.ReadUInt32();
            textureOffset = reader.ReadUInt32();
            id = reader.ReadUInt32();
        }

        public void ExtractData(FileStream fs, BinaryReader reader)
        {
            fs.Seek(textureOffset, SeekOrigin.Begin);
            textureData = reader.ReadBytes((int)width * (int)height);
            palette = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                palette[i] = new Color((float)reader.ReadByte() / 255.0f, (float)reader.ReadByte() / 255.0f, (float)reader.ReadByte() / 255.0f);
            }
        }
    }

    public partial class MdlMesh : GodotObject
    {
        public UInt32 trianglesCount;
        public UInt32 trianglesOffset;
        public UInt32 skinRef;
        public UInt32 normalsCount;
        public UInt32 normalsOffset;

        public MdlTriVert[][] triangles;
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
            // Build tris
            fs.Seek(trianglesOffset, SeekOrigin.Begin);
            triangles = new MdlTriVert[trianglesCount][];
            for (int i = 0; i < trianglesCount; i++)
            {
                UInt16 triCount = reader.ReadUInt16();
                if (triCount == 0) break; // Reached end
                MdlTriVert[] tris = new MdlTriVert[triCount];
                for (int j = 0; j < Math.Abs(triCount); j++)
                {
                    tris[j] = new MdlTriVert(fs, reader);
                    tris[j].type = Math.Sign(triCount);
                }
                triangles[i] = tris;
            }
        }
    }

    public partial class MdlTriVert : GodotObject
    {
        public Int16 vertexIndex;
        public Int16 normalIndex;
        public Int16 s;
        public Int16 t;
        public int type;

        public MdlTriVert(FileStream fs, BinaryReader reader)
        {
            vertexIndex = reader.ReadInt16();
            normalIndex = reader.ReadInt16();
            s = reader.ReadInt16();
            t = reader.ReadInt16();
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

        public void stripToTri(Array<Vector3> finalV, Array<Vector3> finalN, Array<Vector2> uv, Array<UInt32> finalB)
        {
            Array<Vector3> fV = new Array<Vector3>();
            Array<Vector3> fN = new Array<Vector3>();
            Array<Vector2> fuv = new Array<Vector2>();
            Array<UInt32> fB = new Array<UInt32>();
            int size = finalV.Count - 2;
            for (int i = 0; i < size; i++)
            {
                if (i % 2 != 0)
                {
                    rearrange(fV, finalV, i, 0, 2, 1);
                    rearrange(fN, finalN, i, 0, 2, 1);
                    rearrange(fuv, uv, i, 0, 2, 1);
                    rearrange(fB, finalB, i, 0, 2, 1);
                }
                else
                {
                    rearrange(fV, finalV, i, 0, 1, 2);
                    rearrange(fN, finalN, i, 0, 1, 2);
                    rearrange(fuv, uv, i, 0, 1, 2);
                    rearrange(fB, finalB, i, 0, 1, 2);
                }
            }
            finalV = fV;
            finalN = fN;
            uv = fuv;
            finalB = fB;
        }


        public void fanToTri(Array<Vector3> finalV, Array<Vector3> finalN, Array<Vector2> uv, Array<UInt32> finalB)
        {
            Array<Vector3> fV = new Array<Vector3>();
            Array<Vector3> fN = new Array<Vector3>();
            Array<Vector2> fuv = new Array<Vector2>();
            Array<UInt32> fB = new Array<UInt32>();
            int size = finalV.Count - 2;
            for (int i = 0; i < size; i++)
            {
                rearrange(fV, finalV, 0, 0, i + 1, i + 2);
                rearrange(fN, finalN, 0, 0, i + 1, i + 2);
                rearrange(fuv, uv, 0, 0, i + 1, i + 2);
                rearrange(fB, finalB, 0, 0, i + 1, i + 2);
            }
            finalV = fV;
            finalN = fN;
            uv = fuv;
            finalB = fB;
        }

        void rearrange(Array<Vector3> dest, Array<Vector3> arr, int i, int a, int b, int c)
        {
            dest.Add(arr[i + a]);
            dest.Add(arr[i + b]);
            dest.Add(arr[i + c]);
        }
        void rearrange(Array<Vector2> dest, Array<Vector2> arr, int i, int a, int b, int c)
        {
            dest.Add(arr[i + a]);
            dest.Add(arr[i + b]);
            dest.Add(arr[i + c]);
        }
        void rearrange(Array<UInt32> dest, Array<UInt32> arr, int i, int a, int b, int c)
        {
            dest.Add(arr[i + a]);
            dest.Add(arr[i + b]);
            dest.Add(arr[i + c]);
        }


        public SurfaceTool createMesh(Array<MdlBone> bones, Array<Vector3> pVertices, Array<Vector3> pNormals, int type, Array<Vector2> pUv, Array<UInt32> pBoneIndices, int textureIndex, int lastTextureIdx, SurfaceTool runningMesh = null)
        {
            Array<Vector3> finalV = new Array<Vector3>();
            Array<Vector3> finalN = new Array<Vector3>();
            Array<UInt32> finalB = new Array<UInt32>();
            for (int i = 0; i < pVertices.Count; i++)
            {
                runningMesh.SetNormal(pNormals[i]);
                runningMesh.SetUV(pUv[i]);

                UInt32 boneIndex = pBoneIndices[i];
                Vector3 vertex = pVertices[i];
                vertex = bones[(int)pBoneIndices[i]].transform * vertex;
                finalV.Add(vertex);
                finalN.Add(pNormals[i]);
                finalB.Add(boneIndex);
            }

            if (type == -1)
                fanToTri(finalV, finalN, pUv, finalB);
            else if (type == 1)
                stripToTri(finalV, finalN, pUv, finalB);

            for (int i = 0; i < pVertices.Count; i++)
            {
                runningMesh.SetNormal(finalN[i]);
                runningMesh.SetUV(pUv[i]);
                runningMesh.SetWeights(new float[4] { 1, 0, 0, 0 });
                runningMesh.SetBones(new int[4] { (int)finalB[i], 0, 0, 0 });
                runningMesh.AddVertex(finalV[i]);
            }

            return runningMesh;
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
            // Full render
            SurfaceTool runningMesh = null;
            int lastTextureIdx = -1;
            ArrayMesh totalMesh = new ArrayMesh();
            for (int i = 0; i < meshCount; i++)
            {
                MdlMesh mesh = meshes[i];
                mesh.Build(fs, reader);
                int polyIdx = 0;
                foreach(MdlTriVert[] poly in meshes[i].triangles)
                {
                    Array<Vector3> pVertices = new Array<Vector3>();
                    Array<Vector3> pNormals = new Array<Vector3>();
                    Array<Vector2> pUv = new Array<Vector2>();
                    Array<string> pTex = new Array<string>();
                    Array<UInt32> pBones = new Array<UInt32>();


                    foreach (MdlTriVert vert in poly)
                    {
                        pVertices.Add(vertices[vert.vertexIndex]);
                        pNormals.Add(normals[vert.normalIndex]);
                        pUv.Add(new Vector2(vert.s, vert.t));
                        pBones.Add(boneMap[vert.vertexIndex]);
                    }

                    if (runningMesh == null)
                    {
                        runningMesh = new SurfaceTool();
                        //Create mat
                        StandardMaterial3D mat = new StandardMaterial3D();
                        //mat.albedo_texture = //Set Tex
                        //mat.uv1_scale.x /= text.get_width()
                        //mat.uv1_scale.y /= text.get_height()
                        runningMesh.SetMaterial(mat);
                        runningMesh.Begin(Mesh.PrimitiveType.Triangles);
                        lastTextureIdx = (int)mesh.skinRef;
                    }

                    if (lastTextureIdx != mesh.skinRef) //#if texture changed
                    {
                        StandardMaterial3D mat = new StandardMaterial3D();
                        runningMesh.SetMaterial(mat);
                        runningMesh.Commit(totalMesh);
                        //totalMesh.surface_set_material(textureIdx,mat)
                        runningMesh = new SurfaceTool();
                        runningMesh.Begin(Mesh.PrimitiveType.Triangles);
                    }
                    runningMesh = createMesh(bones, pVertices, pNormals, poly[0].type, pUv, pBones, (int)mesh.skinRef, lastTextureIdx, runningMesh);
            
                    if (i == meshCount - 1 && polyIdx == mesh.triangles.Length - 1)
                    {
                        StandardMaterial3D mat = new StandardMaterial3D();
                        runningMesh.SetMaterial(mat);
                        runningMesh.Commit(totalMesh);
                    }
                    lastTextureIdx = (int)mesh.skinRef;
                    polyIdx += 1;
                }
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

    public Transform3D GetTransform(Vector3 pos, Vector3 rot)
    {
        Transform3D t = Transform3D.Identity;
        t.Origin = pos;
        t.Basis = t.Basis.Rotated(new Vector3(1, 0, 0), rot.X);
        t.Basis = t.Basis.Rotated(new Vector3(0, 1, 0), rot.Y);
        t.Basis = t.Basis.Rotated(new Vector3(0, 0, 1), rot.Z);
        return t;
    }

    public void boneItt3(MdlFrame frame, UInt32 bonesCount)
    {
        for (int boneIdx = 0; boneIdx < bonesCount; boneIdx++)
        {
            MdlBone bone = bones[boneIdx];
            Vector3 boneRot = bone.rot;
            Vector3 bonePos = bone.pos;
            Vector3 framePos = frame.pos[boneIdx];
            Vector3 frameRot = frame.rot[boneIdx];

            bone.restTransform = GetTransform(bonePos, boneRot);

            Transform3D animatedTransform = GetTransform(bonePos + framePos, boneRot + frameRot);
            boneLocalTransforms.Add(animatedTransform);
            boneLocalTransformsInv.Add(animatedTransform.Inverse());
        }

        for (int boneIdx = 0; boneIdx < bonesCount; boneIdx++)
        {
            MdlBone bone = bones[boneIdx];
            Transform3D t = boneLocalTransforms[boneIdx];

            int parentIdx = (int)bone.parentIndex;

            while (parentIdx >= 0)
            {
                MdlBone parentBone = bones[parentIdx];
                t *= boneLocalTransforms[parentIdx];
                parentIdx = (int)parentBone.parentIndex;
            }
            bone.transform = t;
        }
    }


    override public void Import(FileStream fs, BinaryReader reader)
    {
        // Parse header
        header = new MdlHeader(fs, reader);
        for (int i = 0; i < header.sequenceGroupsCount; i++)
        {
            //Find mdl dependencies ?
        }
        // Parse embedded textures
        textures = new Array<MdlTexture>();
        if (header.texturesCount > 0)
        {
            fs.Seek(header.texturesOffset, SeekOrigin.Begin);
            for (int i = 0; i < header.texturesCount; i++)
            {
                textures.Add(new MdlTexture(fs, reader));
            }
            for (int i = 0; i < header.texturesCount; i++)
            {
                //textures[i].ExtractData(fs, reader);
            }
        }
        else
        {
            // Find external texture ?
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
        // Build sequence
        for (int i = 0; i < header.sequencesCount; i++)
        {
            sequences[i].Build(fs, reader, bones);
        }
        // Parse boneItt ?
        boneLocalTransforms = new Array<Transform3D>();
        boneLocalTransformsInv = new Array<Transform3D>();
        boneItt3(sequences[0].blends[0].frames[0], header.bonesCount);

        // Parse bodyParts
        bodyParts = new Array<MdlBodyPart>();
        fs.Seek(header.bodyPartsOffset, SeekOrigin.Begin);
        for (int i = 0; i < header.bodyPartsCount; i++)
        {
            bodyParts.Add(new MdlBodyPart(fs, reader, bones));
        }
        // Parse models
        Array<MeshInstance3D> meshNodeArr = new Array<MeshInstance3D>();
        foreach (MdlBodyPart part in bodyParts)
        {
            foreach (MdlModel model in part.models)
            {
                var meshNode = new MeshInstance3D();
                //meshNode.Mesh = model.Build();
                meshNode.Name = model.name;
                //meshNode.Visible = part.visible;
                meshNodeArr.Add(meshNode);
            }
        }

        // Build model
        Skeleton3D skeletonNode = new Skeleton3D();
        skeletonNode.Name = "Model";
        foreach (MdlBone bone in bones)
        {
            skeletonNode.AddBone(bone.name);
        }
        for (int boneIdx = 0; boneIdx < header.bonesCount; boneIdx++)
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
            if (animationName == "deploy")
                animationName = "draw"; //we rename grenades anim ????
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
                var boneRestTransform = GetTransform(bone.pos, bone.rot);
                var animParentPath = "../" + skeletonNode.Name + ":" + bone.name;
                var trackPosIdx = anim.AddTrack(Animation.TrackType.Position3D);
                var trackRotIdx = anim.AddTrack(Animation.TrackType.Rotation3D);
                var trackScaleIdx = anim.AddTrack(Animation.TrackType.Scale3D);
                anim.TrackSetPath(trackPosIdx, animParentPath);
                anim.TrackSetPath(trackRotIdx, animParentPath);
                anim.TrackSetPath(trackScaleIdx, animParentPath);

                for (int i = 0; i < seq.frameCount; i++)
                {
                    var frameData = seq.blends[0].frames[i];
                    var allPos = frameData.pos;
                    var allRot = frameData.rot;
                    var pos = bone.pos + allPos[boneIdx];
                    var rot = bone.rot + allRot[boneIdx];

                    Transform3D t = boneLocalTransforms[boneIdx] * (boneLocalTransformsInv[boneIdx] * GetTransform(pos, rot));

                    t.Translated(pos);
                    var rotQuat = t.Basis.GetRotationQuaternion();



                    var keyLocation = Vector3.Zero;
                    var keyRotation = Quaternion.Identity;
                    var keyScale = new Vector3(1, 1, 1);
                    if (t.Origin != keyLocation || keyRotation != rotQuat)
                    {
                        keyLocation = t.Origin;
                        keyRotation = rotQuat;
                        anim.PositionTrackInsertKey(trackPosIdx, i * delta, keyLocation);
                        anim.RotationTrackInsertKey(trackRotIdx, i * delta, keyRotation);
                        anim.ScaleTrackInsertKey(trackScaleIdx, i * delta, keyScale);
                    }
                }
            }
        }
        animPlayer.AddAnimationLibrary("mdl", animLibrary);
        skeletonNode.AddChild(animPlayer);

        // Add Mesh
        foreach (Node3D mesh in meshNodeArr)
        {
            skeletonNode.AddChild(mesh);
        }

        //skeletonNode.RotationDegrees.X = -90;
        //skeletonNode.RotationDegrees.Z = 90;
        gdModel = skeletonNode;
    }
}
