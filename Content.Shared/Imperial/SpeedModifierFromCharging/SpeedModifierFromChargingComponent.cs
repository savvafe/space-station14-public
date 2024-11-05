namespace Content.Shared.Imperial.SpeedModifierFromCharging;


/// <summary>
/// Changes speed depending on battery charge
/// </summary>
[RegisterComponent]
public sealed partial class SpeedModifierFromChargingComponent : Component
{
    /// <summary>
    /// Cell slot id
    /// </summary>
    [DataField(required: true)]
    public string CellSlotId = string.Empty;

    [DataField]
    public float SprintSpeedModifierOnCharge = 1.0f;

    [DataField]
    public float WalkSpeedModifierOnCharge = 1.0f;

    [DataField]
    public float SprintSpeedModifierOnDischarge = 1.0f;

    [DataField]
    public float WalkSpeedModifierOnDischarge = 1.0f;

    /// <summary>
    /// Should we discharge the battery when unequipped?
    /// </summary>
    [DataField]
    public bool DrawOnUnequipped = false;

    /// <summary>
    /// Should we discharge the battery when equipped?
    /// </summary>
    [DataField]
    public bool DrawOnEquipped = true;


    [ViewVariables]
    public bool Drawing = false;

    [ViewVariables]
    public TimeSpan NextBatteryUpdate = TimeSpan.FromSeconds(0);
}
