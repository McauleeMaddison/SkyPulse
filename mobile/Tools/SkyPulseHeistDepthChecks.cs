using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SkyPulse.Mobile;

// Disposable Editor QA: checks presentation without changing puzzle rules.
public static class SkyPulseHeistDepthChecks
{
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    static object Get(object source,string name)=>source.GetType().GetField(name,F).GetValue(source);
    static object Call(object source,string name,params object[] args)=>source.GetType().GetMethod(name,F).Invoke(source,args);
    static void Check(bool ok,string note){if(!ok)throw new Exception(note);Debug.Log("HEIST_DEPTH_CHECK: "+note);}
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        Call(game,"ResetToMenu");var campaign=typeof(SkyPulseBlackoutChecks).GetMethod("CaptureCampaign",F).Invoke(null,new object[]{7});game.GetType().GetField("blackoutCampaign",F).SetValue(game,campaign);
        for(int vault=0;vault<8;vault++)
        {
            Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,vault);Call(game,"RestartBlackoutVault");
            var board=(SkyPulseHeistBoard)Get(game,"blackoutBoard");var console=(Transform)Get(board,"console");var camera=(Camera)Get(board,"opticalCamera");
            Check(Mathf.Abs(Mathf.DeltaAngle(console.localEulerAngles.x,28))<.01f,"Fixed 28-degree view reveals actual optic depth: vault "+vault);
            var platen=(MeshRenderer)Get(board,"platenRenderer");Check(platen.GetComponent<MeshFilter>().sharedMesh.bounds.size.z>.47f,"Vault has a solid raised chassis: "+vault);
            var bounds=platen.GetComponent<MeshFilter>().sharedMesh.bounds;
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                var point=platen.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));var view=camera.WorldToViewportPoint(point);
                Check(view.x>=0&&view.x<=1&&view.y>=0&&view.y<=1,"Chassis depth stays inside the board camera: "+vault);
            }
            var session=(BlackoutSession)Get(game,"blackoutSession");var modules=(Array)Get(board,"modules");
            if(vault==2)Check(((RectInt)Get(board,"layout")).height==3,"Unused padded rows do not leave an empty half-board below Split Signal");
            Check(((Transform)Get(modules.GetValue(session.Puzzle.Start),"Emitter")).gameObject.activeSelf,"Source has a directional physical emitter: "+vault);
            foreach(int tile in Enumerable.Range(0,session.Puzzle.Count).Where(t=>session.Puzzle.Tile(t)=='E'))Check(((Transform)Get(modules.GetValue(tile),"Receiver")).gameObject.activeSelf,"Uplink has a distinct receiver crown: "+vault);
            var targetPositions=(Vector2[])Get(game,"blackoutPositions");
            foreach(int tile in Enumerable.Range(0,session.Puzzle.Count).Where(session.Puzzle.Rotatable))
            {
                var module=(Transform)Get(modules.GetValue(tile),"Root");var screen=camera.WorldToViewportPoint(module.position);var centre=new Vector2((screen.x-.5f)*1080,(screen.y-.5f)*1080);
                Check(Vector2.Distance(centre,targetPositions[tile])<12,"Touch stays centred on the raised optic: "+vault+" / "+tile);
            }
        }
        var liveBoard=(SkyPulseHeistBoard)Get(game,"blackoutBoard");float? result=null;
        foreach(int fps in new[]{30,60,120})
        {
            var fixture=new BlackoutSession(SkyPulseBlackoutCampaign.Stages[0]);var points=new Vector2[49];liveBoard.ReducedMotion=false;liveBoard.MotionPaused=false;liveBoard.Bind(fixture,points);int tile=SkyPulseMissionChecks.Solve(fixture.Puzzle)[0];fixture.Rotate(tile);
            for(int i=0;i<fps;i++)liveBoard.Tick(1f/fps);float angle=liveBoard.VisualAngle(tile);if(result.HasValue)Check(Mathf.Abs(Mathf.DeltaAngle(angle,result.Value))<.01f,"Rotor easing agrees at "+fps+" FPS");result=angle;
        }
        for(int i=0;i<10;i++)liveBoard.Tick(1f/60);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)liveBoard.Tick(1f/60);Check(GC.GetAllocatedBytesForCurrentThread()==before,"Warmed raised-board updates allocate no managed bytes");
        Call(game,"ResetToMenu");Call(game,"InitializeMissionServices");yield return null;Debug.Log("SKYPULSE_HEIST_DEPTH_PASS");
    }
}
