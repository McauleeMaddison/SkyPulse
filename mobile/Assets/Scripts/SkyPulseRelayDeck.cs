using UnityEngine;
using UnityEngine.Rendering;

namespace SkyPulse.Mobile
{
    /// <summary>Fixed runway whose bright front edge is the existing ground collision height.</summary>
    public sealed class SkyPulseRelayDeck : MonoBehaviour
    {
        Mesh mesh;
        Material material;
        static readonly int DistanceId=Shader.PropertyToID("_Distance"),ClockId=Shader.PropertyToID("_Clock");
        public void Build(float width)
        {
            float x=width*.5f;
            mesh=new Mesh{name="Relay runway slab and ground lip"};
            mesh.vertices=new[]{new Vector3(-x,0,0),new Vector3(x,0,0),new Vector3(x,.95f,3),new Vector3(-x,.95f,3),new Vector3(-x,-.6f,-.4f),new Vector3(x,-.6f,-.4f)};
            mesh.normals=new[]{Vector3.up,Vector3.up,Vector3.up,Vector3.up,Vector3.back,Vector3.back};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(width,0),new Vector2(width,1),new Vector2(0,1),new Vector2(0,-1),new Vector2(width,-1)};
            mesh.triangles=new[]{0,1,2,0,2,3,4,5,1,4,1,0};mesh.RecalculateBounds();
            material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/RelayRunway")){name="Relay moving runway panels"};
            var filter=gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=9;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }
        public void SetVisual(float distance,float clock){material.SetFloat(DistanceId,distance);material.SetFloat(ClockId,clock);}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
