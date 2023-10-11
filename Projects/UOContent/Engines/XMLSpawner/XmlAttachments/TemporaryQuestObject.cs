using Server.Items;
using System;

namespace Server.Engines.XmlSpawner2
{
    // When this attachment is deleted, the object that it is attached to will be deleted as well.
    // The quest system will automatically delete these attachments after a quest is completed.
    // Specifying an expiration time will also allow you to give objects limited lifetimes.
    public class TemporaryQuestObject : XmlAttachment, ITemporaryQuestAttachment
    {

        private Mobile m_QuestOwner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile QuestOwner
        {
            get => m_QuestOwner;
            set => m_QuestOwner = value;
        }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public TemporaryQuestObject(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public TemporaryQuestObject(string questname)
        {
            Name = questname;
        }

        [Attachable]
        public TemporaryQuestObject(string questname, double expiresin)
        {
            Name = questname;
            Expiration = TimeSpan.FromMinutes(expiresin);
        }

        [Attachable]
        public TemporaryQuestObject(string questname, double expiresin, Mobile questowner)
        {
            Name = questname;
            Expiration = TimeSpan.FromMinutes(expiresin);
            QuestOwner = questowner;
        }

        public override void OnDelete()
        {
            base.OnDelete();

            // delete the object that it is attached to
            if (AttachedTo is Mobile m)
            {
                // dont allow deletion of players
                if (!m.Player)
                {
                    SafeMobileDelete(m);
                    //((Mobile)AttachedTo).Delete();
                }
            }
            else if (AttachedTo is Item it)
            {
                SafeItemDelete(it);
                //((Item)AttachedTo).Delete();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            // version 0
            writer.Write(m_QuestOwner);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();

            // version 0
            m_QuestOwner = reader.ReadMobile();

        }

        public override void AddProperties(ObjectPropertyList list)
        {
            if (Expiration > TimeSpan.Zero)//{0:MM/dd/yy H:mm:ss zzz}
            {
                list.Add(1005189, string.Format("{0:dd MMM HH:mm} UTC", ExpirationEnd));//Sparirà il: ~1_val~
            }
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            if (from == null)
            {
                return null;
            }

            if (from.AccessLevel == AccessLevel.Player)
            {
                return new LogEntry(1005124, string.Format("{0}\t{1}\t{2}", Expiration.Days, Expiration.Hours, Expiration.Minutes));
            }
            else if (Expiration > TimeSpan.Zero)
            {
                return new LogEntry(LocalizerDef(), string.Format("{1} finisce in {0} min.", Expiration.TotalMinutes, Name));
            }
            else
            {
                return new LogEntry(LocalizerDef(), string.Format("{1}: QuestOwner {0}", QuestOwner, Name));
            }
        }
    }
}
