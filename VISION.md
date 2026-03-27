# VISION.md - Tap Miner

Current source of truth for the game's player-facing direction.

This document is intentionally practical. It describes the current gameplay truth we are building toward, the MVP app shape, and the first playtest protocol. If code or older docs disagree with this file, this file wins unless a newer decision replaces it.

## One Sentence

Tap Miner is a mobile lane-based survival game where the player stays alive by reading routes, swapping between three lanes, breaking blocks at the right time, and avoiding being crushed against the ceiling while chasing gold for the next run.

## Core Fantasy

The player is not winning through brute force.

The player is surviving through a series of small correct decisions under pressure:
- read the route
- choose the lane
- break at the right time
- risk greed when it is worth it
- recover control before the ceiling wins

## Truth Of The Run

These rules define the run moment to moment:

1. The main threat is being crushed against the ceiling.
2. The ceiling is the primary fail condition and must be readable in the play space.
3. A player mistake is choosing or staying on a route that does not let them recover control in time.
4. A block is not death by itself. A block is delay, pressure, and lost position.
5. Tap is used to break a block in the current lane and regain control.
6. Swipe is used to change route and avoid future pressure.
7. Blocks rising under the player create the core pressure loop.
8. Gold exists to make the player consider riskier routes.
9. Damage exists as a buffer for mistakes, but HP is not the main loss condition.
10. Death must be explainable by route and timing, not by hidden system logic.
11. A good run feels like repeated recovery under pressure.
12. A bad run feels like accumulated small mistakes that end at the ceiling.

## Core Loop

The player loop is:

1. Read the next route
2. Commit to a lane
3. Break or avoid pressure
4. Risk for gold or healing
5. Lose or regain control
6. Die, collect results, upgrade, and restart

## Object Roles

Every major runtime object must have a clear role in the loop.

### Breakable Blocks

- They create pressure
- They delay the player
- They can be removed with tap
- They are the main "recover control" interaction

### Explosive Hazards

- They do not replace ceiling pressure
- They punish bad route or bad timing
- They remove HP
- They slow the player state for 1-2 seconds
- They must always be readable before resolution

### HP Collectables

- They reward risk
- They compensate for previous mistakes
- They should create route choice, not free healing

### Gold Collectables

- They create greed
- They motivate risky routing
- They fund persistent upgrades

## System Layers

### Primary Systems

These are required for the game identity to work:

- ceiling pressure
- three-lane movement
- rising blocks
- tap to break block
- gold collection
- death -> results -> restart loop

### Secondary Systems

These support the core loop but should never replace it:

- explosive hazards
- HP
- hazard slowdown
- HP collectables
- upgrades
- HUD feedback
- pause and settings

### Future Systems

These are valid future additions, but they are not required for the first strong version:

- more hazard archetypes
- more powerups
- bosses
- new tracks or scenes
- cosmetics
- long-term progression map

## MVP App Structure

The first complete app version should include these screens:

### Boot / Loading

Purpose:
- load player progress
- enter the app quickly

### Main Menu

Purpose:
- fast entry into play

Must have:
- Play
- Upgrades
- Settings or Pause access if needed

### Run Screen

Purpose:
- play the core survival loop

Must have:
- fixed ceiling
- player in one of three lanes
- rising blocks
- breakable blocks
- explosive hazards
- HP collectables
- gold collectables
- HP HUD
- depth HUD
- gold HUD
- readable feedback for break, hit, collect, death

### Results Screen

Purpose:
- explain outcome and motivate replay

Must have:
- cause of death
- depth reached
- gold earned this run
- total gold
- Play Again
- Upgrades

### Upgrades Screen

Purpose:
- improve the next run

Must have:
- current upgrade list
- prices
- current levels
- immediate purchase feedback

### Pause / Settings

Purpose:
- control the session without breaking the app loop

Must have:
- resume
- restart
- sound
- vibration
- return to menu

## MVP Upgrades

The first upgrade set should be small, readable, and immediately felt in the next run.

### Drill Power

Role:
- control recovery

Effect:
- break blocks faster or more reliably

### Move Speed

Role:
- route control

Effect:
- move between lanes faster

### Max HP

Role:
- error buffer

Effect:
- survive one more mistake

### Gold Value

Role:
- greed payoff

Effect:
- gain more gold from risky routing

### Hazard Recovery

Role:
- post-hit recovery

Effect:
- reduce the impact or duration of hazard slowdown

## Rules For Future Additions

Any new hazard, collectable, or powerup must clearly support one of these functions:

- control
- pressure
- recovery
- greed
- survivability

If a new feature does not support one of those functions, it should not enter the MVP.

## Playtest Goals

The first playtests are checking whether the game is understandable, fair, and replayable.

### Success Signals

- the player understands the ceiling as the main threat
- the player understands that blocks create pressure
- the player uses swipe and tap as part of one coherent survival loop
- the player notices gold and healing as route incentives
- the player can explain their death
- the player wants to press Play Again

### Failure Signals

- the player cannot explain why they died
- the player does not understand why or when to tap
- hazards feel random
- routing feels fake or obvious every time
- upgrades feel like abstract numbers with no run impact
- death does not create replay intent

## First Playtest Checklist

For each tester, evaluate:

### First Read

- Do they understand the goal in the first 5-10 seconds?
- Do they identify the ceiling as the core threat?

### Controls

- Do swipes feel reliable?
- Does tap feel like a meaningful recovery action?

### Route Choice

- Do they notice safe, risky, and rewarding paths?
- Do they ever change route because of gold or healing?

### Hazard Fairness

- Do they understand why they took damage?
- Does slowdown feel like a readable punishment?

### Restart Intent

- Do they want to immediately play another run?

### Upgrade Value

- After buying an upgrade, do they feel it in the next run?

## 10-Minute Playtest Script

### 0:00-1:00 First Contact

Tell the tester:
"Try to understand the game on your own. I will not explain unless you ask."

Observe:
- what they look at first
- whether they notice the ceiling
- whether they understand swipe and tap

### 1:00-3:00 First Runs

Observe:
- first death
- first route mistakes
- whether they can explain what happened

Ask:
- Why did you die?
- What do you think you should have done differently?

### 3:00-5:00 Route Choice Check

Observe:
- whether they go for gold
- whether they go for healing
- whether they see a safer and riskier path

Ask:
- When do you choose the more dangerous path?
- Did you notice gold and healing in advance?

### 5:00-7:00 Hazard Check

Observe:
- whether hazard telegraph is understood
- whether damage feels fair
- whether slowdown still leaves readable recovery

Ask:
- Why did you get hit?
- After getting hit, did you still know how to save yourself?

### 7:00-9:00 Upgrade Check

Have the tester buy 1-2 upgrades and run again.

Ask:
- Which upgrade did you choose and why?
- Did it feel useful in the next run?

### 9:00-10:00 Exit Interview

Ask:
- What is the main danger in the game?
- What was the clearest thing in the game?
- What was the least clear thing?
- Why did you usually die?
- Do you want to play again right now?

## Decision Policy

After playtests, changes should be prioritized in this order:

1. clarity
2. controls and response
3. route choice
4. hazard fairness
5. reward motivation
6. upgrade tuning

Do not tune numbers first if the game is still unclear.

## Temporary Long-Term Goal Policy

The long-term structure is intentionally not locked yet.

For now:
- short-term goal: survive deeper and extract more gold
- medium-term goal: grow stronger for the next runs
- long-term goal: decide later after playtest evidence

Possible future long-term structures include:
- bosses
- new scenes or tracks
- stronger meta progression
- cosmetics

Those are future framing decisions, not current MVP blockers.
