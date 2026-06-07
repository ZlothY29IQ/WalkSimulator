using GorillaLocomotion;
using UnityEngine;

namespace WalkSimulator.Console;

public class ConsoleUtils
{
    private static         int?                                  noInvisLayerMask;
    
    public static int NoInvisLayerMask()
    {
        noInvisLayerMask ??= ~(
                                  1 << LayerMask.NameToLayer("TransparentFX")    |
                                  1 << LayerMask.NameToLayer("Ignore Raycast")   |
                                  1 << LayerMask.NameToLayer("Zone")             |
                                  1 << LayerMask.NameToLayer("Gorilla Trigger")  |
                                  1 << LayerMask.NameToLayer("Gorilla Boundary") |
                                  1 << LayerMask.NameToLayer("GorillaCosmetics") |
                                  1 << LayerMask.NameToLayer("GorillaParticle"));

        return noInvisLayerMask ?? GTPlayer.Instance.locomotionEnabledLayers;
    }

    public static void TeleportPlayer(Vector3 destinationPosition)
    {
        GTPlayer.Instance.TeleportTo(FormatTeleportPosition(destinationPosition), GTPlayer.Instance.transform.rotation);
        VRRig.LocalRig.transform.position = destinationPosition;
    }

    public static Vector3 FormatTeleportPosition(Vector3 teleportPosition) =>
            teleportPosition - GorillaTagger.Instance.bodyCollider.transform.position +
            GorillaTagger.Instance.transform.position;
    
    public static void TeleportToMap(string mapName)
        {
            string mapTrigger = "";
            string networkTrigger = "";

            switch (mapName)
            {
                case "Forest":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/TreeRoomSpawnForestZone";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Forest, Tree Exit";

                    break;

                case "City":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/ForestToCity";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - City Front";

                    break;

                case "Canyons":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/ForestCanyonTransition";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Canyon";

                    break;

                case "Clouds":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityToSkyJungle";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Clouds From Computer";

                    break;

                case "Caves":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/ForestToCave";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Cave";

                    break;

                case "Beach":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/BeachToForest";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Beach for Computer";

                    break;

                case "Mountains":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityToMountain";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Mountain";

                    break;

                case "Basement":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityToBasement";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Basement For Computer";

                    break;

                case "Metropolis":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/MetropolisOnly";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Metropolis from Computer";

                    break;

                case "Arcade":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityToArcade";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - City frm Arcade";

                    break;

                case "Critters":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityCrittersTransition";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - City from Critters";

                    break;

                case "Rotating":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityToRotating";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Rotating Map";

                    break;

                case "Bayou":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/BayouOnly";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - BayouComputer2";

                    break;

                case "Virtual Stump":
                {
                    VirtualStumpTeleporter vstumpt = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/VirtualStump_HeadsetTeleporter/TeleporterTrigger").GetComponent<VirtualStumpTeleporter>();
                    vstumpt.gameObject.transform.parent.parent.parent.parent.parent.parent.gameObject.SetActive(true);
                    vstumpt.gameObject.transform.parent.parent.parent.parent.gameObject.SetActive(true);
                    vstumpt.TeleportPlayer();
                    return;
                }

                case "Lava Forest":
                    mapTrigger     = "Environment Objects/05Maze_PersistentObjects/GhostReactorElevatorManager/VIMForestLavaElevator/Triggers/VIMExp1_SetZoneTrigger";
                    networkTrigger = "Environment Objects/05Maze_PersistentObjects/GhostReactorElevatorManager/VIMForestLavaElevator/Triggers/JoinRoomTrigger";

                    break;

                case "Skate Park":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/ForestToHoverboard";
                    networkTrigger = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Hoverboard from Forest";

                    break;

                case "Monke Blocks":
                    mapTrigger     = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/MonkeBlocksElevatorExit";
                    networkTrigger = "Environment Objects/05Maze_PersistentObjects/GhostReactorElevatorManager/MonkeBlocksElevator/Triggers/JoinRoomTrigger";

                    break;
            }

            GameObject.Find(mapTrigger)?.GetComponent<GorillaSetZoneTrigger>()?.OnBoxTriggered();
            GameObject.Find(networkTrigger)?.SetActive(false);
            TeleportPlayer(GameObject.Find(mapTrigger)?.transform.position ?? VRRig.LocalRig.transform.position);
        }
}