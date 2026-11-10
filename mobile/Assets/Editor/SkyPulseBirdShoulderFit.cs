using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SkyPulse.Mobile;

public static partial class SkyPulseBirdRosterWorkshop
{
    // Only the three measured low mounts move. The approved Finch, reference
    // artwork, wing sculpture and source materials retain their existing shape.
    public static float ShoulderLift(Design d)
    {
        float top=d.body.Min(p=>p.y),height=d.body.Max(p=>p.y)-top;
        if((d.wingRoot.y-top)/height<=.45f)return 0;
        return Mathf.Min(height*.16f,d.wingRoot.y-(top+height*.40f))*.01f;
    }
    public static bool FitShoulders(SkyPulseNeonFinchPrototype rig,Design d)
    {
        float lift=ShoulderLift(d);if(lift<=0)return false;
        float original=(d.body.Average(p=>p.y)-d.wingRoot.y)*.01f;
        float delta=original+lift-rig.leftWing.position.y;
        if(Mathf.Abs(delta)<.00001f)return false;
        rig.leftWing.position+=Vector3.up*delta;rig.rightWing.position+=Vector3.up*delta;
        foreach(var child in rig.body.GetComponentsInChildren<Transform>(true))
            if(child.parent==rig.body&&(child.name.StartsWith("Fuselage shoulder saddle mounting",StringComparison.Ordinal)||child.name.StartsWith("Fitted shoulder mounting flange",StringComparison.Ordinal)))child.position+=Vector3.up*delta;
        return true;
    }
    static readonly Dictionary<string,string> ShoulderOriginals=new Dictionary<string,string>{
        {"newbird01","4d2f3d174e28ed0b25138548d73f3d55c33495849bbc102ef48f1730630320fb"},
        {"newbird05","82d982b33accb6730b5cd1239bbe7d27667ed6faff78945de73d28ba29fd6583"},
        {"newbird06","26159cb979f1ad907d72acdbaa351669d904605d13a1504930da2061435c6957"}
    };
    static string ShoulderHash(string path)
    {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    public static void FitSavedShoulders()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before fitting saved shoulders.");
        foreach(var d in LoadDesigns())
        {
            if(ShoulderLift(d)<=0)continue;
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(d));
            if(EditorUtility.IsDirty(asset))throw new InvalidOperationException("Preserving unsaved owner edits: "+d.name);
            var root=PrefabUtility.LoadPrefabContents(PrefabPath(d));
            try
            {
                var rig=root.GetComponent<SkyPulseNeonFinchPrototype>();rig.ResetPose();
                if(!FitShoulders(rig,d))continue;
                if(!ShoulderOriginals.TryGetValue(d.id,out string original)||ShoulderHash(PrefabPath(d))!=original)
                    throw new InvalidOperationException("Preserving owner-edited shoulder prefab: "+d.name);
                rig.CaptureBindPose();PrefabUtility.SaveAsPrefabAsset(root,PrefabPath(d));Debug.Log("SHOULDER_FIT: "+d.id+" · "+ShoulderLift(d));
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
