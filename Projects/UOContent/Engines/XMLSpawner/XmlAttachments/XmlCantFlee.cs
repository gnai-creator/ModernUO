using Server.Mobiles;

namespace Server.Engines.XmlSpawner2
{
    public class XmlCantFlee : XmlAttachment
    {
        // a serial constructor is REQUIRED
        public XmlCantFlee(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlCantFlee()
        {
        }

        public override void OnAttach()
        {
            base.OnAttach();

            if (!(AttachedTo is BaseCreature))
            {
                Delete();
            }
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