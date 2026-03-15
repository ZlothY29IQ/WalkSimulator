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
}