using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlRedenzioni : XmlAttachment
    {
        private string m_DataValue;
        private int m_Count = 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public string RedenzioniRicevute
        {
            get => string.Format("{0} - {1} - redenzioni ricevute (prima di cancellazione attach): {2}", Name, m_DataValue, m_Count);
            set { }
        }

        public override string AttachedBy => RedenzioniRicevute;
        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlRedenzioni(ASerial serial) : base(serial)
        {
        }

        public XmlRedenzioni(string name, string data, TimeSpan expiresin)
        {
            Name = name;
            m_DataValue = data;
            Expiration = expiresin;
            ++m_Count;
        }

        public override void OnBeforeReattach(XmlAttachment old)
        {
            base.OnBeforeReattach(old);
            if (old is XmlRedenzioni)
            {
                m_Count += ((XmlRedenzioni)old).m_Count;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(m_DataValue);
            writer.Write(m_Count);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            // version 0
            m_DataValue = reader.ReadString();
            m_Count = reader.ReadInt();
        }
    }
}
