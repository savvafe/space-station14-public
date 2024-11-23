using Content.Client.Imperial.ShowPopupOnJoin.UI;
using Content.Shared.CCVar;
using Content.Shared.Imperial.ICCVar;
using Content.Shared.Imperial.ShowPopupOnJoin;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.Imperial.ShowPopupOnJoin;

public sealed class ShowPopupOnJoin : SharedShowPopupOnJoin
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    public void Initialize()
    {
        _netManager.RegisterNetMessage<RequestPopupContentMessage>();
        _netManager.RegisterNetMessage<PopupContentMessage>(OnPopupContentMessage);
    }

    public void Open()
    {
        if (!_configManager.GetCVar(ICCVars.ShowPopupOnJoin.Enabled))
            return;

        _netManager.ClientSendMessage(new RequestPopupContentMessage());
    }

    void OnPopupContentMessage(PopupContentMessage message) =>
        new PopupWindow(message).OpenCentered();
}
