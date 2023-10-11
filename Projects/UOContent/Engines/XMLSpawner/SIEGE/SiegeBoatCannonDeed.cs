using Server.Multis;
using Server.Targeting;
using System;

namespace Server.Items
{
    public class SiegeBoatCannonDeed : Item
    {
        public override int LabelNumber => 504497;
        [Constructable]
        public SiegeBoatCannonDeed() : base(0x14F0)
        {
            Hue = 0x488;
            Weight = 25.0f;
            LootType = LootType.Regular;
        }

        public SiegeBoatCannonDeed(Serial serial)
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
                Weight = 25f;
            }
        }

        public void BeginPlace(Mobile from)
        {
            from.BeginTarget(-1, true, TargetFlags.None, new TargetCallback(Placement_OnTarget));
        }

        public void Placement_OnTarget(Mobile from, object targeted)
        {
            Map map = from.Map;
            if (map == null)
            {
                return;
            }

            if (!(targeted is IPoint3D p))
            {
                return;
            }

            Point3D loc = new Point3D(p);
            if (!from.InRange(loc, 2))
            {
                from.SendLocalizedMessage(504499);//"Il punto è troppo lontano!");
                return;
            }

            if (p is Plank)
            {
                from.SendLocalizedMessage(504517);//"Se lo piazzi li, come pensi di scendere dopo?");
            }
            else if (p is StaticTarget)
            {
                StaticTarget st = (StaticTarget)p;
                loc.Z -= TileData.ItemTable[st.ItemID & TileData.MaxItemValue].CalcHeight;

                BaseBoat boat = BaseBoat.FindBoatAt(loc, map);
                loc.Z += 1; //nudge-up

                if (boat != null)
                {
                    //un accrocco, ma o così o niente da fare ;)
                    if (st.ItemID == 16049)
                    {
                        EndPlace(from, loc, 0, boat);
                        return;
                    }
                    else if (st.ItemID == 16010)
                    {
                        EndPlace(from, loc, 1, boat);
                        return;
                    }
                    else if (st.ItemID == 16041 || st.ItemID == 16050 || st.ItemID == 16100 || st.ItemID == 16102)
                    {
                        EndPlace(from, loc, 2, boat);
                        return;
                    }
                    else if (st.ItemID == 16005 || st.ItemID == 16007 || st.ItemID == 15937)
                    {
                        EndPlace(from, loc, 3, boat);
                        return;
                    }
                    else
                    {
                        from.SendLocalizedMessage(504518);//"Puoi piazzarlo solo lungo l'asse rinforzata laterale della barca, tranne che sull'asse per la discesa");
                    }
                }
            }
            else if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                EndPlace(from, loc, 0);
                return;
            }
            else
            {
                from.SendLocalizedMessage(504519);//"Puoi piazzarlo solo sulle navi!");
            }
        }

        public void EndPlace(Mobile from, Point3D loc, int facing)
        {
            EndPlace(from, loc, facing, null);
        }

        public void EndPlace(Mobile from, Point3D loc, int facing, BaseBoat boat)
        {
            if (from == null)
            {
                return;
            }

            if (this == null || Deleted)
            {
                return;
            }

            SiegeBoatCannon cannon = new SiegeBoatCannon();

            if (boat != null)
            {
                if (boat.SiegeWeapon.Count > 0)
                {
                    if (boat.MaxCannons <= boat.SiegeWeapon.Count)
                    {
                        cannon.Delete();
                        from.SendLocalizedMessage(504520);//"Non è possibile mettere altri cannoni qui sopra!");
                        return;
                    }

                    for (int i = boat.SiegeWeapon.Count - 1; i >= 0; --i)
                    {
                        if (loc == boat.SiegeWeapon[i].Location)
                        {
                            cannon.Delete();
                            from.SendLocalizedMessage(504520);//"Non è possibile mettere altri cannoni qui sopra!");
                            return;
                        }
                    }
                }

                boat.SiegeWeapon.Add(cannon);
            }

            Delete();

            cannon.NextFiring = TimeSpan.FromSeconds(cannon.WeaponLoadingDelay);
            cannon.Location = loc;
            cannon.Map = from.Map;
            cannon.Facing = facing;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                BeginPlace(from);
            }
        }
    }
}
