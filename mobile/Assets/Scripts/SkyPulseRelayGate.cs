using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkyPulse.Mobile
{
    /// <summary>
    /// A real, hollow mesh assembly in the existing orthographic flight view.
    /// Its far surfaces render behind the sprite pilot; the near lip renders in
    /// front. The aperture never contains an opaque polygon or a new collider.
    /// Geometry/materials are built once per course and released with the gate.
    /// </summary>
    public sealed class SkyPulseRelayGate : MonoBehaviour
    {
        public const int RearOrder = 10, FrontOrder = 20;
        public MeshRenderer RearSurface { get; private set; }
        public MeshRenderer FrontSurface { get; private set; }
        public const float InnerRadius = RelaySession.ApertureHalfHeight;
        const int Segments = 96;
        static readonly Vector3 Across = new Vector3(.46f, 0, -.887919f);
        static readonly Vector3 Axis = new Vector3(.887919f, 0, .46f);
        static readonly int OpenId = Shader.PropertyToID("_Open"), ClockId = Shader.PropertyToID("_Clock"), PulseId = Shader.PropertyToID("_Pulse"), AccentId = Shader.PropertyToID("_Accent"),ChargeId=Shader.PropertyToID("_Charge");
        public float ChargeFraction { get; private set; }
        Mesh rearMesh, frontMesh;
        Material metal;

        // Temporary construction buffers exist only while a course is created.
        sealed class Geometry
        {
            public readonly List<Vector3> Vertices = new List<Vector3>(10000);
            public readonly List<Vector3> Normals = new List<Vector3>(10000);
            public readonly List<Color> Colours = new List<Color>(10000);
            public readonly List<Vector2> Uv = new List<Vector2>(10000);
            public readonly List<int> Indices = new List<int>(15000);
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color colour, float angle, Vector3 endNormal)
            {
                int start = Vertices.Count;
                Vertices.Add(a); Vertices.Add(b); Vertices.Add(c); Vertices.Add(d);
                Normals.Add(normal); Normals.Add(endNormal); Normals.Add(endNormal); Normals.Add(normal);
                for (int i = 0; i < 4; i++) { Colours.Add(colour); Uv.Add(new Vector2(angle, i < 2 ? 0 : 1)); }
                Indices.Add(start); Indices.Add(start + 1); Indices.Add(start + 2);
                Indices.Add(start); Indices.Add(start + 2); Indices.Add(start + 3);
            }
            public Mesh Finish(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(Vertices); mesh.SetNormals(Normals); mesh.SetColors(Colours); mesh.SetUVs(0, Uv); mesh.SetTriangles(Indices, 0);
                mesh.RecalculateBounds(); mesh.UploadMeshData(false); return mesh;
            }
        }

        public void Build(bool exit)
        {
            var rear = new Geometry(); var front = new Geometry();
            // Bevelled titanium shell, a physically recessed bore and two raised
            // collars. The bore's minimum height is exactly the gameplay opening.
            float radius=InnerRadius+.25f;
            var section = new[] { new Vector2(-.25f,-.13f), new Vector2(-.19f,-.22f), new Vector2(.12f,-.22f), new Vector2(.25f,-.12f), new Vector2(.25f,.12f), new Vector2(.12f,.22f), new Vector2(-.19f,.22f), new Vector2(-.25f,.13f) };
            Ring(rear, front, radius, 0, section, false, false);
            Ring(rear, front, radius+.01f, -.24f, RoundSection(.072f,.055f,8), false, true);
            Ring(rear, front, radius+.01f, .24f, RoundSection(.072f,.055f,8), false, true);
            Ring(rear, front, radius-.13f, -.26f, RoundSection(.022f,.025f,6), true, false);
            Ring(rear, front, radius-.13f, .26f, RoundSection(.022f,.025f,6), true, false);
            Ring(rear, front, radius+.16f, -.26f, RoundSection(.012f,.022f,6), true, false);
            Ring(rear, front, radius+.16f, .26f, RoundSection(.012f,.022f,6), true, false);
            // Six curved armour saddles break up the silhouette, exposing the
            // continuous light conduit between champagne-titanium housings.
            var armour=new[]{new Vector2(-.06f,-.22f),new Vector2(.02f,-.29f),new Vector2(.13f,-.19f),new Vector2(.13f,.19f),new Vector2(.02f,.29f),new Vector2(-.06f,.22f)};
            for(int sector=0;sector<6;sector++)
                Ring(rear,front,radius+.26f,0,armour,false,false,sector*Mathf.PI/3+.16f,.63f,new Color(.52f,.57f,.60f,0));
            // Recessed emitter cartridges and fastening heads give scale to the
            // continuous shell without a polygonal/block gate silhouette.
            for (int i = 0; i < 16; i++)
            {
                float angle = (i + .5f) * Mathf.PI / 8;
                Vector3 radial = Radial(angle), tangent = Across * Mathf.Cos(angle) - Vector3.up * Mathf.Sin(angle);
                Vector3 centre = radial * (radius+.02f) - Axis * .30f;
                Stud(rear, front, centre, tangent, radial, .048f, new Color(.46f,.57f,.63f,0));
                if (i % 2 == 0)
                {
                    Vector3 at = radial * radius - Axis * .305f;
                    AddQuad(rear, front, at-tangent*.105f-radial*.035f, at+tangent*.105f-radial*.035f, at+tangent*.105f+radial*.035f, at-tangent*.105f+radial*.035f, Axis, new Color(.7f,.94f,1,1), angle);
                }
            }
            // Sliding locking shoes sit outside the authoritative aperture, so
            // their decorative release can never delay an already unlocked pass.
            for(int i=0;i<8;i++)
            {
                float a=(i+.5f)*Mathf.PI/4;var radial=Radial(a);var tangent=Across*Mathf.Cos(a)-Vector3.up*Mathf.Sin(a);
                var at=radial*(InnerRadius+.13f)-Axis*.31f;
                var buffer=at.z<0?front:rear;int begin=buffer.Uv.Count;
                buffer.Quad(at-tangent*.11f-radial*.09f,at+tangent*.11f-radial*.09f,at+tangent*.11f+radial*.17f,at-tangent*.11f+radial*.17f,-Axis,new Color(.36f,.48f,.54f,0),a,-Axis);
                for(int v=begin;v<buffer.Uv.Count;v++)buffer.Uv[v]=new Vector2(a,3);
            }
            rearMesh = rear.Finish("Relay far shell and recessed bore"); frontMesh = front.Finish("Relay near collar and aperture lip");
            var shader = Resources.Load<Shader>("SkyPulse/Shaders/RelayTitanium");
            if (shader == null) throw new System.InvalidOperationException("Missing included Relay titanium shader");
            metal = new Material(shader) { name = "Relay titanium and embedded emitters" };
            metal.SetColor(AccentId, exit ? new Color(.24f,1,.65f) : new Color(.12f,.58f,1));
            RearSurface = Surface("Far shell · behind pilot", rearMesh, RearOrder);
            FrontSurface = Surface("Near lip · in front of pilot", frontMesh, FrontOrder);
            SetVisual(exit, 0, 0);
        }

        MeshRenderer Surface(string name, Mesh mesh, int order)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); part.transform.SetParent(transform, false);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.GetComponent<MeshRenderer>(); renderer.sharedMaterial = metal; renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }
        static Vector3 Radial(float angle) => Across * Mathf.Sin(angle) + Vector3.up * Mathf.Cos(angle);
        static Vector2[] RoundSection(float radial, float depth, int sides)
        {
            var points = new Vector2[sides];
            for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;points[i]=new Vector2(Mathf.Cos(a)*radial,Mathf.Sin(a)*depth);}
            return points;
        }
        static void Ring(Geometry rear, Geometry front, float radius, float axial, Vector2[] section, bool luminous, bool collar,float start=0,float span=Mathf.PI*2,Color? tint=null)
        {
            int count=Mathf.Max(4,Mathf.CeilToInt(Segments*span/(Mathf.PI*2)));
            for(int segment=0;segment<count;segment++)
            {
                float a=start+segment*span/count,b=start+(segment+1)*span/count;
                Vector3 ra=Radial(a),rb=Radial(b);
                for(int j=0;j<section.Length;j++)
                {
                    Vector2 p=section[j],q=section[(j+1)%section.Length],edge=q-p;
                    Vector3 normal=(ra*edge.y-Axis*edge.x).normalized,endNormal=(rb*edge.y-Axis*edge.x).normalized;
                    Color colour=luminous?new Color(.65f,.94f,1,1):collar?new Color(.43f,.53f,.59f,0):j==0||j==2||j==4||j==6?new Color(.38f,.47f,.52f,0):j==7?new Color(.055f,.10f,.14f,0):new Color(.13f,.20f,.25f,0);
                    if(tint.HasValue)colour=Color.Lerp(tint.Value,colour,.28f);
                    AddQuad(rear,front,ra*(radius+p.x)+Axis*(axial+p.y),rb*(radius+p.x)+Axis*(axial+p.y),rb*(radius+q.x)+Axis*(axial+q.y),ra*(radius+q.x)+Axis*(axial+q.y),normal,colour,(a+b)*.5f,endNormal);
                }
            }
        }
        static void AddQuad(Geometry rear, Geometry front, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color colour,float angle, Vector3? endNormal=null)
        {
            // Split by actual camera depth, not by a guessed left/right bitmap.
            // The orthographic flight plane and sprite pilot remain at z=0.
            ((a.z+b.z+c.z+d.z)<0?front:rear).Quad(a,b,c,d,normal,colour,angle,endNormal??normal);
        }
        static void Stud(Geometry rear,Geometry front,Vector3 at,Vector3 tangent,Vector3 radial,float radius,Color colour)
        {
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;
                Vector3 p=at+(tangent*Mathf.Sin(a)+radial*Mathf.Cos(a))*radius,q=at+(tangent*Mathf.Sin(b)+radial*Mathf.Cos(b))*radius;
                AddQuad(rear,front,p,q,q+Axis*.025f,p+Axis*.025f,(p+q-at*2).normalized,colour,a);
                AddQuad(rear,front,at+Axis*.025f,p+Axis*.025f,q+Axis*.025f,at+Axis*.025f,Axis,colour,a);
            }
        }
        public void SetVisual(bool open,float clock,float pulse)
        {
            SetCharge(open?2:0,open,clock,pulse);
        }
        public void SetCharge(int crystals,bool open,float clock,float pulse)
        {
            if(metal==null)return;
            ChargeFraction=Mathf.Clamp01(crystals*.5f);metal.SetFloat(ChargeId,ChargeFraction);
            metal.SetFloat(OpenId,open?1:0);metal.SetFloat(ClockId,clock);metal.SetFloat(PulseId,pulse);
        }
        void OnDestroy()
        {
            if(rearMesh!=null)Destroy(rearMesh);if(frontMesh!=null)Destroy(frontMesh);if(metal!=null)Destroy(metal);
        }
    }
}
