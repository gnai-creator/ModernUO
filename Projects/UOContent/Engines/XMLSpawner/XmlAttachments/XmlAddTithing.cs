using Server.Mobiles;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlAddTithing : XmlAttachment
    {
        private int m_DataValue;    // default data

        [CommandProperty(AccessLevel.GameMaster)]
        public int Value { get => m_DataValue; set => m_DataValue = value; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlAddTithing(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlAddTithing(int value)
        {
            Value = value;
        }


        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(m_DataValue);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            // version 0
            m_DataValue = reader.ReadInt();
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // apply the mod
            if (AttachedTo is PlayerMobile pm)
            {
                // for players just add it immediately
                pm.FaithPoints += Value;

                pm.SendLocalizedMessage(1005130, Value.ToString());

                // and then remove the attachment
                Timer.DelayCall(TimeSpan.Zero, new TimerCallback(Delete));
                //Delete();
            }
            else if (AttachedTo is Item)
            {
                // dont allow item attachments
                Delete();
            }

        }

        public override bool HandlesOnKilled => true;

        public override void OnKilled(Mobile killed, Mobile killer, bool last)
        {
            base.OnKilled(killed, killer, last);

            if (killer == null)
            {
                return;
            }

            killer.FaithPoints += Value;

            killer.SendLocalizedMessage(1005130, Value.ToString());
        }


        public override LogEntry OnIdentify(Mobile from)
        {
            return new LogEntry(1005129, Value.ToString());
        }
    }
}
