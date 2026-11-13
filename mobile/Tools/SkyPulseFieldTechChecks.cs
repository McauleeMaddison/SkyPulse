using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;
public static class SkyPulseFieldTechChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static readonly List<string> checks=new List<string>();
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static int I(object o,string n)=>(int)Get(o,n);
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);checks.Add(s);Debug.Log("FIELD_TECH_CHECK: "+s);}
    static void Fresh(SkyPulseNativeGame g)=>typeof(SkyPulsePickupChecks).GetMethod("Fresh",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{g});
    static object Collect(SkyPulseNativeGame g,string kind)
    {
        var p=((Array)Get(g,"powerUpPool")).GetValue(0);var gate=((Array)Get(g,"pipePool")).GetValue(0);
        Call(g,"ConfigurePowerUp",p,gate);Set(p,"Kind",Enum.Parse(p.GetType().GetField("Kind",F).FieldType,kind));Call(g,"CollectPowerUp",p);return p;
    }
    public static IEnumerator Live(SkyPulseNativeGame g,Camera camera,RenderTexture target)
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        var folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"field-tech");Directory.CreateDirectory(folder);
        var levels=(IDictionary)Get(g,"upgradeLevels");
        for(int rank=0;rank<=3;rank++)
        {
            Fresh(g);levels["pulse_matrix"]=rank;levels["supply_decoder"]=rank;levels["link_reactor"]=rank;
            int wallet=I(g,"crystals");var p=Collect(g,"SupplyCache");int reward=5+rank*2;
            Check(I(g,"crystals")==wallet+reward&&I(g,"runFieldPickupBonus")==reward,"Supply Cache banks its exact rank "+rank+" value once");
            Call(g,"CollectPowerUp",p);Check(I(g,"crystals")==wallet+reward,"Consumed Supply Cache cannot pay twice: "+rank);
            foreach(var power in new[]{new{kind="TimePulse",timer="slowFieldTimer",seconds=4f},new{kind="CrystalMagnet",timer="magnetHaloTimer",seconds=6f},new{kind="CrystalEcho",timer="crystalEchoTimer",seconds=6f},new{kind="RouteLens",timer="routeLensTimer",seconds=8f}})
            {Collect(g,power.kind);Check(Mathf.Abs((float)Get(g,power.timer)-power.seconds-rank)<.001f,"Pulse Matrix extends "+power.kind+" exactly at rank "+rank);}
            Collect(g,"GateLink");Check(I(g,"gateLinkRemaining")==3&&I(g,"gateLinkReward")==2+rank,"Gate Link snapshots three bounded clean-gate rewards at rank "+rank);
            int beforeLink=I(g,"crystals");
            for(int pass=1;pass<=4;pass++)
            {Set(g,"score",pass);Call(g,"AwardGateLinkPass",pass);int paid=I(g,"crystals");Call(g,"AwardGateLinkPass",pass);Check(I(g,"crystals")==paid&&paid==beforeLink+Math.Min(pass,3)*(2+rank),"Gate callbacks pay once and stop at three: rank "+rank+" / "+pass);}
            Check(I(g,"runFieldPickupBonus")==reward+3*(2+rank),"Bounded field-reward total is exact at rank "+rank);
            Set(g,"score",0);Set(g,"perfectPasses",0);Set(g,"newBest",false);Set(g,"resultCrystalBonusApplied",false);
            levels.Clear();levels["salvage_codec"]=3;levels["apex_matrix"]=3;
            Check((int)Call(g,"ApplyResultTechBonuses")==0,"Percentage rewards cannot compound Supply Cache or Gate Link at rank "+rank);
            Call(g,"RestartFlight");Check(I(g,"gateLinkRemaining")==0&&I(g,"runFieldPickupBonus")==0,"Retry clears the link and run-only field rewards: "+rank);
        }
        Fresh(g);Collect(g,"GateLink");int before=I(g,"crystals");Call(g,"PauseFlight");Call(g,"AwardGateLinkPass",1);var paused=Collect(g,"SupplyCache");
        Check(I(g,"crystals")==before&&I(g,"gateLinkRemaining")==3&&(bool)Get(paused,"Active"),"Pause cannot consume a pickup or a gate-link step");Call(g,"ResumeFlight");
        for(int pass=0;pass<3;pass++)
        {
            var pipes=(Array)Get(g,"pipePool");foreach(var item in pipes)((GameObject)Get(item,"Root")).SetActive(false);
            var pipe=pipes.GetValue(0);((GameObject)Get(pipe,"Root")).SetActive(true);Set(pipe,"X",-4.6f);Set(pipe,"Passed",false);
            ((GameObject)Get(pipe,"Root")).transform.localPosition=new Vector3(-4.6f,0,0);
            Call(g,"UpdatePipes",0f);int paid=I(g,"crystals");Call(g,"UpdatePipes",0f);
            Check(I(g,"score")==pass+1&&paid==before+2*(pass+1)&&I(g,"crystals")==paid,"Actual pipe passage consumes exactly one link step without changing score rules: "+pass);
        }
        foreach(int fps in new[]{30,60,120})
        {
            Fresh(g);levels["pulse_matrix"]=3;Collect(g,"TimePulse");for(int f=0;f<fps*2;f++)Call(g,"UpdatePowerUpEffects",1f/fps);
            Check(Mathf.Abs((float)Get(g,"slowFieldTimer")-5)<.002f,"Extended pickup countdown agrees at "+fps+" Hz");
        }
        Fresh(g);Call(g,"OpenUpgrades");yield return null;
        var tech=(Array)typeof(SkyPulseNativeGame).GetField("Upgrades",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        Check(tech.Length==12&&tech.Cast<object>().Sum(x=>(int)x.GetType().GetProperty("MaxLevel").GetValue(x))==36,"Three new families bring the tree to 36 permanent levels");
        var content=(RectTransform)Get(g,"customizeContent");Check(content.rect.height==4500&&content.GetComponentsInChildren<Text>().Count(x=>x.name.EndsWith(" identity"))==36,"The free-pan tree contains all new labelled fourth tiers");
        yield return SkyPulseTechTreeChecks.Live(g);
        Fresh(g);Call(g,"ResetToMenu");Call(g,"OpenUpgrades");
        foreach(string id in new[]{"pulse_matrix","supply_decoder","link_reactor"})
        {
            var row=tech.Cast<object>().Single(x=>(string)Get(x,"Id")==id);levels.Remove(id);levels[(string)Get(row,"PrerequisiteId")]=2;
            Call(Get(g,"missionStore"),"SetInt",SkyPulseDailyMissions.WalletKey,10000);Set(g,"crystals",10000);
            int quote=(int)Call(g,"UpgradeCrystalPrice",row,0);Call(g,"InspectTechLevel",row,0);Call(g,"ConfirmPurchase");
            Check((int)Call(g,"GetUpgradeLevel",id)==1&&I(g,"crystals")==10000-quote,"Actual purchase charges the quoted Core-adjusted price and enables "+id);
            Call(g,"ConfirmPurchase");Check(I(g,"crystals")==10000-quote,"Repeated purchase callback cannot spend again: "+id);
            Call(g,"SaveProgress");levels.Remove(id);Call(g,"LoadProgress");
            Check((int)Call(g,"GetUpgradeLevel",id)==1,"The new stable upgrade key survives save/load: "+id);
        }
        Fresh(g);Collect(g,"GateLink");for(int i=0;i<20;i++)Call(g,"UpdatePowerUpHud");
        // Reflection itself allocates, so call the private handler through one
        // cached delegate to measure the warmed implementation, not this harness.
        var hud=(Action)Delegate.CreateDelegate(typeof(Action),g,g.GetType().GetMethod("UpdatePowerUpHud",F));
        long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++)hud();
        Check(GC.GetAllocatedBytesForCurrentThread()==allocated,"Warmed Gate Link HUD updates allocate no managed bytes");
        var display=((GameObject)Get(((Array)Get(g,"powerUpPool")).GetValue(0),"Root")).GetComponentInChildren<SkyPulsePickupDisplay>(true);
        var enumType=typeof(SkyPulseNativeGame).GetNestedType("PowerUpKind",BindingFlags.NonPublic);
        foreach(var value in Enum.GetValues(enumType))Check(display.Model((int)value).name.Split('·')[0].Replace(" ","")==value.ToString(),"Pickup sculpture matches its actual gameplay kind: "+value);
        Directory.CreateDirectory(Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"pickups"));
        yield return (IEnumerator)typeof(SkyPulsePickupChecks).GetMethod("Guide",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{g});
        // Retarget only instantiated Hangar rigs. Source prefab bones and the
        // accepted-tap pilot remain untouched; compare all four moving joints.
        var reference=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("SkyPulse/Birds3D/newbird03"));var v=reference.GetComponent<SkyPulseNeonFinchPrototype>();v.animate=v.turntable=false;SkyPulseSolidBirdStudio.MatchVoltcrestDisplayWings(v);
        try
        {
            var manifest=JsonUtility.FromJson<SkyPulseBirdRosterRuntimeBaker.Manifest>(Resources.Load<TextAsset>("SkyPulse/Birds3D/BakeManifest").text);
            foreach(var row in manifest.birds)
            {
                var prefab=Resources.Load<GameObject>("SkyPulse/Birds3D/"+row.id);var source=prefab.GetComponent<SkyPulseNeonFinchPrototype>();var original=source.leftWingTip.localRotation;
                var model=UnityEngine.Object.Instantiate(prefab);var rig=model.GetComponent<SkyPulseNeonFinchPrototype>();rig.animate=rig.turntable=false;rig.ResetPose();var shoulder=rig.leftWing.localPosition;
                try
                {
                    SkyPulseSolidBirdStudio.MatchVoltcrestDisplayWings(rig);
                    foreach(float time in new[]{0f,.18f,.41f,.82f,1.24f,1.65f})
                    {
                        v.SamplePose(time);rig.SamplePose(time);
                        Check(new[]{Quaternion.Angle(v.leftWing.localRotation,rig.leftWing.localRotation),Quaternion.Angle(v.rightWing.localRotation,rig.rightWing.localRotation),Quaternion.Angle(v.leftWingTip.localRotation,rig.leftWingTip.localRotation),Quaternion.Angle(v.rightWingTip.localRotation,rig.rightWingTip.localRotation)}.Max()<.1f,"Hangar wings match Voltcrest's physical pose: "+row.id+" / "+time);
                    }
                    rig.reducedMotion=true;rig.SamplePose(.2f);var held=rig.leftWing.localRotation;rig.SamplePose(.9f);
                    Check(held==rig.leftWing.localRotation&&rig.leftWing.localPosition==shoulder&&source.leftWingTip.localRotation==original,"Reduced Motion and source/shoulder preservation: "+row.id);
                }
                finally{UnityEngine.Object.Destroy(model);}
            }
        }
        finally{UnityEngine.Object.Destroy(reference);}
        var clearance=SkyPulseWingClearanceChecks.InspectAll(0,false,true,true);Check(clearance.birds.All(x=>x.contactPoses==0),"All shared Hangar wing profiles pass the sampled wing/foot audit");
        Fresh(g);File.WriteAllText(Path.Combine(folder,"VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",deviceTested=false,checks=checks.ToArray()},true));Debug.Log("SKYPULSE_FIELD_TECH_PASS");
    }
    [Serializable]sealed class Report{public string result;public bool deviceTested;public string[] checks;}
}
