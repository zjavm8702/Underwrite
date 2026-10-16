var checks = new System.Collections.Generic.List<string>();
System.Action<bool,string> require = (ok,message) => { if(!ok) throw new System.Exception(message); };
var root = "Assets/__UnderwriteAnimationBuilderVerification";
require(!UnityEditor.AssetDatabase.IsValidFolder(root), "Temporary verification folder already exists");
var originals = System.IO.Directory.GetFiles("Assets/Images/Characters", "*.meta", System.IO.SearchOption.AllDirectories)
    .Concat(new[]{"Assets/SpriteLibrarys/Protagonist.spriteLib","Assets/Animations/Controller/Controller.controller"})
    .Concat(System.IO.Directory.GetFiles("Assets/Animations/Clib", "*.anim", System.IO.SearchOption.AllDirectories))
    .ToDictionary(p=>p,p=>System.Convert.ToBase64String(System.IO.File.ReadAllBytes(p)));
var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
var wasDirty = activeScene.isDirty;
var previewScene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
UnityEngine.GameObject probe = null;
try
{
    var options = new Underwrite.Editor.AnimationTools.DirectionalAnimationSettings {
        outputFolder = root, characterName = "Verification", library = null, controller = null
    };
    var plan = Underwrite.Editor.AnimationTools.DirectionalAnimationBuilder.Analyze(options);
    require(plan.directions.Count == 8 && plan.directions.All(d=>d.sprites.Length==8),"Cheer scan");
    require(System.Math.Abs(plan.Length-1.56f)<0.001f,"Cheer duration");
    checks.Add("Scan: actual Cheer 8 directions / 64 imported sprites / natural ordering");
    Underwrite.Editor.AnimationTools.DirectionalAnimationBuilder.Build(options);
    var libraryPath = root+"/Verification.spriteLib";
    var controllerPath = root+"/Verification.controller";
    var library = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.Animation.SpriteLibraryAsset>(libraryPath);
    var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
    require(library && controller,"Generated library/controller");
    require(library.GetCategoryNames().Count()==8,"Eight categories");
    require(library.GetCategoryNames().All(c=>library.GetCategoryLabelNames(c).Count()==8),"64 library labels");
    var state = controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Cheer").state;
    var tree = state.motion as UnityEditor.Animations.BlendTree;
    require(tree && tree.children.Length==8 && tree.blendType==UnityEditor.Animations.BlendTreeType.SimpleDirectional2D,"Directional BlendTree");
    require(tree.blendParameter=="DirectionX" && tree.blendParameterY=="DirectionY","Direction parameters");
    require(controller.parameters.All(p=>p.type==UnityEngine.AnimatorControllerParameterType.Float),"Float parameter types");
    probe = new UnityEngine.GameObject("Verification Probe");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(probe,previewScene);
    var renderer = probe.AddComponent<UnityEngine.SpriteRenderer>();
    probe.AddComponent<UnityEngine.U2D.Animation.SpriteLibrary>().spriteLibraryAsset = library;
    var resolver = probe.AddComponent<UnityEngine.U2D.Animation.SpriteResolver>();
    for(int d=0;d<8;d++)
    {
        require(UnityEngine.Vector2.Distance(tree.children[d].position,plan.directions[d].position)<0.0001f,"Direction position "+d);
        var clip = tree.children[d].motion as UnityEngine.AnimationClip;
        require(clip && !UnityEditor.AnimationUtility.GetAnimationClipSettings(clip).loopTime,"One-shot clips");
        require(System.Math.Abs(clip.length-1.56f)<0.02f,"Clip length");
        var bindings = UnityEditor.AnimationUtility.GetCurveBindings(clip);
        require(bindings.Length==1 && bindings[0].propertyName=="m_SpriteHash" && bindings[0].isDiscreteCurve,"Discrete SpriteResolver binding");
        var curve = UnityEditor.AnimationUtility.GetEditorCurve(clip,bindings[0]);
        require(curve.length==9,"Eight frame keys plus last hold endpoint");
        float time=0;
        for(int f=0;f<8;f++)
        {
            clip.SampleAnimation(probe,time+0.002f);
            resolver.ResolveSpriteToSpriteRenderer();
            require(renderer.sprite==plan.directions[d].sprites[f],"Playback mismatch "+plan.directions[d].code+" frame "+f+" label="+resolver.GetLabel());
            time+=plan.durations[f];
        }
    }
    checks.Add("Playback: all 64 frame hashes sample to the exact expected Sprite");
    checks.Add("Controller: eight normalized directions, SimpleDirectional2D, DirectionX/Y Float");
    checks.Add("Timing: 8 discrete keys + final hold, 1.56s, Loop disabled");
    var guids = plan.directions.Select(d=>d.clipPath).Concat(new[]{libraryPath,controllerPath}).ToDictionary(p=>p,p=>UnityEditor.AssetDatabase.AssetPathToGUID(p));
    bool duplicateBlocked=false;
    try { Underwrite.Editor.AnimationTools.DirectionalAnimationBuilder.Build(options); } catch(System.InvalidOperationException) { duplicateBlocked=true; }
    require(duplicateBlocked,"Duplicate generation should require explicit update");
    var extra = controller.layers[0].stateMachine.AddState("KeepManualState");extra.motion=tree;
    controller.AddParameter("ManualTrigger",UnityEngine.AnimatorControllerParameterType.Trigger);
    var transition = extra.AddTransition(state);transition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If,0,"ManualTrigger");
    UnityEditor.EditorUtility.SetDirty(controller);UnityEditor.AssetDatabase.SaveAssetIfDirty(controller);
    var categories = library.GetCategoryNames().Select(c=>new UnityEditor.U2D.Animation.SpriteLibraryCategory(c,library.GetCategoryLabelNames(c).Select(l=>new UnityEditor.U2D.Animation.SpriteLibraryLabel(l,library.GetSprite(c,l))))).ToList();
    categories.Add(new UnityEditor.U2D.Animation.SpriteLibraryCategory("KeepManualCategory",new[]{new UnityEditor.U2D.Animation.SpriteLibraryLabel("Keep",plan.directions[0].sprites[0])}));
    UnityEditor.U2D.Animation.SpriteLibrarySourceAssetFactory.Create(libraryPath,categories);
    UnityEditor.AssetDatabase.ImportAsset(libraryPath,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    options.allowUpdate=true;options.library=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.Animation.SpriteLibraryAsset>(libraryPath);options.controller=controller;
    UnityEditor.EditorUtility.SetDirty(options.library);
    require(UnityEditor.EditorUtility.IsDirty(options.library),"Simulated imported SpriteLibrary dirty flag");
    Underwrite.Editor.AnimationTools.DirectionalAnimationBuilder.Build(options);
    library = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.Animation.SpriteLibraryAsset>(libraryPath);
    require(library.GetSprite("KeepManualCategory","Keep")==plan.directions[0].sprites[0],"Unrelated category lost");
    require(extra.motion==tree && extra.transitions.Length==1 && extra.transitions[0].conditions[0].parameter=="ManualTrigger","Unrelated state/tree/transition lost");
    require(guids.All(p=>UnityEditor.AssetDatabase.AssetPathToGUID(p.Key)==p.Value),"GUID changed during update");
    Underwrite.Editor.AnimationTools.DirectionalAnimationBuilder.Build(options);
    require(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(controllerPath).OfType<UnityEditor.Animations.BlendTree>().Count()==2,"Update leaked orphan BlendTrees");
    checks.Add("Update: same GUIDs, unrelated category/state/transition and shared tree preserved; repeat updates do not leak trees");
    checks.Add("Regression: dirty flag on imported SpriteLibrary does not block generation; current categories preserved");
    bool invalid=false;options.durationsMs="100,200";
    try { Underwrite.Editor.AnimationTools.DirectionalAnimationBuilder.Analyze(options); } catch(System.InvalidOperationException) { invalid=true; }
    require(invalid,"Frame duration count guard");options.durationsMs="240,120,120,120,300,180,180,300";
    controller.AddParameter("WrongType",UnityEngine.AnimatorControllerParameterType.Int);options.parameterX="WrongType";
    invalid=false;try { Underwrite.Editor.AnimationTools.DirectionalAnimationBuilder.Analyze(options); } catch(System.InvalidOperationException) { invalid=true; }
    require(invalid,"Parameter type guard");options.parameterX="DirectionX";
    options.sourceFolder="Assets/DoesNotExist";invalid=false;
    try { Underwrite.Editor.AnimationTools.DirectionalAnimationBuilder.Analyze(options); } catch(System.InvalidOperationException) { invalid=true; }
    require(invalid,"Missing folder guard");
    checks.Add("Preflight: duplicate build, invalid timing, non-Float parameter, missing folder rejected before writes");
    var nested = new Underwrite.Editor.AnimationTools.DirectionalAnimationSettings { outputFolder=root+"/Nested", characterName="Nested", resolverPath="Visual/Body", loop=true };
    Underwrite.Editor.AnimationTools.DirectionalAnimationBuilder.Build(nested);
    var visual = new UnityEngine.GameObject("Visual");visual.transform.SetParent(probe.transform,false);
    var body = new UnityEngine.GameObject("Body");body.transform.SetParent(visual.transform,false);
    var bodyRenderer = body.AddComponent<UnityEngine.SpriteRenderer>();var bodyResolver=body.AddComponent<UnityEngine.U2D.Animation.SpriteResolver>();
    probe.GetComponent<UnityEngine.U2D.Animation.SpriteLibrary>().spriteLibraryAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.Animation.SpriteLibraryAsset>(root+"/Nested/Nested.spriteLib");
    var nestedClip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(root+"/Nested/Cheer/Cheer_S.anim");nestedClip.SampleAnimation(probe,0.75f);bodyResolver.ResolveSpriteToSpriteRenderer();
    require(bodyRenderer.sprite==plan.directions[0].sprites[4],"Relative resolver path playback");
    require(UnityEditor.AnimationUtility.GetAnimationClipSettings(nestedClip).loopTime,"Loop option");
    checks.Add("Nested Animator/Visual/Body binding samples correctly; Loop option works");
}
finally
{
    if(probe) UnityEngine.Object.DestroyImmediate(probe);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(previewScene);
    if(UnityEditor.AssetDatabase.IsValidFolder(root)) UnityEditor.AssetDatabase.DeleteAsset(root);
}
require(originals.All(p=>System.Convert.ToBase64String(System.IO.File.ReadAllBytes(p.Key))==p.Value),"Existing project asset changed during tests");
require(activeScene.isDirty==wasDirty,"Active scene dirty status changed");
checks.Add("Cleanup: temporary test assets removed; existing PNG metadata, library, controller and clips unchanged");
System.IO.Directory.CreateDirectory("Outputs/DirectionalAnimationBuilder");
System.IO.File.WriteAllLines("Outputs/DirectionalAnimationBuilder/verification.txt",checks);
return string.Join("\n",checks);
