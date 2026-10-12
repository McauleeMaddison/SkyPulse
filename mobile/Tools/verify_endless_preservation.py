#!/usr/bin/env python3
"""Verify the changed candidate against the retained build-7 source (local, read-only)."""
import hashlib,json,re,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
SOURCE='mobile/Assets/Scripts/SkyPulseNativeGame.cs'
BASELINE='026e382'
old=subprocess.check_output(['git','show',BASELINE+':'+SOURCE],cwd=ROOT,text=True)
new=(ROOT/SOURCE).read_text()
def bodies(source,name):
    result=[]
    pattern=r'^        private (?:static )?[^\n=]+\b'+re.escape(name)+r'\([^\n]*\)\s*\n        \{'
    for match in re.finditer(pattern,source,re.M):
        start=source.index('{',match.start());depth=0
        for index in range(start,len(source)):
            if source[index]=='{':depth+=1
            elif source[index]=='}':depth-=1
            if depth==0:
                result.append(source[match.start():index+1]);break
    assert result,'Missing method: '+name
    return result
methods=['SimulateFlight','BufferFlapInput','ConsumeBufferedFlap','UpdateBird','UpdateGameplayWingState','UpdateBirdWingMotion','Flap','BirdCollidesWithPipe','CollidersOverlap','ConfigurePipe','UpdateRouteGateMotion','UpdateWorldTransition','BeginWorldTransition','RouteSpeedFraction','RouteGapFraction','UseShield','UpdatePowerUpEffects','CollectPowerUp','CrystalPickupValue','LoadProgress','BeginFlight','CreateCamera','CreateVisuals','GetWorldWidth','GetViewportWidth','RoutePipeSpacing','ActiveScrollSpeed']
checks={}
for name in methods:
    a,b=bodies(old,name),bodies(new,name)
    assert a==b,'Protected Endless method changed: '+name
    checks[name]=hashlib.sha256('\n'.join(b).encode()).hexdigest()
# These original methods gain only dispatch/progress/explicit-source hooks.
hooks={
'SaveProgress':[('            if (!RecoverWalletTransaction()) return;\n','')],
'ConfirmPurchase':[('            if (!RecoverWalletTransaction()) return;\n','')],
'UpdatePipes':[('                    RecordDailyEvent(DailyGameplayEvent.EndlessGate);\n','')],
'CollectCrystalPickup':[('            RecordDailyEvent(DailyGameplayEvent.CrystalPickup, crystalValue);\n','')],
'EndFlight':[('            if (activeMode == SkyPulseMode.PulseRelay) { FinishRelayFailure(lastCrashReason); return; }\n','')],
'BankCollectedCrystals':[('            CreditWallet(amount, WalletRewardSource.FlightPickup);','            crystals += amount;\n            runCrystalsCollected += amount;')],
'ApplyResultTechBonuses':[('                CreditWallet(runCrystalBonus, WalletRewardSource.FlightTechBonus);','                crystals += runCrystalBonus;')]
}
for name,replacements in hooks.items():
    normalized=bodies(new,name)[0]
    for a,b in replacements:normalized=normalized.replace(a,b)
    assert normalized==bodies(old,name)[0],'Non-hook Endless change: '+name
constants=lambda s: re.findall(r'^        private const [^\n]+',s,re.M)
assert constants(old)==constants(new),'Endless constants changed'
def catalogue(source,name):
    start=source.index(' '+name+' =')
    begin=source.index('{',start);depth=0
    for index in range(begin,len(source)):
        if source[index]=='{':depth+=1
        elif source[index]=='}':depth-=1
        if depth==0:return source[begin:index+1]
    raise AssertionError(name)
for name in ['Skins','Worlds','Upgrades']:
    assert catalogue(old,name)==catalogue(new,name),'Stable catalogue changed: '+name
print(json.dumps({'baseline':BASELINE,'protectedMethodCount':len(methods),'exactBodyHashes':checks,'integrationHookMethods':list(hooks),'allEndlessConstantsUnchanged':True},indent=2))
