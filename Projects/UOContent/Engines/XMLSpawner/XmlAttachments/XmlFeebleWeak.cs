//using System;
//using Server;
//using Server.Items;
//using Server.Network;
//using Server.Mobiles;
//
//namespace Server.Engines.XmlSpawner2
//{
//    public class XmlFeebleWeak : XmlAttachment
//    {
//        // a serial constructor is REQUIRED
//        public XmlFeebleWeak(ASerial serial) : base(serial)
//        {
//        }
//
//        [Attachable]
//        public XmlFeebleWeak()
//        {
//        }
//
//        [Attachable]
//        public XmlFeebleWeak(int value)
//        {
//            m_Value = value;
//        }
//        
//        [Attachable]
//        public XmlFeebleWeak(int value, double duration)
//        {
//            m_Value = value;
//            m_Duration = TimeSpan.FromSeconds(duration);
//        }
//
//		public override void OnAttach()
//		{
//		    base.OnAttach();
//
//		    // apply the mod
//            if(AttachedTo is Mobile)
//            {
//                ((Mobile)AttachedTo).AddStatMod( new StatMod( StatType.Dex, "XmlDex"+Name, m_Value, m_Duration ) );
//            }
//            // and then remove the attachment
//			Timer.DelayCall(TimeSpan.Zero, new TimerCallback(Delete));
//            //Delete();
//		}
//    }
//}
