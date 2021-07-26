# DogeMetaversePlay
DogeMetaversePlay is a  sandbox  game based on Unity DOTs

# World
Each world is based on a map of 65536 plots of land (256x256), each land is 50*50*512, Land is the physical space in the metaverse that players use to create games . The land is used to publish your game . Every piece of land has a set of pre-built terrain, but it can be transformed and modified by the user who owns it (or other players they invite). 

# Current Systems
![Screenshot](./Docs/Images/systems.png)

## InitTerrainSystem
- [ ] Load or init Lands.

## SetGunShowBulletSystem
- [x] Show currently selected building box

## BuildBPhysicsWorldSystem
- [x] Collect boxes , calcute their nearby and hide invisible boxes 

## CharacterControllerSystem
- [x] Control chacater movement 

## CharacterGunOneToManyInputSystem
- [x] Build , destroy and change current selected building box

## CharacterHeadOneToManyInputSystem
- [x] Look up and down

# Other Systems

## Save System
Save lands

## Image wall generate system
Create wall from image
- [ ] Image Browser.
- [ ] Wall position editor.
