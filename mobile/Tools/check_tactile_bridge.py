#!/usr/bin/env python3
"""Verify tactile integration boundaries; optionally compile the iOS ARC object.
Physical haptic feel and a full Unity/Xcode device link remain device checks.
"""
import argparse,json,re,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
BASE="17a8f6de216413ee9af6f2d99a9cbd612ec067a8"
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument("--compile-native",action="store_true")
args=parser.parse_args()
def method(source):
 start=source.index("        private void PulseHaptic(float cooldown)")
 brace=source.index("{",start);depth=0
 for at in range(brace,len(source)):
  if source[at]=="{":depth+=1
  elif source[at]=="}":depth-=1
  if depth==0:return source[start:at+1]
 raise AssertionError("Unbalanced haptic helper")
path="mobile/Assets/Scripts/SkyPulseNativeGame.cs"
original=subprocess.check_output(["git","show",BASE+":"+path],cwd=ROOT,text=True)
current=(ROOT/path).read_text()
expected=method(original).replace("Handheld.Vibrate();","SkyPulseTactileFeedback.Pulse(state == FlightState.Impact);")
assert method(current)==expected,"Haptics enabled/cooldown/Editor guard or event selection changed beyond the native bridge call"
bridge=(ROOT/"mobile/Assets/Scripts/SkyPulseTactileFeedback.cs").read_text()
native=ROOT/"mobile/Assets/Plugins/iOS/SkyPulseTactileFeedback.mm"
text=native.read_text();entries=set(re.findall(r'EntryPoint="([^" ]+)"',bridge))
exports=set(re.findall(r'extern "C" void (SkyPulseTactile\w+)\(',text))
assert entries==exports=={"SkyPulseTactilePrepare","SkyPulseTactilePulse"},"Managed and native public ABI names disagree"
assert "UNITY_IOS && !UNITY_EDITOR" in bridge and "#elif !UNITY_EDITOR" in bridge,"Native calls must remain excluded from Editor and other platforms"
assert 'PlayerPrefs.GetInt("skypulse.native.haptics",1)==1' in bridge and "PlayerPrefs.Set" not in bridge,"Silent preparation must respect the existing preference and never write saves"
assert "UIApplicationStateActive" in text and "dispatch_get_main_queue()" in text and "NSThread.isMainThread" in text,"All UIKit work requires active application and main thread"
assert 'UIImpactFeedbackStyleSoft' in text and 'UIImpactFeedbackStyleRigid' in text and '@available(iOS 17.5,*)' in text,"Current view-bound APIs need the iOS15-compatible fallback"
report={"passed":True,"helperMatchesProtectedCooldownAndEnabledPolicy":True,"managedNativeAbiMatches":True,"editorAndOtherPlatformNativeExclusion":True,"prewarmDoesNotWriteSave":True,"nativeCompiled":False,"physicalDeviceVerified":False}
if args.compile_native:
 sdk=subprocess.check_output(["xcrun","--sdk","iphoneos","--show-sdk-path"],text=True).strip()
 compiler=subprocess.check_output(["xcrun","--sdk","iphoneos","--find","clang++"],text=True).strip()
 with tempfile.TemporaryDirectory(prefix="skypulse-tactile-") as temp:
  obj=Path(temp)/"tactile-arm64.o"
  subprocess.run([compiler,"-x","objective-c++","-fobjc-arc","-fblocks","-std=c++17","-target","arm64-apple-ios15.0","-isysroot",sdk,"-Wall","-Wextra","-Werror","-c",str(native),"-o",str(obj)],check=True)
  symbols=subprocess.check_output(["nm","-g",str(obj)],text=True)
  assert all(" T _"+entry in symbols for entry in entries),"Native object exports were mangled or missing"
 report.update(nativeCompiled=True,nativeTarget="arm64-apple-ios15.0",arc=True,compilerWarningsAsErrors=True)
print(json.dumps(report,indent=2))
