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
    public bool AnimatePreview=true;
    MaterialPropertyBlock previewClock;
    void OnEnable()
    {
        foreach(var filter in GetComponentsInChildren<MeshFilter>(true))UploadPreviewMesh(filter.sharedMesh);
    }
    public static void UploadPreviewMesh(Mesh mesh)
    {
        if(mesh==null||!mesh.isReadable)return;
        mesh.vertices=mesh.vertices;mesh.normals=mesh.normals;mesh.colors=mesh.colors;
        for(int part=0;part<mesh.subMeshCount;part++)mesh.SetIndices(mesh.GetIndices(part),mesh.GetTopology(part),part,false);
        mesh.UploadMeshData(false);
    }
    void Update()
    {
        foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            var material=renderer.sharedMaterial;if(material==null)continue;
            material.SetColor("_ThemeAccent",Accent);material.SetColor("_ThemeEnergy",Energy);material.SetColor("_MetalTone",Metal);material.SetFloat("_LightRate",LightRate);material.SetFloat("_Detail",Detail);
            // Animate the inspection scene without writing a changing clock into
            // shared material assets or altering the gameplay bake.
            if(previewClock==null)previewClock=new MaterialPropertyBlock();
            renderer.GetPropertyBlock(previewClock);previewClock.SetFloat("_Clock",AnimatePreview?(float)(UnityEditor.EditorApplication.timeSinceStartup%4096):0);
            renderer.SetPropertyBlock(previewClock);
        }
    }
}
#endif
