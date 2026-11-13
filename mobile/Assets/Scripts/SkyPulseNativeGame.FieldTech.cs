using UnityEngine;
namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        int gateLinkRemaining,gateLinkReward,gateLinkLastScore=-1,runFieldPickupBonus;
        Sprite supplyCacheSprite,gateLinkSprite;
        string gateLinkHudLabel="LINK";
        float PickupDuration(float baseSeconds)=>baseSeconds+(activeMode==SkyPulseMode.EndlessRoute?GetUpgradeLevel("pulse_matrix"):0);
        int SupplyCacheValue()=>5+2*GetUpgradeLevel("supply_decoder");
        int GateLinkValue()=>2+GetUpgradeLevel("link_reactor");
        void AwardFieldPickup(int amount)
        {
            if(amount<=0||activeMode!=SkyPulseMode.EndlessRoute||state!=FlightState.Playing)return;
            // Count the bounded pickup award separately: result percentage bonuses
            // never compound it. The existing wallet journal persists this credit.
            runFieldPickupBonus+=amount;BankCollectedCrystals(amount);
        }
        void AwardSupplyCache()
        {
            int value=SupplyCacheValue();AwardFieldPickup(value);
            ShowEndlessPickupBanner("SUPPLY CACHE · +"+value+" ✦",Hex("#ffd166"));
        }
        void BeginGateLink()
        {
            gateLinkRemaining=3;gateLinkReward=GateLinkValue();gateLinkLastScore=score;
            gateLinkHudLabel="LINK +"+gateLinkReward;
            ShowEndlessPickupBanner("GATE LINK · NEXT 3 CLEAN GATES",Hex("#b8a0ff"));
        }
        void AwardGateLinkPass(int gateScore)
        {
            if(activeMode!=SkyPulseMode.EndlessRoute||state!=FlightState.Playing||gateLinkRemaining<=0||gateScore<=gateLinkLastScore)return;
            gateLinkLastScore=gateScore;gateLinkRemaining--;
            AwardFieldPickup(gateLinkReward);UpdatePowerUpHud();
            ShowEndlessPickupBanner("GATE LINK · +"+gateLinkReward+" ✦",Hex("#b8a0ff"));
        }
        static Sprite CreateFieldPickupSprite(bool link)
        {
            const int n=192;var t=new Texture2D(n,n,TextureFormat.RGBA32,false){name=link?"Gate Link guide emblem":"Supply Cache guide emblem",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                var v=new Vector2((x+.5f)/n-.5f,(y+.5f)/n-.5f);float a=0;
                if(link){for(int i=0;i<3;i++)a=Mathf.Max(a,1-PickupEdge(.023f,.035f,Mathf.Abs((v-new Vector2((i-1)*.23f,i==1?-.04f:.04f)).magnitude-.16f)));}
                else {float hex=Mathf.Max(Mathf.Abs(v.y),Mathf.Abs(v.x)*.866f+Mathf.Abs(v.y)*.5f);a=(1-PickupEdge(.33f,.35f,hex))*PickupEdge(.25f,.27f,hex);a=Mathf.Max(a,(1-PickupEdge(.06f,.08f,Mathf.Abs(v.x)))*(1-PickupEdge(.19f,.21f,Mathf.Abs(v.y))));}
                pixels[y*n+x]=new Color(link?.69f:1,link?.64f:.83f,link?1:.42f,a);
            }
            t.SetPixels(pixels);t.Apply(false,true);return CreateSprite(t,n);
        }
    }
}
