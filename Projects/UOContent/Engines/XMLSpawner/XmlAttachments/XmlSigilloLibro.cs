using Server.Items;

namespace Server.Engines.XmlSpawner2
{
    public class XmlSigilloLibro : XmlAttachment
    {
        public XmlSigilloLibro(string attacherAccount)
        {
            Name = attacherAccount;
        }

        public XmlSigilloLibro(ASerial serial) : base(serial)
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

        public override void OnDelete()
        {
            if (AttachedTo is BaseBook)
            {
                ((BaseBook)AttachedTo).Writable = true;
            }
        }

        public override void OnAttach()
        {
            if (AttachedTo is BaseBook)
            {
                ((BaseBook)AttachedTo).Writable = false;
            }
            else
            {
                Delete();
            }
        }
    }
}