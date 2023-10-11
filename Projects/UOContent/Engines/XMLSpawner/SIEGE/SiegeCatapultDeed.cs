using Server.Multis;
using Server.Targeting;

namespace Server.Items
{
    public class SiegeCatapultDeed : Item
    {
        public override int LabelNumber => 504500;
        [Constructable]
        public SiegeCatapultDeed() : base(0x14F0)
        {
            Hue = 0x488;
            Weight = 30.0f;
            LootType = LootType.Regular;
        }

        public SiegeCatapultDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            if (version < 1)
            {
                Name = null;
                Weight = 30.0f;
            }
        }

        public bool ValidatePlacement(Mobile from, Point3D loc)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            if (!from.InRange(GetWorldLocation(), 1))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return false;
            }

            Map map = from.Map;

            if (map == null)
            {
                return false;
            }

            BaseHouse house = BaseHouse.FindHouseAt(loc, map, 20);

            if (house != null && !house.IsFriend(from))
            {
                from.SendLocalizedMessage(500269); // You cannot build that there.
                return false;
            }

            BaseBoat boat = BaseBoat.FindBoatAt(loc, map);

            if (boat != null)
            {
                from.SendLocalizedMessage(500269); // You cannot build that there.
                return false;
            }

            if (!map.CanFit(loc, 20))
            {
                from.SendLocalizedMessage(500269); // You cannot build that there.
                return false;
            }

            return true;
        }

        public void BeginPlace(Mobile from)
        {
            from.BeginTarget(-1, true, TargetFlags.None, new TargetCallback(Placement_OnTarget));
        }

        public void Placement_OnTarget(Mobile from, object targeted)
        {
            if (!(targeted is IPoint3D p))
            {
                return;
            }

            Point3D loc = new Point3D(p);
            if (!from.InRange(loc, 6))
            {
                from.SendLocalizedMessage(504499);//"Il punto è troppo lontano!");
                return;
            }

            if (p is StaticTarget)
            {
                loc.Z -= TileData.ItemTable[((StaticTarget)p).ItemID & TileData.MaxItemValue].CalcHeight; /* NOTE: OSI does not properly normalize Z positioning here.
																							* A side affect is that you can only place on floors (due to the CanFit call).
																							* That functionality may be desired. And so, it's included in this script.
																							*/
            }

            if (ValidatePlacement(from, loc))
            {
                EndPlace(from, loc);
            }
        }

        public void EndPlace(Mobile from, Point3D loc)
        {
            if (from == null)
            {
                return;
            }

            Delete();
            new SiegeCatapult
            {
                Facing = Direzionatore.TargetDirectionHandle(from, loc),
                Location = loc,
                Map = from.Map
            };
        }

        public override void OnDoubleClick(Mobile from)
        {
            BeginPlace(from);
        }
    }
}
