using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ElectroMouse.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ElectroMouseComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Energy = 1;

    [DataField("stolenEssenceCurrencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string StolenEnergyCurrencyPrototype = "StolenEnergy";

    /// <summary>
    /// Prototype to spawn when the entity dies.
    /// </summary>
    [DataField("spawnOnDeathPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnOnDeathPrototype = "AnomalyCoreElectroMouse";

    [DataField("harvestDebuffs")]
    public Vector2 HarvestDebuffs = new(5, 5);

    [DataField("blinkSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg")
    {
        Params = AudioParams.Default.WithVolume(5f)
    };
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadCost")]
    public FixedPoint2 OverloadCost = -40;

    [DataField("overloadDebuffs")]
    public Vector2 OverloadDebuffs = new(3, 8);

    [ViewVariables(VVAccess.ReadWrite), DataField("overloadRadius")]
    public float OverloadRadius = 5f;

    [ViewVariables(VVAccess.ReadWrite), DataField("overloadZapRadius")]
    public float OverloadZapRadius = 2f;
    [DataField] public EntityUid? Action;
}
