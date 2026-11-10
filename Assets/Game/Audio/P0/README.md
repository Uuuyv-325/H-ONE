# Technical P0 sound

Seven original synthesized cues, created without third-party recordings or libraries. Project source assets; they may be changed and shipped with this project. Generator: `tools/p0/generate_audio.py` (Python standard library, deterministic seed).

All WAVs are PCM, 48 kHz, 24 bit, mono. `p0_charge_loop.wav` is a one-second periodic loop; its integer-frequency oscillators complete whole cycles at the boundary. Other files are short decaying cues for charge start, launch, wall, failure, reform and collection.

`CoreAudioFeedback` receives completed gameplay events and routes through `Core.mixer/SFX`. Charge loops stop on state exit/cancel/focus loss, gameplay sources pause with the game, and subscriptions/loops are released on disable/unload. Mixer parameters: `MusicVol`, `SFXVol`; music is not yet installed.

These samples exercise event wiring and lifecycle. They have not passed listening, loudness or final delivery acceptance. They do not replace the lyre/drum/flute samples, music, manifests or the complete P0 audio batch.

As of 2026-11-09, the entire `tools/` directory is ignored by Git. The generator remains on this machine; new checkouts include the WAVs and import metadata, without the local generator.
