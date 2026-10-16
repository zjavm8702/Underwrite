var assetPath = "Assets/Underwrite/Art/ProtagonistBaseV2/protagonist_parts_seven_v1.png";
UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate);
var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
if (importer == null) throw new System.Exception("New parts atlas is missing.");
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
importer.spritePixelsPerUnit = 100;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
importer.maxTextureSize = 8192;
importer.isReadable = true;
importer.alphaIsTransparency = true;
importer.mipmapEnabled = false;
importer.filterMode = UnityEngine.FilterMode.Bilinear;
importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
importer.SaveAndReimport();

var factories = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
factories.Init();
var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
if (provider == null) throw new System.Exception("Sprite data provider unavailable; no slicing performed.");
provider.InitSpriteEditorDataProvider();
var edit = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if (edit == null) throw new System.Exception("Sprite edit capability unavailable; no slicing performed.");
var capability = edit.GetEditCapability();
foreach (var required in new[]{UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite,UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect,UnityEditor.U2D.Sprites.EEditCapability.EditSpriteName,UnityEditor.U2D.Sprites.EEditCapability.EditPivot})
    if (!capability.HasCapability(required)) throw new System.Exception("Importer does not support " + required);
var names = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
if (names == null) throw new System.Exception("Sprite name/file-ID provider unavailable; no slicing performed.");
var rects = new System.Collections.Generic.List<UnityEditor.SpriteRect>();
foreach (var line in System.IO.File.ReadAllLines("work/front_parts_rects.tsv"))
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    var cells = line.Split('\t');
    rects.Add(new UnityEditor.SpriteRect {
        name=cells[0], rect=new UnityEngine.Rect(int.Parse(cells[1]),int.Parse(cells[2]),int.Parse(cells[3]),int.Parse(cells[4])),
        pivot=new UnityEngine.Vector2(float.Parse(cells[5],System.Globalization.CultureInfo.InvariantCulture),float.Parse(cells[6],System.Globalization.CultureInfo.InvariantCulture)),
        alignment=UnityEngine.SpriteAlignment.Custom,spriteID=UnityEngine.GUID.Generate()
    });
}
if (rects.Count != 7) throw new System.Exception("Exactly seven part rectangles are required.");
provider.SetSpriteRects(rects.ToArray());
names.SetNameFileIdPairs(rects.Select(r => new UnityEditor.SpriteNameFileIdPair(r.name,r.spriteID)));
provider.Apply(); importer.SaveAndReimport();

var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(assetPath);
var output = "Outputs/ProtagonistParts_v2/parts";
System.IO.Directory.CreateDirectory(output);
foreach (var part in rects)
{
    var r=part.rect;
    var image=new UnityEngine.Texture2D((int)r.width,(int)r.height,UnityEngine.TextureFormat.RGBA32,false);
    image.SetPixels(texture.GetPixels((int)r.x,(int)r.y,(int)r.width,(int)r.height));image.Apply();
    System.IO.File.WriteAllBytes(output+"/"+part.name+".png",UnityEngine.ImageConversion.EncodeToPNG(image));
    UnityEngine.Object.DestroyImmediate(image);
}
UnityEditor.AssetDatabase.SaveAssets();
return new {parts=rects.Count, atlas=assetPath, pngFolder=output, existingScene=UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path};
