using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlQuestAttachment : XmlAttachment
    {
        public DateTime Date { get; set; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlQuestAttachment(ASerial serial)
            : base(serial)
        {
        }

        [Attachable]
        public XmlQuestAttachment(string name)
        {
            Name = name;
            Date = DateTime.UtcNow;
        }

        [Attachable]
        public XmlQuestAttachment(string name, double expiresin)
        {
            Name = name;
            Date = DateTime.UtcNow;
            Expiration = TimeSpan.FromMinutes(expiresin);

        }

        [Attachable]
        public XmlQuestAttachment(string name, DateTime value, double expiresin)
        {
            Name = name;
            Date = value;
            Expiration = TimeSpan.FromMinutes(expiresin);

        }


        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(Date);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            // version 0
            Date = reader.ReadDateTime();
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            if (from == null || from.AccessLevel == AccessLevel.Player)
            {
                return null;
            }

            if (Expiration > TimeSpan.Zero)
            {
                //Quest '~1_val~' Completata ~2_val~ finisce in ~3_val~ min
                return new LogEntry(1005173, string.Format("{0}\t{1}\t{2:F2}", Name, Date, Expiration.TotalMinutes));
            }
            else
            {
                //Quest '~1_val~' Completata ~2_val~
                return new LogEntry(1005174, string.Format("{0}\t{1}", Name, Date));
            }
        }
    }
}
