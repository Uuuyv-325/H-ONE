# Remaining supplied artwork — delivery02-20261110

Read-only source: sibling `H-ONE-arts`. `SOURCE_USAGE.md` snapshots its current
README. `manifest.json` records each source SHA-256, exact crop, resizing,
canvas, pivot, PPU and padded atlas regions. Only local Pillow image/layout
adaptation was used. No AI image generation, repainting or training was used.
Layered drawing sources and an independent attribution/license manifest remain
undelivered. The local recipe is `tools/art/prepare_delivery02.py`; tools remain
ignored by Git. Existing Delivery01 and P0 exports/GUIDs are retained.

- `jump.png` → `human_jump.png`: one airborne pose, including descent, returning
  to idle/walk on actual grounding. 1024×832, 512 PPU, foot pivot (512,32).
  No new jump physics or separate falling animation is implied.
- `IMG_6092.PNG` → `lyre.png`: Lyre prefab, real shared-budget star reflection,
  latched receiver driving the gallery gate. The effective string plane is a
  separate thin collider, not the decorative frame.
- `IMG_6089.PNG` → `drum_idle.png`; `IMG_6090.PNG` → `drum_hit_00–09.png`:
  512×1024, 512 PPU, centre pivot, 20 fps one-shot on actual impact. The supplied
  animation ends after 0.5 s; damage/activation never waits for it. Drum wave
  targets use explicit references and a 3-unit trial radius.
- `IMG_6076/6078/6077.PNG` → `terrain_middle/left/right.png`: 1-unit grid art
  inside authored Solid rectangles. Separate `terrain_soil.png` is cropped
  from the supplied square for deeper rows. Mesh UV clipping preserves texture
  scale for fractional edges and thin platforms; existing box physics stays
  authoritative. Rounded/rough art edges still require final art acceptance.
- `IMG_6075.PNG` → `terrain_brush.png`: circle brush preview in the existing
  Chapter Level Editor. The brush selects whole grid cells and skips existing
  terrain; it does not make a circular world collider or a new movement rule.
  The resulting terrain uses the three square drawings.
- `terrain_atlas.png`: padded 2048×1024 layout of those supplied exports for
  one terrain mesh/material per rectangle, without mipmaps. UV rectangles are
  explicit configuration in ChapterArt; no Multiple-mode slice IDs are changed.

`ChapterArt.asset` in Delivery01 explicitly references these new exports and
the terrain material. `Solid`, `Lyre`, `Drum`, `BrittleWall` prefabs, the factory,
runtime catalog and scene authoring are updated together. The wall, ordinary
gate, drum wave, aim preview and teaching remain partly technical placeholders;
no missing final art or instrument audio is claimed delivered.

`IMG_6091.PNG` is explicitly deferred by the user on 2026-11-10; do not infer a
mechanism from its appearance. `walking.gif` and `turn_star.gif` remain alternate
references for the PNG sequences already integrated in Delivery01.

Integration and runtime evidence: `docs/11-剩余美术与格点地形接入.md` and the
existing placeholder checklist. This is not acceptance of the complete P0/P1/P2
art batches or completion of either authored Chapter 1 level.

2026-11-11 attribution update: the supplied artwork is by **不眠巷**, as
identified by the user. Credits are also recorded in the main-menu Credits and
root CREDITS.md; source hashes, usage policy, crops and remaining deliveries above
are unchanged.
