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
    // These source images depict a downstroke. Unfold the distal feather fan
    // into real lateral space without rolling the shoulder mounting off its seat.
    // The approved Finch and four already outboard reference rigs are untouched.
    public static bool FitWingFans(SkyPulseNeonFinchPrototype rig,Design d)
    {
        if(!d.id.StartsWith("newbird",StringComparison.Ordinal))return false;
        var near=Quaternion.Euler(70,0,0);var far=Quaternion.Euler(-70,0,0);
        if(Quaternion.Angle(rig.leftWingTip.localRotation,near)<.0001f&&Quaternion.Angle(rig.rightWingTip.localRotation,far)<.0001f)return false;
        rig.leftWingTip.localRotation=near;rig.rightWingTip.localRotation=far;return true;
    }
    static readonly Dictionary<string,string> WingFanOriginals=new Dictionary<string,string>{
        {"newbird01","ca83520e2ddfd19cad15a948af8cc386171f38e9f3bf1ab77eb72eae6a937ce5"},
        {"newbird02","084f0302646dd229c6e30acebac2b6fd7e4456478711a9d7b08c7b9cb0301b48"},
        {"newbird03","866616674a36ce15328d86554647a514f9fd9ca02904ed4516893921463dbcb3"},
        {"newbird04","54184bf272c5a2c5e82ac8c8808513dd3e7d341fc6646e4bee9a52feabdfe877"},
        {"newbird05","c025fb05abe3721b398b19d8dd986f0ee03099e40d99cebbfbaa1ed65994bf24"},
        {"newbird06","f39e5b8c68bb09e4418bae02e69b73a2b7db37ebb9943801fdf885b5a076f9b5"},
        {"newbird07","921766915c750dfde7c394d540019e273d7ddadd0dfb065688c3abe2d8423d79"},
        {"newbird08","58dff895041ccaf47a4b7c3bc75c10b5bb984e9cc41b73b07d39b9c1f2f9f001"},
        {"newbird09","59ca35a6893f34007bbba781702fb772c5c8cb40c7b498bc7d7937dfbec9c35e"},
        {"newbird10","421da24a8657ea207752e2f73c55c8497c5ab7d1ba914a8ba68294af1adf2898"}
    };
    public static void FitSavedWingFans()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006"||EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Review wing fan corrections in the isolated Editor before promotion.");
        foreach(var d in LoadDesigns())
        {
            if(!WingFanOriginals.ContainsKey(d.id))continue;
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(d));
            if(EditorUtility.IsDirty(asset))throw new InvalidOperationException("Preserving unsaved bird edits: "+d.name);
            var root=PrefabUtility.LoadPrefabContents(PrefabPath(d));
            try
            {
                var rig=root.GetComponent<SkyPulseNeonFinchPrototype>();rig.ResetPose();
                if(!FitWingFans(rig,d))continue;
                if(ShoulderHash(PrefabPath(d))!=WingFanOriginals[d.id])throw new InvalidOperationException("Preserving edited wing-fan prefab: "+d.name);
                rig.CaptureBindPose();PrefabUtility.SaveAsPrefabAsset(root,PrefabPath(d));Debug.Log("WING_FAN_FIT: "+d.id);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }

}
