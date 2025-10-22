# Nuclear Option Tactitools

A **very WIP** mod for game Nuclear Option.
Comprises the following components :
- A module for creating new joystick inputs, thanks to some simple ReWired reverse engineering
- A module for creating new UI elements, on the HUD, Flight HUD and flight instruments
- An interception vector displayed on the tactical screen for single mobile targets
- The ability to save and recall a target group for later use
- A decoupling of the different countermeasure and weapon slots switching, each has its own button now

More to follow soon !


# About

# Main features

### Interception vector on the screen for single mobile targets
- Only works for single targets
- Takes 3 seconds to spool up
- ETA and bearing are displayed at the bottom of the target screen
- The interception solution is not updated if the target is not tracked

### Target group save and recall (HOTAS only)
- Can be assigned to any peripheral button
  - **Long press** -> Save target group
  - **Short press** -> Recall target group
- Saved target group persists after respawn

### Separate, dedicated buttons for weapon slot selection (HOTAS only)
- Can be assigned to any peripheral button
- Direct-select weapon slots via dedicated buttons (0–5)
- Slot order is based on the order weapons are first shown on the loadout screen
- Long-press on slot 0 toggles Turret Auto Control

### Separate, dedicated buttons for Flares and Jammer selection
- Can be assigned to any peripheral button

### HUD delivery bar and per-shot indicators to indicate launch/detonation “delivery” status
- Show icons for each launched missile/bomb on the target screen; icons persist ~2s after impact and clear on respawn
- Color delivery: green = armor hit, red = miss for instant outcome feedback
- Distinct shapes: missiles rotate to diamonds, bombs remain square for quick ordnance ID

### Weapon & Countermeasure Display MFD (HOTAS support)
- Shows flares/jammer status, current weapon name, and ammo in the cockpit
- Toggle between modded and original content
  - Can be assigned to any peripheral button
- Per-airframe layouts

### Unit marker distance indicator
- Changes HMD marker orientation when within a configurable distance threshold
  - The enemy unit's icon points downwards when the enemy unit is under the threshold
  - The speed at which the icon rotates when crossing the threshold indicates the enemy unit's speed
- Optional “near” sound cue

### Cockpit MFD color customization
- Set main MFD color
- Optional alternative attitude (horizon/ground) colors
- Optional use of vanilla UI elements alongside modded pages

### Artificial Horizon for Chicane, Ibis and Tarantula
- Horizon line always shown
- Cardinal directions are indicated and hidden when in front of the main HUD

### Extras and experimental
- Optional AA Unit icon recolor on the main map
- Optional boot screen animation

# Installing

# FAQ

# Roadmap

# Contributing