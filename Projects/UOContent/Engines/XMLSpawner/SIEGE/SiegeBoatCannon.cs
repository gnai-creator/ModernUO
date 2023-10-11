using Server.Engines.XmlSpawner2;
using Server.Multis;
using System;

namespace Server.Items
{

    public class SiegeBoatCannon : BaseSiegeWeapon
    {
        public override int LabelNumber => 1023735;

        public override double WeaponLoadingDelay => 20.0;  // base delay for loading this weapon
        public override double WeaponStorageDelay => 15.0;  // base delay for packing away this weapon

        public override double RangeReductionWhenDamaged => 0.5;  //50% to 100% depending on damage taken by cannon
        public override double DamageReductionWhenDamaged => 0.6;  //60% to 100% depending on damage taken by cannon 

        //no override, rimane al 100% rispetto la norma!
        //public override double WeaponDamageFactor { get { return base.WeaponDamageFactor * 1; } } // damage multiplier for the weapon
        public override double WeaponRangeFactor => base.WeaponRangeFactor * 2;  //  range multiplier for the weapon

        public override int MinTargetRange => 1;  // target must be further away than this
        public override int MinStorageRange => 1;  // player must be at least this close to store the weapon
        public override int MinFiringRange => 3;  // player must be at least this close to fire the weapon

        public override bool IsDraggable { get => false; set { } }
        public override bool Parabola => false;  // whether the weapon needs to consider line of sight when selecting a target
        public override int StoredWeaponID => 8191;
        public override int BaseLenght => CannonNorth.Length;
        // facing 0
        public static int[] CannonWest = new int[] { 3735 };
        public static int[] CannonWestXOffset = new int[] { 1 };
        public static int[] CannonWestYOffset = new int[] { 0 };
        // facing 1
        public static int[] CannonNorth = new int[] { 3736 };
        public static int[] CannonNorthXOffset = new int[] { 0 };
        public static int[] CannonNorthYOffset = new int[] { 1 };
        // facing 2
        public static int[] CannonEast = new int[] { 3737 };
        public static int[] CannonEastXOffset = new int[] { 0 };
        public static int[] CannonEastYOffset = new int[] { 0 };
        // facing 3
        public static int[] CannonSouth = new int[] { 3738 };
        public static int[] CannonSouthXOffset = new int[] { 0 };
        public static int[] CannonSouthYOffset = new int[] { 0 };

        private Type[] m_allowedprojectiles = new Type[] { typeof(SiegeCannonball) };

        public override Type[] AllowedProjectiles => m_allowedprojectiles;

        [Constructable]
        public SiegeBoatCannon()
            : this(0)
        {
        }

        [Constructable]
        public SiegeBoatCannon(int facing)
        {
            Facing = facing;
            FixedFacing = true;
            // addon the components
            for (int i = 0; i < CannonNorth.Length; ++i)
            {
                AddComponent(new SiegeComponent(0, Name), 0, 0, 0);
            }

            // assign the facing
            if (facing < 0)
            {
                facing = 3;
            }

            if (facing > 3)
            {
                facing = 0;
            }

            // set the default props
            Weight = 50f;

            // make them siegable by default
            // XmlSiege( hitsmax, resistfire, resistphysical, wood, iron, stone, itemid da distrutto -- 0 = distrutto e basta)
            XmlAttach.AttachTo(this, new XmlSiege(1000, 250, 250, 20, 30, 0, 0, 0));

            // and draggable
            //XmlAttach.AttachTo(this, new XmlDrag());

            // undo the temporary hue indicator that is set when the xmlsiege attachment is added
            Hue = 0;
        }

        public SiegeBoatCannon(Serial serial)
            : base(serial)
        {
        }

        public override Point3D ProjectileLaunchPoint
        {
            get
            {
                if (Components != null && Components.Count > 0)
                {
                    switch (Facing)
                    {
                        case 0:
                            return new Point3D(CannonWestXOffset[0] + Location.X - 1, CannonWestYOffset[0] + Location.Y, Location.Z + 1);
                        case 1:
                            return new Point3D(CannonNorthXOffset[0] + Location.X - 1, CannonNorthYOffset[0] + Location.Y - 1, Location.Z + 1);
                        case 2:
                            return new Point3D(CannonEastXOffset[0] + Location.X, CannonEastYOffset[0] + Location.Y - 1, Location.Z + 1);
                        case 3:
                            return new Point3D(CannonSouthXOffset[0] + Location.X - 1, CannonSouthYOffset[0] + Location.Y, Location.Z + 1);
                    }
                }

                return (Location);
            }
        }

        public override void LaunchProjectile(Mobile from, Item projectile, IEntity target, Point3D targetloc, TimeSpan delay)
        {
            base.LaunchProjectile(from, projectile, target, targetloc, delay);

            SpecialEffects(from, projectile);
        }

        public override void SpecialEffects(Mobile from, Item projectile)
        {
            // show the cannon firing animation with explosion sound
            Effects.SendLocationEffect(this, Map, 0x36B0, 16, 1);
            Effects.PlaySound(this, Map, 0x11D);
        }

        public override void UpdateDisplay()
        {
            if (Components != null && Components.Count > 0)
            {
                int z = Components[0].Location.Z;

                int[] itemid = null;
                int[] xoffset = null;
                int[] yoffset = null;

                switch (Facing)
                {
                    case 0: // West
                        itemid = CannonWest;
                        xoffset = CannonWestXOffset;
                        yoffset = CannonWestYOffset;
                        break;
                    case 1: // North
                        itemid = CannonNorth;
                        xoffset = CannonNorthXOffset;
                        yoffset = CannonNorthYOffset;
                        break;
                    case 2: // East
                        itemid = CannonEast;
                        xoffset = CannonEastXOffset;
                        yoffset = CannonEastYOffset;
                        break;
                    case 3: // South
                        itemid = CannonSouth;
                        xoffset = CannonSouthXOffset;
                        yoffset = CannonSouthYOffset;
                        break;
                }

                if (itemid != null && xoffset != null && yoffset != null && Components.Count == itemid.Length)
                {
                    for (int i = 0; i < Components.Count; ++i)
                    {
                        Components[i].ItemID = itemid[i];
                        Point3D newoffset = new Point3D(xoffset[i], yoffset[i], 0);
                        Components[i].Offset = newoffset;
                        Components[i].Location = new Point3D(newoffset.X + X, newoffset.Y + Y, z);
                    }
                }
            }
        }

        public override void OnDelete()
        {
            if (Map != null && Map != Map.Internal)
            {
                BaseBoat boat = BaseBoat.FindBoatAt(Location, Map);
                if (boat != null)
                {
                    boat.SiegeWeapon.Remove(this);
                }
            }

            base.OnDelete();
        }

        public override void StoreWeapon_Callback((Mobile, BaseSiegeWeapon) state)
        {
            Mobile from = state.Item1;
            BaseSiegeWeapon weapon = state.Item2;

            if (weapon == null || weapon.Deleted)
            {
                return;
            }

            if (from == null)
            {
                weapon.Storing = false;
                return;
            }

            if (!from.Alive)
            {
                weapon.Storing = false;
                return;
            }

            BaseBoat boat = BaseBoat.FindBoatAt(from.Location, from.Map);
            if (boat == null && from.AccessLevel == AccessLevel.Player)
            {
                from.SendLocalizedMessage(504465);//"Devi trovarti sulla nave per smontarlo!");
                weapon.Storing = false;
                return;
            }

            // make sure that there is only one person nearby
            IPooledEnumerable<Mobile> moblist = from.Map.GetMobilesInRange(weapon.Location, MinStorageRange);
            int count = 0;
            if (moblist != null)
            {
                foreach (Mobile m in moblist)
                {
                    if (m.Player && m.AccessLevel < AccessLevel.Counselor)
                    {
                        ++count;
                    }
                }
                moblist.Free();
            }
            if (count > 1)
            {
                from.SendLocalizedMessage(504466);//"Troppi giocatori qui vicino, smontaggio fallito.");
                weapon.Storing = false;
                return;
            }

            // make sure that the player is still next to the weapon
            if (!from.InRange(weapon.Location, MinStorageRange) || from.Map != weapon.Map)
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                from.SendLocalizedMessage(504467, GetNameString());//, weapon.Name);
                weapon.Storing = false;
                return;
            }

            SiegeBoatCannonDeed sbcd = new SiegeBoatCannonDeed
            {

                // use the crate itemid while stored -> 8191
                ItemID = StoredWeaponID,
                LootType = LootType.Regular,
                Name = weapon.Name,
                Hue = 0
            };
            weapon.Delete();
            from.AddToBackpack(sbcd);

            from.SendLocalizedMessage(504468);//"Smontaggio completato.");
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
                Weight = 50f;
            }
        }
    }
}
