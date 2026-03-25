# T002 - Define Segment Grammar

## Scope
This artifact defines the formal segment grammar for Tap Miner: Collapse Run.

In scope:
- Segment types
- Safe path definition
- Reward path definition
- Hazard limits
- Forbidden combinations
- Depth bucket rules
- Spawn and distribution constraints
- Readability and fairness constraints

Out of scope:
- Segment generator implementation
- Scene composition
- Prefab composition
- Runtime mechanics
- New resource systems
- Combat rules
- Economy tuning
- Any feature not required to formalize segment grammar

## Rule Format
All grammar rules in this document are explicit and testable.

- If a segment structure is not explicitly allowed here, it is not allowed.
- If a composition rule is not explicitly defined here, it must not be assumed.
- The grammar defines legal segment layouts only. It does not define generation code or runtime behaviors beyond layout legality.

## Core Terms

### Segment
A segment is one authored or generated run slice that presents a single readable traversal choice space to the player.

### Lane
A lane is a traversable path line through a segment.

### Safe path
A safe path is the minimum guaranteed traversable route through a segment that contains no mandatory hazard contact.

### Reward path
A reward path is an optional traversable route that offers reward access at the cost of greater pathing demand or hazard proximity.

### Hazard node
A hazard node is any segment element that can make a route unsafe.

### Reward node
A reward node is any segment element placed to justify optional risk-taking.

### Readability
Readability is the player's ability to identify the safe path, optional reward path, and blocking hazards before commitment is required.

### Fairness
Fairness means a segment never requires blind failure, impossible routing, or unavoidable hazard contact on the guaranteed safe path.

## 1. Segment Types

Exactly four segment types are allowed in T002.

### Type `S0_StartSafe`
Purpose:
- Establish immediate readability and baseline traversal.

Required structure:
- Exactly one safe path.
- Zero reward paths or one reward path.
- Zero hazards on the safe path.

Forbidden structure:
- More than one reward path.
- Any forced hazard contact.

### Type `S1_StandardChoice`
Purpose:
- Present a primary safe route and an optional alternate route.

Required structure:
- Exactly one safe path.
- Zero or one reward path.
- At least one route choice point if a reward path exists.

Forbidden structure:
- More than one reward path.
- More than one independent choice split.

### Type `S2_RewardRisk`
Purpose:
- Present explicit optional risk for reward while preserving one safe completion route.

Required structure:
- Exactly one safe path.
- Exactly one reward path.
- Reward path must be optional.
- Reward path must be visually separable from the safe path before commitment.

Forbidden structure:
- Reward path identical to safe path.
- Mandatory reward pickup routing.

### Type `S3_HazardPressure`
Purpose:
- Increase pressure while keeping a fair guaranteed route.

Required structure:
- Exactly one safe path.
- Zero or one reward path.
- Hazard density may exceed earlier types only within hazard limits and bucket rules.

Forbidden structure:
- Full-path hazard wall.
- Unreadable hazard overlap.

## 2. Safe Path Definition

The safe path is mandatory in every legal segment.

### Safe path requirements
- Every legal segment must contain exactly one safe path.
- The safe path must connect segment entry to segment exit.
- The safe path must be readable before player commitment.
- The safe path must remain legal under the segment's declared depth bucket.
- The safe path must not require reward collection.
- The safe path must not require intentional hazard contact.

### Safe path limits
- Safe path branching is not allowed. There is one guaranteed safe completion route.
- The safe path may visually approach hazards, but it must not require touching or crossing an active hazard node.
- If a reward path exists, failure on the reward path must not invalidate the safe path.

### Safe path test rule
A segment fails grammar validation if a player cannot identify at least one non-hazard completion route from entry to exit before commitment.

## 3. Reward Path Definition

Reward paths are optional by rule.

### Reward path requirements
- A reward path may exist only if the segment type allows it.
- A segment may contain at most one reward path.
- The reward path must diverge from the safe path at a readable choice point.
- The reward path must rejoin the segment flow or terminate at a reward endpoint that still returns control to the safe completion flow.
- The reward path must be optional.

### Reward path limits
- The reward path must not hide the safe path.
- The reward path must not fully occlude upcoming hazards.
- The reward path must not require a blind jump, blind timing guess, or off-screen commitment.
- Reward placement must justify the route's higher risk or higher execution demand.

### Reward path test rule
A segment fails grammar validation if the reward path is mandatory, visually indistinguishable from the safe path, or cannot be identified before the player must commit.

## 4. Hazard Limits

Hazards may create pressure but not invalidate fairness.

### Global hazard limits
- Every segment must preserve exactly one safe path free of mandatory hazard contact.
- Hazards must not fully seal segment entry.
- Hazards must not fully seal segment exit.
- Hazards must not create an unavoidable fail state on every available route.
- Hazards must not overlap so heavily that route identity becomes unreadable.

### Per-segment hazard limits
- `S0_StartSafe`: zero hazard nodes on the safe path; at most one hazard cluster off the safe path.
- `S1_StandardChoice`: zero hazard nodes on the safe path; limited hazard presence may pressure the reward path.
- `S2_RewardRisk`: zero hazard nodes on the safe path; reward path may contain hazards but must remain readable.
- `S3_HazardPressure`: safe path remains contact-free; hazard density may increase near the safe path but cannot collapse route readability.

### Hazard test rule
A segment fails grammar validation if all visible routes require hazard contact or if hazard placement makes safe-route identification unreliable.

## 5. Forbidden Combinations

The following combinations are illegal in all depth buckets.

- No safe path present.
- More than one safe path claimed.
- More than one reward path.
- Reward path identical to the safe path.
- Safe path blocked by a hazard wall.
- Segment entry blocked by hazards with no legal traversal route.
- Segment exit blocked by hazards with no legal traversal route.
- Reward path hidden behind off-screen information only.
- Reward path requiring blind commitment before the player can read the risk.
- Hazard placement that visually masks route boundaries.
- Two independent choice splits in one segment.
- Simultaneous forced hazard contact and mandatory reward routing.
- Hazard-only segment with no optionality and no safe route.
- Start segment using `S2_RewardRisk` or `S3_HazardPressure`.
- Any segment composition that depends on secret rules outside this document.

## 6. Depth Bucket Rules

Exactly four depth buckets are allowed in T002.

### Bucket `D0_Intro`
Allowed segment types:
- `S0_StartSafe`
- `S1_StandardChoice`

Bucket rules:
- First segment of a run must be `S0_StartSafe`.
- `S2_RewardRisk` is not allowed.
- `S3_HazardPressure` is not allowed.

### Bucket `D1_Early`
Allowed segment types:
- `S0_StartSafe`
- `S1_StandardChoice`
- `S2_RewardRisk`

Bucket rules:
- At least one segment in any three-segment window must remain `S0_StartSafe` or `S1_StandardChoice`.
- `S3_HazardPressure` is not allowed.

### Bucket `D2_Mid`
Allowed segment types:
- `S1_StandardChoice`
- `S2_RewardRisk`
- `S3_HazardPressure`

Bucket rules:
- `S3_HazardPressure` cannot appear in two consecutive segments.
- Any `S3_HazardPressure` segment must be followed by a segment with a clearly readable safe path and no increased pressure beyond the same bucket baseline.

### Bucket `D3_Late`
Allowed segment types:
- `S1_StandardChoice`
- `S2_RewardRisk`
- `S3_HazardPressure`

Bucket rules:
- `S3_HazardPressure` may appear consecutively only once; three consecutive `S3_HazardPressure` segments are forbidden.
- A sequence containing two consecutive `S3_HazardPressure` segments must be followed by a non-`S3_HazardPressure` segment.
- Safe path, reward path, and readability invariants still apply with no exceptions.

### Depth progression test rule
A bucket assignment fails grammar validation if it allows a segment type forbidden by that bucket or creates an illegal pressure streak.

## 7. Spawn and Distribution Constraints

These constraints govern legal segment distribution without defining generator code.

### Global distribution constraints
- A run must start with `S0_StartSafe`.
- A segment's declared depth bucket must determine its legal type set.
- No segment may spawn with a type not allowed by its bucket.

### Local sequence constraints
- Consecutive identical segment types are allowed only if they do not break depth bucket rules.
- Two consecutive segments that both contain a reward path are allowed only if each still preserves full readability.
- A segment containing both maximal bucket pressure and a reward path is legal only when the reward path remains optional and readable.

### Recovery distribution constraints
- After any illegal-pressure-near-streak would occur, the next legal segment must be a lower-pressure type.
- Distribution must never create a run slice in which the player faces only unreadable or high-pressure options.

### Spawn test rule
A sequence fails grammar validation if any segment type, adjacency, or pressure streak violates its bucket or fairness rules.

## 8. Readability and Fairness Constraints

Readability and fairness are non-optional invariants.

### Readability constraints
- Segment entry, safe path, and exit direction must be visually inferable before commitment.
- If a reward path exists, its divergence point must be visible before the player must choose.
- Hazards must be readable as hazards before they become unavoidable.
- Route identity must remain understandable without relying on hidden timing knowledge.

### Fairness constraints
- A player must be able to complete every legal segment by choosing and following the safe path.
- Reward failure risk may increase difficulty but must remain optional.
- A segment must not punish the player for choosing the safe path correctly.
- Difficulty may escalate by bucket, but legality and readability may not degrade.

### Fairness test rule
A segment fails grammar validation if a reasonable player cannot distinguish the safe route and optional risk before commitment.

## 9. Coverage Check

This document explicitly covers:
- segment vocabulary
- legal segment types
- safe path requirements
- reward path requirements
- hazard limits
- illegal combinations
- depth buckets
- legal distribution constraints
- readability rules
- fairness rules

## 10. Unresolved Edge Case Check

The following edge cases are explicitly resolved.

- A segment without a reward path is legal if its type allows zero reward paths.
- A segment without hazards is legal if its type allows that structure and still preserves one safe path.
- A reward path may rejoin the safe flow, but it may not replace the safe path.
- A segment may visually narrow the safe path, but it may not convert that path into mandatory hazard contact.
- Increased depth pressure may change allowed segment types, but it may not remove the guaranteed safe path.
- Readability constraints override pressure ambition: an unreadable high-pressure segment is illegal even in `D3_Late`.

## Completion Statement
T002 is complete only if downstream validation confirms:
- segment grammar is explicit and testable
- safe, reward, and hazard composition is unambiguous
- forbidden combinations are listed explicitly
- depth buckets are defined clearly
- fairness and readability rules are explicit
- no hidden edge-case gaps remain
