using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // A flexible mesh over each existing registered pose. Authored up/down wing
    // poses remain the primary stroke; joint/tail flex and station light add life.
    // Does not move the pilot transform, collider, input state or flight forces.
    public sealed class SkyPulseBirdRig : MonoBehaviour
    {
        [Serializable] sealed class ShapeFile { public int columns,rows;public Shape[] frames; }
        [Serializable] sealed class Shape { public string path,cells;public int width,height;[NonSerialized] public byte[] coverage; }
        struct PendingShape { public Sprite Sprite;public string Path; }
        static readonly HashSet<Sprite> prepared=new HashSet<Sprite>(),queued=new HashSet<Sprite>();
        static readonly List<PendingShape> pending=new List<PendingShape>(256);
        static Dictionary<string,Shape> shapes;
        static int shapeColumns,shapeRows;
        SpriteRenderer target;Material material,original;MaterialPropertyBlock block;
        static readonly int RigId=Shader.PropertyToID("_Rig"),BoundsId=Shader.PropertyToID("_RigBounds"),TintId=Shader.PropertyToID("_LightTint");
        public void Bind(SpriteRenderer renderer)
        {
            target=renderer;original=target.sharedMaterial;material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/MechanicalBird")){name="Mechanical plumage light and flexible joints"};block=new MaterialPropertyBlock();target.sharedMaterial=material;
        }
        public void PrepareFrames(Sprite[] frames){if(frames!=null)for(int i=0;i<frames.Length;i++)Prepare(frames[i]);}
        void Prepare(Sprite sprite)=>PrepareSprite(sprite);

        // All bird views use the same measured shape: flight, Home, Hangar,
        // training and lineage. Pixels are never recoloured or rewritten.
        // Coverage excludes disconnected fragments left in source-sheet margins.
        public static void PrepareSprite(Sprite sprite,string resourcePath=null)
        {
            if(sprite==null||prepared.Contains(sprite)||!queued.Add(sprite))return;
            pending.Add(new PendingShape{Sprite=sprite,Path=resourcePath});
        }
        // Unity only permits OverrideGeometry within its player loop. Loaders
        // can run during Awake or an Editor preview, so they only enqueue work.
        // The normal LateUpdate callbacks consume the batch before rendering.
        public static bool FlushPending()
        {
            if(pending.Count==0)return false;
            bool changed=false;
            for(int i=0;i<pending.Count;i++)
            {
                var request=pending[i];if(request.Sprite==null)continue;
                PrepareSpriteNow(request.Sprite,request.Path);changed=true;
            }
            pending.Clear();queued.Clear();
            if(changed)
            {
                // Construction-time refresh only. Warm animation never searches
                // the scene or allocates: reused poses leave the queue empty.
                foreach(var image in UnityEngine.Object.FindObjectsByType<Image>())
                    if(image.useSpriteMesh && image.sprite!=null && prepared.Contains(image.sprite))image.SetVerticesDirty();
            }
            return changed;
        }
        void LateUpdate()=>FlushPending();
        static void PrepareSpriteNow(Sprite sprite,string resourcePath)
        {
            if(sprite==null||!prepared.Add(sprite))return;
            if(shapes==null)
            {
                shapes=new Dictionary<string,Shape>(StringComparer.Ordinal);
                var text=Resources.Load<TextAsset>("SkyPulse/characters/bird-silhouette-coverage");
                var data=text==null?null:JsonUtility.FromJson<ShapeFile>(text.text);
                if(data!=null&&data.columns>0&&data.columns<=96&&data.rows>0&&data.rows<=96)
                {
                    shapeColumns=data.columns;shapeRows=data.rows;
                    if(data.frames!=null)foreach(var item in data.frames)
                    {
                        if(item==null||string.IsNullOrEmpty(item.path)||string.IsNullOrEmpty(item.cells))continue;
                        var bytes=Convert.FromBase64String(item.cells);
                        if(bytes.Length*8<shapeColumns*shapeRows)continue;
                        item.coverage=bytes;int slash=item.path.LastIndexOf('/');
                        shapes[item.path.Substring(slash+1)]=item;
                    }
                }
            }
            string key=resourcePath;
            if(string.IsNullOrEmpty(key))key=sprite.texture==null?sprite.name:sprite.texture.name;
            int separator=key.LastIndexOf('/');key=key.Substring(separator+1);
            shapes.TryGetValue(key,out var shape);
            if(shape!=null&&(sprite.texture.width!=shape.width||sprite.texture.height!=shape.height))shape=null;
            int columns=shape==null?22:shapeColumns,rows=shape==null?18:shapeRows;
            var vertices=new Vector2[(columns+1)*(rows+1)];var indices=new List<ushort>(columns*rows*3);
            var size=sprite.rect.size;var b=sprite.bounds;
            Vector2 lo=(Vector2)b.min*sprite.pixelsPerUnit+sprite.pivot,hi=(Vector2)b.max*sprite.pixelsPerUnit+sprite.pivot;
            lo=new Vector2(Mathf.Clamp(lo.x,0,size.x),Mathf.Clamp(lo.y,0,size.y));hi=new Vector2(Mathf.Clamp(hi.x,0,size.x),Mathf.Clamp(hi.y,0,size.y));
            // Keep the complete original vertex extent even when unused cells
            // are omitted, preserving artwork bounds, pivots and registration.
            // OverrideGeometry takes rectangle pixels, not Sprite.vertices units.
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
                vertices[y*(columns+1)+x]=new Vector2(Mathf.Lerp(lo.x,hi.x,x/(float)columns),Mathf.Lerp(lo.y,hi.y,y/(float)rows));
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {
                if(shape!=null&&!Covered(shape,sprite.rect,vertices[y*(columns+1)+x],vertices[(y+1)*(columns+1)+x+1]))continue;
                int i=y*(columns+1)+x;
                indices.Add((ushort)i);indices.Add((ushort)(i+columns+1));indices.Add((ushort)(i+1));
                indices.Add((ushort)(i+1));indices.Add((ushort)(i+columns+1));indices.Add((ushort)(i+columns+2));
            }
            // A malformed optional shape record must never hide a player's bird.
            if(indices.Count==0)
            {
                for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
                {int i=y*(columns+1)+x;indices.Add((ushort)i);indices.Add((ushort)(i+columns+1));indices.Add((ushort)(i+1));indices.Add((ushort)(i+1));indices.Add((ushort)(i+columns+1));indices.Add((ushort)(i+columns+2));}
            }
            sprite.OverrideGeometry(vertices,indices.ToArray());
        }
        static bool Covered(Shape shape,Rect rect,Vector2 lo,Vector2 hi)
        {
            int left=Mathf.Clamp(Mathf.FloorToInt((rect.x+lo.x)/shape.width*shapeColumns),0,shapeColumns-1);
            int right=Mathf.Clamp(Mathf.CeilToInt((rect.x+hi.x)/shape.width*shapeColumns)-1,0,shapeColumns-1);
            int bottom=Mathf.Clamp(Mathf.FloorToInt((rect.y+lo.y)/shape.height*shapeRows),0,shapeRows-1);
            int top=Mathf.Clamp(Mathf.CeilToInt((rect.y+hi.y)/shape.height*shapeRows)-1,0,shapeRows-1);
            for(int y=bottom;y<=top;y++)for(int x=left;x<=right;x++)
            {int bit=(shapeRows-1-y)*shapeColumns+x;if((shape.coverage[bit>>3]&(1<<(bit&7)))!=0)return true;}
            return false;
        }
        public void Present(float clock,float impulse,float glide,bool reduced,Color light)
        {
            if(target==null||target.sprite==null)return;Prepare(target.sprite);var b=target.sprite.bounds;
            block.SetVector(RigId,new Vector4(clock,impulse,glide,reduced?0:1));block.SetVector(BoundsId,new Vector4(b.min.x,b.min.y,1/Mathf.Max(.001f,b.size.x),1/Mathf.Max(.001f,b.size.y)));block.SetColor(TintId,light);target.SetPropertyBlock(block);
        }
        void OnDestroy(){if(target!=null&&target.sharedMaterial==material)target.sharedMaterial=original;if(material!=null)Destroy(material);}
    }
}
