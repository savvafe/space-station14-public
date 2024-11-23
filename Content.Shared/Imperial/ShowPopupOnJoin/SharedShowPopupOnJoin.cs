using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Imperial.ShowPopupOnJoin;

public abstract class SharedShowPopupOnJoin
{
    public sealed class RequestPopupContentMessage : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;
        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) { }
        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) { }
    }

    public sealed class PopupContentMessage : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Link { get; set; } = "";
        /// <summary>
        /// Бинарное квадратное изображение QR кода
        /// </summary>
        public string QRData { get; set; } = "";

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            Title = buffer.ReadString();
            Content = buffer.ReadString();
            Link = buffer.ReadString();

            QRData = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(Title);
            buffer.Write(Content);
            buffer.Write(Link);

            buffer.Write(QRData);
        }
    }
}
