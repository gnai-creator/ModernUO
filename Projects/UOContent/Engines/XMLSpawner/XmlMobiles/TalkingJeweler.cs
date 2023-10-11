using System.Collections.Generic;

namespace Server.Mobiles
{
    public class TalkingJeweler : TalkingBaseVendor
    {
        private List<SBInfo> m_SBInfos = new List<SBInfo>();
        protected override List<SBInfo> SBInfos => m_SBInfos;

        [Constructable]
        public TalkingJeweler() : base("the jeweler")
        {
            SetSkill(SkillName.ItemID, 64.0, 100.0);
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBJewel());
        }

        //		public override bool OnBuyItems(Mobile buyer, List<BuyItemResponse> list)
        //		{
        //			foreach ( BuyItemResponse buy in list )
        //			{
        //				if(buy.GetType() == typeof(
        //			}
        //			
        //			return base.OnBuyItems(buyer, list);
        //		}

        public TalkingJeweler(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}
