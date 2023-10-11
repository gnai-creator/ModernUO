using Server.Mobiles;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlChampionLoot : XmlAttachment
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public double SpecificaPercent { get; private set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public double ExpVialPercent { get; private set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public int MinGold { get; private set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxGold { get; private set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public int Area { get; private set; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlChampionLoot(ASerial serial)
            : base(serial)
        {
        }

        [Attachable]
        public XmlChampionLoot(int area, int mingold, int maxgold) : this(area, mingold, maxgold, 0.0)
        {
        }

        [Attachable]
        public XmlChampionLoot(int area, int mingold, int maxgold, double expvialpercent)
        {
            Area = Math.Max(0, Math.Min(16, area));
            MinGold = Math.Max(0, Math.Min(500, Math.Min(mingold, maxgold)));
            MaxGold = Math.Max(0, Math.Min(1000, Math.Max(mingold, maxgold)));
            ExpVialPercent = Math.Max(0.0, Math.Min(1.0, expvialpercent));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            writer.Write(Area);
            writer.Write(MinGold);
            writer.Write(MaxGold);
            writer.Write(ExpVialPercent);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            Area = reader.ReadInt();
            MinGold = reader.ReadInt();
            MaxGold = reader.ReadInt();
            ExpVialPercent = reader.ReadDouble();
        }

        public override void OnAttach()
        {
            base.OnAttach();
            if (Name != null || !(AttachedTo is BaseCreature))
            {
                // dont allow item or non null name attachments
                Delete();
            }
        }

        public override bool HandlesOnKilled => true;

        public override void OnKilled(Mobile killed, Mobile killer, bool last)
        {
            if (Deleted)
            {
                return;
            }

            base.OnKilled(killed, killer, last);

            if (killed == null || killer == null)
            {
                return;
            }

            Map map = killed.Map;
            int iX = killed.X;
            int iY = killed.Y;

            LootPack.ChampGoodies(Area, map, iX, iY, MinGold, MaxGold, ExpVialPercent);
            Delete();
        }
    }
}
