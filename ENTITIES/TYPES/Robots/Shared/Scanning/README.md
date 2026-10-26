# Robot scan projection — RM3

`ScanBeam.tscn` is independently reusable under any socket whose forward direction is -Z. After Ready, Configure(length, fullAngle, color, opacity, brightness, lineSpeed), then SetScanning(true/false). It has no knowledge of targets, terrain, patrol or damage.

One cached 32-segment unit cone is scaled per instance. A shared shader with one material per beam supplies translucent cyan lines and a stronger moving band using shader TIME. No raymarching, light, shadow caster, particles, layered volume or per-frame CPU uniform writes. Hidden means no beam draw and no component processing/timers. Geometry has no caps or collision; length fading softens the end. Normal-based fade reduces hard grazing edges. Ordinary spatial fog and depth testing remain enabled; shader does not create another environment.

RobotController handles head sweep and inverse art-scale compensation, keeping the configured world-space range aligned with the logical sensor. This is a cosmetic cone, not pixel-accurate terrain projection: terrain opaque depth occludes it; transparent foliage sorting remains renderer-dependent. Optional ground footprint is deferred until the cone's appearance is reviewed. Defaults are 45m length, 35° full angle, opacity 0.12, brightness 1.2.

Visual/runtime acceptance is pending: check side/front/interior views, fog day/night, foliage/depth sorting and brightness in Forward+ on the owner's Windows runtime. Only source/resource checks were possible here. Tune opacity before adding effects; GPU overdraw has not been measured.

RM5 raises defaults to opacity 0.30 / brightness 2.0 / band contrast 1.5 and keeps a minimum grazing-angle contribution so the cone does not vanish so easily. Independent F9 band contrast is a uniform configured on edits, not a per-frame CPU write. Final brightness and transparent overdraw remain unmeasured and subject to local visual acceptance.
