var root = "Assets/Images/Characters/Protagonist/Motion_v1";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var paths = System.IO.Directory.GetFiles(root, "*.png", System.IO.SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')).OrderBy(p=>p).ToArray();
if(paths.Length!=72) throw new System.Exception("Expected 72 new frames, got "+paths.Length);
var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
factory.Init();
var lines = new System.Collections.Generic.List<string>();
foreach(var path in paths)
{
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
    importer.spritePixelsPerUnit = 100;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.mipmapEnabled = false;
    importer.alphaIsTransparency = true;
    importer.sRGBTexture = true;
    importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
    importer.maxTextureSize = 2048;
    importer.filterMode = UnityEngine.FilterMode.Bilinear;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    var settings = new UnityEditor.TextureImporterSettings();
    importer.ReadTextureSettings(settings);
    settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
    importer.SetTextureSettings(settings);
    importer.SaveAndReimport();

    var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
    provider.InitSpriteEditorDataProvider();
    var edit = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
    if(edit==null) throw new System.Exception("Missing frame edit capability: "+path);
    var capability = edit.GetEditCapability();
    foreach(var flag in new[]{UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite, UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect, UnityEditor.U2D.Sprites.EEditCapability.EditPivot})
        if(!capability.HasCapability(flag)) throw new System.Exception("Unsupported capability "+flag+": "+path);
    var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
    var old = provider.GetSpriteRects();
    if(old.Length>1) throw new System.Exception("Expected at most one owned sprite: "+path);
    var rect = old.Length==1 ? old[0] : new UnityEditor.SpriteRect{spriteID=UnityEngine.GUID.Generate()};
    rect.name = System.IO.Path.GetFileNameWithoutExtension(path);
    rect.rect = new UnityEngine.Rect(0,0,tex.width,tex.height);
    rect.alignment = UnityEngine.SpriteAlignment.Custom;
    rect.pivot = new UnityEngine.Vector2(.5f,13f/tex.height);
    provider.SetSpriteRects(new[]{rect});
    var ids = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
    if(ids==null) throw new System.Exception("Missing name/file ID provider: "+path);
    ids.SetNameFileIdPairs(new[]{new UnityEditor.SpriteNameFileIdPair(rect.name,rect.spriteID)});
    provider.Apply();
    importer.SaveAndReimport();
    var sprite = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<UnityEngine.Sprite>().Single();
    if(UnityEngine.Vector2.Distance(sprite.pivot,new UnityEngine.Vector2(tex.width*.5f,13))>.01f) throw new System.Exception("Pivot mismatch: "+path);
    lines.Add(path+" | "+sprite.rect+" | pivot="+sprite.pivot+" | PPU="+sprite.pixelsPerUnit+" | "+importer.textureCompression);
}
System.IO.File.WriteAllLines("Outputs/ProtagonistMotion_v1/unity_import_report.txt",lines);
return "Imported "+paths.Length+" frames; Uncompressed, Bilinear, full canvas and common ground pivot. Existing Idle/Animator/scene untouched.";
