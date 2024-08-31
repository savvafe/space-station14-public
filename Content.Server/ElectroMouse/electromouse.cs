using Content.Server.Actions;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.ElectroMouse.Events;
using Content.Shared.Alert;
using Content.Shared.Revenant;
using Content.Shared.FixedPoint;
using Content.Shared.ElectroMouse.Components;

namespace Content.Server.ElectroMouse.EntitySystems;


public sealed partial class RevenantSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string RevenantShopId = "ActionElectroMouseShop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectroMouseComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseShopActionEvent>(OnShop);
        SubscribeLocalEvent<ElectroMouseComponent, HarvestEvent>(UpdateEnergy);

    }

    private void OnShop(EntityUid uid, ElectroMouseComponent component, ElectroMouseShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

    private void OnMapInit(EntityUid uid, ElectroMouseComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.Action, RevenantShopId);
    }

    private void OnChangeEssenceAmount(EntityUid uid, ElectroMouseComponent component)
    {
        ChangeEssenceAmount(uid, component.Energy, component);
    }
    public bool ChangeEssenceAmount(EntityUid uid, FixedPoint2 amount, ElectroMouseComponent? component = null, bool allowDeath = true, bool regenCap = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!allowDeath && component.Energy + amount <= 0)
            return false;

        component.Energy += amount;

        if (regenCap)
            FixedPoint2.Min(component.Energy, component.EssenceRegenCap);

        if (TryComp<StoreComponent>(uid, out var store))
            _store.UpdateUserInterface(uid, uid, store);

        _alerts.ShowAlert(uid, AlertType.Essence, (short) Math.Clamp(Math.Round(component.Energy.Float() / 10f), 0, 16));

        if (component.Energy <= 0)
        {
            Spawn(component.SpawnOnDeathPrototype, Transform(uid).Coordinates);
            QueueDel(uid);
        }
        return true;
    }
    private void UpdateEnergy(EntityUid uid, ElectroMouseComponent component, HarvestEvent args)
    {
        // Get the ElectroMouseComponent instance from the entity
        if (EntityManager.TryGetComponent(uid, out ElectroMouseComponent? electroMouseComponent))
        {
            ChangeEssenceAmount(uid, electroMouseComponent.Energy, component);
            _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
                { {component.StolenEssenceCurrencyPrototype, electroMouseComponent.Energy} }, uid);
        }
    }

}
