# Tap Miner: Collapse Run — Changelog

---

## TM-CORE-04 — Player Position + Collapse Band Calibration (2026-03-27)

**Files changed:** `RunPresentationController.cs`

### CHANGE 1 — Player Y lowered to -1.5f
Both occurrences of `playerVisualRoot.localPosition` changed from `(0f, 1.5f, -0.3f)` to `(0f, -1.5f, -0.3f)`. Player now sits in the lower half of the screen, below center.

### CHANGE 2 — Collapse band starts above camera top
On RunActive entry, `collapseRoot.position.y` is set so that the band's world Y = `camera.position.y + orthographicSize + 1.0f` (1 unit above the visible screen top). Formula: `collapseRoot.y = bandStartWorldY - 3.35f` (subtracting the band's localY offset within collapseRoot).

### CHANGE 3 — Descent rate changed to scrollSpeed × 0.3f/s
Removed `collapseProgress * 1.85f` formula. Now uses per-frame accumulation: `_collapseBandDescentY += GetScrollSpeed() * 0.3f * Time.deltaTime` when RunActive and not paused. Position: `collapseRoot.position.y = bandStartY - _collapseBandDescentY - surge`. `_collapseBandDescentY` resets to 0 on RunActive entry.

### CHANGE 4 — RunActive entry log
Added `Debug.Log("[INIT] playerY=... bandStartY=...")` on RunActive entry to confirm spawn positions.

### Math
- Camera: orthographicSize=4.85, position.y=0.15 → top edge = 5.0 → band start world Y = 6.0 → `collapseRoot.y = 6.0 - 3.35 = 2.65`
- Player world Y = -1.5
- Travel required = 6.0 − (−1.5) = 7.5 units
- Rate = scrollSpeed(3.0) × 0.3 = 0.9 u/s → ~8.3 seconds to contact at rest

---

## TM-CORE-03 — Speed Calibration + Segment Spawn + Player Offset (2026-03-27)

**Files changed:** `RunPresentationController.cs`

### FIX 1 — Collapse band speed
Removed hardcoded `3.8f` multiplier. Band now descends using `_collapseBandDescentY += GetScrollSpeed() * 0.6f * Time.deltaTime` each frame during RunActive. Position formula: `collapseRoot.localPosition.y = -_collapseBandDescentY`. Band reaches player when `collapseRenderers[0].transform.position.y <= playerVisualRoot.position.y` (visual contact check). `_collapseBandDescentY` resets to 0 on RunActive entry (new run / restart).

### FIX 2 — Player Y raised to +1.5f
Both occurrences of `playerVisualRoot.localPosition` changed from `(0f, -0.45f, -0.3f)` to `(0f, 1.5f, -0.3f)`. Player now floats above the lane cubes with clear visual gap.

### FIX 3 — Segment markers scroll with world + respawn below
Removed per-frame pin of `markerRoot` to `playerVisualRoot.position.y - 2.0f`. Now:
- Each frame during RunActive (when not paused, not blocked): `markerRoot.position += Vector3.up * scrollSpeed * deltaTime` — markers drift up with the world
- On RunActive entry: `markerRoot.position.y = playerVisualRoot.position.y - 2.5f` (initial placement)
- On segment advance (detected in `UpdateMarkerColorsIfNeeded`): `markerRoot.position.y = playerVisualRoot.position.y - 2.5f` (respawn row below player)
- Marker gap increased from `2.0f` to `2.5f` below player

### FIX 4 — Empty cells (BUG 5 from TM-CORE-02)
Already implemented in TM-CORE-02. Marker cube `SetActive(false)` for safe/empty lanes. No additional changes.

### BUG 4 despawn threshold
Increased from `playerY + 1.0f` to `playerY + 2.0f` so markers remain visible through more of their upward drift before being hidden.

---

## TM-CORE-02 — Core Loop Bug Fixes (2026-03-27)

**Files changed:** `AppBootstrap.cs`, `RunPresentationController.cs`

### BUG 1 — Auto-collect coins
Coins in REWARD lanes are now automatically collected when the player is in the reward lane and the world scrolls past. Added `TryAutoCollectRewardForCurrentSegment()` called from `UpdateBlockDetection()` each frame when no block is present. Guard `_lastAutoCollectedRewardSegmentIndex` prevents double-collection per segment. Shows `+N` feedback on collect.

### BUG 2 — Collapse death on visual contact
Death now fires when the collapse band's world Y visually reaches the player Y, not when the timer reaches 1.0. Removed the kill trigger from `UpdateCollapseByTime()` (timer now only clamps at 1.0). Visual check added in `UpdateWorldPresentation()` inside the `collapseRenderers` block: `if (bandWorldY <= playerVisualRoot.position.y) → bootstrap.NotifyLethalDamage()`. Collapse multiplier corrected from `1.1f` to `3.8f` so the band travels the full distance to reach the player at full fill (3.35 units band offset − (−0.45) player Y = 3.8 units of travel).

### BUG 3 — Player Y position
Raised `playerVisualRoot.localPosition.y` from `-1.95f` to `-0.45f` (+1.5 units). Player no longer clips into the segment cubes.

### BUG 4 — Cube despawn above player
Marker cubes are now hidden each frame when `markerRenderers[laneIndex].transform.position.y > playerVisualRoot.position.y + 1.0f`. Re-enabled on segment change in `UpdateMarkerColorsIfNeeded()`.

### BUG 5 — Empty/safe cells render as cubes
Marker cubes are now only rendered for BLOCK, HAZARD, and REWARD cells. Safe and empty lanes set `markerRenderers[laneIndex].gameObject.SetActive(false)`. Logic: `hasCubeContent = hasBlock || hasHazard || hasReward`.

### New fields — AppBootstrap.cs
- `_lastAutoCollectedRewardSegmentIndex` — tracks last auto-collected reward segment to prevent double-collect
- `_scrollUnitsAtLastSegmentAdvance` — accumulator for scroll-distance-based segment advance trigger
- `SegmentScrollDistance = 4.0f` — world units of scroll required to auto-advance one segment

### New methods — AppBootstrap.cs
- `TryAutoCollectRewardForCurrentSegment()` — grants loot when player lane matches reward lane
- `TryAutoAdvanceSegment()` — advances to next segment every 4 scroll units when no block is present
