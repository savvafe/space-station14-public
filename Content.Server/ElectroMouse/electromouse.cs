using Content.Server.Actions;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.ElectroMouse.Events;
using Content.Shared.Alert;
using Content.Shared.Revenant;
using Content.Shared.FixedPoint;
using Content.Shared.ElectroMouse.Components;
using Content.Shared.Interaction;
using Content.Server.Revenant.Components;
using Content.Server.Power.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Content.Shared.Revenant.Components;
using Content.Shared.Stunnable;
using Content.Shared.StatusEffect;

namespace Content.Server.ElectroMouse.EntitySystems;


public sealed partial class RevenantSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string RevenantShopId = "ActionElectroMouseShop";
    private const string AddEnergyId = "ActionElectroMouseAddEnergy";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectroMouseComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseShopActionEvent>(OnShop);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseAddEnergyEvent>(AddEnergy);
        SubscribeLocalEvent<ElectroMouseComponent, InteractNoHandEvent>(OnInteract);
        SubscribeLocalEvent<ElectroMouseComponent, HarvestEvent>(OnHarvest);
    }


    private void OnShop(EntityUid uid, ElectroMouseComponent component, ElectroMouseShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

    private void AddEnergy(EntityUid uid, ElectroMouseComponent component, ElectroMouseAddEnergyEvent args)
    {
        UpdateEnergy(uid, component);
    }

    private void OnMapInit(EntityUid uid, ElectroMouseComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.Action, RevenantShopId);
    }

    public bool ChangeEssenceAmount(EntityUid uid, FixedPoint2 amount, ElectroMouseComponent? component = null, bool allowDeath = true, bool regenCap = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!allowDeath && component.Energy + amount <= 0)
            return false;

        component.Energy += amount;

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
    private void UpdateEnergy(EntityUid uid, ElectroMouseComponent component)
    {
        var store = EnsureComp<StoreComponent>(uid);
        // Get the ElectroMouseComponent instance from the entity
        if (EntityManager.TryGetComponent(uid, out ElectroMouseComponent? electroMouseComponent))
        {
            ChangeEssenceAmount(uid, electroMouseComponent.Energy, component);
            _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { component.StolenEnergyCurrencyPrototype, component.Energy } }, uid, store);
        }
    }
    private void OnInteract(EntityUid uid, ElectroMouseComponent component, InteractNoHandEvent args)
    {
        if (args.Target == args.User || args.Target == null)
            return;
        var target = args.Target.Value;

        if (!HasComp<ApcPowerReceiverComponent>(target))
            return;

        args.Handled = true;
        BeginHarvestDoAfter(uid, target, component);
    }


    private void BeginHarvestDoAfter(EntityUid uid, EntityUid target, ElectroMouseComponent revenant)
    {

        var doAfter = new DoAfterArgs(EntityManager, uid, revenant.HarvestDebuffs.X, new HarvestEvent(), uid, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = false, // stuns itself
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _appearance.SetData(uid, RevenantVisuals.Harvesting, true);

        _popup.PopupEntity(Loc.GetString("revenant-soul-begin-harvest", ("target", target)),
            target, PopupType.Large);

        TryUseAbility(uid, revenant, 0, revenant.HarvestDebuffs);
    }

    private void OnHarvest(EntityUid uid, ElectroMouseComponent component, HarvestEvent args)
    {
        if (args.Cancelled)
        {
            _appearance.SetData(uid, RevenantVisuals.Harvesting, false);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        _appearance.SetData(uid, RevenantVisuals.Harvesting, false);

        if (!TryComp<EssenceComponent>(args.Args.Target, out var essence))
            return;

        _popup.PopupEntity(Loc.GetString("revenant-soul-finish-harvest", ("target", args.Args.Target)),
            args.Args.Target.Value, PopupType.LargeCaution);

        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>{ {component.StolenEnergyCurrencyPrototype, 5} }, uid);
        essence.Harvested = true;


        if (!HasComp<ApcPowerReceiverComponent>(args.Args.Target))
            return;

        args.Handled = true;
    }
    private bool TryUseAbility(EntityUid uid, ElectroMouseComponent component, FixedPoint2 abilityCost, Vector2 debuffs)
    {
        if (component.Energy <= abilityCost)
        {
            _popup.PopupEntity(Loc.GetString("revenant-not-enough-essence"), uid, uid);
            return false;
        }

        var tileref = Transform(uid).Coordinates.GetTileRef();
        if (tileref != null)
        {
            if (_physics.GetEntitiesIntersectingBody(uid, (int) CollisionGroup.Impassable).Count > 0)
            {
                _popup.PopupEntity(Loc.GetString("revenant-in-solid"), uid, uid);
                return false;
            }
        }

        ChangeEssenceAmount(uid, abilityCost, component, false);

        _statusEffects.TryAddStatusEffect<CorporealComponent>(uid, "Corporeal", TimeSpan.FromSeconds(debuffs.Y), false);
        _stun.TryStun(uid, TimeSpan.FromSeconds(debuffs.X), false);

        return true;
    }

}

