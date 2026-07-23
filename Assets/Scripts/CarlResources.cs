using System;
using UnityEngine;

/// <summary>
/// Hidden energy and oil trackers (0–100). Metered by <see cref="ResourceHud"/>.
/// </summary>
[DefaultExecutionOrder(50)]
public class CarlResources : MonoBehaviour
{
    public const float MaxValue = 100f;
    public const float PickupRestoreAmount = 65f;
    /// <summary>Full → empty while standing still.</summary>
    public const float EnergyIdleDrainPerSecond = MaxValue / 100f;
    /// <summary>Full → empty while walking (~4× idle).</summary>
    public const float EnergyWalkDrainPerSecond = MaxValue / 25f;
    public const float OilDrainPerUnitWalked = MaxValue / (2f * AspectRatioCamera.WorldWidth);
    /// <summary>Trigger radius a bit larger than Carl's body (box is 0.55 wide).</summary>
    public const float PickupRadius = 0.42f;

    const string DefaultSpawnResource = "DefaultLevelSpawns";

    [SerializeField] LevelSpawnConfig spawnConfig;

    public float Energy { get; private set; } = MaxValue;
    public float Oil { get; private set; } = MaxValue;
    public float EnergyFraction => Energy / MaxValue;
    public float OilFraction => Oil / MaxValue;
    public bool IsEnergyLow => Energy < MaxValue * 0.5f;
    public bool IsOilLow => Oil < MaxValue * 0.5f;
    public bool IsGameOver { get; private set; }
    public LevelSpawnConfig SpawnConfig => spawnConfig;

    public event Action Changed;
    public event Action GameOver;

    bool _walkedThisFixedUpdate;

    public void SetSpawnConfig(LevelSpawnConfig config)
    {
        spawnConfig = config;
    }

    void Awake()
    {
        if (spawnConfig == null)
            spawnConfig = Resources.Load<LevelSpawnConfig>(DefaultSpawnResource);

        EnsurePickupRadius();
    }

    void EnsurePickupRadius()
    {
        var triggers = GetComponents<CircleCollider2D>();
        CircleCollider2D pickup = null;
        for (var i = 0; i < triggers.Length; i++)
        {
            if (triggers[i] != null && triggers[i].isTrigger)
            {
                pickup = triggers[i];
                break;
            }
        }

        if (pickup == null)
            pickup = gameObject.AddComponent<CircleCollider2D>();

        pickup.isTrigger = true;
        pickup.radius = PickupRadius;
    }

    void Start()
    {
        SpawnPickups();
        ResourceHud.EnsureFor(this);
    }

    void SpawnPickups()
    {
        if (spawnConfig == null || spawnConfig.Pickups == null || spawnConfig.Pickups.Length == 0)
            return;

        foreach (var entry in spawnConfig.Pickups)
            ResourcePickup.Spawn(entry.type, entry.position);
    }

    public void RegisterSelfWalkDistance(float distance)
    {
        if (distance <= 0f || IsGameOver)
            return;

        _walkedThisFixedUpdate = true;
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

    void FixedUpdate()
    {
        if (IsGameOver)
            return;

        float rate = _walkedThisFixedUpdate ? EnergyWalkDrainPerSecond : EnergyIdleDrainPerSecond;
        _walkedThisFixedUpdate = false;

        float previousEnergy = Energy;
        Energy = Mathf.Max(0f, Energy - rate * Time.fixedDeltaTime);

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
