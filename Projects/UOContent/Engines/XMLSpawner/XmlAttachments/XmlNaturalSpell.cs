using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlNaturalSpell : XmlAttachment
    {
        private TimeSpan m_Duration = TimeSpan.FromSeconds(240.0);       // default 240 sec duration
        private int m_Value = 15;       // default value of 30

        [CommandProperty(AccessLevel.GameMaster)]
        public int Value { get => m_Value; set => m_Value = value; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlNaturalSpell(ASerial serial) : base(serial)
        {
        }


        public XmlNaturalSpell()
        {
        }

        [Attachable]
        public XmlNaturalSpell(int value)
        {
            m_Value = value;
        }

        [Attachable]
        public XmlNaturalSpell(int value, double duration)
        {
            m_Value = value;
            m_Duration = TimeSpan.FromSeconds(duration);
        }

        public override void OnAttach()
        {
            // apply the mod

            if (AttachedTo is Mobile m)
            {
                m.AddStatMod(new StatMod(StatType.Str, "NaturalSpell_Str", m_Value, m_Duration));
                m.AddStatMod(new StatMod(StatType.Int, "NaturalSpell_Int", m_Value, m_Duration));
                m.Hue = 1946;
                m.PlaySound(0x5C6);
                m.FixedParticles(0x3709, 1, 30, 9904, 1108, 6, EffectLayer.RightFoot);
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
                m.RemoveStatMod("NaturalSpell_Str");
                m.RemoveStatMod("NaturalSpell_Int");
                m.Hue = Utility.RandomMinMax(0x741, 0x745);
            }
        }
    }
}