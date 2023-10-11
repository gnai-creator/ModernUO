using Server.Items;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlGargish : XmlAttachment
    {
        private TimeSpan m_Duration = TimeSpan.FromSeconds(240.0);       // default 240 sec duration
        private int m_Value = 10;       // default value of 30
        private Item m_wing;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Value { get => m_Value; set => m_Value = value; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlGargish(ASerial serial) : base(serial)
        {
        }


        public XmlGargish()
        {
        }

        [Attachable]
        public XmlGargish(int value)
        {
            m_Value = value;
        }

        [Attachable]
        public XmlGargish(int value, double duration)
        {
            m_Value = value;
            m_Duration = TimeSpan.FromSeconds(duration);
        }
        public override void OnAttach()
        {
            // apply the mod

            if (AttachedTo is Mobile m)
            {
                m.AddStatMod(new StatMod(StatType.Str, "Gargish", m_Value, m_Duration));                                                                                                                                                                                                                                                                                                                                                                                       
                m.PlaySound(0x19E);
                m.FixedParticles(0x3709, 1, 30, 9904, 1108, 6, EffectLayer.RightFoot);
                m_wing = new DaemonWing();
                m.EquipItem(m_wing);
                Expiration = m_Duration;
            }
        }

        public override void OnDelete()
        {
            if (AttachedTo is Mobile m)
            {
                ((Mobile)AttachedTo).RemoveStatMod("Gargish");
                if (m_wing != null)
                {
                    m_wing.Delete();
                }
            }
        }
    }
}