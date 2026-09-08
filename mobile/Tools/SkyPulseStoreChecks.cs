using System;
using SkyPulse.Mobile;
using SkyPulse.Mobile.Editor;
using UnityEditor.Build;
using UnityEngine;

// Copy into the isolated QA project's Assets/Editor alongside the smoke harness.
public static class SkyPulseStoreChecks
{
    public static void Run()
    {
        foreach (var url in new[] { "", "http://skypulse.invalid/privacy", "javascript:alert(1)",
            "https://localhost/privacy", "https://127.0.0.1/privacy", "https://example.com/privacy",
            "https://user:password@site.org/privacy" })
            if (SkyPulsePublicLinks.IsPublicHttpsUrl(url)) throw new Exception("Unsafe store URL accepted: " + url);
        if (!SkyPulsePublicLinks.IsPublicHttpsUrl("https://developer.apple.com/privacy/"))
            throw new Exception("Public HTTPS URL rejected");
        var links = SkyPulsePublicLinks.Load();
        if (string.IsNullOrEmpty(links.privacyUrl) || string.IsNullOrEmpty(links.supportUrl))
        {
            var rejected = false;
            try { SkyPulseReleaseBuild.ValidateAppStore(); }
            catch (BuildFailedException) { rejected = true; }
            if (!rejected) throw new Exception("Incomplete App Store candidate was accepted");
        }
        Debug.Log("SKYPULSE_STORE_CHECKS_PASS");
    }
}
