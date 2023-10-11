using Server.Gumps;

/*
** QuestRewardStone
** used to open the QuestPointsRewardGump that allows players to purchase rewards with their XmlQuestPoints Credits.
*/

namespace Server.Items
{
    public class QuestRewardStone : Item
    {
        [Constructable]
        public QuestRewardStone() : base(0xED4)
        {
            Movable = false;
            Name = "Quest Point Rewards";
        }

        public QuestRewardStone(Serial serial) : base(serial)
        {
        }

        public override bool HandlesOnMovement => true;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m.Player)
            {
                if (!m.InRange(Location, GrabRange))
                {
                    m.CloseGump(typeof(QuestRewardGump));
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

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), GrabRange))
            {
                from.SendGump(new QuestRewardGump(from, 0));
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }
    }
}
