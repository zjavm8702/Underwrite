var active=UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
try
{
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Underwrite/ProtagonistMasterRigV3/WalkingStudy/Protagonist_Walking.prefab");
    var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>("Assets/Underwrite/ProtagonistMasterRigV3/WalkingStudy/Walking.anim");
    var actor=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
    actor.GetComponent<UnityEngine.Animator>().enabled=false;
    foreach(var t in actor.GetComponentsInChildren<UnityEngine.Transform>())t.gameObject.layer=26;
    var cg=new UnityEngine.GameObject("Walking verification camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,scene);
    var camera=cg.AddComponent<UnityEngine.Camera>();camera.orthographic=true;camera.orthographicSize=7.8f;camera.transform.position=new UnityEngine.Vector3(0,7.2f,-20);
    camera.cullingMask=1<<26;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.15f,.17f,.2f,0);camera.allowHDR=false;camera.allowMSAA=false;
    cg.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
    var deform=typeof(UnityEngine.U2D.Animation.SpriteSkin).GetMethod("DeformForPreviewUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
    for(int f=0;f<48;f++)
    {
        clip.SampleAnimation(actor,clip.length*f/48);
        foreach(var skin in actor.GetComponentsInChildren<UnityEngine.U2D.Animation.SpriteSkin>())deform.Invoke(skin,null);
        var rt=UnityEngine.RenderTexture.GetTemporary(640,960,24,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB);
        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});
        var previous=UnityEngine.RenderTexture.active;UnityEngine.RenderTexture.active=rt;var image=new UnityEngine.Texture2D(640,960,UnityEngine.TextureFormat.RGBA32,false);
        image.ReadPixels(new UnityEngine.Rect(0,0,640,960),0,0);image.Apply();System.IO.File.WriteAllBytes("Outputs/WalkingStudy_v2/frames/"+f.ToString("D3")+".png",image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.RenderTexture.active=previous;UnityEngine.RenderTexture.ReleaseTemporary(rt);
    }
    return "Rendered 48 frames of the corrected Walking clip";
}
finally {UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
