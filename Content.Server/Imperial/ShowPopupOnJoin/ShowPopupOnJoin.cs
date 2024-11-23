using Content.Shared.Imperial.ICCVar;
using Content.Shared.Imperial.ShowPopupOnJoin;
using Microsoft.EntityFrameworkCore.Query;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

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
        var data = new PopupContentMessage()
        {
            Content = _config.GetCVar(ICCVars.ShowPopupOnJoin.Content),
            Title = _config.GetCVar(ICCVars.ShowPopupOnJoin.Title),
            Link = _config.GetCVar(ICCVars.ShowPopupOnJoin.Link),
            QRData = _config.GetCVar(ICCVars.ShowPopupOnJoin.QR)
        };

        if (string.IsNullOrEmpty(data.Content) &&
            string.IsNullOrEmpty(data.Title) &&
            string.IsNullOrEmpty(data.Link) &&
            string.IsNullOrEmpty(data.QRData)
        )
            return;

        _netManager.ServerSendMessage(data, msg.MsgChannel);
    }
}
