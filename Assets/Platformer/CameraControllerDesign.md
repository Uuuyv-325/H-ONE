# Camera Controller Design

## Framing

The player sits at 29% of the view width while moving right and 68% while moving left. The horizontal anchor moves between those positions at 1.5 screen fractions per second, so changing direction does not jump the focal point. The vertical anchor is 25% of the view height. The camera center is clamped to the finite map rectangle inset by the camera half extents. The `Geometry` group remains available for level editing.

The camera stays still while the focus is inside a 0.42 by 0.34 world-unit dead zone. Once tracking starts, it continues during movement and returns to idle after movement and camera motion settle inside the zone.

## Tracking Law

The camera is updated once per frame. Let `c` be its current position, `t` the current clamped focus, and `deltaTarget` the player's displacement since the previous camera update. For `dt > 0` and follow strength `k = 12/s`, calculate

```text
translated = c + deltaTarget
alpha = 1 - exp(-k * dt)
candidate = translated + alpha * (t - translated)
c_next = clamp(candidate, min(c, t), max(c, t))  // independently per axis
```

The first term transfers the player's actual displacement to the camera, including Rigidbody interpolation. If the target and anchor move at constant velocity and the camera is aligned with the focus, `translated = t` every frame: camera position and velocity match exactly in steady state. The exponential correction removes any remaining position error, and `0 <= alpha < 1` cannot cross the focus by itself. The final projection prevents the transferred displacement from taking the camera past the current focus when the player abruptly reverses. There is no acceleration limit; this is intentional because A/D changes player velocity instantly and strong tracking takes priority.

This is a per-frame no-overshoot claim relative to the *current* focus. When the focus itself crosses the previous camera position on a reversal, the camera can change direction immediately, but its new position stays between its old position and the new focus. Map clamping is applied after this projection, so the camera cannot show space outside the level. Near a map edge the camera may not reach its preferred anchor.
