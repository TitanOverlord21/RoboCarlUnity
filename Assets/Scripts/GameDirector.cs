using System.Collections;
using UnityEngine;

/// <summary>
/// Legacy spawner kept for the scene object; spawning runs on CarlResources.
/// </summary>
public class GameDirector : MonoBehaviour
{
    void Start()
    {
        if (FindFirstObjectByType<CarlResources>() != null)
            return;

        StartCoroutine(SpawnWithoutCarlResources());
    }

    IEnumerator SpawnWithoutCarlResources()
    {
        yield return new WaitForSecondsRealtime(2f);
        ResourcePickup.Spawn(PickupType.Oil, new Vector2(-1.8f, -6.88f));

        yield return new WaitForSecondsRealtime(3f);
        ResourcePickup.Spawn(PickupType.Energy, new Vector2(1.8f, -6.88f));
    }
}
