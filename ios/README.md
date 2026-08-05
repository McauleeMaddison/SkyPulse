# iPhone packaging handoff

SkyPulse’s native mobile route now uses Unity 6 + C# in `../mobile/`. The older Kivy game is a frozen desktop reference, not the release path.

Packaging for iPhone needs Unity’s iOS export support and a Mac with a full, iPhone-compatible Xcode app. This Mac has Xcode 15.2 installed, but an iPhone running iOS 27 needs a much newer Xcode/macOS combination.

## Ready-to-use release art

- `../assets/images/branding/skypulse-app-icon.png` — 1254 × 1254 icon source
- `../assets/images/branding/skypulse-launch-art.png` — 941 × 1672 portrait launch artwork

Add them to the target's Asset Catalog as the app icon and launch-screen artwork. Keep the launch artwork behind the native `SKYPULSE` word mark so no title is baked into the image.

## Pre-TestFlight checklist

1. Activate Unity Personal and install Unity’s iOS export module.
2. Build the `mobile/` Unity project on an Xcode-equipped Mac that supports the target iPhone’s iOS version.
3. Test on a small iPhone and a notched iPhone: pause, home-indicator clearance, all shop cards, tutorial, daily rewards, mute, and haptics.
4. Play a ten-minute session on device and inspect frame stability, heat, and battery use.
5. Archive, upload to TestFlight, then run a final real-device pass from the TestFlight build.
