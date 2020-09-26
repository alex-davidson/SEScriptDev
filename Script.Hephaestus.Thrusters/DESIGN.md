# Hephaestus Thruster Control

The Hephaestus mining platform has three tiers of eight engine modules, comprising three thrusters each.
* The primary engines are fixed mounts around the stern, pointing rearwards.
* The midships engines provide lateral thrust in space, or assist the primary engines when under gravity.
* The bow engines provide braking thrust in space, or assist the primary engines when under gravity.

The ship lands and takes off vertically on planets. The stern should always be 'downwards' under gravity,
unless lithobraking is being employed.

Side view:
```
     U          U
     O----------O   BOW
     |          |
     |          |
    >O          O<  MIDSHIPS
     |          |
     O          O   PRIMARY
    / \--------/ \
```

Top-down:
```
        AB          BA
    ,--O O O------O O O--.
    |                    |
    O  TOWER      TOWER  O
 AD O    A          B    O BC
    O                    O
    |                    |
    |                    |
    O                    O
 DA O  TOWER      TOWER  O CB
    O    D          C    O
    |                    |
    `--O O O------O O O--'
        DC          CD
```

Conventions:
* Each engine module is named for its nearest tower, then the other tower on that side, in that order.
* Each midships or bow engine module has an 'inner' and 'outer' rotor. The 'outer' rotor is the one nearest
  the corner of the ship.
* All angles are in degrees, since the game's UI favours them.
* For any rotor or module, '0 degrees' must mean 'thruster aligned with primary engines', ie. pointing directly
  backwards/downwards.
  * Each module's rotors must obviously rotate in opposite directions.
  * When describing the angle of rotation of a *thruster module*, it is assumed that it is rotated away from
    the ship's hull. Module angles are always positive.
  * When describing the angle of rotation of a *rotor*, this angle is always *clockwise, facing the rotor
    from the axle*.

The thruster rotors are controlled by tier.
* The midships modules can point outwards (90 degrees) or backwards (5 degrees).
* The bow modules can point forwards (180 degrees), outwards (90 degrees) or backwards (15 degrees).

The difference between 30 and 15 degrees bow thruster rotation equates to ~2.4 thrusters, or ~1.76 Kt of launch
mass.

## Ergonomics and Experience

Given a target angle for a tier, the control computer should seek to rotate all engine modules on that tier to
that angle, then lock them. Locking ensures that the thrusters will work properly with subgrid thruster scripts.

* Rotation commands are given as `set <tier> <preset>`.
  * An unrecognised tier should sound an alarm: ship is not configured properly.
  * An unrecognised preset should sound an alarm: ship is not configured properly.
* If the same preset is requested for a tier twice in a short period of time, 'emergency mode' should be used:
  increase the rotor speeds.
* Any request for a tier cancels any previous request for that tier.

Tech notes:

Rotors should accelerate and decelerate throughout their motion. Model this as a critically-damped spring, with
total rotation time controlled by the spring constant.

Consider a 'test mode' where modules oscillate about their starting position by one or two degrees, in sync.
Directional misconfigurations can be detected by noting 'stuck' or out-of-sync modules.

Any detected configuration change should raise some kind of minor alert condition aboard the ship.

## Rotor Control

Each module is controlled by a pair of rotors facing each other.
* The zero point is always the same angle.
* Since the rotors face each other, they must rotate in opposite directions.
  * If a pair of rotors' angles do not add up to `0 mod 360`, sound an alarm: ship is broken and/or non-Euclidean.
  * Rotor displacement should be `-20cm` for all facing rotors.
* 'Clockwise, facing the rotor from the axle' is always 'positive' angular deflection.

For any pair of rotors, we want to provide an engine module angular deflection and have the pair figure it out
for themselves.

We need to know where the rotors are on the hull; `Inner, CD` rotating clockwise from zero will deflect the
thrusters correctly, but `Inner, DC` doing similarly will create a hot mess (at least 2000 degrees C). Use the test
mode (mentioned above) to check this.

One of the rotors in a pair will always rotate clockwise for positive module deflection and can therefore be
considered to be the 'governing' rotor. Its angular velocity and position can be interpreted directly as the 'state'
of the entire module.

Tech notes:

Governing rotors *point anticlockwise* around the ship, as they *rotate clockwise* to deflect thrusters (from the
zero point) away from the hull.

So given the circular relationship `A > B > C > D > A` and a module `XY`:
* If `X` > `Y`, the inner rotor governs (eg. AB)
* If `Y` > `X`, the outer rotor governs (eg. BA)

## Code Structure

Since this is a special-purpose script devised for a single ship, all block names and conventions can be hardcoded.
At construction time:
1. Load any saved state.
2. Request a rescan.
3. Set script run rate.

Every update consists of:
1. Update the script clock.
2. Process provided commands.
3. Run any pending work.
4. Update error status (trigger alarms, etc).
5. Set script run rate: 10 ticks if pending command, 100 ticks otherwise.

Commands:
* `test` - Enter test mode.
* `set <tier> <preset>` - Rotated the named tier to a named preset.
* `stop` - Stop all running commands.
* `rescan` - Rescan all blocks after all running commands complete. Rebuild internal structures.
* `reset` - Cancel all running commands and clear test passes.

Pending work:
* Test mode: iterator.
* Rotate thruster modules: iterator.
* Default mode: lock all module rotors and set limits of motion.

State management:
* Each module has a structure referencing its governing and following rotors, and providing initiators for each
  operation.
  * An initiator returns an enumerator.
* Each operation maintains state internally per-module.

## Operations

Each module's 'state' is determined by its governing rotor. A running command basically consists of pumping an iterator
for each module.

### Test mode

Parameters: a direction (positive or negative).

* Capture starting angle. Pick an angle 2 degrees away in the requested direction. Update limits of motion for test mode.
  Control rotation speed via spring constant as usual.
* When the rotor reaches the desired angle: set velocity to zero, lock the rotor.

When all rotors report complete, test mode orchestrator should reverse direction and repeat.

### Move module to preset

Parameters: spring constant, and a reference to the clock.

* Update limits of motion.
* On each iteration, update target velocity based on current location and velocity.
* When the rotor reaches the desired angle: set velocity to zero, lock the rotor.

When all rotors report complete, module orchestrator should report complete.

## Error Reporting

Classes of error:
* Sanity checks: rotor angles, etc.
* Safety concerns: rotors outside limits.
* General warnings: missing rotors.

All of these need to be reported on a display, and many should trigger audible alarms too.
