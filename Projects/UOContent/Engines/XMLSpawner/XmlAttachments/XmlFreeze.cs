using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlFreeze : XmlAttachment
    {

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlFreeze(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlFreeze()
        {
        }

        [Attachable]
        public XmlFreeze(double seconds)
        {
            Expiration = TimeSpan.FromSeconds(seconds);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            if (from == null || from.AccessLevel == AccessLevel.Player)
            {
                return null;
            }

            if (Expiration > TimeSpan.Zero)
            {
                return new LogEntry(LocalizerDef(), string.Format("Freeze expires in {0} secs", Expiration.TotalSeconds));
            }
            else
            {
                return new LogEntry(LocalizerDef(), "Frozen");
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();

            // remove the mod
            if (AttachedTo is Mobile)
            {
                ((Mobile)AttachedTo).Frozen = false;
            }
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // apply the mod
            if (AttachedTo is Mobile)
            {
                ((Mobile)AttachedTo).Frozen = true;
                ((Mobile)AttachedTo).ProcessDelta();
            }
            else
            {
                Delete();
            }
        }

    }
}
