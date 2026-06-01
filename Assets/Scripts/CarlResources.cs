using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Hidden energy and oil trackers (0–100). Not shown in the UI.
/// </summary>
public class CarlResources : MonoBehaviour
{
    public const float MaxValue = 100f;
    public const float PickupRestoreAmount = 65f;
    public const float EnergyDrainPerSecond = MaxValue / 25f;
    public const float OilDrainPerUnitWalked = MaxValue / (2f * AspectRatioCamera.WorldWidth);

    const float OilSpawnDelay = 2f;
    const float BatterySpawnDelay = 5f;
    const float FloorTopY = -7.16f;
    const float PickupRestY = 0.28f;

    public float Energy { get; private set; } = MaxValue;
    public float Oil { get; private set; } = MaxValue;
    public float EnergyFraction => Energy / MaxValue;
    public float OilFraction => Oil / MaxValue;
    public bool IsEnergyLow => Energy < MaxValue * 0.5f;
    public bool IsOilLow => Oil < MaxValue * 0.5f;
    public bool IsGameOver { get; private set; }

    public event Action Changed;
    public event Action GameOver;

    void Start()
    {
        StartCoroutine(SpawnPickupsRoutine());
    }

    IEnumerator SpawnPickupsRoutine()
    {
        yield return new WaitForSecondsRealtime(OilSpawnDelay);
        ResourcePickup.Spawn(PickupType.Oil, new Vector2(-1.8f, FloorTopY + PickupRestY));

        yield return new WaitForSecondsRealtime(BatterySpawnDelay - OilSpawnDelay);
        ResourcePickup.Spawn(PickupType.Energy, new Vector2(1.8f, FloorTopY + PickupRestY));
    }

    public void RegisterSelfWalkDistance(float distance)
    {
        if (distance <= 0f || IsGameOver)
            return;

        Oil = Mathf.Max(0f, Oil - distance * OilDrainPerUnitWalked);
        Changed?.Invoke();
    }

    public void RestoreOil()
    {
        if (IsGameOver)
            return;

        Oil = Mathf.Min(MaxValue, Oil + PickupRestoreAmount);
        Changed?.Invoke();
    }

    public void RestoreEnergy()
    {
        if (IsGameOver)
            return;

        Energy = Mathf.Min(MaxValue, Energy + PickupRestoreAmount);
        Changed?.Invoke();
    }

    void Update()
    {
        if (IsGameOver)
            return;

        float previousEnergy = Energy;
        Energy = Mathf.Max(0f, Energy - EnergyDrainPerSecond * Time.deltaTime);

        if (!Mathf.Approximately(previousEnergy, Energy))
            Changed?.Invoke();

        if (Energy <= 0.001f)
            TriggerGameOver();
    }

    void TriggerGameOver()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        Energy = 0f;
        GameOver?.Invoke();
        Changed?.Invoke();
        LoseOverlay.Show();
    }
}
