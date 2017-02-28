// <auto-generated/>
using System;

namespace Telegram.Api.TL.Methods.Messages
{
	/// <summary>
	/// RCP method messages.readFeaturedStickers.
	/// Returns <see cref="Telegram.Api.TL.TLBoolBase"/>
	/// </summary>
	public partial class TLMessagesReadFeaturedStickers : TLObject
	{
		public TLVector<Int64> Id { get; set; }

		public TLMessagesReadFeaturedStickers() { }
		public TLMessagesReadFeaturedStickers(TLBinaryReader from)
		{
			Read(from);
		}

		public override TLType TypeId { get { return TLType.MessagesReadFeaturedStickers; } }

		public override void Read(TLBinaryReader from)
		{
			Id = TLFactory.Read<TLVector<Int64>>(from);
		}

		public override void Write(TLBinaryWriter to)
		{
			to.Write(0x5B118126);
			to.WriteObject(Id);
		}
	}
}