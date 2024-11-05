using Content.Shared.Hands.Components;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Server.Imperial.DoubleEnergySword
{
    public sealed class DoubleEnergySwordSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ItemToggleComponent, ItemToggleActivateAttemptEvent>(OnItemToggleActivateAttempt);
        }

        private void OnItemToggleActivateAttempt(EntityUid uid, ItemToggleComponent component, ref ItemToggleActivateAttemptEvent args)
        {
            if (!IsDoubleEnergySword(uid))
                return;

            component.Predictable = false;

            if (args.User == null || HasFreeHand(args.User.Value))
                return;

            args.Cancelled = true;


        }

        private bool IsDoubleEnergySword(EntityUid uid)
        {
            if (_entityManager.TryGetComponent(uid, out MetaDataComponent? metaData))
            {
                return metaData.EntityPrototype?.ID == "EnergySwordDouble";
            }

            return false;
        }

        private bool HasFreeHand(EntityUid user)
        {
            if (_entityManager.TryGetComponent(user, out HandsComponent? hands))
            {
                return hands.CountFreeHands() > 0;
            }

            return false;
        }
    }
}
