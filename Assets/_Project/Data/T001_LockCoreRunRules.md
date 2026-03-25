# T001 - Lock Core Run Rules

## Scope
This artifact formalizes the core run rules for Tap Miner: Collapse Run.

In scope:
- Run state model
- Movement rule contract
- Damage/death contract
- Restart flow contract
- Failure conditions
- Allowed transitions

Out of scope:
- Scene setup
- Prefab setup
- UI layout
- Numerical balance values
- Content tuning
- Any gameplay feature not explicitly defined below

## Rule Format
Every rule in this document is explicit. If a behavior is not listed here, it is not allowed to exist as an implicit rule.

## 1. Run State Model

### State: `RunReady`
Purpose:
- A fresh run context exists but the run is not yet simulating.

Entry conditions:
- Application creates the first playable run context.

Entry actions:
- Reset run-scoped data to its start-of-run values.
- Clear any terminal flags from the previous run.
- Set movement processing to disabled.
- Set damage processing to disabled.

Allowed while in state:
- Present ready-to-start state.
- Accept a start-run command.

Forbidden while in state:
- Player movement simulation.
- Damage intake.
- Death resolution.

Exit condition:
- A valid start-run command is accepted.

Exit actions:
- Enable movement processing.
- Enable damage processing.

### State: `RunActive`
Purpose:
- The run is live and gameplay simulation is allowed.

Entry conditions:
- `RunReady` accepts a start-run command.

Entry actions:
- Mark the run as the current active run.
- Open movement processing.
- Open damage processing.

Allowed while in state:
- Movement updates.
- Damage intake.
- Lethal and non-lethal hit resolution.

Forbidden while in state:
- Restart execution without first leaving `RunActive`.

Exit conditions:
- A damage event reduces current health to zero or below.
- A system-level run-invalid condition is raised from the failure conditions list.

Exit actions:
- Close movement processing immediately.
- Close damage processing immediately.
- Freeze the run as terminal.

### State: `RunDeathResolved`
Purpose:
- The active run has ended and the death outcome is finalized.

Entry conditions:
- `RunActive` exits because a failure condition ended the run.

Entry actions:
- Mark the run result as failed.
- Keep movement processing disabled.
- Keep damage processing disabled.
- Keep the run terminal and non-recoverable.

Allowed while in state:
- Present death/failure outcome.
- Accept a restart request.

Forbidden while in state:
- Resume the failed run.
- Apply additional damage.
- Simulate movement.

Exit condition:
- A valid restart request is accepted.

Exit actions:
- Begin creation of a fresh run context.

### State: `RunRestarting`
Purpose:
- The previous run is discarded and a fresh run context is being prepared.

Entry conditions:
- `RunDeathResolved` accepts a restart request.

Entry actions:
- Invalidate the old failed run context.
- Create a new run context.
- Reset all run-scoped values.

Allowed while in state:
- Internal reinitialization only.

Forbidden while in state:
- Movement simulation.
- Damage intake.
- New restart requests.
- Any reuse of the failed run state.

Exit condition:
- Fresh run context initialization completes successfully.

Exit actions:
- Open the fresh run immediately as the new active run.
- Enable movement processing.
- Enable damage processing.

## 2. Movement Rule Contract

### Movement authority
- Movement may be processed only in `RunActive`.
- Movement must not be processed in `RunReady`, `RunDeathResolved`, or `RunRestarting`.
- Movement input received outside `RunActive` is ignored and must not be queued for later replay.

### Movement owner
- The current active run owns movement authority.
- Only one active run may own movement authority at a time.

### Movement pipeline
- Read movement input for the current frame or tick.
- Validate that the current state is `RunActive`.
- Resolve movement for the active run.
- Commit the resulting position/state update once.
- If the state check fails, resolve no movement and commit nothing.

### Movement invariants
- Movement cannot occur before the run starts.
- Movement stops immediately when the run leaves `RunActive`.
- Movement cannot continue during death handling.
- Movement cannot carry over from a failed run into a restarted run.

### Hidden-rule lock
- No background movement, delayed drift, or inherited velocity is allowed unless a future task explicitly adds it.

## 3. Damage/Death Contract

### Damage authority
- Damage may be applied only in `RunActive`.
- Damage must not be applied in `RunReady`, `RunDeathResolved`, or `RunRestarting`.

### Damage resolution
- Each accepted damage event updates the active run exactly once.
- After damage is applied, health is evaluated immediately.
- If health remains above zero, the run stays in `RunActive`.
- If health reaches zero or below, the run ends immediately.

### Death rule
- Death is terminal for the current run.
- A dead run cannot return to `RunActive`.
- Additional damage after death is ignored.

### Death entry behavior
- The lethal event closes movement processing.
- The lethal event closes damage processing.
- The run transitions to `RunDeathResolved` without any hidden intermediate gameplay state.

### Hidden-rule lock
- No revive, continue, second life, grace save, or post-death damage handling is allowed unless a future task explicitly adds it.

## 4. Restart Flow Contract

### Restart source
- Restart may be requested only from `RunDeathResolved`.
- Restart is a one-tap action: the same accepted restart request both creates the fresh run and starts it.

### Restart sequence
1. Accept restart request from `RunDeathResolved`.
2. Transition to `RunRestarting`.
3. Discard the failed run context.
4. Create and initialize a fresh run context.
5. Transition directly to `RunActive`.
6. Begin the fresh run without requiring an additional start-run command.

### Restart guarantees
- Restart always creates a fresh run.
- Restart never resumes the failed run in place.
- Restart never preserves terminal flags from the failed run.
- Restart requires exactly one accepted user action after death.
- Restart never reopens movement or damage before the new run reaches `RunActive`.

### Hidden-rule lock
- No auto-restart, checkpoint restore, partial reset, or silent restart is allowed unless a future task explicitly adds it.

## 5. Failure Conditions

The current run fails only when one of the conditions below occurs.

### Failure condition F1: Lethal damage
- Definition: accepted damage reduces health to zero or below during `RunActive`.
- Result: transition from `RunActive` to `RunDeathResolved`.

### Failure condition F2: Run-invalid system state
- Definition: the run loses a required active run context while in `RunActive`.
- Result: transition from `RunActive` to `RunDeathResolved`.
- Purpose: prevents undefined behavior if simulation no longer has a valid active run to own movement and damage.

### Explicit non-failure cases
- A run does not fail merely because it is in `RunReady`.
- A run does not fail merely because restart initialization is in progress.
- A restart request is not itself a failure condition.

## 6. Unresolved Edge Case Check

The following edge cases are explicitly resolved by this artifact.

- Input received during `RunReady` is ignored until a valid start-run command is accepted.
- Input received during `RunDeathResolved` or `RunRestarting` is ignored and does not carry into the fresh run.
- A lethal damage event ends the run immediately; no extra movement or damage may resolve after that event.
- If multiple failure causes are detected in the same resolution step, the outcome is still a single transition to `RunDeathResolved`.
- Restart cannot be requested from `RunActive` or `RunReady`.
- Restart cannot reopen the failed run; it must create a fresh one.

## 7. Allowed Transitions

| From | To | Allowed | Trigger |
|---|---|---|---|
| `RunReady` | `RunActive` | Yes | Valid start-run command |
| `RunActive` | `RunDeathResolved` | Yes | `F1` or `F2` |
| `RunDeathResolved` | `RunRestarting` | Yes | Valid restart request |
| `RunRestarting` | `RunActive` | Yes | Fresh run initialization complete |
| `RunReady` | `RunDeathResolved` | No | Not allowed |
| `RunReady` | `RunRestarting` | No | Not allowed |
| `RunActive` | `RunReady` | No | Not allowed |
| `RunActive` | `RunRestarting` | No | Not allowed |
| `RunDeathResolved` | `RunActive` | No | Not allowed |
| `RunRestarting` | `RunReady` | No | Not allowed |
| Any state | Same state by implicit rule | No | Only explicit transitions are valid |

## State Entry/Exit Summary

| State | Entry defined | Exit defined |
|---|---|---|
| `RunReady` | Yes | Yes |
| `RunActive` | Yes | Yes |
| `RunDeathResolved` | Yes | Yes |
| `RunRestarting` | Yes | Yes |

## Ambiguity Check
- Start of play is explicit: `RunReady` to `RunActive` only on valid start-run command.
- End of run is explicit: `RunActive` to `RunDeathResolved` only on `F1` or `F2`.
- Restart is explicit and one-tap: `RunDeathResolved` to `RunRestarting` to `RunActive` from a single accepted restart request.
- No hidden intermediate run states are allowed.
- No hidden recovery rules are allowed.
- No hidden movement or damage behavior is allowed outside `RunActive`.

## Completion Statement
T001 is complete only if downstream validation confirms:
- no ambiguity
- all states have entry/exit
- no hidden rules
- report status satisfies Validator v1 conditions
