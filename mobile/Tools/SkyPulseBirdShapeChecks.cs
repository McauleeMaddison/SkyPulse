using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using SkyPulse.Mobile;

// Disposable-QA verification of source-preserving bird geometry. Does not read
// player saves, mutate source textures, or write assets in the shipping project.
public static class SkyPulseBirdShapeChecks
{
    [Serializable] sealed class ShapeFile { public int columns,rows;public Shape[] frames; }
    [Serializable] sealed class Shape { public string path,cells,sourceSha256;public int width,height; }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);Debug.Log("BIRD_SHAPE_CHECK: "+message);}
    public static IEnumerator Live()
    {
        yield return null;
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        var data=JsonUtility.FromJson<ShapeFile>(Resources.Load<TextAsset>("SkyPulse/characters/bird-silhouette-coverage").text);
        Check(data.columns==64&&data.rows==48&&data.frames.Length==90,"All fifteen birds retain six measured flap poses on a bounded 64 by 48 grid");
        var paths=new HashSet<string>(StringComparer.Ordinal);int knownFragments=0;
        foreach(var frame in data.frames)
        {
            Check(paths.Add(frame.path)&&frame.path.Contains("/roster/")&&!frame.path.Contains("frame-07")&&!frame.path.Contains("frame-08"),"Unique flap source; hit and unlock particles stay authored: "+frame.path);
            string sourcePath="Assets/Resources/"+frame.path+".png";
            using(var sha=SHA256.Create())Check(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(sourcePath))).Replace("-","").ToLowerInvariant()==frame.sourceSha256,"Shape evidence matches original unchanged PNG: "+frame.path);
            var texture=Resources.Load<Texture2D>(frame.path);Check(texture!=null&&texture.width==frame.width&&texture.height==frame.height,"Source canvas dimensions match shape coverage");
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.43f,.61f),97.5f);
            var pivot=sprite.pivot;var bounds=sprite.bounds;var rect=sprite.rect;float ppu=sprite.pixelsPerUnit;
            SkyPulseBirdRig.PrepareSprite(sprite,frame.path);SkyPulseBirdRig.FlushPending();
            Check(sprite.vertices.Length>100&&sprite.triangles.Length>0,"Flexible bird mesh remains visible: "+frame.path);
            Check(sprite.pivot==pivot&&sprite.rect==rect&&sprite.pixelsPerUnit==ppu&&Vector3.Distance(sprite.bounds.min,bounds.min)<.001f&&Vector3.Distance(sprite.bounds.max,bounds.max)<.001f,"Shape cleanup preserves registered bounds, pivot, rect and PPU");
            if(frame.path.EndsWith("volt-frame-06-v1",StringComparison.Ordinal))
            {
                Check(!Contains(sprite,new Vector2(256,1)),"Detached cyan star below the downstroke is outside the rendered bird mesh");
                Check(Contains(sprite,new Vector2(380,184)),"Downstroke torso stays inside the rendered bird mesh");knownFragments++;
            }
            for(int i=0;i<5;i++)SkyPulseBirdRig.PrepareSprite(sprite,frame.path);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)SkyPulseBirdRig.PrepareSprite(sprite,frame.path);
            Check(GC.GetAllocatedBytesForCurrentThread()==before,"Prepared shape does not allocate while poses are reused");
            UnityEngine.Object.DestroyImmediate(sprite);
        }
        Check(knownFragments==1,"Known cyan sheet-fragment regression was exercised");
        foreach(var message in ShaderUtil.GetShaderMessages(Resources.Load<Shader>("SkyPulse/Shaders/MechanicalBird")))Check(message.severity.ToString()!="Error","Bird shader compiles: "+message.message);
        Debug.Log("SKYPULSE_BIRD_SHAPE_PASS");
    }
    static bool Contains(Sprite sprite,Vector2 point)
    {
        var vertices=sprite.vertices;var triangles=sprite.triangles;
        for(int i=0;i<triangles.Length;i+=3)
        {
            Vector2 a=vertices[triangles[i]]*sprite.pixelsPerUnit+sprite.pivot,b=vertices[triangles[i+1]]*sprite.pixelsPerUnit+sprite.pivot,c=vertices[triangles[i+2]]*sprite.pixelsPerUnit+sprite.pivot;
            float ab=Cross(b-a,point-a),bc=Cross(c-b,point-b),ca=Cross(a-c,point-c);
            if((ab>=-.001f&&bc>=-.001f&&ca>=-.001f)||(ab<=.001f&&bc<=.001f&&ca<=.001f))return true;
        }
        return false;
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
}
