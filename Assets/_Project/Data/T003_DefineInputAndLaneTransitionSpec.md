# T003 - Define Input and Lane Transition Spec

## Scope
This artifact defines the formal input handling and lane transition specification for Tap Miner: Collapse Run.

In scope:
- Input handling contract
- Lane transition contract
- Tap, hold, and swipe interpretation rules within current scope
- Transition timing rules
- Collision timing assumptions
- Input buffering policy
- Invalid input behavior
- State-specific input permissions
- Edge-case closures

Out of scope:
- Controller implementation
- Animation implementation
- Scene or prefab setup
- Runtime movement tuning
- New input systems
- New mechanics beyond lane transition formalization

## Rule Format
All rules in this document are explicit and testable.

- If an input behavior is not explicitly allowed here, it is not allowed.
- If a lane-transition rule is not explicitly defined here, it must not be assumed.
- This artifact defines legal behavior for interpreting player intent and lane changes; it does not define implementation code.

## Dependencies
This specification uses the official run states defined in T001:
- `RunReady`
- `RunActive`
- `RunDeathResolved`
- `RunRestarting`

## Core Terms

### Input event
A single player interaction sample submitted to the gameplay input interpreter.

### Lane change request
A valid directional request to move from the current lane to an adjacent lane.

### Lane transition
The bounded movement action that begins when a lane change request is accepted and ends when the player is fully committed to the target lane.

### Transition owner
The active run instance that currently owns input and lane transition authority.

### Idle lane state
The player is fully aligned to one lane and is not transitioning.

### Transitioning state
The player is between lane transition start and lane transition end.

## 1. Input Handling Contract

### Input authority
- Gameplay movement input may be interpreted only while the run state is `RunActive`.
- Only the current active run may own gameplay input authority.
- Inputs received outside allowed states are handled only according to the state-specific rules in this document.

### Accepted movement intents
- `SwipeLeft` requests a move to the adjacent lane on the left.
- `SwipeRight` requests a move to the adjacent lane on the right.

### Non-movement interaction intents within current scope
- `Tap` is not used for lane transition.
- `Hold` is not used for lane transition.
- Vertical swipe input is out of scope for T003 and must not be interpreted as a lane transition command.

### Input interpretation contract
- Each input event is evaluated once in arrival order.
- A movement input event may produce either an accepted lane change request or an explicit rejection outcome.
- Input interpretation must not infer hidden combo rules, gesture chaining, or delayed intent upgrades.

### Test rule
The input contract fails validation if the same movement event can legally resolve to more than one lane-transition outcome.

## 2. Lane Transition Contract

### Lane model assumptions
- Lane transition operates on an ordered lane set.
- A player occupies exactly one committed lane when idle.
- A legal lane change request targets one adjacent lane only.

### Legal lane transition conditions
A lane change request is accepted only when all of the following are true:
- Run state is `RunActive`.
- Player is in idle lane state.
- Requested direction maps to an adjacent in-bounds lane.
- No lane transition is already in progress.
- No explicit state lock from T001 prevents movement processing.

### Lane transition start
When a request is accepted:
- Transition start occurs immediately at acceptance time.
- Source lane is the currently committed lane at acceptance time.
- Target lane is the adjacent lane in the accepted direction.
- Transition owner becomes the active run's current lane transition process.

### Lane transition end
A lane transition ends only when:
- The player is fully committed to the target lane.
- The committed lane value updates from source lane to target lane.
- Transitioning state is cleared and idle lane state resumes.

### Lane transition invariants
- Only one lane transition may be active at a time.
- A transition cannot change direction mid-flight.
- A transition cannot skip over an intermediate lane.
- A transition cannot partially commit to two lanes at once.

### Test rule
The lane transition contract fails validation if start conditions, end conditions, or committed-lane ownership are ambiguous.

## 3. Tap, Hold, and Swipe Interpretation Rules

### Swipe rules
- Only horizontal swipe gestures are legal lane transition inputs in T003.
- A horizontal swipe maps to exactly one adjacent-lane request.
- Swipe strength, flourish, or gesture style must not change the semantic outcome once the swipe is classified as left or right.

### Tap rules
- `Tap` does not trigger lane movement.
- `Tap` has no fallback conversion into swipe.
- `Tap` may still exist for non-movement UI or future tasks, but it has no gameplay lane-transition meaning in T003.

### Hold rules
- `Hold` does not trigger lane movement.
- `Hold` does not auto-repeat lane requests.
- Releasing a hold does not convert the hold into a swipe.

### Mixed or unclear gesture rules
- If a gesture cannot be classified as `SwipeLeft` or `SwipeRight` under the active input recognizer, it is invalid for lane transition and must be rejected with no movement result.

## 4. Transition Timing Rules

### Timing ownership
- A lane transition has exactly two timing boundaries: accepted start and completed end.
- Transition timing begins at accepted start, not at first touch-down.
- Transition timing ends at completed end, not at input release.

### Timing behavior
- During transitioning state, no new lane change request may be accepted.
- During transitioning state, the committed lane remains the source lane until transition end.
- At transition end, the committed lane changes atomically to the target lane.

### Timing guarantees
- Input arrival during transitioning state never retroactively changes the current transition.
- A rejected input has no lingering effect once its evaluation is complete.
- Transition timing is authoritative over gesture spam; extra requests during an active transition do not modify the current result.

### Test rule
The timing contract fails validation if input release timing, touch duration, or gesture overlap can legally alter an already accepted lane transition.

## 5. Collision Timing Assumptions

These assumptions formalize how collisions should be reasoned about during future movement implementation.

### Collision ownership rule
- Collision resolution during a lane transition must evaluate against the transitioning player state, not against two fully committed lanes at once.

### Collision state assumptions
- Before transition start, collision evaluation uses the committed source lane.
- Between transition start and transition end, collision evaluation uses the active transitioning state.
- After transition end, collision evaluation uses the committed target lane.

### Collision fairness assumptions
- Collision evaluation must not double-hit the player by treating source and target lanes as simultaneously fully committed.
- Collision evaluation must not snap the player to the target lane before transition end.
- Collision evaluation must not ignore the fact that a lane transition is in progress.

### Test rule
The collision contract fails validation if the same obstacle can legally collide as though the player were fully in both source and target lanes at the same instant.

## 6. Input Buffering Policy

T003 defines a no-buffer policy.

### No-buffer rules
- No input buffering is allowed for gameplay lane transition requests.
- A lane change request received during transitioning state is rejected immediately.
- A lane change request received in a non-permitted run state is rejected immediately.
- Rejected requests are not queued for future replay.

### Buffering test rule
This policy fails validation if an input can be stored and later applied after the original evaluation window has ended.

## 7. Invalid Input Behavior

### Invalid movement requests
An input is invalid for lane transition when any of the following is true:
- Run state does not permit gameplay movement input.
- Gesture is not classified as `SwipeLeft` or `SwipeRight`.
- Requested target lane is out of bounds.
- A lane transition is already in progress.

### Invalid input outcomes
- Invalid input produces no lane transition.
- Invalid input does not change the committed lane.
- Invalid input does not start a partial transition.
- Invalid input does not enter a buffered queue.
- Invalid input does not cancel an already active transition.

### Test rule
Invalid input handling fails validation if any invalid input can still produce movement, buffering, or transition mutation.

## 8. State-Specific Input Permissions

### State `RunReady`
- Allowed: start-run command from T001.
- Forbidden for gameplay movement: all lane transition requests.
- Outcome for lane movement input: reject with no movement and no buffering.

### State `RunActive`
- Allowed: gameplay lane movement input according to this T003 contract.
- Allowed: `SwipeLeft` and `SwipeRight` when all acceptance conditions are true.
- Forbidden: accepting lane input during an already active transition.

### State `RunDeathResolved`
- Allowed: restart request from T001.
- Forbidden for gameplay movement: all lane transition requests.
- Outcome for lane movement input: reject with no movement and no buffering.

### State `RunRestarting`
- Allowed: no gameplay movement input.
- Forbidden: all lane transition requests.
- Outcome for lane movement input: reject with no movement and no buffering.

## 9. Edge-Case Closures

The following edge cases are explicitly resolved.

- Two opposite swipe inputs arriving during one active transition are both rejected after the first accepted request because no new request may be accepted during transitioning state.
- Repeating the same swipe during an active transition does not accelerate, refresh, or extend the transition.
- Swiping toward a non-existent lane is invalid and results in no movement.
- Tap followed by hold, or hold followed by release, does not produce lane movement unless a valid horizontal swipe is independently recognized.
- A movement input arriving on the same update step as run death is valid only if the run is still `RunActive` at evaluation time; otherwise it is rejected.
- A movement input arriving on the same update step as restart initialization is rejected because `RunRestarting` does not permit gameplay movement input.
- Input spam cannot create multi-lane skipping because each accepted input can target only one adjacent lane and buffering is disabled.

## Completion Statement
T003 is complete only if downstream validation confirms:
- input interpretation is explicit and testable
- lane transition start and end conditions are explicit
- invalid input outcomes are explicit
- collision timing assumptions are explicit
- state-specific input permissions are explicit
- no hidden ambiguity remains
