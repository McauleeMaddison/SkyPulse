#if UNITY_EDITOR
using UnityEngine;

// This component is compiled only in the Editor; its scenes live under Editor.
// Moving child MeshFilters changes the structure, not the game's flight rules.
[ExecuteAlways]
public sealed class SkyPulseBackgroundAuthoring : MonoBehaviour
{
    [HideInInspector] public int World,Variant;
    [HideInInspector] public bool Relay,Horizon;
    [HideInInspector] public string[] OriginalNames;
    [HideInInspector] public Vector3[] OriginalPositions,OriginalScales;
    [HideInInspector] public Quaternion[] OriginalRotations;
    [HideInInspector] public Transform BeaconMount;
    [HideInInspector] public Vector3 OriginalMountPosition,OriginalMountScale=Vector3.one;
    [HideInInspector] public Quaternion OriginalMountRotation=Quaternion.identity;
    [HideInInspector] public Vector4[] OriginalBeaconPoints,BeaconColours;
    Vector4[] currentPoints;
    public Vector4[] BeaconPoints()
    {
        if(OriginalBeaconPoints==null)return new Vector4[5];
        if(currentPoints==null||currentPoints.Length!=OriginalBeaconPoints.Length)currentPoints=new Vector4[OriginalBeaconPoints.Length];
        var delta=BeaconMount!=null?transform.worldToLocalMatrix*BeaconMount.localToWorldMatrix*Matrix4x4.TRS(OriginalMountPosition,OriginalMountRotation,OriginalMountScale).inverse:Matrix4x4.identity;
        float scale=Mathf.Max(delta.MultiplyVector(Vector3.right).magnitude,delta.MultiplyVector(Vector3.up).magnitude);
        for(int i=0;i<currentPoints.Length;i++)
        {
            var original=OriginalBeaconPoints[i];var at=delta.MultiplyPoint3x4(new Vector3(original.x,original.y,original.z));
            currentPoints[i]=new Vector4(at.x,at.y,at.z,BeaconMount!=null?original.w*scale:0);
        }
        return currentPoints;
    }
    void Update()
    {
        if(Relay||Horizon||BeaconColours==null||OriginalBeaconPoints==null||OriginalBeaconPoints.Length!=5)return;
        var points=BeaconPoints();
        foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            renderer.sharedMaterial.SetVectorArray("_BeaconPoints",points);renderer.sharedMaterial.SetVectorArray("_BeaconColours",BeaconColours);
        }
    }
}

#endif
