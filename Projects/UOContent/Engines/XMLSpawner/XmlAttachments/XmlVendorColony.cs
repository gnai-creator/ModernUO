using Server.Mobiles;
using Server.Towns;

namespace Server.Engines.XmlSpawner2
{
    public class XmlVendorColony : XmlAttachment
    {
        private Town m_OwnerGuild;
        [CommandProperty(AccessLevel.Counselor)]
        public Town OwnerGuild => m_OwnerGuild;

        [Attachable]
        public XmlVendorColony(int colony)
        {
            m_OwnerGuild = Town.Find(colony);
        }

        public XmlVendorColony(ASerial serial) : base(serial)
        {
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // announce it to the mob
            if (m_OwnerGuild == null || (!(AttachedTo is Banker) && !(AttachedTo is BaseVendor)))
            {
                Delete();
            }
        }

        public override bool OnDragLift(Mobile from, Item item)
        {
            if (from.Town == m_OwnerGuild)
            {
                return true;
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(m_OwnerGuild);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            m_OwnerGuild = reader.ReadTown();
        }
    }
}