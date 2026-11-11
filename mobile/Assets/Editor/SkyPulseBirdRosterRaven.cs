using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using SkyPulse.Mobile;

// Reference coordinates determine each bird's silhouette; every visible part is
// closed, sculpted geometry. The approved Finch and source artwork are never edited.
public static partial class SkyPulseBirdRosterWorkshop
{
    const string SculptRevision="SkyPulse engineered roster sculpture v5";
    const string SculptBodyJoint="01 Sculpted continuous fuselage and flight bank v5";
    static readonly Dictionary<string,string[]> SculptKnownGenerated = new Dictionary<string,string[]>
    {
        {"chrome_raven",new[]{"f4f7d5fcc4cd8b7b6d987a73b5bcea3276c9d66d853852db03c62d66f9852e92","6c0248b35bb2bb12963bcda849ea99f87137e8f4f3eb8c68e6efb47519b81de0","8bd2abe4d951bfeab37ab356e96ee9d44ce4bc182063e39d8edf9ce9f3a59e6d","e60f564ce9e950a2d706b8a3aad24aa46297b27fc4a19ff72ef9eb8a3e358dfd","2d2c7bcb9b4ce3343b3fbb89f13866a7c807eab85ac8e740018624a377eb0d54","afacabf612baf8c4d5f485149cd3b05968f00c7409ee7bd8ed61e439a595f61e","1181d9065bec6f1695bf1b9c55d1f603ada47333811af0de2c5300ecb3f9aef5","857b0b3f3f7fec17bb028df67a6039b73d18b7bcefb1f0952367dcbb588e9ba5"}},
        {"prism_hummingbird",new[]{"d2fba009f8ea67f891128ac72c8e639ad4d55588ecebaafe0b5d8595a3767487","ad5537bf1129a671a9c835038b26b7cf98cbc408cefe1b2520d23104ff9bb36b","9888577e142c49741604b28921695b4e3f62de8016226c57e0be0b3732cda129","22931770bde28fd664bbcf60c8e90ee830810e465fb6a1f75f871613af47eeec","8dadf0d869124817529b3ab0405318d720592de4c9828de4bda2024dedacb222","9781b7bee2d1db89b416d1f9269a961d256c04d9bfdf042fa3f9c81e19f2009c","1657e6d0d360c345f291d903a200bc9571abfb52d55eca5bbff2ff6e8f40c524","356c0668caa439bf0975833ddcfc0ab9a8e8402521934c8dbc259c3e1cda632e"}},
        {"koiwing_glider",new[]{"e6f4f60160d0e28e34c66d3d07ef2ee05138f72615386c9e888e9a129501a4c7","fa6c4627c8399cbdf3ac56ad3fb0d09a5802745d49826d1e57ffdb841b641134","7f747d8f46e6538fc89665cf5584c994bd8f87aeee118f8f31cc92135bea2799","76eec2a99bc7b3b57a804d38b7d13803375fdc3d5ce95121a3e28045adc97ba6","2025aa60e72b7d23b6b8399f1104fde58acd4c8ac8fe26264c6d7bacfad89597","c2f98a5503bbb09f59689f304da516cdd679a521c54d716c2c683594758d1e90","a76b0c57650ae98474e22873c44355c334135129dfaa74f8c09ad80ac4d322e0","ebde4f44a19d293b33cb105e0a338b29d4ec17f32ed562bdc5014b24a4ac5ad2"}},
        {"verdant_kite",new[]{"fe8f5e78e22d7405e430746004fa09caaf16e533ec82552ebd5a9a3fba34177c","dac71bc5e180470bec334f58ac40e250429f10eed2dc8f3d85374a3d134e54e8","7c726226acc63d9cf1daeb3fd75d7cc17e5e9c18cd4c26d21db2d39fee7c0955","3c6e101697a2460757f979d4e8420643804bb16ca738532323b817c658ed1a10","5769688337d26da756ba91d9b07d17a97a6f045d0d08827d21c5ee1267ec1e80","0295dc5651f5089fa882e28907cdfd7fafc471783e4d8af67871a65603f434b6","ab63c2a20f71fb0d2da4e039ee7a205fdfb662068b710f5cc262e4718abda46f","c06a3ffa8744b47e3b3286915184ac4ff11812f64b611fdad700af4379b94df2"}},
        {"newbird01",new[]{"f138480a879396f6a9608deb21081289c9aab9af1ab94ced656e37f62381a3eb","c7d0df3ca407599ddd3bba094d6240ae80df4ddad19edddfee413da7fa561d9a","ae69a8245a6d63d7f0341e0db1709037c4c7bacce104d9f9c1040eac12860f46","f6b683b0c0f2b6d249cee5f61228e42e2cbc63eb0546172a0bcbf3bb4ee44b1b","6f578843b236598c77f86131cbc887a57e749ceeb8bba926ecc70e636b6636a3","7cef671dc0a5ba4cf90eaeecf38a1f5bdb85a84c7314ec5440a8a0c1419e9978","46e4599f19270246203095f58c5a2f230db54a89b1507d77ea70373096e4f514","c0c49776169000d490fdeb747c5e924ae72046cedebe513f2684d06ccccee2bc"}},
        {"newbird02",new[]{"b9f3084d9426d2c7d8f7f21ad7a28c665b53b1b3bc5d488d1789cda07e416bfa","1ccfcd1de1f9041644fb2e97b5d59cb7465db4d59fd83f7ac1dd6e4dbfd29742","81e0d4acf4ad598b9c665c4b175b251c79e3888f363411183424c165b1d6ae72","853b7e20d7f6cfc52d784b3941106ba90d4c5beb7441f5a4e2f210c47f182f79","44183705306930f1221583921caa9bfc9d31b48f8c28b9a7a52a3d6f3dd08818","36786b27a3b0834a290ae8ee5064281af78d187d4345b17429874a8accedafdc","4659a2c12ca55105f3268b0fefb15b2ce1c80c82ce64b2628847be2efb5320f6","03667bb9ced5a6a0b1871fd7f7c1f9470d6591c0160fd88a11e78f161a9fc8e2"}},
        {"newbird03",new[]{"fa75dcee4f70528d46dd3b0bc5e6abfa675661aad3f409261dcfaf158784f8c0","f64ea7068d4c906a89a29d5c053ce22942f68f29041f7bf9dc30f66e3d3ebfa8","2d1c6bfc65f50c847c811532665222c2e8c6d81879608c0c23004036e714cc6b","5f3d506db87428562d18bccbf980d30671934b54abf910a261cec36f2ecc1bdc","5c6ea1db85482a3de1c3ae419c4e1051218caf49ece81282dfc2c4ee1bfef7bb","7f30676da1fb0dce628be8d92a8c43adbf75f2797fb95005a9d61fa427777a0f","14e01f8998741b73e182ffab9ea68b0e4df3649ecaea14255032bcd8407ff234","74798625c844398d5f13de242bb6c83215aefa8fd9d1089c3b40fc803616401e"}},
        {"newbird04",new[]{"f5985ee639122ed169b0f27583c7f9e66781c4d10f593f4bb264b421811aa96f","b178a2c9c51f5983537cb0e031e2e318a51340475462e0e41a44a9903010c4a8","eed00d309a33a3691bc5b2f3999adae6d20f7499d226cec848ff7d2a5bb8dd13","43643678d8a079d9b8aecb49246814212bd1794080d5609af409efa468d7255d","d04a2c55f4a92239ad5b83afe082279e9384d0ad0e78a3c94417a6b178a486de","144cbf645997842e04da3e5d82d40daebe38f25bab73fa0cccdeccf7eae69ab8","c95abbb7ecd6bbc6a872be155816f11d16acf5c0f4004d36fc5d8fb2e225470a","5ea1c57f67b739c54a6a64865a5f7943bcbbbf3baac50ab57411dd5611532dac"}},
        {"newbird05",new[]{"096a44506ad82e547d5cefd2ee8853ec33e5b5132087482f4d054195290ff179","1e25481c64c02c599856a9414c3ae562b7366263e6c9ffced8155ce30c520c0a","30adc84c1d562f28e2b6bd3575271d97a142ea0f5b961989fa51b7c0f231cca5","e52935efec79c78335fb7e73328b912a93580f2c2acc46fc12199ee9040e6180","aa5cac6b1cadc138d3a0951822806b609c2059d4670cef5c6f21f04a11db7780","5a143b6e0187e033db3a2bfce214d58d7578450a21fd81183a9d587e7f01469a","64ed3702b7019dc015edd9f8dfd2c748e151266ac4f305e0db2d0f6e01ed823c","25b13f360b35720dcf55e19a54714d4f1663a3a387719430b8974074840f4ee1"}},
        {"newbird06",new[]{"a4e807c134f983082822516b3db301d910582d816e6f4c268c00ad2e38382b31","ccfac75077f76d326d32031a305a3848f80e686b591ec9dcc3d34bac04baa897","dcb2934574ed33305e1c3bb47ff776703bce3fb3bd440d095775ae22da497d8e","2c838bd9242bf0aa5ff56ffcdfcec973c3ad1053b3df73e62dd12c280da8b654","daefef0cfb8739e7b7c4a4da18b71db3033029a4a41bcc71a6f2ae39037e34e1","cc2e83ad32f2d8b3cc37fd40b352ba26db03fc1314d12331a52559afd5cc5b31","8349230cd507b66fc56d901554e008012c1ebf0032d4b93a5f82253978d0d87c","0f2191d3cf96be6a7d166d064a828cb01aa9e520b9efce8bc7faaf7e3b1f97df"}},
        {"newbird07",new[]{"ad8b8e090cbe781b7e2bc1eb1f96523e5c074c116c78d8baa172374085515b11","a4f5f160a3ca95d32738102dec04e823dd33577ff63bfe54bab0ba1b2887e220","faa3469723fbc5edfee8ba973679e55f2a90665f2abd9fe7bb49f237590e8525","8645517337245da2da82ef1f6ded00edccbc1becbaadc2a511201f724e7a66b5","bec269b2099c27f50f9465263b0c46905dd3ff5f97eb37f7d2ff0aa8456d9cf8","5f3c067ea9e7ec72438ea0f0e99a78b8a768002f0bd5a061f35933c1ef446256","ae0bdab722deb667209a14e5a8e99ce98a7f1e606ae9fde27455c6e0decce8ea","f00a0094d9cbb962333a842b41ccd2e4f107c396689a046dcd7e270ee39941a7"}},
        {"newbird08",new[]{"61e062dc856595b31c37013996c264d0a603944615f138caaa7d72614c095d68","deafb9a96ec00d6d6a297877e112d94b1169074bb44e0e6003fc99fec66a56b3","bba429da610f08085c9114335066b12d71fc8b60f12cf30d8cd022c94032c25e","dacf462761601c509f05e9cc9366a4316a0bd65e0379af0746afe45e7a790cdb","14cfe8ce91717f598c298538e7f6052916089138f900564a8bdcd0a59e6fb9b4","8ee85b36781b4c321d24b3ad85be4a8d1af746ac3b65bca6586f5654084e90da","d3eb8d016b7284ad997ac036c1920cc838be56f8943169d7b41a502fc36ce0a3","f53f7a1d030add1fbfcb6ecd2c04f5344995ec61f2c98efca496a3c349b92805"}},
        {"newbird09",new[]{"a5fe0451b051a7357ca658c5655984b4f801af9ce69aa40f4872e6c0004efe62","36bfd322f1062dd751610ddbe87c72dadcef6a8c6d21db194e6bd16e9d6a52f4","d35e10644646bcb7c56ff4873f46d71c8711aa7162684f4e2f13d2521322afbc","4ef2c6db9189a730f4f7fe5a90bd85a3cba66e47cea883779961329696ce57c4","bc88faf2e586f37ebcfea36589bed232e9f6752a2ae5447568353fad46775919","948ba5eea8d8c91bf31ae0100b0a685b74b5aa3681fa5db45a461cbec2f5a70c","b735426e1937a346b7c78dcf1b58372a496f647b1a51675d9844fa086684f41f","9a5ca6197f69f1f6021b5b2a3376a48dfc59f2659ba735a26f06c7cf19447bf8"}},
        {"newbird10",new[]{"c7e67095e6bb6d8a65e106366f5b1dfb21e96c1c14013d17b252e71a996a1f73","6aa360c0940272f2fbb14345adbd256e63ddba5a511b39351cbcfca7359fc703","e73936cdf7ca37938b9b46823ff22123d232e2e13717d7f4e3799aa4c19ee73b","03c3721de241cf4e5cc89d57947da7451ad2df1f026b12e45a6f4225e7b1951c","51af2ed6fa73a2e2eb1fa4ef01082e6fe68bcd030675c171d37d60975a6c588c","7b871d134c90147018e0b63d916a4910581d16a8fc4e13d4eecb2705b05d648d","a11760ee4da399ba74a57bcc3b54ef75d1380e3ee9965a254479a395e3fc6404","797cbcf19bd1d6a0a8adbb7df887c6b21e16c1d7108dd48e111fc2338074a130"}},
    };
    // This narrow correction is deliberately separate from the v5 roster
    // migration. It may only replace the two known generated magenta studies;
    // artist edits, palettes, scene bindings and all other birds are preserved.
    static readonly Dictionary<string,string[]> MagentaIdentityBefore = new Dictionary<string,string[]>
    {
        {"newbird04",new[]{"8de3c7ca116be13febac1c7fcd909875b1efa3f0becc07ca60cbb6d52b433e22","b178a2c9c51f5983537cb0e031e2e318a51340475462e0e41a44a9903010c4a8","4356a0ef6f104ef487782894732ed5cfe4352abfb1f46309fff8db85b5d90c5a","43643678d8a079d9b8aecb49246814212bd1794080d5609af409efa468d7255d","d04a2c55f4a92239ad5b83afe082279e9384d0ad0e78a3c94417a6b178a486de","144cbf645997842e04da3e5d82d40daebe38f25bab73fa0cccdeccf7eae69ab8","c95abbb7ecd6bbc6a872be155816f11d16acf5c0f4004d36fc5d8fb2e225470a","5ea1c57f67b739c54a6a64865a5f7943bcbbbf3baac50ab57411dd5611532dac"}},
        {"newbird05",new[]{"ba70520a2a734ba6f9f1d23cfe2211fd76e9d8887b21a79682d19db5f00fa914","1e25481c64c02c599856a9414c3ae562b7366263e6c9ffced8155ce30c520c0a","6c514f731b40a833a070341655297fd4afbb4d42d5e9eae4d770573be804641e","e52935efec79c78335fb7e73328b912a93580f2c2acc46fc12199ee9040e6180","aa5cac6b1cadc138d3a0951822806b609c2059d4670cef5c6f21f04a11db7780","5a143b6e0187e033db3a2bfce214d58d7578450a21fd81183a9d587e7f01469a","64ed3702b7019dc015edd9f8dfd2c748e151266ac4f305e0db2d0f6e01ed823c","25b13f360b35720dcf55e19a54714d4f1663a3a387719430b8974074840f4ee1"}}
    };
    static string MagentaIdentityPart(Design d)=>d.name+" source-specific fitted facial mask v6";
    static bool MagentaIdentityAlreadySaved(Design d)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(d));
        return prefab!=null&&prefab.GetComponentsInChildren<MeshFilter>(true).Any(part=>part.name==MagentaIdentityPart(d)+" -1");
    }
    static void MagentaIdentityGuard(Design d)
    {
        ValidateDesign(d);
        if(!MagentaIdentityBefore.TryGetValue(d.id,out var hashes))throw new InvalidOperationException("Only the two source-measured magenta birds may use this correction.");
        var files=SculptFiles(d);string folder=BirdFolder(d);
        for(int i=0;i<files.Length;i++)
        {
            string path=folder+"/"+files[i];
            if(!File.Exists(path)||SculptHash(path)!=hashes[i]||!File.Exists(path+".meta")||SculptHash(path+".meta")!=hashes[i+files.Length])
                throw new InvalidOperationException("Source identity correction preserved an unknown or artist-edited asset: "+path);
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset!=null&&EditorUtility.IsDirty(asset))throw new InvalidOperationException("Save or discard unsaved magenta study edits before revising: "+path);
        }
    }
    [MenuItem("Tools/SkyPulse/Bird Workshop/Refine Chromaflux and Virex source identities")]
    public static void RefineMagentaSourceIdentities()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before revising saved bird structures.");
        var targets=LoadDesigns().Where(d=>d.id=="newbird04"||d.id=="newbird05").ToArray();
        if(targets.Length!=2)throw new InvalidOperationException("Both unique original magenta references are required.");
        foreach(var d in targets)if(!MagentaIdentityAlreadySaved(d))MagentaIdentityGuard(d);
        foreach(var d in targets)if(!MagentaIdentityAlreadySaved(d))ReviseSavedSculpture(d,true);
        Debug.Log("SKYPULSE_MAGENTA_SOURCE_IDENTITIES_SAVED: Chromaflux broad fan and swept paired crown; Virex fine pointed fan and compact triple crest. Source artwork, palettes, Finch and twelve other sculptures preserved.");
    }
    static string SculptHash(string path)
    {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    static string[] SculptFiles(Design d)=>new[]{"Geometry.asset","Finishes.asset",d.name+" Rig.prefab",d.name+" Review.unity"};
    static bool SculptAlreadySaved(Design d)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(d));
        var rig=prefab!=null?prefab.GetComponent<SkyPulseNeonFinchPrototype>():null;
        // A repeat is read-only, including any artist work made after this revision.
        return rig!=null&&rig.body!=null&&rig.body.name==SculptBodyJoint;
    }
    static void SculptGuard(Design d)
    {
        ValidateDesign(d);
        if(!SculptKnownGenerated.TryGetValue(d.id,out var hashes))throw new InvalidOperationException("No generated-asset provenance for "+d.id);
        var files=SculptFiles(d);string folder=BirdFolder(d);
        for(int i=0;i<files.Length;i++)
        {
            string path=folder+"/"+files[i];
            if(!File.Exists(path)||SculptHash(path)!=hashes[i]||!File.Exists(path+".meta")||SculptHash(path+".meta")!=hashes[i+files.Length])
                throw new InvalidOperationException("Sculpture revision preserved an owner-edited or unknown saved asset: "+path);
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if(asset!=null&&EditorUtility.IsDirty(asset))throw new InvalidOperationException("Save or discard unsaved roster edits before revising: "+path);
        }
    }
    [MenuItem("Tools/SkyPulse/Bird Workshop/Revise remaining fourteen engineered sculptures")]
    public static void ReviseRemainingRosterSculptures()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before revising saved roster sculptures.");
        var birds=LoadDesigns();
        if(birds.Length!=14||birds.Any(d=>d.id=="neon_finch"))throw new InvalidOperationException("Only the fourteen remaining reference birds may be revised.");
        // Check the entire roster before writing the first bird. Unknown edits
        // are never legitimised by a structural marker on an older generator.
        foreach(var d in birds)if(!SculptAlreadySaved(d))SculptGuard(d);
        foreach(var d in birds)if(!SculptAlreadySaved(d))ReviseSavedSculpture(d);
        Debug.Log("SKYPULSE_ROSTER_SCULPTURES_V5_SAVED: fourteen closed engineered sculptures; Finch, palettes, review scenes, prefab GUIDs and all eight rig identities retained.");
    }
    [MenuItem("Tools/SkyPulse/Bird Workshop/Revise Chrome Raven reference sculpture")]
    public static void ReviseChromeRaven()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before revising the saved Raven sculpture.");
        var d=LoadDesigns().Single(b=>b.id=="chrome_raven");if(!SculptAlreadySaved(d)){SculptGuard(d);ReviseSavedSculpture(d);}
    }
    static Material[] SculptExistingFinishes(Design d)
    {
        string path=BirdFolder(d)+"/Finishes.asset";var all=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToArray();
        string[] names={"Armour","Feather enamel","Recessed chassis","Ivory alloy","Machined trim","Optical iris","Luminous inlay","Fitted optical glass","live engine fire"};
        var result=new Material[d.jet?9:8];result[0]=AssetDatabase.LoadAssetAtPath<Material>(path);
        for(int i=1;i<result.Length;i++)result[i]=all.Single(m=>m.name==d.name+" · "+names[i]);return result;
    }
    static void ReviseSavedSculpture(Design d,bool sourceIdentityCorrection=false)
    {
        string folder=BirdFolder(d),geometryPath=folder+"/Geometry.asset",prefabPath=PrefabPath(d);
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);var files=SculptFiles(d);
        var backups=files.ToDictionary(f=>f,f=>File.ReadAllBytes(folder+"/"+f));
        var metas=files.ToDictionary(f=>f,f=>File.ReadAllBytes(folder+"/"+f+".meta"));
        string staging="Assets/Editor/__SkyPulseSculptureV5_"+d.id;
        if(Directory.Exists(staging))throw new InvalidOperationException("An unfinished sculpture staging folder is preserved: "+staging);
        Directory.CreateDirectory(staging);AssetDatabase.Refresh();GameObject root=null;bool promoted=false;
        try
        {
            design=d;origin=Mean(d.body);directory=folder;meshes.Clear();finishes=SculptExistingFinishes(d);
            root=new GameObject(d.name+" · "+SculptRevision);var rig=root.AddComponent<SkyPulseNeonFinchPrototype>();
            rig.animate=false;rig.turntable=false;BuildEngineeredSculpt(rig);FitShoulders(rig,design);FitWingFans(rig,design);
            int vertices=meshes.Sum(m=>m.vertexCount);
            if(vertices>40000)throw new InvalidOperationException(d.name+" exceeded its 40,000 authored-vertex mobile budget: "+vertices);
            string tempGeometry=staging+"/Geometry.asset",tempPrefab=staging+"/"+d.name+" Rig.prefab";
            for(int i=0;i<meshes.Count;i++)
            {meshes[i].name=(i+1).ToString("000")+" "+meshes[i].name;if(i==0)AssetDatabase.CreateAsset(meshes[i],tempGeometry);else AssetDatabase.AddObjectToAsset(meshes[i],tempGeometry);}
            AssetDatabase.SaveAssetIfDirty(meshes[0]);rig.CaptureBindPose();rig.animate=true;rig.turntable=true;
            PrefabUtility.SaveAsPrefabAsset(root,tempPrefab);
            string from=AssetDatabase.AssetPathToGUID(tempGeometry),to=AssetDatabase.AssetPathToGUID(geometryPath);
            string prefab=SculptPreserveBindings(existing,AssetDatabase.LoadAssetAtPath<GameObject>(tempPrefab),File.ReadAllText(tempPrefab));
            prefab=prefab.Replace("guid: "+from,"guid: "+to);
            if(prefab.Contains("guid: "+from)||!prefab.Contains("guid: "+to))throw new InvalidOperationException("Sculpture mesh reference remapping failed: "+d.name);
            if(sourceIdentityCorrection)MagentaIdentityGuard(d);else SculptGuard(d);promoted=true;File.Copy(tempGeometry,geometryPath,true);File.WriteAllText(prefabPath,prefab);
            AssetDatabase.ImportAsset(geometryPath,ImportAssetOptions.ForceUpdate);AssetDatabase.ImportAsset(prefabPath,ImportAssetOptions.ForceUpdate);
            foreach(string f in files)if(!File.ReadAllBytes(folder+"/"+f+".meta").SequenceEqual(metas[f]))throw new InvalidOperationException("Sculpture changed an existing asset GUID: "+d.name);
            foreach(string f in new[]{files[1],files[3]})if(!File.ReadAllBytes(folder+"/"+f).SequenceEqual(backups[f]))throw new InvalidOperationException("Sculpture changed the approved finishes or saved review scene: "+d.name);
            Debug.Log("SKYPULSE_SCULPT_V5: "+d.id+"; vertices="+vertices+"; editable mesh parts="+meshes.Count+"; all eight joints preserved");
        }
        catch
        {
            if(promoted){foreach(var pair in backups)File.WriteAllBytes(folder+"/"+pair.Key,pair.Value);AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);}throw;
        }
        finally
        {if(root!=null)UnityEngine.Object.DestroyImmediate(root);AssetDatabase.DeleteAsset(staging);meshes.Clear();RavenSupports.Clear();AssetDatabase.Refresh();}
    }
    static string SculptPreserveBindings(GameObject original,GameObject staged,string yaml)
    {
        if(original==null||staged==null)throw new InvalidOperationException("roster prefab identities are missing.");
        var from=staged.GetComponent<SkyPulseNeonFinchPrototype>();var to=original.GetComponent<SkyPulseNeonFinchPrototype>();
        if(from==null||to==null)throw new InvalidOperationException("roster rig identity is missing.");
        var map=new Dictionary<long,long>();
        long Id(UnityEngine.Object asset)
        {if(asset==null||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long id))throw new InvalidOperationException("Cannot read a roster local file identity.");return id;}
        void Pair(UnityEngine.Object a,UnityEngine.Object b)
        {long first=Id(a),last=Id(b);if(map.TryGetValue(first,out long found)&&found!=last)throw new InvalidOperationException("Conflicting roster identity mapping.");map[first]=last;}
        Pair(staged,original);Pair(staged.transform,original.transform);Pair(from,to);
        Transform[] first={from.body,from.head,from.leftWing,from.rightWing,from.leftWingTip,from.rightWingTip,from.tail,from.tailTip};
        Transform[] last={to.body,to.head,to.leftWing,to.rightWing,to.leftWingTip,to.rightWingTip,to.tail,to.tailTip};
        for(int i=0;i<first.Length;i++){Pair(first[i],last[i]);Pair(first[i].gameObject,last[i].gameObject);}
        if(map.Count!=19||map.Values.Distinct().Count()!=19)throw new InvalidOperationException("roster root, rig and eight joints must have distinct preserved identities.");
        var declarations=Regex.Matches(yaml,@"(?m)^--- !u!\d+ &(-?\d+)$").Cast<Match>().Select(m=>long.Parse(m.Groups[1].Value)).ToArray();
        if(map.Values.Any(id=>declarations.Contains(id)&&!map.ContainsKey(id)))throw new InvalidOperationException("A roster preserved identity collides with a new part.");
        string guidOriginal=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
        // Existing review/gallery placement and motion overrides must continue
        // targeting the same root, rig and joint local IDs. Unexpected child
        // overrides are preserved by refusing this explicit replacement.
        foreach(string scene in new[]{ScenePath(design),GalleryPath})
        {
            if(!File.Exists(scene))continue;
            var references=Regex.Matches(File.ReadAllText(scene),@"fileID: (-?\d+), guid: "+guidOriginal+@"(?:,|})");
            foreach(Match match in references)
            {long id=long.Parse(match.Groups[1].Value);if(id!=0&&id!=100100000&&!map.Values.Contains(id))throw new InvalidOperationException("Saved roster scene contains an unsupported part override; preserved: "+scene);}
        }
        yaml=Regex.Replace(yaml,@"(?m)^(--- !u!\d+ &)(-?\d+)$",m=>m.Groups[1].Value+(map.TryGetValue(long.Parse(m.Groups[2].Value),out long id)?id:long.Parse(m.Groups[2].Value)));
        // Only local references are remapped. Mesh/material external file IDs
        // retain their independently authored sub-asset identities.
        yaml=Regex.Replace(yaml,@"(\{fileID: )(-?\d+)(\})",m=>m.Groups[1].Value+(map.TryGetValue(long.Parse(m.Groups[2].Value),out long id)?id:long.Parse(m.Groups[2].Value))+m.Groups[3].Value);
        var remapped=Regex.Matches(yaml,@"(?m)^--- !u!\d+ &(-?\d+)$").Cast<Match>().Select(m=>long.Parse(m.Groups[1].Value)).ToArray();
        if(remapped.Distinct().Count()!=declarations.Length||map.Values.Any(id=>!remapped.Contains(id)))throw new InvalidOperationException("roster local identity remapping failed.");
        return yaml;
    }
    static Vector2[] RvPoints(params float[] xy){var p=new Vector2[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector2(xy[i*2],xy[i*2+1]);return p;}
    static Vector2[] RvInset(Vector2[] polygon,float inset)
    {
        var c=Center(polygon);var result=new Vector2[polygon.Length];
        for(int i=0;i<result.Length;i++){var v=polygon[i]-c;float length=v.magnitude;result[i]=c+v*(Mathf.Max(0,length-inset)/Mathf.Max(.001f,length));}return result;
    }
    static float RvCross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static List<int> RvTriangles(Vector2[] polygon)
    {
        var ids=Enumerable.Range(0,polygon.Length).ToList();float area=0;for(int i=0;i<polygon.Length;i++)area+=RvCross(polygon[i],polygon[(i+1)%polygon.Length]);
        if(area<0)ids.Reverse();var result=new List<int>();int guard=0;
        while(ids.Count>3&&guard++<polygon.Length*polygon.Length)
        {
            bool cut=false;
            for(int k=0;k<ids.Count;k++)
            {
                int a=ids[(k+ids.Count-1)%ids.Count],b=ids[k],c=ids[(k+1)%ids.Count];var pa=polygon[a];var pb=polygon[b];var pc=polygon[c];
                if(RvCross(pb-pa,pc-pb)<=.0001f)continue;
                bool contains=false;
                foreach(int point in ids)
                {if(point==a||point==b||point==c)continue;var p=polygon[point];if(RvCross(pb-pa,p-pa)>.0001f&&RvCross(pc-pb,p-pb)>.0001f&&RvCross(pa-pc,p-pc)>.0001f){contains=true;break;}}
                if(contains)continue;result.Add(a);result.Add(b);result.Add(c);ids.RemoveAt(k);cut=true;break;
            }
            if(!cut)throw new InvalidOperationException("Invalid traced Raven plate polygon.");
        }
        if(ids.Count==3)result.AddRange(ids);return result;
    }
    // Closed bevel plate. The cap and all four bevel rings share the same
    // adaptively split perimeter, so a fitted curved face has no T-junctions.
    static Mesh RvSolid(Vector2[] polygon,Func<Vector2,float> depth,int side,float halfThickness=1.05f,float bevel=.65f,float edgeLimit=16)
    {
        float area=0;for(int i=0;i<polygon.Length;i++)area+=RvCross(polygon[i],polygon[(i+1)%polygon.Length]);
        if(area<0)polygon=Enumerable.Reverse(polygon).ToArray();
        var inner=RvInset(polygon,bevel);var points=inner.ToList();var triangles=RvTriangles(inner);
        var perimeter=Enumerable.Range(0,inner.Length).ToList();
        var outer=Enumerable.Range(0,inner.Length).ToDictionary(i=>i,i=>polygon[i]);
        float lengthLimit=edgeLimit*edgeLimit;
        for(int iteration=0;iteration<8192;iteration++)
        {
            int first=-1,last=-1;float longest=lengthLimit;
            for(int t=0;t<triangles.Count;t+=3)for(int k=0;k<3;k++)
            {
                int a=triangles[t+k],b=triangles[t+(k+1)%3];float length=(points[a]-points[b]).sqrMagnitude;
                if(length>longest){longest=length;first=a;last=b;}
            }
            if(first<0)break;
            if(iteration==8191)throw new InvalidOperationException("Raven plate subdivision budget exceeded.");
            int middle=points.Count;points.Add((points[first]+points[last])*.5f);
            int count=triangles.Count;
            for(int t=0;t<count;t+=3)
            {
                for(int k=0;k<3;k++)
                {
                    int a=triangles[t+k],b=triangles[t+(k+1)%3];
                    if(!((a==first&&b==last)||(a==last&&b==first)))continue;
                    int c=triangles[t+(k+2)%3];
                    triangles[t]=a;triangles[t+1]=middle;triangles[t+2]=c;
                    triangles.Add(middle);triangles.Add(b);triangles.Add(c);break;
                }
            }
            for(int p=0;p<perimeter.Count;p++)
            {
                int a=perimeter[p],b=perimeter[(p+1)%perimeter.Count];
                if(!((a==first&&b==last)||(a==last&&b==first)))continue;
                outer.Add(middle,(outer[a]+outer[b])*.5f);perimeter.Insert(p+1,middle);break;
            }
        }
        var g=new G();int[] fronts=new int[points.Count],backs=new int[points.Count];
        for(int face=0;face<2;face++)for(int i=0;i<points.Count;i++)
        {
            var p=points[i];int id=g.Add(Point(p,side*(depth(p)+(face==0?halfThickness:-halfThickness))),p*.01f);
            if(face==0)fronts[i]=id;else backs[i]=id;
        }
        for(int t=0;t<triangles.Count;t+=3)
        {
            int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
            if(side<0){g.Tri(fronts[a],fronts[b],fronts[c]);g.Tri(backs[a],backs[c],backs[b]);}
            else{g.Tri(fronts[a],fronts[c],fronts[b]);g.Tri(backs[a],backs[b],backs[c]);}
        }
        int n=perimeter.Count;var rings=new int[4][];
        for(int r=0;r<4;r++)
        {
            rings[r]=new int[n];
            for(int i=0;i<n;i++)
            {
                int point=perimeter[i];
                if(r==0){rings[r][i]=fronts[point];continue;}
                if(r==3){rings[r][i]=backs[point];continue;}
                var p=outer[point];rings[r][i]=g.Add(Point(p,side*(depth(p)+(r==1?halfThickness*.42f:-halfThickness*.42f))),p*.01f);
            }
        }
        for(int r=0;r<3;r++)for(int i=0;i<n;i++)
        {int a=rings[r][i],b=rings[r][(i+1)%n],c=rings[r+1][(i+1)%n],d=rings[r+1][i];if(side>0)g.Quad(a,b,c,d);else g.Quad(a,d,c,b);}
        return g.Finish("Conformal closed bevelled mechanical plate");
    }
    struct RvSupportTriangle
    {
        public Vector2 a,b,c;public float az,bz,cz,den;
    }
    static readonly Dictionary<(Mesh,int),Func<Vector2,float>> RavenSupports=new Dictionary<(Mesh,int),Func<Vector2,float>>();
    static Func<Vector2,float> RvSupport(Mesh mesh,Vector3 offset,int side)
    {
        var cacheKey=(mesh,side);
        if(RavenSupports.TryGetValue(cacheKey,out var prior))return prior;
        var vertices=mesh.vertices;var indices=mesh.triangles;
        var bins=new Dictionary<Vector2Int,List<RvSupportTriangle>>();const float cell=8;
        Vector2 Native(Vector3 p)=>new Vector2(p.x*100+origin.x,origin.y-p.y*100);
        for(int i=0;i<indices.Length;i+=3)
        {
            var av=vertices[indices[i]]+offset;var bv=vertices[indices[i+1]]+offset;var cv=vertices[indices[i+2]]+offset;
            var t=new RvSupportTriangle{a=Native(av),b=Native(bv),c=Native(cv),az=side*av.z*100,bz=side*bv.z*100,cz=side*cv.z*100};
            t.den=(t.b.y-t.c.y)*(t.a.x-t.c.x)+(t.c.x-t.b.x)*(t.a.y-t.c.y);if(Mathf.Abs(t.den)<1e-7f)continue;
            int minX=Mathf.FloorToInt(Mathf.Min(t.a.x,Mathf.Min(t.b.x,t.c.x))/cell),maxX=Mathf.FloorToInt(Mathf.Max(t.a.x,Mathf.Max(t.b.x,t.c.x))/cell);
            int minY=Mathf.FloorToInt(Mathf.Min(t.a.y,Mathf.Min(t.b.y,t.c.y))/cell),maxY=Mathf.FloorToInt(Mathf.Max(t.a.y,Mathf.Max(t.b.y,t.c.y))/cell);
            for(int x=minX;x<=maxX;x++)for(int y=minY;y<=maxY;y++)
            {var key=new Vector2Int(x,y);if(!bins.TryGetValue(key,out var list)){list=new List<RvSupportTriangle>();bins.Add(key,list);}list.Add(t);}
        }
        float Sample(Vector2 p)
        {
            if(!bins.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x/cell),Mathf.FloorToInt(p.y/cell)),out var list))return 0;
            float height=float.NegativeInfinity;
            foreach(var t in list)
            {
                float u=((t.b.y-t.c.y)*(p.x-t.c.x)+(t.c.x-t.b.x)*(p.y-t.c.y))/t.den;
                float v=((t.c.y-t.a.y)*(p.x-t.c.x)+(t.a.x-t.c.x)*(p.y-t.c.y))/t.den,w=1-u-v;
                if(u>=-.00001f&&v>=-.00001f&&w>=-.00001f)height=Mathf.Max(height,u*t.az+v*t.bz+w*t.cz);
            }
            return float.IsNegativeInfinity(height)?0:height;
        }
        Func<Vector2,float> sample=Sample;RavenSupports.Add(cacheKey,sample);return sample;
    }
    sealed class SculptProfile
    {
        public Vector2[] boundary;public float minX,maxX,maxHeight,depth,yExponent,zExponent,crownInset,chinInset;
        public SculptProfile(P[] outline,float depth,float yExponent=.80f,float zExponent=.68f)
        {
            boundary=Smooth(Points(outline),4);minX=boundary.Min(p=>p.x);maxX=boundary.Max(p=>p.x);this.depth=depth;this.yExponent=yExponent;this.zExponent=zExponent;
            maxHeight=boundary.Max(p=>p.y)-boundary.Min(p=>p.y);
        }
        public void Section(float x,out float top,out float bottom,out float width)
        {
            top=float.PositiveInfinity;bottom=float.NegativeInfinity;
            float sample=Mathf.Clamp(x,minX+.001f,maxX-.001f);
            for(int i=0;i<boundary.Length;i++)
            {
                var a=boundary[i];var b=boundary[(i+1)%boundary.Length];
                if(sample<Mathf.Min(a.x,b.x)||sample>Mathf.Max(a.x,b.x)||Mathf.Abs(b.x-a.x)<.0001f)continue;
                float y=Mathf.Lerp(a.y,b.y,(sample-a.x)/(b.x-a.x));top=Mathf.Min(top,y);bottom=Mathf.Max(bottom,y);
            }
            if(float.IsInfinity(top)){top=bottom=Center(boundary).y;}
            float t=Mathf.InverseLerp(minX,maxX,x),sweep=Mathf.Max(0,Mathf.Sin(t*Mathf.PI));
            // Reference outlines include the dorsal armour/crest envelope. The
            // actual casting sits inside that envelope, leaving a low swept
            // skull and a slender trunk underneath the separate armour courses.
            top+=crownInset*sweep;bottom-=chinInset*sweep;
            float h=Mathf.Max(0,bottom-top)/Mathf.Max(1,maxHeight);
            width=depth*Mathf.Pow(h,.56f)*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.19f);
        }
        public Vector2 Domain(Vector2 source)
        {
            float x=Mathf.Clamp(source.x,minX+.8f,maxX-.8f);Section(x,out float top,out float bottom,out _);
            float mid=Mathf.Lerp(top,bottom,.49f),h=source.y<mid?mid-top:bottom-mid;
            float c=Mathf.Clamp((mid-source.y)/Mathf.Max(.01f,h),-.985f,.985f);
            float angle=Mathf.Acos(Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),1/yExponent));
            return new Vector2(x,angle*maxHeight/Mathf.PI);
        }
        public Vector3 FromDomain(Vector2 domain,int side,float lift)
        {
            float u=Mathf.Clamp01(Mathf.InverseLerp(minX,maxX,domain.x));
            float angle=Mathf.Clamp(domain.y*Mathf.PI/maxHeight,.035f,Mathf.PI-.035f);
            var p=At(u,angle,lift);p.z*=side;return p;
        }
        public Vector3 At(float t,float angle,float lift=0)
        {
            float x=Mathf.Lerp(minX,maxX,t);Section(x,out float top,out float bottom,out float width);
            float c=Mathf.Cos(angle),s=Mathf.Sin(angle),mid=Mathf.Lerp(top,bottom,.49f),h=c>0?mid-top:bottom-mid;
            float y=mid-Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),yExponent)*h;
            float z=Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),zExponent)*width;
            var outward=new Vector3(0,-c,s).normalized;return new Vector3(x,y,z)+outward*lift;
        }
    }
    static Vector3 SculptNative(Vector3 p)=>Point(new Vector2(p.x,p.y),p.z);
    static Mesh SculptHull(SculptProfile profile,int slices=36,int around=28)
    {
        var g=new G();
        // The terminal rings sit just inside the outline; single cap vertices
        // avoid collapsed rings and the normals/triangles they used to produce.
        for(int row=0;row<=slices;row++)for(int j=0;j<around;j++)
        {float t=Mathf.Lerp(.001f,.999f,row/(float)slices);g.Add(SculptNative(profile.At(t,j*Mathf.PI*2/around)),new Vector2(t,j/(float)around));}
        for(int row=0;row<slices;row++)for(int j=0;j<around;j++)
        {int k=(j+1)%around;g.Quad(row*around+j,row*around+k,(row+1)*around+k,(row+1)*around+j);}
        profile.Section(profile.minX,out float capTop,out float capBottom,out _);int start=g.Add(Point(new Vector2(profile.minX,(capTop+capBottom)*.5f)));
        profile.Section(profile.maxX,out capTop,out capBottom,out _);int end=g.Add(Point(new Vector2(profile.maxX,(capTop+capBottom)*.5f)));
        for(int j=0;j<around;j++){int k=(j+1)%around;g.Tri(start,k,j);g.Tri(end,slices*around+j,slices*around+k);}
        return g.Finish("Continuous section-sculpted closed shell");
    }
    static Vector3 SculptCatmull(Vector3[] path,float t)
    {
        float u=Mathf.Clamp01(t)*(path.Length-1);int i=Mathf.Min(path.Length-2,Mathf.FloorToInt(u));u-=i;
        var a=path[Mathf.Max(0,i-1)];var b=path[i];var c=path[i+1];var d=path[Mathf.Min(path.Length-1,i+2)];
        return .5f*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);
    }
    static Vector3 SculptTubeTangent(Vector3[] path,float t)
    {
        // Widen the finite difference at a stationary spline point, rather
        // than constructing an entire collapsed section from a zero tangent.
        foreach(float step in new[]{.001f,.01f,.05f,.15f})
        {
            var tangent=SculptCatmull(path,Mathf.Min(1,t+step))-SculptCatmull(path,Mathf.Max(0,t-step));
            if(tangent.sqrMagnitude>1e-10f)return tangent.normalized;
        }
        for(int i=1;i<path.Length;i++)if((path[i]-path[i-1]).sqrMagnitude>1e-10f)return (path[i]-path[i-1]).normalized;
        throw new InvalidOperationException("A structural tube has no supported tangent.");
    }
    static Mesh SculptTube(Vector3[] path,float radius,float depth,float endRadius=-1,int segments=14,int around=12)
    {
        if(path==null||path.Length<2)throw new InvalidOperationException("A structural tube needs a supported path.");
        float pathLength=0;for(int i=1;i<path.Length;i++)pathLength+=Vector3.Distance(path[i-1],path[i]);
        if(pathLength<.01f)throw new InvalidOperationException("A structural tube cannot collapse to a zero-length path.");
        if(endRadius<0)endRadius=radius;var g=new G();
        for(int row=0;row<=segments;row++)
        {
            float t=row/(float)segments;var at=SculptCatmull(path,t);var tangent=SculptTubeTangent(path,t);
            var cross=Vector3.Cross(tangent,Vector3.forward).normalized;if(cross.sqrMagnitude<.001f)cross=Vector3.right;
            var normal=Vector3.Cross(tangent,cross).normalized;float r=Mathf.Lerp(radius,endRadius,t);
            for(int j=0;j<around;j++){float angle=j*Mathf.PI*2/around;g.Add(SculptNative(at+cross*(Mathf.Cos(angle)*r)+normal*(Mathf.Sin(angle)*r*depth/Mathf.Max(.1f,radius))),new Vector2(t,j/(float)around));}
        }
        for(int row=0;row<segments;row++)for(int j=0;j<around;j++){int k=(j+1)%around;g.Quad(row*around+j,row*around+k,(row+1)*around+k,(row+1)*around+j);}
        int first=g.Add(SculptNative(path[0])),last=g.Add(SculptNative(path[path.Length-1]));
        for(int j=0;j<around;j++){int k=(j+1)%around;g.Tri(first,k,j);g.Tri(last,segments*around+j,segments*around+k);}return g.Finish("Closed continuous curved structural member");
    }
    static Mesh SculptCuff(Vector3 a,Vector3 b,float across,float depth,float wall)
    {
        var axis=(b-a).normalized;var cross=Vector3.Cross(axis,Vector3.forward).normalized;if(cross.sqrMagnitude<.001f)cross=Vector3.right;
        var normal=Vector3.Cross(axis,cross).normalized;var g=new G();const int n=28;
        float bevel=Mathf.Min(.9f,wall*.3f);float length=Vector3.Distance(a,b);
        var axial=new[]{0f,bevel,Mathf.Max(bevel,length-bevel),length,length,0f};
        var radial=new[]{-.35f,0f,0f,-.35f,-wall,-wall};
        for(int r=0;r<6;r++)for(int j=0;j<n;j++)
        {float angle=j*Mathf.PI*2/n;g.Add(SculptNative(a+axis*axial[r]+cross*(Mathf.Cos(angle)*(across+radial[r]))+normal*(Mathf.Sin(angle)*(depth+radial[r]))));}
        for(int r=0;r<6;r++)for(int j=0;j<n;j++)g.Quad(r*n+j,r*n+(j+1)%n,((r+1)%6)*n+(j+1)%n,((r+1)%6)*n+j);
        return g.Finish("Hollow bevelled mechanical coupling collar");
    }
    static Mesh SculptSpindle(Vector2 at,float z,float radius,float halfDepth,float inner=0)
    {
        if(inner>0)return SculptCuff(new Vector3(at.x,at.y,z-halfDepth),new Vector3(at.x,at.y,z+halfDepth),radius,radius,Mathf.Max(.5f,radius-inner));
        var g=new G();const int n=28;float bevel=Mathf.Min(1,halfDepth*.35f);
        float[] radii={radius-.8f,radius,radius,radius-.8f},zs={z-halfDepth,z-halfDepth+bevel,z+halfDepth-bevel,z+halfDepth};
        for(int r=0;r<4;r++)for(int j=0;j<n;j++){float a=j*Mathf.PI*2/n;g.Add(Point(at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radii[r],zs[r]));}
        for(int r=0;r<3;r++)for(int j=0;j<n;j++)g.Quad(r*n+j,r*n+(j+1)%n,(r+1)*n+(j+1)%n,(r+1)*n+j);
        int front=g.Add(Point(at,z-halfDepth)),back=g.Add(Point(at,z+halfDepth));
        for(int j=0;j<n;j++){int k=(j+1)%n;g.Tri(front,k,j);g.Tri(back,3*n+j,3*n+k);}return g.Finish("Solid chamfered actuator spindle");
    }
    // Swept blade: the perimeter is an engineering taper rather than a sine-
    // shaped leaf. Its crown, underside, leading bevel and trailing bevel form
    // one closed airfoil section through every longitudinal row.
    static float SculptChord(float t)
    {
        if(t<.13f)return Mathf.Lerp(.38f,.82f,t/.13f);
        if(t<.28f)return Mathf.Lerp(.82f,1,(t-.13f)/.15f);
        if(t<.52f)return Mathf.Lerp(1,.72f,(t-.28f)/.24f);
        if(t<.78f)return Mathf.Lerp(.72f,.30f,(t-.52f)/.26f);
        return Mathf.Lerp(.30f,.012f,(t-.78f)/.22f);
    }
    static Vector3 SculptBladeAt(Blade b,float t,float q,float z,int side,float span,float thickness,float size=1)
    {
        var axis=b.tip.V-b.root.V;float length=Mathf.Max(1,axis.magnitude);var normal=new Vector2(-axis.y,axis.x)/length;
        var at=Vector2.Lerp(b.root.V,b.tip.V,t)+normal*(b.curve*Mathf.Sin(t*Mathf.PI));
        var p=at+normal*(q*b.width*SculptChord(t)*size);
        float crown=Mathf.Max(0,1-Mathf.Abs(q)*1.8f)*b.width*.055f*Mathf.Sin(Mathf.PI*Mathf.Lerp(.13f,.92f,t));
        float sweep=span*(t*t*(3-2*t))+3.3f*Mathf.Sin(t*Mathf.PI);
        return new Vector3(p.x,p.y,z+side*(sweep+crown+thickness));
    }
    static Mesh SculptBlade(Blade b,float z,int side,float span,float thickness=1.1f)
    {
        var section=new[]{new Vector2(-1,-.12f),new Vector2(-.97f,.32f),new Vector2(-.68f,.82f),new Vector2(-.18f,1),new Vector2(0,1.02f),new Vector2(0,1.02f),new Vector2(.48f,.77f),new Vector2(.96f,.22f),new Vector2(1,-.10f),new Vector2(.96f,-.47f),new Vector2(.45f,-.62f),new Vector2(-.22f,-.69f),new Vector2(-.70f,-.53f),new Vector2(-.97f,-.33f)};
        var g=new G();const int rows=14;int n=section.Length;
        for(int r=0;r<=rows;r++)for(int j=0;j<n;j++)
        {
            float t=r/(float)rows,q=section[j].x;float taper=Mathf.Lerp(.72f,1,Mathf.Sin(t*Mathf.PI))*(t>.9f?Mathf.Lerp(1,.12f,(t-.9f)/.1f):1);
            g.Add(SculptNative(SculptBladeAt(b,t,q,z,side,span,section[j].y*thickness*taper)),new Vector2(t,(q+1)*.5f));
        }
        for(int r=0;r<rows;r++)for(int j=0;j<n;j++)
        {
            if((section[j]-section[(j+1)%n]).sqrMagnitude<1e-8f)continue;
            g.Quad(r*n+j,r*n+(j+1)%n,(r+1)*n+(j+1)%n,(r+1)*n+j);
        }
        int first=g.Add(SculptNative(SculptBladeAt(b,0,0,z,side,span,0))),last=g.Add(SculptNative(SculptBladeAt(b,1,0,z,side,span,0)));
        for(int j=0;j<n;j++){int k=(j+1)%n;if((section[j]-section[k]).sqrMagnitude<1e-8f)continue;g.Tri(first,k,j);g.Tri(last,rows*n+j,rows*n+k);}return g.Finish("Closed swept flight blade with a machined central ridge");
    }
    static float SculptAirfoilHeight(float q,bool upper)
    {
        var x=upper?new[]{-1f,-.97f,-.68f,-.18f,0f,.48f,.96f,1f}:new[]{-1f,-.97f,-.70f,-.22f,.45f,.96f,1f};
        var y=upper?new[]{-.12f,.32f,.82f,1f,1.02f,.77f,.22f,-.10f}:new[]{-.12f,-.33f,-.53f,-.69f,-.62f,-.47f,-.10f};
        for(int i=1;i<x.Length;i++)if(q<=x[i])return Mathf.Lerp(y[i-1],y[i],Mathf.InverseLerp(x[i-1],x[i],q));return y[y.Length-1];
    }
    static Mesh SculptBladeInlay(Blade b,float z,int side,float span,float thickness,float begin=.28f,float end=.985f,float breadth=.74f,bool reverse=false)
    {
        var g=new G();const int rows=10,cols=6;int stride=(rows+1)*(cols+1);
        for(int face=0;face<2;face++)for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++)
        {
            float u=row/(float)rows,t=Mathf.Lerp(begin,end,u);
            float taper=.035f+.965f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI)),.42f);
            float q=Mathf.Lerp(-breadth,breadth,col/(float)cols)*taper;
            float skin=Mathf.Lerp(.72f,1,Mathf.Sin(t*Mathf.PI))*(t>.9f?Mathf.Lerp(1,.12f,(t-.9f)/.1f):1);
            // Crown is shared exactly with the support. This is a thin solid
            // enamel seat, above the blade face and below its machined rim.
            float top=SculptAirfoilHeight(q,!reverse)*thickness*skin;
            float lift=(face==0?.36f:.16f)*(reverse?-1:1);
            g.Add(SculptNative(SculptBladeAt(b,t,q,z,side,span,top+lift)),new Vector2(t,(q+1)*.5f));
        }
        for(int face=0;face<2;face++)for(int row=0;row<rows;row++)for(int col=0;col<cols;col++)
        {
            int a=face*stride+row*(cols+1)+col,vertB=a+1,c=vertB+cols+1,d=a+cols+1;
            if(face==0)g.Quad(a,vertB,c,d);else g.Quad(a,d,c,vertB);
        }
        var edge=new List<int>();for(int c=0;c<=cols;c++)edge.Add(c);for(int r=1;r<=rows;r++)edge.Add(r*(cols+1)+cols);
        for(int c=cols-1;c>=0;c--)edge.Add(rows*(cols+1)+c);for(int r=rows-1;r>0;r--)edge.Add(r*(cols+1));
        for(int i=0;i<edge.Count;i++){int a=edge[i],vertB=edge[(i+1)%edge.Count];g.Quad(a,a+stride,vertB+stride,vertB);}return g.Finish("Closed seated airfoil enamel inlay");
    }
    static Blade SculptShortBlade(Blade source,float length,float width)
    {return new Blade{root=source.root,tip=new P{x=Mathf.Lerp(source.root.x,source.tip.x,length),y=Mathf.Lerp(source.root.y,source.tip.y,length)},width=source.width*width,curve=source.curve*length,material=source.material,inset=source.inset};}
    static Vector2[] SculptSlot(Vector2 center,float width,float height,float angle=0)
    {
        var p=new[]{new Vector2(-width*.5f+1,-height*.5f),new Vector2(width*.5f-1,-height*.5f),new Vector2(width*.5f,-height*.5f+1),new Vector2(width*.5f,height*.5f-1),new Vector2(width*.5f-1,height*.5f),new Vector2(-width*.5f+1,height*.5f),new Vector2(-width*.5f,height*.5f-1),new Vector2(-width*.5f,-height*.5f+1)};
        float s=Mathf.Sin(angle),c=Mathf.Cos(angle);return p.Select(v=>center+new Vector2(v.x*c-v.y*s,v.x*s+v.y*c)).ToArray();
    }
    struct SculptIdentity
    {
        public float bodyDepth,headDepth,headY,headZ,covert,inlay;
        public SculptIdentity(float bodyDepth,float headDepth,bool facialDisc=false,float covert=.43f,float inlay=.74f)
        {this.bodyDepth=bodyDepth;this.headDepth=headDepth;headY=facialDisc?.94f:.76f;headZ=facialDisc?.88f:.64f;this.covert=covert;this.inlay=inlay;}
    }
    static SculptIdentity SculptIdentityFor(string id)
    {
        switch(id)
        {
            case "chrome_raven":return new SculptIdentity(.78f,.75f,false,.46f,.58f);
            case "prism_hummingbird":return new SculptIdentity(.66f,.70f,false,.38f,.78f);
            case "koiwing_glider":return new SculptIdentity(.72f,.80f,true,.40f,.74f);
            case "verdant_kite":return new SculptIdentity(.72f,.88f,true,.42f,.75f);
            case "newbird01":return new SculptIdentity(.72f,.67f,false,.43f,.74f);
            case "newbird02":return new SculptIdentity(.68f,.66f,false,.39f,.78f);
            case "newbird03":return new SculptIdentity(.72f,.68f,false,.42f,.74f);
            case "newbird04":return new SculptIdentity(.70f,.70f,false,.47f,.77f);
            case "newbird05":return new SculptIdentity(.57f,.58f,false,.39f,.82f);
            case "newbird06":return new SculptIdentity(.70f,.63f,false,.48f,.65f);
            case "newbird07":return new SculptIdentity(.72f,.68f,false,.45f,.72f);
            case "newbird08":return new SculptIdentity(.72f,.66f,false,.43f,.74f);
            case "newbird09":return new SculptIdentity(.68f,.63f,false,.44f,.70f);
            case "newbird10":return new SculptIdentity(.76f,.90f,true,.46f,.75f);
            default:throw new InvalidOperationException("Missing source-sculpture identity: "+id);
        }
    }
    static Mesh SculptWrapCourse(SculptProfile profile,float begin,float end,float lift=1.05f)
    {
        // A full wrapped casting, including its inner wall and narrow rolled
        // leading/trailing bevels. Course seams are real separated geometry.
        var section=new List<Vector2>();float bevel=Mathf.Min(.007f,(end-begin)*.10f);
        section.Add(new Vector2(begin,.15f));
        const int rows=10,around=28;
        for(int i=0;i<=rows;i++)section.Add(new Vector2(Mathf.Lerp(begin+bevel,end-bevel,i/(float)rows),lift));
        section.Add(new Vector2(end,.15f));section.Add(new Vector2(end,-.55f));section.Add(new Vector2(begin,-.55f));
        var g=new G();
        foreach(var ring in section)for(int j=0;j<around;j++)
        {
            float angle=j*Mathf.PI*2/around;
            float u=Mathf.Clamp(ring.x+.032f*Mathf.Cos(angle)*Mathf.Sin(ring.x*Mathf.PI),.012f,.988f);
            g.Add(SculptNative(profile.At(u,angle,ring.y)),new Vector2(u,j/(float)around));
        }
        for(int r=0;r<section.Count;r++)for(int j=0;j<around;j++)g.Quad(r*around+j,r*around+(j+1)%around,((r+1)%section.Count)*around+(j+1)%around,((r+1)%section.Count)*around+j);
        return g.Finish("Closed wrapped armour casting with recessed joint seam");
    }
    static Vector2[] SculptPanelDomain(SculptProfile profile,Vector2[] outline)
    {
        var points=new List<Vector2>();
        foreach(var source in outline)
        {
            var p=profile.Domain(source);if(points.Count==0||Vector2.Distance(points[points.Count-1],p)>.05f)points.Add(p);
        }
        if(points.Count>2&&Vector2.Distance(points[0],points[points.Count-1])<.05f)points.RemoveAt(points.Count-1);
        bool changed=true;
        while(changed&&points.Count>3)
        {
            changed=false;
            for(int i=0;i<points.Count;i++)
            {
                var a=points[(i+points.Count-1)%points.Count];var b=points[i];var c=points[(i+1)%points.Count];
                if(Mathf.Abs(RvCross(b-a,c-b))<.025f){points.RemoveAt(i);changed=true;break;}
            }
        }
        float area=0;for(int i=0;i<points.Count;i++)area+=RvCross(points[i],points[(i+1)%points.Count]);
        return points.Count<3||Mathf.Abs(area)<1?null:points.ToArray();
    }
    static Mesh SculptProfilePatch(SculptProfile profile,Vector2[] outline,int side,float lift,float halfThickness=.55f,float bevel=.5f,float edgeLimit=7)
    {
        var domain=SculptPanelDomain(profile,outline);if(domain==null)return null;
        // Subdivide in the supported profile domain. Unlike the v3 triangle-
        // depth lookup this cannot fall back to Z=0 outside a shell silhouette.
        var mesh=RvSolid(domain,p=>lift,side,halfThickness,bevel,edgeLimit);var vertices=mesh.vertices;
        for(int i=0;i<vertices.Length;i++)
        {
            var p=new Vector2(vertices[i].x*100+origin.x,origin.y-vertices[i].y*100);float offset=side*vertices[i].z*100;
            vertices[i]=SculptNative(profile.FromDomain(p,side,offset));
        }
        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
    }
    static void SculptProfilePart(Transform parent,string name,SculptProfile profile,Vector2[] contour,int side,int material,float lift,float half=.55f,float bevel=.5f,float edgeLimit=7)
    {
        var mesh=SculptProfilePatch(profile,contour,side,lift,half,bevel,edgeLimit);if(mesh!=null)Part(parent,name,mesh,material);
    }
    static void BuildEngineeredSculpt(SkyPulseNeonFinchPrototype rig)
    {
        RavenSupports.Clear();var d=design;var identity=SculptIdentityFor(d.id);
        var bodyShape=new SculptProfile(d.body,d.bodyDepth*identity.bodyDepth);
        var headShape=new SculptProfile(d.head,d.headDepth*identity.headDepth,identity.headY,identity.headZ);
        bodyShape.crownInset=bodyShape.maxHeight*(d.id=="newbird05"?.15f:.09f);
        bodyShape.chinInset=bodyShape.maxHeight*(d.id=="newbird05"?.09f:.04f);
        bool facialDisc=identity.headY>.9f;
        headShape.crownInset=headShape.maxHeight*(facialDisc?.035f:.14f);
        headShape.chinInset=headShape.maxHeight*(facialDisc?.02f:.03f);
        if(d.id=="newbird04"||d.id=="newbird05")
        {
            headShape.crownInset=headShape.maxHeight*(d.id=="newbird04"?.20f:.25f);
            headShape.chinInset=headShape.maxHeight*.09f;
        }
        var bc=Mean(d.body);var hc=Mean(d.head);
        rig.body=Joint(rig.transform,SculptBodyJoint,Vector3.zero);
        rig.head=Joint(rig.body,"02 Seated helmet, swept cowl and inset optics",Point(hc));
        rig.tail=Joint(rig.body,"05 Sealed tail actuator and power spine",Point(d.tailRoot));
        var fan=Vector2.zero;foreach(var blade in d.tail)fan+=blade.root.V;fan/=d.tail.Length;
        rig.tailTip=Joint(rig.tail,"06 Articulated cambered tail fan",Point(fan));
        var bodyMesh=SculptHull(bodyShape);Part(rig.body,"Continuous armoured fuselage",bodyMesh,2);
        var headMesh=SculptHull(headShape,32,28);Part(rig.head,"Continuous fitted helmet",headMesh,2);
        float rear=d.id=="prism_hummingbird"?.30f:.34f,front=d.id=="chrome_raven"?.69f:.66f;
        Part(rig.body,"Rear wrapped fuselage casting",SculptWrapCourse(bodyShape,.035f,rear-.006f),d.bodyMaterial);
        Part(rig.body,"Shoulder and dorsal wrapped casting",SculptWrapCourse(bodyShape,rear+.006f,front-.006f),d.bodyMaterial);
        Part(rig.body,"Forward chest and neck casting",SculptWrapCourse(bodyShape,front+.006f,.965f),d.bodyMaterial);
        Part(rig.head,"Swept rear helmet casting",SculptWrapCourse(headShape,.035f,.435f,.90f),d.headMaterial);
        Part(rig.head,"Seated forward helmet cowl",SculptWrapCourse(headShape,.447f,.965f,.90f),d.headMaterial);
        var neckA=Vector2.Lerp(bc,hc,.43f);var neckB=Vector2.Lerp(bc,hc,.85f);var neckMid=Vector2.Lerp(neckA,neckB,.5f);
        float neckRadius=Mathf.Min((bodyShape.maxX-bodyShape.minX)*.14f,(headShape.maxX-headShape.minX)*.24f);
        if(d.id=="newbird04"||d.id=="newbird05")neckRadius=(headShape.maxX-headShape.minX)*(d.id=="newbird04"?.32f:.27f);
        float neckDepth=Mathf.Min(headShape.depth,bodyShape.depth)*.86f;
        var neckPath=new[]{new Vector3(neckA.x,neckA.y,0),new Vector3(neckMid.x,neckMid.y,0),new Vector3(neckB.x,neckB.y,0)};
        Part(rig.body,"Continuous supported neck casting",SculptTube(neckPath,neckRadius,neckDepth,neckRadius*.82f,18,20),d.id=="newbird04"||d.id=="newbird05"?3:d.bodyMaterial);
        var neckAxis=(neckB-neckA).normalized;var collar=Vector2.Lerp(neckA,neckB,.76f);
        Part(rig.body,"Neck flexible seated coupling",SculptCuff(new Vector3(collar.x-neckAxis.x*2.7f,collar.y-neckAxis.y*2.7f,0),new Vector3(collar.x+neckAxis.x*2.7f,collar.y+neckAxis.y*2.7f,0),neckRadius*.96f,neckDepth+.45f,1.8f),2);
        var lip=collar+neckAxis*2.7f;
        Part(rig.head,"Fitted helmet retaining collar",SculptCuff(new Vector3(lip.x-neckAxis.x*.7f,lip.y-neckAxis.y*.7f,0),new Vector3(lip.x+neckAxis.x*1.6f,lip.y+neckAxis.y*1.6f,0),neckRadius*.98f,neckDepth+1,1.2f),4);
        foreach(int side in new[]{-1,1})
        {
            int panelIndex=0;
            foreach(var panel in d.panels??Array.Empty<Panel>())
            {
                bool onHead=panel.surface=="head";var parent=onHead?rig.head:rig.body;var profile=onHead?headShape:bodyShape;
                var polygon=Points(panel.outline);int material=panel.material;
                // A same-colour region is still its own measured casting; its
                // rolled edge and joint gap carry structure under real light.
                if(material==6)
                {
                    SculptProfilePart(parent,"Recessed source energy housing "+side+" "+panelIndex,profile,polygon,side,2,1.6f,.55f,.4f);
                    SculptProfilePart(parent,"Inset source energy aperture "+side+" "+panelIndex,profile,RvInset(polygon,1.25f),side,material,2.18f,.25f,.25f);
                }
                else SculptProfilePart(parent,"Fitted source armour casting "+side+" "+panelIndex,profile,polygon,side,material,material==(onHead?d.headMaterial:d.bodyMaterial)?1.85f:1.42f,.60f,.45f);
                panelIndex++;
            }
            if(d.id=="newbird04"||d.id=="newbird05")BuildMagentaSourceArmour(rig,bodyShape,headShape,side);
            BuildSculptOptics(rig.head,headShape,side);
        }
        foreach(var crest in d.crest??Array.Empty<Panel>())
        {
            var polygon=Points(crest.outline);
            if(d.id=="newbird04"||d.id=="newbird05")
            {
                // Both source crowns trail back from the helmet. Compress the
                // upright envelope into that swept silhouette instead of tall
                // generic antennae; Virex retains its compact triple crest.
                float seatX=d.id=="newbird04"?421:374,seatY=d.id=="newbird04"?287:309;
                float sweep=d.id=="newbird04"?1.14f:1.04f,rise=d.id=="newbird04"?.62f:.54f;
                polygon=polygon.Select(p=>new Vector2(seatX+(p.x-seatX)*sweep,seatY+(p.y-seatY)*rise)).ToArray();
            }
            float minX=polygon.Min(p=>p.x),maxX=polygon.Max(p=>p.x);
            float center=Center(polygon).x;
            // The magenta references have separate crown blades, not a single
            // paper-thin central comb. Spread their measured castings into real
            // near/far seats while retaining each original outline and colour.
            int crestIndex=Array.IndexOf(d.crest,crest);
            float crownSeat=d.id=="newbird04"?(crestIndex==0?-5.8f:crestIndex==1?5.8f:crestIndex==2?-2.6f:2.6f)
                :d.id=="newbird05"?(crestIndex==0?-6f:crestIndex==1?6f:crestIndex==2?0:crestIndex==3?-3f:3f):0;
            var vane=RvSolid(polygon,p=>crownSeat+1.2f*Mathf.Sin(Mathf.InverseLerp(minX,maxX,p.x)*Mathf.PI),1,Mathf.Clamp(d.headDepth*.085f,1.8f,2.8f),.65f,7);
            Part(rig.head,"Swept bevelled crown vane "+meshes.Count,vane,crest.material);
        }
        if(d.id=="chrome_raven")BuildChromeDorsalMantle(rig,bodyShape);
        BuildSculptBeak(rig.head);
        BuildSculptWing(rig,-1,bodyMesh,out rig.leftWing,out rig.leftWingTip);
        BuildSculptWing(rig,1,bodyMesh,out rig.rightWing,out rig.rightWingTip);
        BuildSculptTail(rig,fan);BuildSculptTalons(rig,bodyShape);
    }
    static void BuildMagentaSourceArmour(SkyPulseNeonFinchPrototype rig,SculptProfile body,SculptProfile head,int side)
    {
        var d=design;bool broad=d.id=="newbird04";
        // These are independently traced source surfaces. In particular the
        // broad black mask and white neck shield distinguish Chromaflux from
        // Virex's low forward visor and narrower white cheek. They sit below
        // the existing connected optics and never float over the glass.
        var mask=broad?RvPoints(394,278,407,273,424,276,441,285,451,296,443,307,427,311,410,303,398,291)
            :RvPoints(349,298,361,294,377,298,395,307,403,317,392,325,376,325,360,315);
        SculptProfilePart(rig.head,MagentaIdentityPart(d)+" "+side,head,mask,side,2,1.78f,.31f,.34f,5);
        var cheek=broad?RvPoints(395,294,404,302,416,309,435,313,423,317,407,310,398,302)
            :RvPoints(346,310,355,316,369,325,383,330,369,332,354,324,345,317);
        SculptProfilePart(rig.head,d.name+" measured white cheek shield "+side,head,cheek,side,3,2.04f,.32f,.28f,5);
        var back=broad?new[]{
            RvPoints(235,304,254,290,277,281,299,279,316,285,300,291,278,292,256,302,241,314),
            RvPoints(268,291,290,280,315,278,339,285,354,296,338,296,319,289,297,292,278,302)}
            :new[]{
            RvPoints(230,318,252,305,276,297,292,298,278,305,254,315,239,327),
            RvPoints(266,296,284,288,305,291,326,301,338,315,324,315,309,303,290,300,276,306)};
        for(int i=0;i<back.Length;i++)SculptProfilePart(rig.body,d.name+" measured overlapping dorsal vane "+side+" "+i,body,back[i],side,i==0?3:broad?0:4,2.40f+i*.40f,.51f,.40f,6);
    }
    static void BuildChromeDorsalMantle(SkyPulseNeonFinchPrototype rig,SculptProfile profile)
    {
        // Three native-reference armour feathers sweep over the back instead
        // of exposing the large, uniformly shaded envelope underneath them.
        var courses=new[]{
            RvPoints(209,207,230,190,255,180,281,176,300,181,288,191,262,190,237,199,218,216),
            RvPoints(235,201,259,191,284,191,304,184,320,193,305,207,282,210,259,209,240,216),
            RvPoints(274,212,298,207,317,194,334,203,345,218,331,233,314,237,299,227,281,223)};
        foreach(int side in new[]{-1,1})for(int i=0;i<courses.Length;i++)
            SculptProfilePart(rig.body,"Chrome swept dorsal armour feather "+side+" "+i,profile,courses[i],side,i==1?1:0,1.95f+i*.38f,.65f,.50f,6);
    }
    static void BuildSculptOptics(Transform head,SculptProfile profile,int side)
    {
        var d=design;var center=new Vector2(d.eye[0],d.eye[1]);
        var outer=EyeContour(center,d.eye[2]*1.05f,d.eye[3]*1.06f,d.eyeShape);
        SculptProfilePart(head,"Connected recessed optical housing "+side,profile,outer,side,2,1.7f,1.2f,.65f,4);
        var glassContour=EyeContour(center,d.eye[2]*.91f,d.eye[3]*.89f,d.eyeShape);
        SculptProfilePart(head,"Seated dark optical glass "+side,profile,glassContour,side,7,3.01f,.24f,.35f,3);
        Part(head,"Slim fitted optical retaining bezel "+side,SculptProfileRim(profile,outer,side,3.55f,.55f,.45f),d.eyeShape=="round"?4:2);
        var irisContour=EyeContour(center+new Vector2(d.eye[2]*.06f,0),d.eye[2]*.58f,d.eye[3]*.59f,d.eyeShape);
        // Each coloured insert has a buried mounting stem. Keep the visible
        // face at the same offset while its back penetrates the actual glass
        // seat; touching coplanar faces are not a mechanical attachment.
        SculptProfilePart(head,"Reference luminous iris "+side,profile,irisContour,side,5,3.10f,.51f,.25f,3);
        SculptProfilePart(head,"Seated optical emitter "+side,profile,EyeContour(center+new Vector2(d.eye[2]*.13f,-d.eye[3]*.08f),d.eye[2]*.16f,d.eye[3]*.22f,d.eyeShape=="triangle"?"triangle":"round"),side,6,3.40f,.45f,.15f,2);
        var brow=RvPoints(center.x-d.eye[2]*1.2f,center.y-d.eye[3]*.72f,center.x-d.eye[2]*.70f,center.y-d.eye[3]*1.23f,center.x+d.eye[2]*.30f,center.y-d.eye[3]*.92f,center.x+d.eye[2]*1.04f,center.y-d.eye[3]*.17f,center.x+d.eye[2]*.72f,center.y-d.eye[3]*.34f,center.x-d.eye[2]*.23f,center.y-d.eye[3]*.76f);
        SculptProfilePart(head,"Sculpted protective sensor brow "+side,profile,brow,side,d.headMaterial==3?4:d.headMaterial,3.30f,.52f,.45f,5);
    }
    static Mesh SculptProfileRim(SculptProfile profile,Vector2[] contour,int side,float lift,float width,float height)
    {
        var domain=SculptPanelDomain(profile,contour);var p=Smooth(domain,domain.Length>10?1:4);var center=Center(p);var g=new G();int n=p.Length;
        for(int j=0;j<8;j++)for(int i=0;i<n;i++)
        {
            var tangent=(p[(i+1)%n]-p[(i+n-1)%n]).normalized;var normal=new Vector2(-tangent.y,tangent.x);if(Vector2.Dot(normal,p[i]-center)<0)normal=-normal;
            float a=j*Mathf.PI*2/8;g.Add(SculptNative(profile.FromDomain(p[i]+normal*(width*Mathf.Cos(a)),side,lift+height*Mathf.Sin(a))));
        }
        for(int j=0;j<8;j++)for(int i=0;i<n;i++)g.Quad(j*n+i,j*n+(i+1)%n,((j+1)%8)*n+(i+1)%n,((j+1)%8)*n+i);
        return g.Finish("Closed profile-fitted optical retaining rim");
    }
    static Mesh SculptContourRim(Vector2[] contour,Func<Vector2,float> skin,int side,float width,float height)
    {
        var p=Smooth(contour,contour.Length>10?1:4);var center=Center(p);var g=new G();int n=p.Length;
        for(int j=0;j<8;j++)for(int i=0;i<n;i++)
        {
            var prev=p[(i+n-1)%n];var next=p[(i+1)%n];var tangent=(next-prev).normalized;var normal=new Vector2(-tangent.y,tangent.x);
            if(Vector2.Dot(normal,p[i]-center)<0)normal=-normal;
            float a=j*Mathf.PI*2/8;var at=p[i]+normal*(width*Mathf.Cos(a));g.Add(Point(at,side*(skin(at)+height*Mathf.Sin(a))));
        }
        for(int j=0;j<8;j++)for(int i=0;i<n;i++)g.Quad(j*n+i,j*n+(i+1)%n,((j+1)%8)*n+(i+1)%n,((j+1)%8)*n+i);
        return g.Finish("Closed fitted recessed sensor retaining rim");
    }
    static void BuildSculptBeak(Transform head)
    {
        var d=design;var p=Points(d.beak);var beakShape=new SculptProfile(d.beak,Mathf.Clamp(d.beakDepth,4.5f,11));
        Part(head,"Section-sculpted hooked upper beak",SculptHull(beakShape,20,16),d.beakMaterial);
        float minX=p.Min(v=>v.x),maxX=p.Max(v=>v.x);var jaw=new List<Vector2>();
        for(int i=0;i<=7;i++)
        {
            float x=Mathf.Lerp(minX,maxX-1,i/7f);beakShape.Section(x,out float top,out float bottom,out _);jaw.Add(new Vector2(x,Mathf.Lerp(top,bottom,.76f)));
        }
        for(int i=7;i>=0;i--)
        {
            float x=Mathf.Lerp(minX,maxX-1,i/7f);beakShape.Section(x,out float top,out float bottom,out _);jaw.Add(new Vector2(x,bottom+.6f));
        }
        foreach(int side in new[]{-1,1})
        {
            float Skin(Vector2 at){beakShape.Section(at.x,out float top,out float bottom,out float width);float mid=(top+bottom)*.5f,r=(at.y-mid)/Mathf.Max(1,(bottom-top)*.5f);return width*Mathf.Sqrt(Mathf.Max(0,1-r*r));}
            Part(head,"Articulated lower mandible and mouth split "+side,RvSolid(jaw.ToArray(),p2=>Skin(p2)+.25f,side,.45f,.3f,6),2);
            var root=new Vector2(minX+4,Center(p).y);
            Part(head,"Seated jaw pivot "+side,SculptSpindle(root,side*(beakShape.depth*.75f),2.4f,1.1f),4);
        }
    }
    static void BuildSculptWing(SkyPulseNeonFinchPrototype rig,int side,Mesh body,out Transform wing,out Transform tip)
    {
        var d=design;var identity=SculptIdentityFor(d.id);var hub=d.wingRoot.V;var skin=RvSupport(body,rig.body.position,side);float depth=skin(hub);
        float mount=depth+6.3f;var fan=Vector2.zero;foreach(var b in d.primaries)fan+=b.root.V;fan/=d.primaries.Length;
        wing=Joint(rig.body,(side<0?"03 Near":"04 Far")+" supported shoulder and flight arm",Point(hub,side*mount));
        tip=Joint(wing,"Cambered distal primary fan and coverts",Point(fan,side*(mount+3)));
        var saddle=EyeContour(hub-new Vector2(0,1),16,22,"round");
        Part(rig.body,"Fuselage shoulder saddle mounting "+side,RvSolid(saddle,p=>skin(p)+.8f,side,1.5f,1.1f,7),2);
        Part(rig.body,"Fitted shoulder mounting flange "+side,RvSolid(RvInset(saddle,2),p=>skin(p)+2.2f,side,.9f,.65f,7),4);
        // The actual spindle spans the body-side mount and rotating wing yoke.
        Part(wing,"Load-bearing shoulder spindle "+side,SculptSpindle(hub,side*(depth+3.7f),8.3f,5.0f),2);
        Part(wing,"Outer machined shoulder housing "+side,SculptSpindle(hub,side*(mount+3),12,2.2f,7.7f),4);
        Part(wing,"Recessed shoulder bearing seal "+side,SculptSpindle(hub,side*(mount+4.1f),8,1.3f,5.6f),2);
        Part(wing,"Seated luminous shoulder bearing "+side,RoundSolid(hub,side*(mount+5.1f),5.4f,5.4f,1.65f),5);
        var elbow=Vector2.Lerp(hub,fan,.67f)+new Vector2(0,2);float fanZ=side*(mount+3.4f);
        if(Vector2.Distance(hub,fan)>6)
        {
            var a=new Vector3(hub.x,hub.y,side*(mount+.5f));var b=new Vector3(elbow.x,elbow.y,side*(mount+2));var c=new Vector3(fan.x,fan.y,fanZ);
            Part(wing,"Continuous structural wing forearm "+side,SculptTube(new[]{a,b,c},4.6f,3.8f,3.8f,18,12),2);
            // The casing must enclose the load-bearing member. In v4 its
            // smaller radius buried it inside the chassis and exposed a rod.
            Part(wing,"Enclosing tapered forearm armour casting "+side,SculptTube(new[]{a+new Vector3(0,-.5f,side*1.5f),b+new Vector3(0,-.5f,side*1.5f),c},7.1f,5.6f,5.7f,18,14),d.headMaterial==3?3:0);
            var shoulderDirection=(b-a).normalized;var wristDirection=(c-b).normalized;
            Part(wing,"Forearm seated shoulder ferrule "+side,SculptCuff(a+shoulderDirection*2,a+shoulderDirection*5,7.3f,6.2f,1.4f),4);
            Part(wing,"Forearm wrist retaining ferrule "+side,SculptCuff(c-wristDirection*5,c-wristDirection*2,6.1f,5.2f,1.2f),4);
        }
        Part(tip,"Distal fan load-bearing comb "+side,SculptSpindle(fan,fanZ,8.7f,3.0f),2);
        Part(tip,"Distal fan machined retaining nut "+side,SculptSpindle(fan,fanZ+side*3,6.6f,1.2f,3.6f),4);
        for(int i=0;i<d.primaries.Length;i++)
        {
            var b=d.primaries[i];
            if(d.id=="newbird04")b=SculptShortBlade(b,1,i<4?1.08f:1);
            else if(d.id=="newbird05")b=SculptShortBlade(b,1,i<4?.84f:.91f);
            float z=fanZ+side*(1.7f+i*(d.id=="newbird04"?1.65f:d.id=="newbird05"?1.12f:1.35f));
            float span=10+Mathf.Min(11,Vector2.Distance(b.root.V,b.tip.V)*.044f);
            if(Vector2.Distance(fan,b.root.V)>2)
                Part(tip,"Connected feather socket spar "+side+" "+i,SculptTube(new[]{new Vector3(fan.x,fan.y,fanZ),new Vector3(b.root.x,b.root.y,z)},2.6f,2,2,6,8),2);
            Part(tip,"Primary "+side+" "+i+" solid cambered flight blade",SculptBlade(b,z,side,span,.82f),b.material);
            Part(tip,"Primary "+side+" "+i+" seated enamel face",SculptBladeInlay(b,z,side,span,.82f,.28f,.985f,identity.inlay),b.inset);
            // Two short overlapping rows make a continuous root mantle. Their
            // spacing and sweep come from this bird's measured primary axes.
            if(i<d.primaries.Length-1)
            {
                var covert=SculptShortBlade(b,identity.covert,.68f);float covertZ=z+side*3.4f;
                int covertMaterial=d.id=="newbird04"?(i<4?3:i%3==0?4:d.bodyMaterial)
                    :d.id=="newbird05"?(i<3?3:i%3==0?4:d.bodyMaterial):i%3==0?4:d.bodyMaterial;
                Part(tip,"Layered sculpted root covert "+side+" "+i,SculptBlade(covert,covertZ,side,span*.31f,.72f),covertMaterial);
                if(i%2==0)Part(tip,"Covert enamel detail "+side+" "+i,SculptBladeInlay(covert,covertZ,side,span*.31f,.72f,.27f,.95f,.61f),b.material);
            }
        }
        // Machined yoke fingers physically enclose the fan root, instead of a
        // decorative washer floating over unattached feather plates.
        for(int n=0;n<2;n++)
        {
            var at=fan+new Vector2((n==0?-1:1)*6,-2);var end=fan+new Vector2((n==0?-1:1)*7,11);
            Part(tip,"Enclosing distal bearing yoke "+side+" "+n,SculptTube(new[]{new Vector3(at.x,at.y,fanZ-3*side),new Vector3(at.x,at.y+5,fanZ+4*side),new Vector3(end.x,end.y,fanZ+6*side)},2.1f,1.8f,1.6f,8,8),4);
        }
    }
    static void BuildSculptTail(SkyPulseNeonFinchPrototype rig,Vector2 fan)
    {
        var d=design;var a=d.tailRoot.V;var axis=fan-a;
        if(axis.sqrMagnitude<16)
        {
            // Several references seat every tail blade at the actuator pivot.
            // Give that socket a real inboard shaft, rather than sweeping a
            // zero-length cylinder whose coincident rings have zero normals.
            var tips=Vector2.zero;foreach(var blade in d.tail)tips+=blade.tip.V;tips/=d.tail.Length;
            axis=tips-fan;if(axis.sqrMagnitude<.01f)axis=Vector2.left;axis.Normalize();a=fan-axis*14;
        }
        else axis.Normalize();
        Part(rig.tail,"Continuous sealed tail transmission",SculptTube(new[]{new Vector3(a.x,a.y,0),new Vector3(fan.x,fan.y,0)},6.6f,8.2f,5.4f,12,12),2);
        var collar=Vector2.Lerp(a,fan,.65f);
        Part(rig.tail,"Machined tail retaining collar",SculptCuff(new Vector3(collar.x-axis.x*3,collar.y-axis.y*3,0),new Vector3(collar.x+axis.x*3,collar.y+axis.y*3,0),8.4f,10,2),4);
        Part(rig.tailTip,"Tail fan supported root hub",SculptSpindle(fan,0,8,7),2);
        for(int i=0;i<d.tail.Length;i++)
        {
            var b=d.tail[i];float z=(i-(d.tail.Length-1)*.5f)*3.4f;float span=(i-(d.tail.Length-1)*.5f)*3.0f;
            if(Vector2.Distance(fan,b.root.V)>2)Part(rig.tailTip,"Tail feather root socket "+i,SculptTube(new[]{new Vector3(fan.x,fan.y,0),new Vector3(b.root.x,b.root.y,z)},2.5f,2.5f,2.2f,6,8),2);
            Part(rig.tailTip,"Tail "+i+" closed cambered load-bearing blade",SculptBlade(b,z,1,span,1.35f),b.material);
            Part(rig.tailTip,"Tail "+i+" seated upper enamel",SculptBladeInlay(b,z,1,span,1.35f,d.id=="chrome_raven"?.45f:.22f,.86f,.67f),b.inset);
            // Reverse faces use the same curve with the camber mirrored around
            // the blade's centerline; enough separation prevents coplanar faces.
            Part(rig.tailTip,"Tail "+i+" seated reverse enamel",SculptBladeReverseInlay(b,z,span,1.35f,d.id=="chrome_raven"?.45f:.22f),b.inset);
        }
        if(d.jet)BuildJet(rig,fan);
    }
    static Mesh SculptBladeReverseInlay(Blade b,float z,float span,float thickness,float begin)
    {return SculptBladeInlay(b,z,1,span,thickness,begin,.86f,.67f,true);}
    static void BuildSculptTalons(SkyPulseNeonFinchPrototype rig,SculptProfile profile)
    {
        var d=design;var c=Mean(d.body);profile.Section(c.x-12,out _,out float lower,out _);
        foreach(int side in new[]{-1,1})
        {
            float x=c.x-12+(side>0?13:0),z=side*profile.depth*.58f;var hip=new Vector2(x,lower-12);var ankle=new Vector2(x+5,lower+14);
            Part(rig.body,"Seated landing hip bearing "+side,SculptSpindle(hip,z,6.2f,4.7f),2);
            Part(rig.body,"Landing hip bearing cap "+side,SculptSpindle(hip,z+side*4.3f,4.6f,1.1f),4);
            var leg=new[]{new Vector3(hip.x,hip.y,z),new Vector3(x-3,lower+2,z),new Vector3(ankle.x,ankle.y,z)};
            Part(rig.body,"Continuous curved landing shank "+side,SculptTube(leg,3.4f,3.0f,2.8f,12,10),2);
            Part(rig.body,"Fitted shank armour "+side,SculptTube(new[]{leg[0]+new Vector3(0,0,side*1),leg[1]+new Vector3(0,0,side*1)},2.8f,2.2f,2.4f,8,8),d.bodyMaterial);
            Part(rig.body,"Articulated ankle knuckle "+side,SculptSpindle(ankle,z,4.1f,4.5f),4);
            for(int toe=0;toe<3;toe++)
            {
                float tz=z+(toe-1)*4.7f;float reverse=toe==2?1:-1;float reach=toe==0?21:toe==1?15:12;
                var path=new[]{new Vector3(ankle.x,ankle.y,tz),new Vector3(ankle.x+reverse*7,ankle.y+10,tz),new Vector3(ankle.x+reverse*reach,ankle.y+17,tz),new Vector3(ankle.x+reverse*(reach+2),ankle.y+10,tz),new Vector3(ankle.x+reverse*(reach-2),ankle.y+6,tz)};
                Part(rig.body,"Curved articulated landing claw "+side+" "+toe,SculptTube(path,2.3f,2.6f,.23f,16,8),2);
                Part(rig.body,"Machined claw hook tip "+side+" "+toe,SculptTube(new[]{SculptCatmull(path,.63f),SculptCatmull(path,.83f),path[4]},1.15f,1.3f,.12f,8,8),4);
            }
        }
    }
}
