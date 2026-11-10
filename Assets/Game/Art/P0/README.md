# Technical P0 art

Seven original geometric sprites created for the M0/M1 core greybox. No external images, fonts or samples were used. They are project source assets and may be changed and shipped with this project.

Source: `tools/p0/generate_art.py` (Python + Pillow, explicit `--overwrite` to rebuild). Human source canvas: 256×256; others: 128×128. Import scale: 128 PPU. Preserve the accompanying `.meta` files.

As of 2026-11-09, `tools/` is local-only and ignored by Git. The original generator remains on this machine but is no longer included in new checkouts; the seven PNGs and their import metadata remain versioned.

These are readability placeholders, not approved storybook art or the complete P0 delivery. The project's importer auto-trimmed these sprites; the human Visual is aligned to the collider's feet. Validate full-canvas import and fixed foot pivots before producing animation frames. Stone collision roots and sprite Visual children remain separate.
