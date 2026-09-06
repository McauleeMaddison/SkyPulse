"""Check registration coverage and detect source artwork changes requiring recalibration."""
import hashlib
import json
import math
from pathlib import Path
import re
import struct

mobile = Path(__file__).resolve().parents[1]
resources = mobile / "Assets/Resources"
source = (mobile / "Assets/Scripts/SkyPulseNativeGame.cs").read_text()
expected = set(re.findall(r'"(SkyPulse/characters/roster/[^"\n]+-frame-0[1-6](?:-v1)?)"', source))
frames = json.loads((resources / "SkyPulse/characters/bird-frame-registration.json").read_text())["frames"]
actual = {frame["path"] for frame in frames}
assert len(frames) == len(actual) == 90, "Expected 90 distinct flight frame registrations"
assert actual == expected, f"Registration mismatch: {actual ^ expected}"

for frame in frames:
    path = resources / (frame["path"] + ".png")
    data = path.read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", path
    assert struct.unpack(">II", data[16:24]) == (frame["sourceWidth"], frame["sourceHeight"]), path
    assert hashlib.sha256(data).hexdigest() == frame["sourceSha256"], f"Recalibrate changed artwork: {path}"
    assert re.search(r"^  nPOTScale: 0$", Path(str(path) + ".meta").read_text(), re.M), f"Importer stretches the canvas: {path}"
    assert all(math.isfinite(frame[key]) for key in ("pivotX", "pivotY", "scale")), path
    assert 0 <= frame["pivotX"] <= 1 and 0 <= frame["pivotY"] <= 1, path
    assert .75 <= frame["scale"] <= 1.25, f"Unexpected body scale: {path}"

print("PASS: all 90 flight frames across 15 birds have valid registration and unchanged source artwork.")
