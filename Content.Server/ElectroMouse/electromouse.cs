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
using Content.Server.Light.Components;
using Content.Server.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using System.Linq;
using Content.Shared.Doors.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.Damage;
using Content.Shared.ElectroMouseHarvested;

namespace Content.Server.ElectroMouse.EntitySystems;


public sealed partial class ElectroMouseSystem : EntitySystem
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
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public Vector2? Coordinates;
    [ValidatePrototypeId<EntityPrototype>]
    private const string RevenantShopId = "ActionElectroMouseShop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectroMouseComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseShopActionEvent>(OnShop);
        SubscribeLocalEvent<ElectroMouseComponent, InteractNoHandEvent>(OnInteract);
        SubscribeLocalEvent<ElectroMouseComponent, HarvestEvent>(OnHarvest);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseOverloadLightsActionEvent>(OnOverloadLightsAction);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseDashEvent>(DashAbility);
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
    private void DashAbility(EntityUid uid, ElectroMouseComponent comp, ElectroMouseDashEvent args)
    {
        if (!HasComp<ElectroMouseComponent>(uid))
        {
            return;
        }
        var entityManager = IoCManager.Resolve<IEntityManager>();

        var xformSystem = entityManager.System<TransformSystem>();

        var energy = 2;

        var mapPosition = xformSystem.GetWorldPosition(uid);
        var reactionBounds = new Box2(mapPosition - new Vector2(energy, energy), mapPosition + new Vector2(energy, energy));

        var newPosition = Coordinates;

        newPosition = GetPositionFromRotation(reactionBounds, energy, uid);

        if (newPosition != null)
            xformSystem.SetWorldPosition(
                uid,
                (Vector2) newPosition
            );
        _audio.PlayPvs(comp.BlinkSound, uid);
    }
    private static Vector2 GetPositionFromRotation(Box2 reactionBounds, float energy, EntityUid uid)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();

        var xformSystem = entityManager.System<TransformSystem>();

        var resultVector = Angle.FromDegrees(45).RotateVec(
            xformSystem.GetWorldRotation(uid).RotateVec(new Vector2(energy, energy))
        );

        return reactionBounds.Center - resultVector;
    }
    public bool ChangeEnergyAmount(EntityUid uid, FixedPoint2 amount, ElectroMouseComponent? component = null, bool allowDeath = true, bool regenCap = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!allowDeath && component.Energy + amount <= 0)
            return false;
        if (!HasComp<DamageableComponent>(uid))
            return false;
        component.Energy += amount;

        if (TryComp<StoreComponent>(uid, out var store))
            _store.UpdateUserInterface(uid, uid, store);

        _alerts.ShowAlert(uid, AlertType.Essence, (short) Math.Clamp(Math.Round(component.Energy.Float() / 10f), 0, 16));

        return true;
    }
    private void AddEnergy(EntityUid uid, ElectroMouseComponent component, int energ)
    {
        var store = EnsureComp<StoreComponent>(uid);
        // Get the ElectroMouseComponent instance from the entity
        if (EntityManager.TryGetComponent(uid, out ElectroMouseComponent? electroMouseComponent))
        {
            ChangeEnergyAmount(uid, electroMouseComponent.Energy, component);
            _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { component.StolenEnergyCurrencyPrototype, energ } }, uid, store);
        }
    }

    private void OnInteract(EntityUid uid, ElectroMouseComponent component, InteractNoHandEvent args)
    {
        if (args.Target == null)
            return;
        var target = args.Target.Value;
        if (HasComp<PoweredLightComponent>(target))
        {
            args.Handled = _ghost.DoGhostBooEvent(target);
            return;
        }
        if (HasComp<ElectroMouseHarvestedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("electromouse-harvested"), uid, uid);
            return;
        }
        if (!HasComp<ApcPowerReceiverComponent>(target))
            return;
        if (HasComp<DoorComponent>(target))
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
        if (args.Handled || args.Target == null || !HasComp<ApcPowerReceiverComponent>(args.Target))
            return;
        var target = args.Target.Value;
        _appearance.SetData(uid, RevenantVisuals.Harvesting, false);
        _popup.PopupEntity(Loc.GetString("revenant-soul-finish-harvest", ("target", target)),
            target, PopupType.Large);
        AddComp<ElectroMouseHarvestedComponent>(target);
        AddEnergy(uid, component, 5);
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

        ChangeEnergyAmount(uid, abilityCost, component, false);

        _statusEffects.TryAddStatusEffect<CorporealComponent>(uid, "Corporeal", TimeSpan.FromSeconds(debuffs.Y), false);
        _stun.TryStun(uid, TimeSpan.FromSeconds(debuffs.X), false);

        return true;
    }
    private void OnOverloadLightsAction(EntityUid uid, ElectroMouseComponent component, ElectroMouseOverloadLightsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.OverloadCost, component.OverloadDebuffs))
            return;

        args.Handled = true;

        var xform = Transform(uid);
        var poweredLights = GetEntityQuery<PoweredLightComponent>();
        var mobState = GetEntityQuery<MobStateComponent>();
        var lookup = _lookup.GetEntitiesInRange(uid, component.OverloadRadius);
        //TODO: feels like this might be a sin and a half
        foreach (var ent in lookup)
        {
            if (!HasComp<MobStateComponent>(ent)) continue; // Imperial Space overload-lights-fix
            if (!_mobState.IsAlive(ent)) continue; // Imperial Space overload-lights-fix

            var nearbyLights = _lookup.GetEntitiesInRange(ent, component.OverloadZapRadius) // Imperial Space overload-lights-fix
                .Where(e => !HasComp<RevenantOverloadedLightsComponent>(e) &&
                            _interact.InRangeUnobstructed(e, uid, -1)).ToArray();

            if (!nearbyLights.Any())
                continue;

            //get the closest light
            var allLight = nearbyLights.OrderBy(e =>
                Transform(e).Coordinates.TryDistance(EntityManager, xform.Coordinates, out var dist) ? component.OverloadZapRadius : dist);
            var comp = EnsureComp<RevenantOverloadedLightsComponent>(allLight.First());
            comp.Target = ent; //who they gon fire at?
        }
    }
}

