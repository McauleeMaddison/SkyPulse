#if UNITY_EDITOR
using UnityEngine;
[ExecuteAlways]
public sealed class SkyPulsePipeAuthoring : MonoBehaviour
{
    [HideInInspector] public int World;
    public Color Accent,Energy,Metal;
    [Range(.4f,1.6f)] public float LightRate=1;
    [Range(0,1.5f)] public float Detail=1;
    [HideInInspector] public Bounds BarrelBounds,CollarBounds;
    void Update()
    {
        foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            var material=renderer.sharedMaterial;if(material==null)continue;
            material.SetColor("_ThemeAccent",Accent);material.SetColor("_ThemeEnergy",Energy);material.SetColor("_MetalTone",Metal);material.SetFloat("_LightRate",LightRate);material.SetFloat("_Detail",Detail);
        }
    }
}
#endif
