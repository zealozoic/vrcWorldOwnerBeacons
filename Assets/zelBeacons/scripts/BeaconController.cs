
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BeaconController : UdonSharpBehaviour
{
    public bool showBeacons = true;//boolean for toggling beacons on/off locally

    [Tooltip("Empty Prefab to instantiate when needed")]
    public GameObject empty;

    [Tooltip("Blue Beacon that follows the Player named in [World Creator]")]
    public Transform beaconBlue;
    [Tooltip("Name of Player to tie the Blue Beacon to (ex: world creator)")]
    public string worldCreator;
    private VRCPlayerApi blueOwner;//VRCPlayerApi object to tie to the blue owner
    private string blueName = "";//populates if the Blue Owner is in the instance

    [Tooltip("Green Beacon that follows the players named in [AdminNamesParent]")]
    public Transform beaconGreen;

    [Tooltip("Red Beacon that follows the World Master (oldest user in instance)")]
    public Transform beaconRed;
    private VRCPlayerApi redOwner;//VRCPlayerApi object to tie to the red owner
    private string redName = "";//populates if the Red Owner is in the instance

    private float updateTimer = 0;//time remaining until next heavy update
    [Tooltip("Time in seconds of each timer iteration (0.5 - 1.0 recommended)")]
    public float updateTimerLength = 0.5f;

    [Tooltip("Parent GameObject holding a list of other GameObjects that detail the UserNames of each admin")]
    public Transform adminNamesParent;
    [Tooltip("A GameObject that disables itself when you are listed in the [Admin Names Parent] list")]
    public GameObject adminMenuParent;
    [Tooltip("A GameObject that holds all Admin Beacons")]
    public Transform greenAdminParent;
    [Tooltip("A GameObject that holds a list of the Target Locations for each Admin object")]
    public Transform greenAdminTargets;
    [Tooltip("A GameObject that holds a list of admins that have left the world and need to be removed from the [GreenAdminParent] and [GreenAdminTargets] GameObjects")]
    public Transform greenAdminsToRemove;
    [Tooltip("A GameObject that holds a list of admins that have joined the world and need to be added to the [GreenAdminParent] and [GreenAdminTargets] GameObjects")]
    public Transform greenAdminsToAdd;

    [Tooltip("Smoothing for Admin Beacons. Default = 0.02. This is best left at a low number to mask the slow update process of Admin Beacons")]
    public float greenTargetSmoothing = 0.02f;
    [Tooltip("Smoothing for Master and World Creator Beacons. Default = 0.02")]
    public float blueAndRedTargetSmoothing = 0.02f;

    [Tooltip("How far above the head the beacon floats. Default = 0.5")]
    public float distanceAboveHead = 0.5f;
    [Tooltip("How far each beacon spaces each other when they overlap. Default = 0.5")]
    public float beaconSpacing = 0.5f;

    private void Start()
    {
        //if updatetimer is less than 0.1, may cause severe stuttering. Recommended is still 0.5 - 1.0
        updateTimerLength = Mathf.Max(0.1f, updateTimerLength);

        //Quick and simple method to disable the "Admin Menu Parent" GameObject when the local player's name is listed in Admin names list.
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        for (int i = 0; i < adminNamesParent.childCount; i++)
        {
            string adminName = adminNamesParent.GetChild(i).gameObject.name;
            if (adminName == localPlayer.displayName)
            {
                adminMenuParent.SetActive(false);
            }
        }
        setBeacons(showBeacons);
    }
    private void Update()
    {
        //
        updateTimer -= Time.fixedDeltaTime;
        if (updateTimer < 0)
        {
            updateTimer = updateTimerLength;
            if (greenAdminsToAdd.childCount < 1 && greenAdminsToRemove.childCount < 1)
            {
                UpdateBeaconGreenTargets();
            }
        }
        UpdateAllBeacons();
    }

    private void UpdateAllBeacons()
    {
        UpdateBeaconBlue();
        UpdateBeaconGreen();
        UpdateBeaconRed();
    }

    private void UpdateBeaconBlue()
    {
        if (Utilities.IsValid(blueOwner))
        {
            Vector3 blueHeadPosition = blueOwner.GetBonePosition(HumanBodyBones.Head);
            if (blueHeadPosition.sqrMagnitude < 0.01f)
            {
                blueHeadPosition = blueOwner.GetPosition();
            }
            Vector3 plumbobBluePosition = new Vector3(blueHeadPosition.x, blueHeadPosition.y + distanceAboveHead, blueHeadPosition.z);
            beaconBlue.transform.position = Vector3.Lerp(beaconBlue.transform.position, plumbobBluePosition, blueAndRedTargetSmoothing);
        }
        else
        {
            beaconBlue.transform.position = Vector3.zero;
        }
    }

    private void UpdateBeaconGreenTargets()
    {
        if (greenAdminTargets.childCount > 0)
        {
            if (greenAdminPos == -1)
            {
                greenAdminPos = 0;
                playerList = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
                playerList = VRCPlayerApi.GetPlayers(playerList);
            }
            
            VRCPlayerApi playerID = playerList[greenAdminPos];
            string playerName = playerID.displayName;
            for(int i=0; i<adminNamesParent.childCount; i++)
            {
                if (playerName == adminNamesParent.GetChild(i).gameObject.name)
                {
                    Vector3 targetPos = playerID.GetBonePosition(HumanBodyBones.Head);
                    if (targetPos.sqrMagnitude < 0.01f)
                    {
                        targetPos = playerID.GetPosition();
                    }
                    float verticalOffset = distanceAboveHead;
                    if (playerName == redName)
                    {
                        verticalOffset += beaconSpacing;
                    }
                    if (playerName == blueName)
                    {
                        verticalOffset += beaconSpacing;
                    }
                    for (int j=0; j<greenAdminTargets.childCount; j++)
                    {
                        Transform greenAdminTargetID = greenAdminTargets.GetChild(j);
                        if (playerName == greenAdminTargetID.gameObject.name)
                        {
                            greenAdminTargetID.position = new Vector3(targetPos.x, targetPos.y + verticalOffset, targetPos.z);
                            break;
                        }
                    }
                    break;
                }
            }
            greenAdminPos += 1;
            if (greenAdminPos > playerList.Length - 1)
            {
                greenAdminPos = 0;
            }

        }
    }

    int greenAdminPos = -1;
    VRCPlayerApi[] playerList;

    private void UpdateBeaconGreen()
    {

        if (greenAdminTargets.childCount > 0)
        {
            for (int i = 0; i < greenAdminTargets.childCount; i++)
            {
                Transform greenAdminTarget = greenAdminTargets.GetChild(i);
                Transform greenAdminID = greenAdminParent.GetChild(i);
                Vector3 targetPosition = Vector3.Lerp(greenAdminID.position, greenAdminTarget.position, greenTargetSmoothing);
                greenAdminID.position = targetPosition;
            }
            
        }

        if (greenAdminsToAdd.childCount > 0)
        {
            greenAdminPos = -1;
            Transform greenAdminID = greenAdminsToAdd.GetChild(0);
            greenAdminID.SetParent(greenAdminTargets);
            GameObject greenBeacon = Instantiate(beaconGreen.gameObject);
            greenBeacon.transform.SetParent(greenAdminParent);
            greenBeacon.name = greenAdminID.gameObject.name;
            if (showBeacons)
            {

            }
            else
            {
                greenBeacon.SetActive(false);
            }

        }
        if (greenAdminsToRemove.childCount > 0)
        {
            greenAdminPos = -1;
            Transform greenAdminToRemove = greenAdminsToRemove.GetChild(0);
            for(int i=0; i<greenAdminTargets.childCount; i++)
            {
                Transform greenAdminTarget = greenAdminTargets.GetChild(i);
                if (greenAdminTarget.gameObject.name == greenAdminToRemove.gameObject.name)
                {
                    Destroy(greenAdminTarget.gameObject);
                    break;
                }
            }
            for(int i=0; i<greenAdminParent.childCount; i++)
            {
                Transform greenAdminID = greenAdminParent.GetChild(i);
                if (greenAdminID.gameObject.name == greenAdminToRemove.gameObject.name)
                {
                    Destroy(greenAdminID.gameObject);
                    break;
                }
            }
            Destroy(greenAdminToRemove.gameObject);
        }
    }

    private void UpdateBeaconRed()
    {
        if (Utilities.IsValid(redOwner))
        {
            Vector3 redHeadPosition = redOwner.GetBonePosition(HumanBodyBones.Head);
            if (redHeadPosition.sqrMagnitude < 0.01f)
            {
                redHeadPosition = redOwner.GetPosition();
            }
            float verticalOffset = distanceAboveHead;
            if (redOwner.displayName == blueName)
            {
                verticalOffset += beaconSpacing;
            }
            Vector3 plumbobRedPosition = new Vector3(redHeadPosition.x, redHeadPosition.y + verticalOffset, redHeadPosition.z);
            beaconRed.transform.position = Vector3.Lerp(beaconRed.transform.position, plumbobRedPosition, blueAndRedTargetSmoothing);
        }
        else
        {
            beaconRed.transform.position = Vector3.zero;
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        base.OnPlayerJoined(player);
        greenAdminPos = -1;
        bool isMaster = player.isMaster;
        if (isMaster)
        {
            redOwner = player;
            redName = player.displayName;
        }
        if (worldCreator == player.displayName)
        {
            blueOwner = player;
            blueName = player.displayName;
        }
        for (int i = 0; i < adminNamesParent.childCount; i++)
        {
            string adminName = adminNamesParent.GetChild(i).gameObject.name;
            if (player.displayName == adminName)
            {
                GameObject greenToAdd = Instantiate(empty);
                greenToAdd.name = adminName;
                greenToAdd.transform.SetParent(greenAdminsToAdd);
            }
        }
    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        base.OnPlayerLeft(player);
        greenAdminPos = -1;
        if (greenAdminParent.childCount > 0)
        {
            for (int i = 0; i < greenAdminTargets.childCount; i++)
            {
                Transform greenAdminTarget = greenAdminTargets.GetChild(i);
                if (player.displayName == greenAdminTarget.name)
                {
                    GameObject greenAdminToRemove = Instantiate(empty);
                    greenAdminToRemove.name = greenAdminTarget.name;
                    greenAdminToRemove.transform.SetParent(greenAdminsToRemove);
                }
            }
        }
        if (redName == player.displayName)
        {
            redName = "";
            foreach (VRCPlayerApi otherPlayer in playerList)
            {
                if (Utilities.IsValid(otherPlayer)){
                    if (otherPlayer.isMaster)
                    {
                        redOwner = otherPlayer;
                        redName = otherPlayer.displayName;
                    }
                }
            }
        }
        if (player.displayName == worldCreator)
        {
            blueName = "";
            blueOwner = null;
        }
    }

    public override void Interact()
    {
        base.Interact();
        toggleBeacons();
    }

    public void toggleBeacons()
    {
        if (showBeacons)
        {
            showBeacons = false;
        }
        else
        {
            showBeacons = true;
        }
        setBeacons(showBeacons);
    }

    public void setBeacons(bool enabled)
    {
        beaconRed.gameObject.SetActive(enabled);
        beaconBlue.gameObject.SetActive(enabled);
        if (greenAdminParent.childCount > 0)
        {
            for (int i = 0; i < greenAdminParent.childCount; i++)
            {
                greenAdminParent.gameObject.SetActive(enabled);
            }
        }
    }
}

