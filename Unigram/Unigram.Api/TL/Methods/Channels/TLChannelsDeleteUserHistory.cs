// <auto-generated/>
using System;

namespace Telegram.Api.TL.Methods.Channels
{
	/// <summary>
	/// RCP method channels.deleteUserHistory.
	/// Returns <see cref="Telegram.Api.TL.TLMessagesAffectedHistory"/>
	/// </summary>
	public partial class TLChannelsDeleteUserHistory : TLObject
	{
		public TLInputChannelBase Channel { get; set; }
		public TLInputUserBase UserId { get; set; }

		public TLChannelsDeleteUserHistory() { }
		public TLChannelsDeleteUserHistory(TLBinaryReader from)
		{
			Read(from);
		}

		public override TLType TypeId { get { return TLType.ChannelsDeleteUserHistory; } }

		public override void Read(TLBinaryReader from)
		{
			Channel = TLFactory.Read<TLInputChannelBase>(from);
			UserId = TLFactory.Read<TLInputUserBase>(from);
		}

		public override void Write(TLBinaryWriter to)
		{
			to.Write(0xD10DD71B);
			to.WriteObject(Channel);
			to.WriteObject(UserId);
		}
	}
}