using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.SpeedModifierFromCharging;


public sealed class SharedSpeedModifierFromChargingSystem : EntitySystem
{
    [Dependency] private readonly SharedPowerCellSystem _powerCellSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeedModifierFromChargingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SpeedModifierFromChargingComponent, GotUnequippedEvent>(OnGotUnEquipped);

        SubscribeLocalEvent<SpeedModifierFromChargingComponent, PowerCellChangedEvent>(OnPowerCellChanged);

        SubscribeLocalEvent<SpeedModifierFromChargingComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnSpeedRefresh);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<SpeedModifierFromChargingComponent, PowerCellDrawComponent>();

        while (enumerator.MoveNext(out var uid, out var component, out var powerCellDrawComponent))
        {
            if (_timing.CurTime <= component.NextBatteryUpdate) continue;
            if (powerCellDrawComponent.Drawing) continue;
            if (!component.Drawing) continue;

            component.Drawing = false;

            if (!_containerSystem.TryGetContainingContainer(uid, out var container)) return;

            _speedModifierSystem.RefreshMovementSpeedModifiers(container.Owner);
        }
    }

    private void OnGotEquipped(EntityUid uid, SpeedModifierFromChargingComponent component, GotEquippedEvent args)
    {
        _speedModifierSystem.RefreshMovementSpeedModifiers(args.Equipee);
        _powerCellSystem.SetPowerCellDrawEnabled(uid, component.DrawOnEquipped);

        component.Drawing = component.DrawOnEquipped;
    }

    private void OnGotUnEquipped(EntityUid uid, SpeedModifierFromChargingComponent component, GotUnequippedEvent args)
    {
        _speedModifierSystem.RefreshMovementSpeedModifiers(args.Equipee);
        _powerCellSystem.SetPowerCellDrawEnabled(uid, component.DrawOnUnequipped);

        component.Drawing = component.DrawOnUnequipped;
    }

    private void OnSpeedRefresh(EntityUid uid, SpeedModifierFromChargingComponent component, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        args.Args.ModifySpeed(
            component.Drawing ? component.WalkSpeedModifierOnCharge : component.SprintSpeedModifierOnDischarge,
            component.Drawing ? component.SprintSpeedModifierOnCharge : component.SprintSpeedModifierOnDischarge
        );
    }

    private void OnPowerCellChanged(EntityUid uid, SpeedModifierFromChargingComponent component, PowerCellChangedEvent args)
    {
        if (!_containerSystem.TryGetContainingContainer(uid, out var container)) return;

        _speedModifierSystem.RefreshMovementSpeedModifiers(container.Owner);

        if (!TryComp<PowerCellDrawComponent>(uid, out var powerCellDrawComponent)) return;

        _speedModifierSystem.RefreshMovementSpeedModifiers(container.Owner);
        component.NextBatteryUpdate = powerCellDrawComponent.NextUpdateTime;
    }
}
