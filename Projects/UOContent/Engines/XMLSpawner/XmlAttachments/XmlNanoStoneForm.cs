using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlNanoStoneForm : XmlAttachment
    {
        private TimeSpan m_Duration = TimeSpan.FromSeconds(120.0);       // default 120 sec duration
        private int m_Value = 5;       // default value of 10

        [CommandProperty(AccessLevel.GameMaster)]
        public int Value { get => m_Value; set => m_Value = value; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlNanoStoneForm(ASerial serial) : base(serial)
        {
        }


        public XmlNanoStoneForm()
        {
        }

        [Attachable]
        public XmlNanoStoneForm(int value)
        {
            m_Value = value;
        }

        [Attachable]
        public XmlNanoStoneForm(int value, double duration)
        {
            m_Value = value;
            m_Duration = TimeSpan.FromSeconds(duration);
        }

        public override void OnAttach()
        {
            // apply the mod

            if (AttachedTo is Mobile m)
            {
                m.AddStatMod(new StatMod(StatType.Str, "StoneForm", m_Value, m_Duration));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Energy, +1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Fire, +1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Poison, +1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Cold, +1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Physical, +30));
                m.Hue = 2171;
                m.PlaySound(0x1F6);
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
                ((Mobile)AttachedTo).RemoveStatMod("StoneForm");
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Energy, -1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Fire, -1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Poison, -1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Cold, -1));
                m.AddResistanceMod(new ResistanceMod(ResistanceType.Physical, -30));
                m.Hue = Utility.RandomMinMax(0x748, 0x74D);
            }
        }
    }
}