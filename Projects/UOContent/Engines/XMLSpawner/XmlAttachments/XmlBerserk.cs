using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlBerserk : XmlAttachment
    {
        private TimeSpan m_Duration = TimeSpan.FromSeconds(120.0);       // default 120 sec duration
        private int m_Value = 30;       // default value of 30
        private int dex_Value = -10;       // default value of 30

        [CommandProperty(AccessLevel.GameMaster)]
        public int Value { get => m_Value; set => m_Value = value; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlBerserk(ASerial serial) : base(serial)
        {
        }


        public XmlBerserk()
        {
        }

        [Attachable]
        public XmlBerserk(int value)
        {
            m_Value = value;
            dex_Value = value;
        }

        [Attachable]
        public XmlBerserk(int value, double duration)
        {
            m_Value = value;
            dex_Value = value;
            m_Duration = TimeSpan.FromSeconds(duration);
        }

        public override void OnAttach()
        {
            // apply the mod

            if (AttachedTo is Mobile m)
            {
                m.AddStatMod(new StatMod(StatType.Str, "Berserk", m_Value, m_Duration));
                m.AddStatMod(new StatMod(StatType.Dex, "Berserk2", dex_Value, m_Duration));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Fire, -1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Poison, -1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Physical, -100));
                m.Hue = 2145;
                m.PlaySound(0x19E);
                m.FixedParticles(0x3709, 1, 30, 9904, 1108, 6, EffectLayer.RightFoot);
                m.PublicOverheadMessage(Server.Network.MessageType.Regular, 0x22, 1075102);//true, "Muahahahaha!  I'll feast on your flesh.");
                Expiration = m_Duration;
            }
            else
            {
                Delete();
            }
        }

        public override void OnDelete()
        {
            if (AttachedTo is Mobile m)
            {
                ((Mobile)AttachedTo).RemoveStatMod("Berserk");
                ((Mobile)AttachedTo).RemoveStatMod("Berserk2");
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Fire, 1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Poison, 1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Physical, 100));
                m.Hue = Utility.RandomMinMax(0x741, 0x745);
            }
        }
    }
}