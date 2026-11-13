using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using SkyPulse.Mobile;
public static class SkyPulseCatalogChecks
{
    const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance;
    public static void Pure()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        var errors=new List<string>();Application.LogCallback capture=(text,stack,kind)=>{if(kind==LogType.Error&&text.StartsWith("SkyPulse:"))errors.Add(text);};
        var type=typeof(SkyPulseNativeGame);var validate=type.GetMethod("ValidateTechTreeContracts",Flags);var families=(Array)type.GetField("Upgrades",Flags).GetValue(null);
        if(families.Length!=12)throw new Exception("Expected the twelve shipping Tech families");
        Application.logMessageReceived+=capture;
        try
        {
            validate.Invoke(null,null);if(errors.Count!=0)throw new Exception("Valid twelve-family catalog emits an error: "+string.Join(";",errors));
            var first=families.GetValue(0);var second=families.GetValue(1);var id=first.GetType().GetField("Id",Flags);var prices=first.GetType().GetField("LevelPrices",Flags);var prerequisite=first.GetType().GetField("PrerequisiteId",Flags);var level=first.GetType().GetField("PrerequisiteLevel",Flags);
            string original=(string)id.GetValue(first);var originalPrices=(int[])prices.GetValue(first);var originalPrerequisite=prerequisite.GetValue(first);var originalLevel=level.GetValue(first);
            try
            {
                id.SetValue(first,id.GetValue(second));errors.Clear();validate.Invoke(null,null);if(!errors.Exists(e=>e.Contains("duplicate Tech id")))throw new Exception("Duplicate IDs escaped the validator");id.SetValue(first,original);
                prices.SetValue(first,new[]{0,1,2});errors.Clear();validate.Invoke(null,null);if(!errors.Exists(e=>e.Contains("non-positive upgrade price")))throw new Exception("Invalid prices escaped the validator");prices.SetValue(first,originalPrices);
                prerequisite.SetValue(first,"missing.qa.family");level.SetValue(first,2);errors.Clear();validate.Invoke(null,null);if(!errors.Exists(e=>e.Contains("missing Tech id")))throw new Exception("Missing dependency escaped the validator");
            }
            finally{id.SetValue(first,original);prices.SetValue(first,originalPrices);prerequisite.SetValue(first,originalPrerequisite);level.SetValue(first,originalLevel);}
            errors.Clear();validate.Invoke(null,null);if(errors.Count!=0)throw new Exception("Restored catalog is not clean");
        }
        finally{Application.logMessageReceived-=capture;}
        Debug.Log("SKYPULSE_CATALOG_PASS: Twelve real families validate without errors; duplicate IDs, invalid costs and missing dependencies still fail negative controls.");
    }
}
