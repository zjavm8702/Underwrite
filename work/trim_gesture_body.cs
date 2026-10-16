var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factory.Init();
var path="Assets/Underwrite/ProtagonistMasterRigV3/NiceStudy/Art/Protagonist_GestureBody.png";
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
var cap=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if(cap==null||!cap.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect))throw new System.Exception("Mesh edit unsupported");
var source=factory.GetSpriteEditorDataProviderFromObject(UnityEditor.AssetImporter.GetAtPath("Assets/Underwrite/Art/ProtagonistBaseV2/protagonist_master_preserved_v3.png"));source.InitSpriteEditorDataProvider();
var sourceMesh=source.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteMeshDataProvider>();var id=source.GetSpriteRects().Single().spriteID;
var v=sourceMesh.GetVertices(id);var indices=sourceMesh.GetIndices(id);var keep=new System.Collections.Generic.List<int>();
for(int i=0;i<indices.Length;i+=3)
{
    bool inside=true;
    for(int j=0;j<3;j++)
    {
        var q=v[indices[i+j]].position;float x=q.x+250,y=1496-q.y;
        float cut=y<350?UnityEngine.Mathf.Lerp(363,368,UnityEngine.Mathf.InverseLerp(285,350,y)):
            y<500?UnityEngine.Mathf.Lerp(368,377,UnityEngine.Mathf.InverseLerp(350,500,y)):
            y<550?UnityEngine.Mathf.Lerp(377,372,UnityEngine.Mathf.InverseLerp(500,550,y)):
            y<600?UnityEngine.Mathf.Lerp(372,358,UnityEngine.Mathf.InverseLerp(550,600,y)):
            y<650?UnityEngine.Mathf.Lerp(358,350,UnityEngine.Mathf.InverseLerp(600,650,y)):350;
        if(y>=285&&y<=850&&x<cut){inside=false;break;}
    }
    if(inside)keep.AddRange(new[]{indices[i],indices[i+1],indices[i+2]});
}
var mesh=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteMeshDataProvider>();var rect=provider.GetSpriteRects().Single();
mesh.SetVertices(rect.spriteID,v);mesh.SetIndices(rect.spriteID,keep.ToArray());mesh.SetEdges(rect.spriteID,new UnityEngine.Vector2Int[0]);provider.Apply();importer.SaveAndReimport();
return Underwrite.EditorTools.GesturePreviewBuilder.Capture(.57f,"Outputs/NiceStudy_v1/peak_final.png");
