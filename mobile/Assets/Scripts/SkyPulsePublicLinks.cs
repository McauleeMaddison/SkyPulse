using System;
using UnityEngine;

namespace SkyPulse.Mobile
{
    [Serializable]
    public sealed class SkyPulsePublicLinks
    {
        public string privacyUrl = "";
        public string supportUrl = "";

        public static SkyPulsePublicLinks Load()
        {
            var asset = Resources.Load<TextAsset>("SkyPulsePublicLinks");
            return asset == null ? new SkyPulsePublicLinks() :
                JsonUtility.FromJson<SkyPulsePublicLinks>(asset.text) ?? new SkyPulsePublicLinks();
        }

        public static bool IsPublicHttpsUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                uri.Scheme == Uri.UriSchemeHttps && uri.HostNameType == UriHostNameType.Dns &&
                !uri.IsLoopback && uri.Host.Contains(".") && string.IsNullOrEmpty(uri.UserInfo) &&
                uri.Host != "example.com" && !uri.Host.EndsWith(".example.com") &&
                !uri.Host.EndsWith(".local") && !uri.Host.EndsWith(".invalid");
        }
    }
}
