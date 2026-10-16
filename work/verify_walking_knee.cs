var path="Assets/Underwrite/ProtagonistMasterRigV3/WalkingStudy/";
var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(path+"Walking.anim");
float maxKeyJump=0;
foreach(var binding in UnityEditor.AnimationUtility.GetCurveBindings(clip))
{
    if(binding.propertyName!="localEulerAnglesRaw.z")continue;
    var curve=UnityEditor.AnimationUtility.GetEditorCurve(clip,binding);
    for(int i=1;i<curve.length;i++)maxKeyJump=UnityEngine.Mathf.Max(maxKeyJump,UnityEngine.Mathf.Abs(curve[i].value-curve[i-1].value));
}
var actor=UnityEditor.PrefabUtility.LoadPrefabContents(path+"Protagonist_Walking.prefab");
try
{
    var all=actor.GetComponentsInChildren<UnityEngine.Transform>();
    var knees=new[]{all.Single(t=>t.name=="Knee_R"),all.Single(t=>t.name=="Knee_L")};
    var previous=new UnityEngine.Vector3[2];float maxKneeStep=0;
    for(int frame=0;frame<=288;frame++)
    {
        clip.SampleAnimation(actor,frame/240f);
        for(int leg=0;leg<2;leg++)
        {
            if(frame>0)maxKneeStep=UnityEngine.Mathf.Max(maxKneeStep,UnityEngine.Vector3.Distance(previous[leg],knees[leg].position));
            previous[leg]=knees[leg].position;
        }
    }
    if(maxKeyJump>5 || maxKneeStep>.1f)throw new System.Exception("Knee continuity check failed: "+maxKeyJump+", "+maxKneeStep);
    System.IO.File.WriteAllText("Outputs/WalkingStudy_v2/validation.json","{\n  \"max_rotation_key_jump_degrees\": "+maxKeyJump.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\n  \"max_knee_step_world_at_240hz\": "+maxKneeStep.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\n  \"samples\": 289,\n  \"original_rotation_jump_degrees\": 359.785919,\n  \"passed\": true\n}");
    return new {maxKeyJump,maxKneeStep,samples=289};
}
finally {UnityEditor.PrefabUtility.UnloadPrefabContents(actor);}
