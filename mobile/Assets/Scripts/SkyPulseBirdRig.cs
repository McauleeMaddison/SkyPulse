using System.Collections.Generic;
using UnityEngine;

namespace SkyPulse.Mobile
{
    // A flexible mesh over each existing registered pose. Authored up/down wing
    // poses remain the primary stroke; joint/tail flex and station light add life.
    // Does not move the pilot transform, collider, input state or flight forces.
    public sealed class SkyPulseBirdRig : MonoBehaviour
    {
        readonly HashSet<Sprite> prepared=new HashSet<Sprite>();
        SpriteRenderer target;Material material,original;MaterialPropertyBlock block;
        static readonly int RigId=Shader.PropertyToID("_Rig"),BoundsId=Shader.PropertyToID("_RigBounds"),TintId=Shader.PropertyToID("_LightTint");
        public void Bind(SpriteRenderer renderer)
        {
            target=renderer;original=target.sharedMaterial;material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/MechanicalBird")){name="Mechanical plumage light and flexible joints"};block=new MaterialPropertyBlock();target.sharedMaterial=material;
        }
        public void PrepareFrames(Sprite[] frames){if(frames!=null)for(int i=0;i<frames.Length;i++)Prepare(frames[i]);}
        void Prepare(Sprite sprite)
        {
            if(sprite==null||!prepared.Add(sprite))return;
            const int columns=22,rows=18;var vertices=new Vector2[(columns+1)*(rows+1)];var triangles=new ushort[columns*rows*6];var size=sprite.rect.size;var b=sprite.bounds;
            Vector2 lo=(Vector2)b.min*sprite.pixelsPerUnit+sprite.pivot,hi=(Vector2)b.max*sprite.pixelsPerUnit+sprite.pivot;
            lo=new Vector2(Mathf.Clamp(lo.x,0,size.x),Mathf.Clamp(lo.y,0,size.y));hi=new Vector2(Mathf.Clamp(hi.x,0,size.x),Mathf.Clamp(hi.y,0,size.y));
            // OverrideGeometry accepts rectangle pixels, and applies the pivot
            // and pixels-per-unit conversion itself (not Sprite.vertices units).
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)vertices[y*(columns+1)+x]=new Vector2(Mathf.Lerp(lo.x,hi.x,x/(float)columns),Mathf.Lerp(lo.y,hi.y,y/(float)rows));
            int at=0;for(int y=0;y<rows;y++)for(int x=0;x<columns;x++){int i=y*(columns+1)+x;triangles[at++]=(ushort)i;triangles[at++]=(ushort)(i+columns+1);triangles[at++]=(ushort)(i+1);triangles[at++]=(ushort)(i+1);triangles[at++]=(ushort)(i+columns+1);triangles[at++]=(ushort)(i+columns+2);}
            sprite.OverrideGeometry(vertices,triangles);
        }
        public void Present(float clock,float impulse,float glide,bool reduced,Color light)
        {
            if(target==null||target.sprite==null)return;Prepare(target.sprite);var b=target.sprite.bounds;
            block.SetVector(RigId,new Vector4(clock,impulse,glide,reduced?0:1));block.SetVector(BoundsId,new Vector4(b.min.x,b.min.y,1/Mathf.Max(.001f,b.size.x),1/Mathf.Max(.001f,b.size.y)));block.SetColor(TintId,light);target.SetPropertyBlock(block);
        }
        void OnDestroy(){if(target!=null&&target.sharedMaterial==material)target.sharedMaterial=original;if(material!=null)Destroy(material);}
    }
}
