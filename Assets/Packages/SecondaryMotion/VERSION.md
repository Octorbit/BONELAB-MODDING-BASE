# SecondaryMotion v2.3.0
# v2.3.0

### Feature Additions
* SyncUpdateRateToGameTime field, sets the component's update rate to match the update rate of the game, disregarding what the component's UpdateRate is set to.

### Bug Fixes
* Better physics update rate handling, reducing jitter issues on some systems
* FixedUpdate now properly functions as it should

# v2.2.0

### Feature Additions

* Added a custom inspector window for SecondaryMotion.cs, making it look much nicer in editor

# v2.1.0

### DRAWBRIDGE

Added a gating system for game-specific code

Any code that tries to use game-specific stuff, like stuff that uses DynamicBonesBL or BoneLib for BONELAB, will only run if those mods are installed and the game running is BONELAB.

This means the mod will work just fine without having those mods installed, it just won't run its code that uses them.

Why is it named DRAWBRIDGE?

It prevents code from running if the requirements aren't met!

...and also because i've been playing a lot of death stranding 2 and drawbridge is a thing in death stranding 2.

### Feature Additions

* Added the ability to only simulate the local player's bones, so Fusion players don't simulate their Secondary Motion bones, for performance (Default is off)

# v2.0.2

### Behaviour Changes
* "Root" field now automatically sets itself to the object you added the SecondaryMotion script to

# v2.0.1

### Feature Changes
* Visibility Culling now uses camera frustrums from the main player camera, hopefully preventing issues in Fusion. Feature remains experimental.

# v2.0.0

### BREAKING CHANGES
Gravity calculations have been redone, so old values may seem way too low. -9.81 is now the baseline value for standard gravity!
"Enable Gravity" was also added, which is false by default, so mods using the old versions will lose gravity entirey.

The issues this specifically fixes are:
* Gravity is now world space
* Gravity is no longer applying too little force

### Bug Fixes
* Fixed MaxAngle not working if Elasticity is 0

# v1.2.0

### Feature Additions
* Added Visibility Culling (EXPERIMENTAL)
* Added HelpURLs to the editor scripts

# v1.1.0

### Feature Additions
* Added MaxAngle and MaxAngleCurve fields

# v1.0.2

### Bug Fixes
* Fixed bone rotations in mirrors
* Fixed jitter issues on frame drops

# v1.0.1

### Package Changes
* Redid icon and README a bit

### Bug Fixes
* Add handling for when DynamicBones is present on the same object as SecondaryMotion, as a soft dependency so it is not required to have it installed

# v1.0.0
* Initial Release