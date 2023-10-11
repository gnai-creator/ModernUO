using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlUrloGorbak : XmlAttachment
    {
        private TimeSpan m_Duration = TimeSpan.FromSeconds(240.0);       // default 60 sec duration
        private int m_Value = 5;       // default value of 5

        [CommandProperty(AccessLevel.GameMaster)]
        public int Value { get => m_Value; set => m_Value = value; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlUrloGorbak(ASerial serial) : base(serial)
        {
        }


        public XmlUrloGorbak()
        {
        }

        [Attachable]
        public XmlUrloGorbak(int value)
        {
            m_Value = value;
        }

        [Attachable]
        public XmlUrloGorbak(int value, double duration)
        {
            m_Value = value;
            m_Duration = TimeSpan.FromSeconds(duration);
        }

        public override void OnAttach()
        {
            // apply the mod
            if (AttachedTo is Mobile m)
            {
                m.AddStatMod(new StatMod(StatType.Dex, "UrloGorbak", m_Value, m_Duration));
                if (m.Hits < 80)
                {
                    m.AddStatMod(new StatMod(StatType.Dex, "UrloGorbak", m_Value, m_Duration));
                    m.Hits = m.Hits + 30;
                    m.PlaySound(0x1AB);
                }
                else
                {
                    m.PlaySound(0x1B0);
                }
                m.PublicOverheadMessage(Server.Network.MessageType.Regular, 0x22, 506099);//true, "*Urla*");
                m.Hue = 1998;
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
                ((Mobile)AttachedTo).RemoveStatMod("UrloGorbak");
                m.Hue = Utility.RandomMinMax(0x416, 0x41B);
            }
        }
    }
}