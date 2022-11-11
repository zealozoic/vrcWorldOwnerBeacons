# vrcWorldOwnerBeacons
A Beacon system for use in VRChat worlds to label World Owners, Masters and Admins

The purpose of this script is to provide an easy way for people to implement tags or icons that hover above users in their VRChat world.  
Currently, there are three different beacons configured to hover over users:  
  - World Master  
    - Displays over the oldest user in the instance  
    - Useful for finding who may have control over a video player or interactive game  
  - World Creator  
    - Displays over the user configured in the "World Creator" string parameter  
    - This could be anyone, not just the actual world uploader. Perhaps the world is a commission or meant for another user.  
  - World Admins  
    - Displays over users configured in the list of world admins  
    - These are typically users who have special permissions such as those who may activate DJ booths or access a configuration room for the world  
    - For now it just disables an object specified in the "Admin Menu Parent" gameobject parameter  
    - Useful for removing a collider to a secret room or special buttons  
    - More functionality for admins will be added in the future  

# Requirements
- VRC World SDK (Found in VRC Creator Companion)
- VRC Udon Sharp (Found in VRC Creator Companion)
- "BeaconController.cs" script and "BeaconController.asset" (as well as associated meta files) found in \Assets\zelBeacons\scripts\

# How To Use
- Be sure the correct unity version is installed: [VRC Currently Supported Unity Version](https://docs.vrchat.com/docs/current-unity-version).
- The only required file is essentially the script and its compiled asset, however the "beacons.prefab" prefab in \Assets\zelBeacons\prefabs\ is my recommended way to get started
- To set up the asset, there are a number of required gameobjects that must be configured and filled into the appropriate parameter slots:
  - Empty: An empty prefab that is used to instantiate and configure empty gameobjects.
  - Beacon Blue: This is the Blue Beacon GameObject that follows the World Creator. (Does not have to be blue)
  - World Creator: A string that defines the name of the World Creator user. This could be anyone, not just the actual world uploader.
  - Beacon Green: This is the Green Beacon GameObject that follows the admin Users. These are instantiated as copies. (does not have to be green)
  - Beacon Red: This is the Red Beacon GameObject that follows the world Master. (Does not have to be red)
  - Update Timer Length: The time in seconds between each "heavy update".
  - Admin Names: A GameObject who's children are other GameObjects who's names represent each admin in the world.
  - Admin Menu Parent: This is a work in progress. Currently this object disables itself when the script detects that you are an admin in the world.
  - Green Admin Parent: An empty GameObject that holds all of the instantiated Green Beacon GameObjects when Admins join the world.
  - Green Admin Targets: An empty GameObject that holds all of the target locations for each Green Beacon GameObject.
  - Green Admins To Remove: A list of admins that have left the world and need their Green Beacons (and Targets) destroyed.
  - Green Admins To Add: A list of admins that have joined the world and need their Green Beacons (and Targets) instantiated.
  - Green Target Smoothing: how quickly the Green Beacons move towards their target location. (lower values will be smoother and slower).
  - Blue and Red Target Smoothing: how quickly the Blue and Red Beacons move towards their target location. (lower values will be smoother and slower).

![image](https://user-images.githubusercontent.com/115526707/201248907-ccd6d4c0-18f7-4f84-98d4-8d97cddf220e.png)


# How it works
  - On Player Joined & On Player Left
    - Blue Beacon (World Creator)
      - When a player joins or leaves, the script will compare the name of the player to the name configured in "World Creator". If the name matches, it will assign/remove the blue beacon to/from that player.
    - Red Beacon (World Master)
      - When a player joins, the script will check to see if that player is currently the world master. If a player leaves, it will cycle through the list of players to find the next master.
    - Green Beacons (World Admins)
      - When a player joins, the script will compare the name of the new player to the list of names defined by the Children GameObject's names under the "Admin Names Parent" GameObject. If the name matches, the "Admin Menu Parent" GameObject is disabled, and a new Green Beacon GameObject is Instantiated and tracked to the player.
      - When a player leaves, the script will compare the name of the player to the list of names defined by the Children GameObject's names under the "Admin Names Parent" Gameobject. If The name matches, the script will search for the instanciated Green Beacon Objects and destroy them.
  - Heavy Updates:
    - This script tries to reduce the amount of processing by bundling computationally expensive updates into "heavy updates" that are triggered once every x seconds (defined in Update Timer Length).
    - Every heavy update, the script will grab a full list of every player in the instance and compare it to the list of admins, as well as updates every target position for the admin beacons.
