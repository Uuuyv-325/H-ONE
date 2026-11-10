# Delivered art, integration sample 01

Prepared on 2026-11-09 from the read-only sibling `H-ONE-arts` directory.
The source README now allows read-only visual inspection, cropping, layout
adaptation, project copies and renaming. AI image generation based on these
images and use as a training dataset remain prohibited. `SOURCE_USAGE.md`
records the supplied permission/description at preparation time. No image
synthesis, repainting or training process was used.

The entire `tools/` directory is local-only and ignored by Git. The preparation
scripts named below remain on this machine and are not included in a fresh
checkout; the exported art, import metadata and manifest remain versioned.

`tools/art/prepare_delivery.py` prepares the 38 PNG exports using Pillow and
NumPy, recording source SHA-256, exact source-pixel crop rectangles, paste
offsets, output canvases, pivots and PPU in `manifest.json`. The source images
remain untouched. To repeat locally, run it from the WSL project with the
sibling source directory available; `--source /path/to/H-ONE-arts` overrides it.
The derived exports may be regenerated in place; preserve their Unity `.meta`
files. Do not write generated files back into the source directory.

`tools/art/configure_delivery.cs` is a standalone Editor `script eval` block
for initial TextureImporter settings and the `ChapterArt.asset` references.
Run it in the intended live Editor after the files import. It only configures
these new, individually cropped Single-mode sprites; no existing P0 texture,
Multiple-mode slice, scene GUID or gameplay collider is rewritten.

- Idle/walk: four frames each, 1024x832 canvas, common foot anchor at (512,800)
  in top-left coordinates; 512 PPU, normalized Unity pivot (0.5,32/832).
  These retain the supplied drawing's resolution, rather than resampling to
  the earlier proposed 256x256 delivery size. This is a trial integration
  setting, not a global art-specification change.
- Launch: ten 832x832 centered frames, 512 PPU; visuals overlap the first
  0.18 seconds of actual star flight. Gameplay does not wait for animation.
- Star: four 704x704 centered frames, 512 PPU. Fragment: four 256x256 centered
  frames, 256 PPU. Shared canvases keep scale stable while shapes change.
- Plate up/down: same 544x128 canvas and 384 PPU; the source's lower pressed
  top stays lower in the same canvas. PressurePlate remains the authority
  for occupancy; PressurePlateArtView only chooses the drawing.
- Checkpoint: cropped original standing stone, behind the player. No activated
  drawing was supplied; this is not presented as a complete checkpoint state set.
- RockDark remains the Stone prefab drawing. As of `socket-art-20261110`,
  PlatformSocket and PlateSocket use RockDark when empty and RockLight while
  actually occupied, including the full one-second recall. StarSocketArtView
  reads the real ShadowService state; the old socket tint no longer recolors
  these drawings. The centered 995x1024, 512 PPU sprites keep their GUIDs and
  pivots; socket visuals are 0.7 units tall. Shadow support colliders and
  gameplay roots stay unchanged. The shadow core and supporting edge remain
  separate layers. Title is used by MainMenu (2026-11-10).
- Background/grass and pause drawings retain 16:9 layouts at 1920x1080.
  Pause variants map 0=IMG_6055, 1=IMG_6054, 2=IMG_6053, 3=main-menu.
  The illustrated icons remain part of the supplied illustration; functional
  existing menu buttons stay separately labelled and bound on the right.

The source is the user's delivered raster artwork; no layered source project,
separate author/third-party attribution manifest or completed P0 approval was
supplied. The repository manifest records provenance and the provided usage
permission, without claiming authorship or independent rights verification.
See `docs/06-占位美术替换Checklist.md` and `docs/09-美术试装记录.md` for actual
integration scope, remaining states and runtime validation. P0 fallback assets
continue serving unsupported states/objects; this is not a completed chapter.

2026-11-11 attribution update: the supplied artwork is by **不眠巷**, as
identified by the user. Credits are also recorded in the main-menu Credits and
root CREDITS.md; source hashes, usage policy, crops and remaining deliveries above
are unchanged.
