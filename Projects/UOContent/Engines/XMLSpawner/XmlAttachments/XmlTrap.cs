namespace Server.Engines.XmlSpawner2
{
    public class XmlTrap : XmlAttachment
    {
        [Attachable]
        public XmlTrap()
        {
        }

        // a serial constructor is REQUIRED
        public XmlTrap(ASerial serial) : base(serial)
        {
        }

        public override void OnAttach()
        {
            base.OnAttach();
        }

        public override void OnTrigger(object activator, Mobile from)
        {

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
    }
}