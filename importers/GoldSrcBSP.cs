using Godot;
using Godot.Collections;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using static Godot.RenderingServer;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

public partial class GoldSrcBSP : DataPack
{
    private string skyName;
    private Vector3 startOrigin;

    private Godot.Collections.Dictionary<string, Lump> lumps;
    private Godot.Collections.Array<Face> faces;
    private Godot.Collections.Array<Model> models;
    private Godot.Collections.Array<Plane> planes;
    private Godot.Collections.Array<TextureInfo> textureInfos;
    private Godot.Collections.Array<Texture> textures;
    private Godot.Collections.Array<Vector3> vertices;
    private Godot.Collections.Array<Edge> edges;
    private Godot.Collections.Array<Int32> surfedges;
    private Godot.Collections.Array<Variant> entities;

    /* The file starts with an array of entries for the so-called lumps.
     * A lump is more or less a section of the file containing a specific
     * type of data. The lump entries in the file header address these lumps,
     * accessed by the 15 predefined indexes.
     */
    private partial class Lump : GodotObject
    {
        public static readonly string[] Def = {
            "Entities",           // MAP entity text buffer	
            "Planes",             // Plane array	
            "Textures",           // Plane array	
            "Vertices",           // Vertex array	
            "Visibility",         // Compressed PVS data and directory for all clusters	
            "Nodes",              // Internal node array for the BSP tree	
            "TextureInfo",        // Face texture application array	
            "Faces",              // Face array	
            "Lighting",           // Lighting	
            "ClipNodes",          // Internal leaf array of the BSP tree	
            "Leaves",             // Index lookup table for referencing the face array from a leaf	
            "MarkSurfaces",       // ?	
            "Edges",              // Edge array	
            "Surfedges",          // Index lookup table for referencing the edge array from a face	
            "Models",             // Models mini bsp tree
        };

        public UInt32 offset; // offset (in bytes) of the data from the beginning of the file
        public UInt32 length; // length (in bytes) of the lump data

        public Lump(FileStream fs, BinaryReader reader)
        {
            offset = reader.ReadUInt32();
            length = reader.ReadUInt32();
        }
    }
    
    /* The face lump contains the surfaces of the scene. */
    private partial class Face : GodotObject
    {
        public UInt16 planeIndex;       // index of the plane the face is parallel to
        public UInt16 planeSide;        // set if the normal is parallel to the plane normal

        public UInt32 surfedgeIndex;    // index of the first edge (in the surface edge array)
        public UInt16 surfedgeCount;    // number of consecutive edges (in the face edge array)
       
        public UInt16 texInfoIndex;     // index of the texture info structure	
    
        public byte[] lightmapStyles;   // styles (bit flags) for the lightmaps
        public UInt32 lightmapOffset;   // offset of the lightmap (in bytes) in the lightmap lump
        
        public Face(FileStream fs, BinaryReader reader)
        {
            planeIndex = reader.ReadUInt16();
            planeSide = reader.ReadUInt16();
            surfedgeIndex = reader.ReadUInt32();
            surfedgeCount = reader.ReadUInt16();
            texInfoIndex = reader.ReadUInt16();
            lightmapStyles = new byte[4] {
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte()
            };
            lightmapOffset = reader.ReadUInt32();
        }
    }
    
    /* The texinfo lump contains informations about how textures are applied to surfaces.
     * The lump itself is an array of binary data structures.
     */
    private partial class TextureInfo : GodotObject
    {
        public Vector3 vs;
        public float   sShift; // Texture shift in s direction

        public Vector3 vt;
        public float   tShift; // Texture shift in t direction
       
        public UInt32  textureIndex; // Index of corresponding texture in texture lump
        public UInt32  flags;
        
        public TextureInfo(FileStream fs, BinaryReader reader)
        {
            vs = ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()), false);
            sShift = reader.ReadSingle();

            vt = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()), false);
            tShift = reader.ReadSingle();
        
            textureIndex = reader.ReadUInt32();
            flags = reader.ReadUInt32();
        }
    }
    
    /* A model is kind of a mini BSP tree.
     * Its size is determinded by the bounding box spaned by the first to members
     * of this struct. The major difference between a model and the BSP tree holding
     * the scene is that the models use a local coordinate system for their vertexes
     * and just state its origin in world coordinates. During rendering the coordinate
     * system is translated to the origin of the model (glTranslate()) and moved back
     * after the models BSP tree has been traversed. Furthermore their are 4 indexes
     * into node arrays. The first one has proofed to index the root node of the mini
     * BSP tree used for rendering. The other three indexes could probably be used for
     * collision detection, meaning they point into the clipnodes, but I am not sure
     * about this. The meaning of the next value is also somehow unclear to me.
     * Finally their are direct indexes into the faces array, not taking the redirecting
     * by the marksurfaces. 
     */
    private partial class Model : GodotObject
    {
        public float[]   mins;      // BBox boundaries min
        public float[]   maxs;      // BBox boundaries max

        public Vector3   origin;    // Coordinates to move the // coordinate system
        public UInt32[]  headNodes; // Index into nodes array
       
        public UInt32   visLeafs;   // ??
        public UInt32   faceIndex;  // Offset of faces from the beginning of file
        public UInt32   faceCount;      // Count of faces
        
        public Model(FileStream fs, BinaryReader reader)
        {
            mins = new float[3] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
            maxs = new float[3] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
            origin = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            headNodes = new UInt32[4] { reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32() };
            visLeafs = reader.ReadUInt32();
            faceIndex = reader.ReadUInt32();
            faceCount = reader.ReadUInt32();
        }
    }

    /* Each of this structures defines a plane in 3-dimensional space by using the
     * Hesse normal form: normal * point - distance = 0
     */
    private partial class Plane : GodotObject
    {
        public Vector3 normal;  // plane normal vector
        public float   dist;    // Plane equation is: normal * X = dist
        public UInt32  type;    // Plane type, see #defines
        
        public Plane(FileStream fs, BinaryReader reader)
        {
            normal = DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            dist = reader.ReadSingle();
            type = reader.ReadUInt32();
        }
    }

    /* The texture lump is somehow a bit more complex then the other lumps,
     * because it is possible to save textures directly within the BSP file
     * instead of storing them in external WAD files.
     */
    private partial class Texture : GodotObject
    {
        public string   textureName; // Texture name (16 Chars max)
        public UInt32   width;        // Texture width
        public UInt32   height;       // Texture height
        public UInt32[] offsets;      // Offsets to texture mipmaps BSPMIPTEX; (4 Levels)
        
        public Texture(FileStream fs, BinaryReader reader)
        {
            textureName = ExtractString(reader.ReadBytes(16));
            width = reader.ReadUInt32();
            height = reader.ReadUInt32();
            offsets = new UInt32[] { reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32() };
        }
    }
    
    /* */
    private partial class Edge : GodotObject
    {
        public UInt16[] verticesIndex; // index of the edge vertices in vertices array
        
        public Edge(FileStream fs, BinaryReader reader)
        {
            verticesIndex = new UInt16[2] { reader.ReadUInt16(), reader.ReadUInt16() };
        }
    }
    
    override public void Import(FileStream fs, BinaryReader reader)
    {
        base.Import(fs, reader);
        // Here we should be just after the header
        lumps = new Godot.Collections.Dictionary<string, Lump>();
        for (int i = 0; i < Lump.Def.Length; i++) {
            lumps[Lump.Def[i]] = new Lump(fs, reader);
        }
        // Parse Entities
        entities = new Godot.Collections.Array<Variant>();
        fs.Seek(lumps["Entities"].offset, SeekOrigin.Begin);
        string[] rawEntities = ExtractString(reader.ReadBytes((int)lumps["Entities"].length)).Replace("\" \"", "\": \"").Replace("\"\n\"", "\",\"").Replace("\\", "/").Replace("\n", "").Split('}');
        foreach (string rawEntity in rawEntities)
        {
            if (rawEntity == "") continue;
            entities.Add(Json.ParseString(rawEntity + "}"));
        }
        // Parse Faces
        faces = new Godot.Collections.Array<Face>();
        fs.Seek(lumps["Faces"].offset, SeekOrigin.Begin);
        while (fs.Position < lumps["Faces"].offset + lumps["Faces"].length) {
            faces.Add(new Face(fs, reader));
        }
        // Parse TextureInfo
        textureInfos = new Godot.Collections.Array<TextureInfo>();
        fs.Seek(lumps["TextureInfo"].offset, SeekOrigin.Begin);
        while (fs.Position < lumps["TextureInfo"].offset + lumps["TextureInfo"].length) {
            textureInfos.Add(new TextureInfo(fs, reader));
        }
        // Parse Models
        models = new Godot.Collections.Array<Model>();
        fs.Seek(lumps["Models"].offset, SeekOrigin.Begin);
        while (fs.Position < lumps["Models"].offset + lumps["Models"].length) {
            models.Add(new Model(fs, reader));
        }
        // Parse Planes
        planes = new Godot.Collections.Array<Plane>();
        fs.Seek(lumps["Planes"].offset, SeekOrigin.Begin);
        while (fs.Position < lumps["Planes"].offset + lumps["Planes"].length) {
            planes.Add(new Plane(fs, reader));
        }
        // Parse Textures
        textures = new Godot.Collections.Array<Texture>();
        fs.Seek(lumps["Textures"].offset, SeekOrigin.Begin);
        UInt32 texturesCount = reader.ReadUInt32();       // Number of BSPMIPTEX structures
        UInt32[] texturesOffsets = new UInt32[texturesCount]; // Distance in bytes from the beginning of the texture lump to each of the texture structs
        for (int i = 0; i < texturesCount; i++) {
            texturesOffsets[i] = reader.ReadUInt32(); 
        }
        foreach (UInt32 offset in texturesOffsets) {
            fs.Seek(lumps["Textures"].offset + offset, SeekOrigin.Begin);
            textures.Add(new Texture(fs, reader));
        }
        // Parse vertices
        vertices = new Godot.Collections.Array<Vector3>();
        fs.Seek(lumps["Vertices"].offset, SeekOrigin.Begin);
        while (fs.Position < lumps["Vertices"].offset + lumps["Vertices"].length) {
            vertices.Add(DataPack.ConvertVector(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())));
        }
        // Parse Edges
        edges = new Godot.Collections.Array<Edge>();
        fs.Seek(lumps["Edges"].offset, SeekOrigin.Begin);
        while (fs.Position < lumps["Edges"].offset + lumps["Edges"].length) {
            edges.Add(new Edge(fs, reader));
        }
        // Parse SurfEdges
        surfedges = new Godot.Collections.Array<Int32>();
        fs.Seek(lumps["Surfedges"].offset, SeekOrigin.Begin);
        while (fs.Position < lumps["Surfedges"].offset + lumps["Surfedges"].length) {
            surfedges.Add(reader.ReadInt32());
        }
    }
    
    override public Godot.Collections.Array<Variant> GetLevels()
    {
        return new Godot.Collections.Array<Variant>() { GetFileName() };
    }
    public Vector3 GetLevelStart()
    {
        return startOrigin;
    }


    override public Godot.Collections.Array<Variant> GetEntities()
    {
        return entities;
    }

    public Texture2D[] GetSkyCubemapTextures(Godot.Collections.Array<Asset> files)
    {
        string[] cubemapSuffixes = new string[] { "LF", "BK", "RT", "FT", "UP", "DN" };
        Texture2D[] cubemapTextures = new Texture2D[cubemapSuffixes.Length];
        for (int i = 0; i < cubemapSuffixes.Length; i++)
        {
            for (int fileId = 0; fileId < files.Count; fileId++)
            {
                if (files[fileId].gdTextures.ContainsKey(skyName + cubemapSuffixes[i]))
                {
                    cubemapTextures[i] = files[fileId].gdTextures[skyName + cubemapSuffixes[i]];
                    break;
                }
            }
            if (cubemapTextures[i] == null) cubemapTextures[i] = GD.Load<Texture2D>("res://assets/NotFound.png");
        }
        return cubemapTextures;
    }

    override public Node3D BuildGDLevel(string levelId, string shading, Godot.Collections.Array<Asset> files)
    {
        Material NotFoundMaterial = GD.Load<StandardMaterial3D>("res://materials/NotfoundMaterial.tres");
        // Level
        Node3D mapNode = new Node3D();
        mapNode.Name = "Map";
        ArrayMesh skyMesh = new ArrayMesh();
        for (int modelIdx = 0; modelIdx < models.Count; modelIdx++) {
            Godot.Collections.Dictionary<string, SurfaceTool> surfaceTools = new Godot.Collections.Dictionary<string, SurfaceTool>();
            UInt32 faceEnd = models[modelIdx].faceIndex + models[modelIdx].faceCount;
            for (UInt32 faceIdx = models[modelIdx].faceIndex; faceIdx < faceEnd; faceIdx++) {
                Face face = faces[(int)faceIdx];
                TextureInfo texinfo = textureInfos[face.texInfoIndex];
                Texture texture = null;
                string faceTextureName = Convert.ToString(texinfo.textureIndex);
                if (texinfo.textureIndex < textures.Count) {
                    // In older versions of BSP, there is no texture
                    texture = textures[(int)texinfo.textureIndex];
                    faceTextureName = texture.textureName;
                }
                // Create one surfaceTool per texture
                SurfaceTool surfaceTool = null;
                if (surfaceTools.ContainsKey(faceTextureName)) {
                    surfaceTool = surfaceTools[faceTextureName];
                } else {
                    // Create faces materials
                    Material material = null;
                    if (shading == "texturized") {
                        material = new StandardMaterial3D();
                        (material as StandardMaterial3D).TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
                        for (int fileId = 0; fileId < files.Count; fileId++) {
                            if (files[fileId].gdTextures.ContainsKey(texture.textureName)) {
                                (material as StandardMaterial3D).AlbedoTexture = files[fileId].gdTextures[texture.textureName];
                                break;
                            }
                        }
                        if ((material as StandardMaterial3D).AlbedoTexture == null)
                        {
                            material = NotFoundMaterial;
                        } else {
                            if ((material as StandardMaterial3D).AlbedoTexture.HasAlpha())
                                (material as StandardMaterial3D).Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                            if (texture.textureName.Contains("~"))
                            {
                                (material as StandardMaterial3D).EmissionEnabled = true;
                                (material as StandardMaterial3D).Emission = new Color(1, 1, 1);
                                (material as StandardMaterial3D).EmissionEnergyMultiplier = 1.0f;
                            }
                        }
                    } else if (shading == "shaded") {
                        material = new StandardMaterial3D();
                        (material as StandardMaterial3D).AlbedoColor = new Color((float)GD.RandRange(0.0, 1.0), (float)GD.RandRange(0.0, 1.0), (float)GD.RandRange(0.0, 1.0));
                    } else if (shading == "wireframe") {
                        material = GD.Load<ShaderMaterial>("res://materials/WireframeMaterial.tres");
                    }
                    surfaceTool = new SurfaceTool();
                    surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
                    surfaceTool.SetMaterial(material.Duplicate() as Material);
                    surfaceTools[faceTextureName] = surfaceTool;
                }
                // Compute faces
                Vector3 faceNormal = face.planeSide != 0 ? -planes[face.planeIndex].normal : planes[face.planeIndex].normal;
                float texScaleX = (1.0f / (UNIT_SCALE * texture.width));
                float texScaleY = (1.0f / (UNIT_SCALE * texture.height));
                Vector3[] faceVertices = new Vector3[face.surfedgeCount];
                Vector2[] faceUVs = new Vector2[face.surfedgeCount];
                Vector3[] faceNormals = new Vector3[face.surfedgeCount];
                for (UInt32 seIdx = 0; seIdx < face.surfedgeCount; seIdx++) {
                    int surfedge = surfedges[(int)(face.surfedgeIndex + seIdx)];
                    Edge edge = edges[Math.Abs(surfedge)];
                    Vector3 vertex = surfedge >= 0 ? vertices[(int)edge.verticesIndex[0]] : vertices[(int)edge.verticesIndex[1]];
                    faceVertices[seIdx] = vertex;
                    faceUVs[seIdx] = new Vector2(
                        vertex.Dot(texinfo.vs) * texScaleX + texinfo.sShift / texture.width,
                        vertex.Dot(texinfo.vt) * texScaleY + texinfo.tShift / texture.height
                    );
                    faceNormals[seIdx] = faceNormal;
                }
                surfaceTool.AddTriangleFan(faceVertices, faceUVs, new Color[0] {}, new Vector2[0] {}, faceNormals); 
            }
            StaticBody3D modelNode = new StaticBody3D();
            modelNode.Name = "Model" + modelIdx.ToString();
            // Render mesh
            MeshInstance3D modelMesh = new MeshInstance3D();
            modelMesh.Name = "Mesh";
            ArrayMesh arrayMesh = null;
            var keys = surfaceTools.Keys;
            foreach (string textureName in keys) {
                surfaceTools[textureName].GenerateTangents();
                if (textureName == "SKY")
                    //Sky is extracted to a separate mesh to be toggled on/off on demand
                    skyMesh = surfaceTools[textureName].Commit(skyMesh);
                else
                    arrayMesh = surfaceTools[textureName].Commit(arrayMesh);
            }
            modelMesh.Mesh = arrayMesh;
            modelNode.AddChild(modelMesh);
            // Generate Collision
            CollisionShape3D collider = new CollisionShape3D();
            collider.Name = "Collider";
            collider.Shape = arrayMesh.CreateTrimeshShape();
            modelNode.AddChild(collider);
            mapNode.AddChild(modelNode);
        }
        // Generate sky mesh
        Node3D skyNode = new Node3D();
        skyNode.Name = "Skybox";
        mapNode.AddChild(skyNode);
        MeshInstance3D skyMeshI = new MeshInstance3D();
        skyMeshI.Mesh = skyMesh;
        skyNode.AddChild(skyMeshI);
        ParseEntities(mapNode, files);
        return mapNode;
    }

    public void ParseEntities(Node3D mapNode, Godot.Collections.Array<Asset> files)
    {
        Godot.Collections.Dictionary<string, Variant> targets = new Godot.Collections.Dictionary<string, Variant>();
        Node3D pointEntitiesNode = new Node3D();
        pointEntitiesNode.Name = "PointEntities";
        mapNode.AddChild(pointEntitiesNode);
        Node3D modelEntitiesNode = new Node3D();
        modelEntitiesNode.Name = "ModelEntities";
        mapNode.AddChild(modelEntitiesNode);
        Node3D pathsNode = new Node3D();
        pathsNode.Name = "Paths";
        mapNode.AddChild(pathsNode);
        // First pass, setup things
        foreach (Godot.Collections.Dictionary<string, string> entity in entities)
        {
            if (entity.ContainsKey("CLASSNAME"))
            {
                // Entity is target, register for later
                if (entity.ContainsKey("TARGETNAME"))
                {
                    // Some BSP have duplicate fields... we register once
                    if (!targets.ContainsKey(entity["TARGETNAME"]))
                        targets.Add(entity["TARGETNAME"], entity);
                }
            }
        }
        // Second pass, lets do magic
        foreach (Godot.Collections.Dictionary<string, string> entity in entities)
        {
            Node3D entityNode = null;
            Node3D child = null;
            // Entity has a internal use
            if (entity.ContainsKey("CLASSNAME"))
            {
                // Contains BSP generic data
                if (entity["CLASSNAME"] == "WORLDSPAWN")
                {
                    if (entity.ContainsKey("SKYNAME"))
                    {
                        skyName = entity["SKYNAME"];
                    }
                }
                // 3D Sprite Decal
                else if (entity["CLASSNAME"] == "INFODECAL")
                {
                    entityNode = new Decal();
                    Texture2D tex = null;
                    for (int fileId = 0; fileId < files.Count; fileId++)
                    {
                        if (files[fileId].gdTextures.ContainsKey(entity["TEXTURE"]))
                        {
                            tex = files[fileId].gdTextures[entity["TEXTURE"]];
                            break;
                        }
                    }
                    if (tex == null) tex = GD.Load<Texture2D>("res://assets/NotFound.png");
                    (entityNode as Decal).TextureAlbedo = tex;
                }
                // Basic light
                else if (entity["CLASSNAME"] == "LIGHT")
                {
                    OmniLight3D light = new OmniLight3D();
                    string[] color = entity["_LIGHT"].Split(" ");
                    light.LightColor = new Color(float.Parse(color[0]) / 255.0f, float.Parse(color[1]) / 255.0f, float.Parse(color[2]) / 255.0f);
                    child = light;
                }
                // Positional SoundEffect
                else if (entity["CLASSNAME"] == "AMBIENT_GENERIC")
                {
                    string soundName = entity["MESSAGE"].Split("/")[^1].Split(".")[0];
                    for (int fileId = 0; fileId < files.Count; fileId++)
                    {
                        if (files[fileId].gdSounds.ContainsKey(soundName))
                        {
                            AudioStreamPlayer3D audioPlayer = new AudioStreamPlayer3D();
                            audioPlayer.Name = soundName;
                            audioPlayer.UnitSize = 100;
                            audioPlayer.MaxDistance = 22.0f;
                            audioPlayer.Stream = files[fileId].gdSounds[soundName];
                            audioPlayer.VolumeDb = -20;
                            audioPlayer.Autoplay = true;
                            child = audioPlayer;
                            break;
                        }
                    }
                }
                // Laser / Beam
                else if (entity["CLASSNAME"] == "ENV_LASER")
                {
                    child = GD.Load<PackedScene>("res://prefabs/Beam.tscn").Instantiate() as Node3D;
                    string[] vecs = entity["ORIGIN"].Split(" ");
                    Vector3 origin = ConvertVector(new Vector3(float.Parse(vecs[0]), float.Parse(vecs[1]), float.Parse(vecs[2])));
                    child.Call("configure", origin, entity["LASERTARGET"]);
                }
                // Map entrypoint
                else if (entity["CLASSNAME"] == "INFO_PLAYER_START")
                {
                    string[] vecs = entity["ORIGIN"].Split(" ");
                    startOrigin = ConvertVector(new Vector3(float.Parse(vecs[0]), float.Parse(vecs[1]), float.Parse(vecs[2])));
                }
                // Paths in 3D
                else if (entity["CLASSNAME"] == "FUNC_TRAIN")
                {
                    Path3D trainPath = new Path3D();
                    trainPath.Name = entity["TARGETNAME"];
                    trainPath.Curve = new Curve3D();
                    Array<string> targetStack = new Array<string>();
                    string targetName = entity["TARGET"];
                    do
                    {
                        if (!targets.ContainsKey(targetName)) break; // Target is missing
                        targetStack.Add(targetName);
                        Dictionary<string, string> targetEntity = (Dictionary<string, string>)targets[targetName];
                        string[] vecs = targetEntity["ORIGIN"].Split(" ");
                        Vector3 targetOrigin = ConvertVector(new Vector3(float.Parse(vecs[0]), float.Parse(vecs[1]), float.Parse(vecs[2])));
                        trainPath.Curve.AddPoint(targetOrigin);
                        targetName = targetEntity.ContainsKey("TARGET") ? targetEntity["TARGET"] : null;
                    } while (!targetStack.Contains(targetName));
                    if (targetStack.Contains(targetName))
                        trainPath.Curve.AddPoint(trainPath.Curve.GetPointPosition(0)); // Make loop
                    PathFollow3D wagon = (GD.Load<PackedScene>("res://prefabs/Wagon.tscn").Instantiate() as PathFollow3D);
                    wagon.Call("configure", 1.0f);
                    trainPath.AddChild(wagon);
                    pathsNode.AddChild(trainPath);
                }
                // Entity is not a point
                else if (entity.ContainsKey("MODEL") && entity["MODEL"].StartsWith("*"))
                {
                    string modelName = "Model" + entity["MODEL"].Replace("*", "");
                    entityNode = mapNode.GetNode(modelName) as Node3D;
                    // Set entity as area (non-block)
                    if (true) // was entity["CLASSNAME"] != "FUNC_BREAKABLE", removed to be able to go through
                    {
                        Area3D replaceNode = new Area3D();
                        replaceNode.Name = modelName;
                        foreach (Node3D eChild in entityNode.GetChildren())
                        {
                            entityNode.RemoveChild(eChild);
                            replaceNode.AddChild(eChild);
                        }
                        entityNode.QueueFree(); // Release unnecessary rigidbody
                        entityNode = replaceNode;
                        MeshInstance3D modelMesh = entityNode.GetNode("Mesh") as MeshInstance3D;
                        int surfaceCount = modelMesh.GetSurfaceOverrideMaterialCount();
                        for (int surface = 0; surface < surfaceCount; surface++)
                        {
                            BaseMaterial3D originalMat = modelMesh.GetActiveMaterial(surface) as BaseMaterial3D;
                            originalMat.Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
                            originalMat.AlbedoColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
                            //shaderMat.SetShaderParameter("albedo", originalMat.AlbedoTexture);
                            modelMesh.SetSurfaceOverrideMaterial(surface, originalMat);
                        }
                    }
                    // Transfer to modelEntities
                    modelEntitiesNode.AddChild(entityNode);
                    // Set entity type
                    if (entity["CLASSNAME"] == "FUNC_DOOR")
                    {
                        entityNode.SetScript(GD.Load<Script>("res://scripts/entities/Door.gd"));
                        entityNode.Call("configureDoor", entity);
                    }
                    else if (entity["CLASSNAME"] == "FUNC_BUTTON")
                    {
                        entityNode.SetScript(GD.Load<Script>("res://scripts/entities/Door.gd"));
                        entityNode.Call("configureButton", entity);
                    }
                    else
                    {
                        entityNode.SetScript(GD.Load<Script>("res://scripts/Entity.gd"));
                    }
                    // Configure base entity class
                    entityNode.Call("configure", "model", (string)entity["CLASSNAME"], entity, "");
                }
            }
            // Point entities get placed here
            if (entity.ContainsKey("ORIGIN"))
            {
                if (entityNode == null)
                {
                    entityNode = GD.Load<PackedScene>("res://prefabs/Entity.tscn").Instantiate() as Node3D;
                    entityNode.Name = (string)entity["CLASSNAME"];
                    entityNode.Call("configure", "point", (string)entity["CLASSNAME"], entity, "");
                    if (child != null)
                    {
                        entityNode.AddChild(child);
                    }
                }
                string[] vecs = entity["ORIGIN"].Split(" ");
                entityNode.Position = ConvertVector(new Vector3(float.Parse(vecs[0]), float.Parse(vecs[1]), float.Parse(vecs[2])));
            }
            // Add gizmo to world
            if (entityNode != null)
            {
                pointEntitiesNode.AddChild(entityNode);
            }
        }
    }
}
