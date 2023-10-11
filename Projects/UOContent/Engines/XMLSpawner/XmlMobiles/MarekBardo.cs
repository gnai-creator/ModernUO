using Server.Gumps;

namespace Server.Mobiles
{
    public class XmlBardoMarek : XmlQuestNPC
    {
        [Constructable]
        public XmlBardoMarek() : base(0)
        {
            Blessed = true;
            CantWalk = true;
        }

        public XmlBardoMarek(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile SendGumpTo
        {
            get => null;
            set
            {
                if (value != null && !value.Deleted)
                {
                    value.SendGump(new PremioBardoSpellGump(value));
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}