Bird frame registration
=======================

`Assets/Resources/SkyPulse/characters/bird-frame-registration.json` stores each
flight drawing's pivot and relative pixel density. Measurements align the rigid
face against that bird's frame 04, rather than aligning the changing wing bounds.
They were estimated by face-patch correlation with scale search, then checked on
registered contact sheets for all 15 birds. Different drawn poses can still change
the body's silhouette; registration corrects canvas position and scale.

`scale` is source face size / reference face size. Multiplying sprite pixels per
unit by this value corrects the drawing without stretching its aspect ratio.
Pivots use Unity's bottom-left normalized coordinates. The gameplay transform uses
one scale from the unregistered reference canvas. Menu images explicitly apply the
same pixel density and pivot because UI Image fitting does not honor sprite PPU.
Source PNGs are unchanged. The source dimensions and SHA-256 identify the exact
artwork measured; replacing a drawing requires updating its registration.
Character importers disable power-of-two resizing so rectangular source canvases
retain their aspect ratio. The mobile art optimisation command preserves this.

Original roster poses play in order 01,02,03,04,05,06 and back. The newbird sheets
contain an additional raised pose at 06, so those play 01,06,02,03,04,05 and back.
Taps continue from the current pose. Frame timing keeps fractional elapsed time
but discards a whole-frame backlog after a hitch, preserving adjacent poses.

Run `python3 mobile/Tools/verify_bird_registration.py` from the repository root.
For visual verification in Unity, inspect each bird in the menu and during single
and repeated taps; watch the face/body rather than wing tips. Check both the
raised-to-lowered sequence and its return at 30 and 60 fps. This does not change
flight physics or collision geometry.

Hit and unlock presentation bounds
----------------------------------

`bird-pose-bounds.json` records the nontransparent extents of all 30 dedicated
hit/unlock sprites, including faint effects and a two-pixel margin. Runtime Sprite
rectangles use those measurements so empty source-canvas padding does not shrink
or offset the artwork. Original PNGs remain unchanged. Regenerate the metadata
with `measure_pose_bounds.py` (Pillow) when any pose image changes. Alpha-bounds
fitting does not reconstruct feather tips already clipped in the source drawing.
