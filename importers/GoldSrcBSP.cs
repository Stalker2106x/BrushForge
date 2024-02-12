using Godot;
using Godot.Collections;
using System;
using System.IO;
using Sledge.Formats.Bsp;
using Sledge.Formats.Bsp.Objects;
using Sledge.Formats.Bsp.Lumps;
using Sledge.Formats.Id;
using BrushForge;

public partial class GoldSrcBSP : DataPack
{
    private string skyName;
    private Vector3 startOrigin;

    private ComputedFace[] faces;
    private BspFile bsp;
    
    public partial class ComputedFace
    {
        public Face bspFace;

        public Vector2[] faceUVs;
        public Vector2[] lightmapUVs;

        public Texture2D lightmapTexture;

        public ComputedFace(BspFile bsp, int faceId)
        {
            bspFace = bsp.Faces[faceId];
            // No lightmap
            if (bspFace.LightmapOffset < 0 || bspFace.Styles[0] == byte.MaxValue) return;

            Vector2[] rawUVs = new Vector2[bspFace.NumEdges];
            faceUVs = new Vector2[bspFace.NumEdges];
            lightmapUVs = new Vector2[bspFace.NumEdges];
            Vector2 fmins = new Vector2(int.MaxValue, int.MaxValue);
            Vector2 fmaxs = new Vector2(int.MinValue, int.MinValue);

            // Generate texture coordinates for face
            TextureInfo texinfo = bsp.Texinfo[bspFace.TextureInfo];
            MipTexture texture = bsp.Textures[texinfo.MipTexture];
            for (int edgeN = 0; edgeN < bspFace.NumEdges; edgeN++)
            {
                int surfedge = bsp.Surfedges[bspFace.FirstEdge + edgeN];
                Edge edge = bsp.Edges[Math.Abs(surfedge)];
                Vector3 vertex = bsp.Vertices[surfedge >= 0 ? edge.Start : edge.End].ToGodotVector3();
                rawUVs[edgeN] = new Vector2(
                    (vertex.Dot(texinfo.S.ToGodotVector3()) + texinfo.S.W),
                    (vertex.Dot(texinfo.S.ToGodotVector3()) + texinfo.T.W)
                );
                // For faces, we need to apply texture scale to uvs.
                faceUVs[edgeN] = new Vector2(rawUVs[edgeN].X / texture.Width, rawUVs[edgeN].Y / texture.Height);
                lightmapUVs[edgeN] = rawUVs[edgeN].Floor();
                // We then extract min and max out of the floored values
                if (lightmapUVs[edgeN].X < fmins.X) fmins.X = lightmapUVs[edgeN].X;
                if (lightmapUVs[edgeN].X > fmaxs.X) fmaxs.X = lightmapUVs[edgeN].X;
                if (lightmapUVs[edgeN].Y < fmins.Y) fmins.Y = lightmapUVs[edgeN].Y;
                if (lightmapUVs[edgeN].Y > fmaxs.Y) fmaxs.Y = lightmapUVs[edgeN].Y;
            }

            // Compute lightmap size
            var fcmaxs = (fmaxs / 16.0f).Ceil();
            var ffmins = (fmins / 16.0f).Floor();
            Vector2I lightmapSize = ((Vector2I)fcmaxs - (Vector2I)ffmins) + new Vector2I(1, 1);

            // Load texture from rawLightmap
            //var lightmapData = new Byte[lightmapSize.X * lightmapSize.Y * 3];
            //System.Array.Copy(rawLightmap, lightmapOffset, lightmapData, 0, lightmapSize.X * lightmapSize.Y * 3);
            //Image img = Image.CreateFromData(lightmapSize.X, lightmapSize.Y, false, Image.Format.Rgb8, lightmapData);
            //lightmapTexture = ImageTexture.CreateFromImage(img);

            // Compute lightmap UV
            for (int edge = 0; edge < bspFace.NumEdges; edge++)
            {
                lightmapUVs[edge] = ((rawUVs[edge] - fmins) / (fmaxs - fmins));
            }

        }
    }

    override public void Import(FileStream fs, BinaryReader reader, Godot.Node app)
    {
        bsp = new BspFile(fs);

        //base.Import(fs, reader, app);

        // Build Face UVs & lightmap
        faces = new ComputedFace[bsp.Faces.Count];
        for (int faceId = 0; faceId < bsp.Faces.Count; faceId++)
        {
            faces[faceId] = new ComputedFace(bsp, faceId);
            gdTextures[faceId.ToString()] = faces[faceId].lightmapTexture;
        }
        // Compute dependencies
        Array<Asset> files = (Array<Asset>)app.Get("files");
        foreach (Entity entity in bsp.Entities)
        {
            if (entity.ClassName == "WORLDSPAWN")
            {
                string[] wads = entity.Get("WAD", "").Replace("QUIVER/", "").Split(';');
                foreach (string wad in wads)
                {
                    bool found = false;
                    for (int fileId = 0; fileId < files.Count; fileId++)
                    {
                        if (files[fileId].path.Contains(wad))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        dependencies.Add(wad);
                    }
                }
            }
        }
    }
    
    override public Array<Variant> GetLevels()
    {
        return new Array<Variant>() { GetFileName() };
    }
    public Vector3 GetLevelStart()
    {
        return startOrigin;
    }

    public Texture2D[] GetSkyCubemapTextures(Array<Asset> files)
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

    static private Vector2 ScaleResolution(Vector2 res)
    {
        return new Vector2(1.0f / (UNIT_SCALE * res.X), 1.0f / (UNIT_SCALE * res.Y));
    }

    override public Node3D BuildGDLevel(string levelId, string shading, Array<Asset> files)
    {
        Material NotFoundMaterial = GD.Load<StandardMaterial3D>("res://materials/NotfoundMaterial.tres");
        // Level
        Node3D mapNode = new Node3D();
        mapNode.Name = "Map";
        ArrayMesh skyMesh = new ArrayMesh();
        for (int modelIdx = 0; modelIdx < bsp.Models.Count; modelIdx++) {
            Dictionary<string, SurfaceTool> surfaceTools = new Dictionary<string, SurfaceTool>();
            int faceEnd = bsp.Models[modelIdx].FirstFace + bsp.Models[modelIdx].NumFaces;
            for (int faceIdx = bsp.Models[modelIdx].FirstFace; faceIdx < faceEnd; faceIdx++) {
                ComputedFace face = faces[(int)faceIdx];
                TextureInfo texinfo = bsp.Texinfo[face.bspFace.TextureInfo];
                MipTexture texture = null;
                string faceTextureName = Convert.ToString(texinfo.MipTexture);
                if (texinfo.MipTexture < bsp.Textures.Count) {
                    // In older versions of BSP, there is no texture
                    texture = bsp.Textures[(int)texinfo.MipTexture];
                    faceTextureName = texture.Name;
                }
                // Create one surfaceTool per texture
                SurfaceTool surfaceTool;
                if (surfaceTools.ContainsKey(faceTextureName)) {
                    surfaceTool = surfaceTools[faceTextureName];
                } else {
                    // Create faces materials
                    Material material = new StandardMaterial3D();
                    if (shading == "texturized")
                    {
                        for (int fileId = 0; fileId < files.Count; fileId++)
                        {
                            if (files[fileId].gdTextures.ContainsKey(texture.Name))
                            {
                                (material as StandardMaterial3D).AlbedoTexture = files[fileId].gdTextures[texture.Name];
                                (material as StandardMaterial3D).TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear;
                                break;
                            }
                        }
                        if ((material as StandardMaterial3D).AlbedoTexture == null)
                        {
                            (material as StandardMaterial3D).AlbedoTexture = (NotFoundMaterial as StandardMaterial3D).AlbedoTexture;
                        }
                        else
                        {
                            if ((material as StandardMaterial3D).AlbedoTexture.HasAlpha())
                                (material as StandardMaterial3D).Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                            /*if (texture.textureName.Contains("~"))
                            {
                                (material as StandardMaterial3D).EmissionEnabled = true;
                                (material as StandardMaterial3D).Emission = new Color(1, 1, 1);
                                (material as StandardMaterial3D).EmissionEnergyMultiplier = 1.0f;
                            }*/
                        }
                    }
                    (material as StandardMaterial3D).DetailEnabled = true;
                    (material as StandardMaterial3D).DetailBlendMode = BaseMaterial3D.BlendModeEnum.Mul;
                    (material as StandardMaterial3D).DetailAlbedo = face.lightmapTexture;
                    (material as StandardMaterial3D).DetailUVLayer = BaseMaterial3D.DetailUV.UV2;
                    surfaceTool = new SurfaceTool();
                    surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
                    surfaceTool.SetMaterial(material.Duplicate() as Material);
                    surfaceTools[faceTextureName] = surfaceTool;
                }
                // Compute faces
                //We build the face trifan
                Vector3[] triFanVertices = new Vector3[face.bspFace.NumEdges];
                for (int edgeN = 0; edgeN < face.bspFace.NumEdges; edgeN++)
                {
                    int surfedge = bsp.Surfedges[face.bspFace.FirstEdge + edgeN];
                    Edge edge = bsp.Edges[Math.Abs(surfedge)];
                    triFanVertices[edgeN] = bsp.Vertices[surfedge >= 0 ? edge.Start : edge.End].ToGodotVector3();
                }
                //We send the surface to sftool
                for (int i = 0; i < triFanVertices.Length - 2; i++)
                {
                    for (int summit = 0; summit < 3; summit++)
                    {
                        int trivertIndex = GetSummitVertIndex(-1, i, summit);
                        surfaceTool.SetUV(face.faceUVs[trivertIndex]);
                        surfaceTool.SetUV2(face.lightmapUVs[trivertIndex]);
                        surfaceTool.SetNormal(face.bspFace.Side != 0 ? -bsp.Planes[face.bspFace.Plane].Normal.ToGodotVector3(): bsp.Planes[face.bspFace.Plane].Normal.ToGodotVector3());
                        surfaceTool.AddVertex(triFanVertices[trivertIndex]);
                    }
                }
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
        //ParseEntities(mapNode, files);
        return mapNode;
    }

    /*
    async public void ParseEntities(Node3D mapNode, Array<Asset> files)
    {
        Dictionary<string, Variant> targets = new Dictionary<string, Variant>();
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
        foreach (Dictionary<string, string> entity in bsp.Entities)
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
        foreach (Dictionary<string, string> entity in entities)
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
                        Vector3 targetOrigin = new GVector3(float.Parse(vecs[0]), float.Parse(vecs[1]), float.Parse(vecs[2])).GetGDVector3();
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
                        //entityNode.Call("configureDoor", entity);
                    }
                    else if (entity["CLASSNAME"] == "FUNC_BUTTON")
                    {
                        entityNode.SetScript(GD.Load<Script>("res://scripts/entities/Door.gd"));
                        //entityNode.Call("configureButton", entity);
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
                    //entityNode.Call("configure", "point", (string)entity["CLASSNAME"], entity, "");
                    if (child != null)
                    {
                        entityNode.AddChild(child);
                    }
                }
                string[] vecs = entity["ORIGIN"].Split(" ");
                entityNode.Position = new GVector3(float.Parse(vecs[0]), float.Parse(vecs[1]), float.Parse(vecs[2])).GetGDVector3();
            }
            // Add gizmo to world
            if (entityNode != null)
            {
                pointEntitiesNode.AddChild(entityNode);
            }
        }
    }*/
}
