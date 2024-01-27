extends Node

func getSkybox(skyName):
    var files = get_node("/root/App").files;
    var textureNames = [skyName+"up", skyName+"lf", skyName+"ft", skyName+"rt", skyName+"bk", skyName+"dn"];
    var textures = [];
    for textureName in range(0, textureNames.size()):
        for file in files:
            if file.gdTextures.has(textureName):
                textures.push_back(file.gdTextures[textureName]);
                break;
    return mergeImages([textures[0], textures[0], textures[0], textures[0],
                        textures[1], textures[2], textures[3], textures[4],
                        textures[5], textures[5], textures[5], textures[5]], 4)

func mergeImages(images : Array, columns : int):
    var oneSize = images[0].get_size();
    var totalSize = oneSize * images.size();
    var result = Image.create(totalSize.x, totalSize.y, false, images[0].get_format());
    for x in range(0, totalSize.x):
        for y in range(0, totalSize.y):
            var px = images[(x / oneSize.x) + ((y / oneSize.y) * columns)].get_pixel(x % oneSize.x, y % oneSize.y);
            result.set_pixel(x, y, px);
    return result;

func getTransform(pos, rot):
    var t = Transform3D.IDENTITY
    t.origin = pos 
    t.basis = t.basis.rotated(Vector3(1,0,0), rot.x)
    t.basis = t.basis.rotated(Vector3(0,1,0), rot.y)
    t.basis = t.basis.rotated(Vector3(0,0,1), rot.z)
    print(t)
    return t
