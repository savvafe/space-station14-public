using Content.Shared.Imperial.ICCVar;
using Content.Shared.Imperial.ShowPopupOnJoin;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Imperial.ShowPopupOnJoin;

public sealed class ShowPopupOnJoin : SharedShowPopupOnJoin
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    public void Initialize()
    {
        _netManager.RegisterNetMessage<RequestPopupContentMessage>(OnRequestPopupContentMessage);
        _netManager.RegisterNetMessage<PopupContentMessage>();
    }

    void OnRequestPopupContentMessage(RequestPopupContentMessage msg)
    {
        _netManager.ServerSendMessage(new PopupContentMessage()
        {
            Content = _config.GetCVar(ICCVars.ShowPopupOnJoin.Content),
            Title = _config.GetCVar(ICCVars.ShowPopupOnJoin.Title),
            Link = _config.GetCVar(ICCVars.ShowPopupOnJoin.Link),
            QRData = _config.GetCVar(ICCVars.ShowPopupOnJoin.QR)
        }, msg.MsgChannel);
    }
}
