using Server.Mobiles;

namespace Server.Engines.XmlSpawner2
{
    public class XmlCorpseName : XmlAttachment
    {
        [Attachable]
        public XmlCorpseName(string name)
        {
            Name = name;
        }

        public XmlCorpseName(ASerial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }

        public override void OnAttach()
        {
            if (!(AttachedTo is BaseCreature))
            {
                Delete();
            }
        }
    }
}