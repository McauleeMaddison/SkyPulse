# iPhone packaging handoff

SkyPulse uses Kivy. Packaging for iPhone needs a Mac with the full Xcode app installed; this workspace currently has only Apple Command Line Tools.

## Ready-to-use release art

- `../assets/images/branding/skypulse-app-icon.png` — 1254 × 1254 icon source
- `../assets/images/branding/skypulse-launch-art.png` — 941 × 1672 portrait launch artwork

Add them to the target's Asset Catalog as the app icon and launch-screen artwork. Keep the launch artwork behind the native `SKYPULSE` word mark so no title is baked into the image.

## Pre-TestFlight checklist

1. Build with the current Kivy iOS toolchain on an Xcode-equipped Mac.
2. Test on a small iPhone and a notched iPhone: pause, home-indicator clearance, all shop cards, tutorial, daily rewards, mute, and haptics.
3. Play a ten-minute session on device and inspect frame stability, heat, and battery use.
4. Archive, upload to TestFlight, then run a final real-device pass from the TestFlight build.
