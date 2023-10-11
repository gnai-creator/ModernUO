using Server.Engines.XmlSpawner2;
using System;

namespace Server.Items
{
    public class SiegeCannon : BaseSiegeWeapon
    {
        public override int LabelNumber => 1023723;
        public override double WeaponLoadingDelay => 15.0;  // base delay for loading this weapon
        public override double WeaponStorageDelay => 15.0;  // base delay for packing away this weapon

        public override double WeaponDamageFactor => base.WeaponDamageFactor * 1.2;  // damage multiplier for the weapon
        public override double WeaponRangeFactor => base.WeaponRangeFactor * 1.2;  //  range multiplier for the weapon

        public override int MinTargetRange => 1;  // target must be further away than this
        public override int MinStorageRange => 1;  // player must be at least this close to store the weapon
        public override int MinFiringRange => 3;  // player must be at least this close to fire the weapon

        public override bool Parabola => false;  // whether the weapon needs to consider line of sight when selecting a target
        public override int BaseLenght => CannonNorth.Length;
        // facing 0
        public static int[] CannonWest = new int[] { 3726, 3727, 3728 };
        public static int[] CannonWestXOffset = new int[] { -1, 0, 1 };
        public static int[] CannonWestYOffset = new int[] { 0, 0, 0 };
        // facing 1
        public static int[] CannonNorth = new int[] { 3725, 3724, 3723 };
        public static int[] CannonNorthXOffset = new int[] { 0, 0, 0 };
        public static int[] CannonNorthYOffset = new int[] { -1, 0, 1 };
        // facing 2
        public static int[] CannonEast = new int[] { 3734, 3733, 3732 };
        public static int[] CannonEastXOffset = new int[] { 1, 0, -1 };
        public static int[] CannonEastYOffset = new int[] { 0, 0, 0 };
        // facing 3
        public static int[] CannonSouth = new int[] { 3729, 3730, 3731 };
        public static int[] CannonSouthXOffset = new int[] { 0, 0, 0 };
        public static int[] CannonSouthYOffset = new int[] { 1, 0, -1 };

        private Type[] m_allowedprojectiles = new Type[] { typeof(SiegeCannonball) };

        public override Type[] AllowedProjectiles => m_allowedprojectiles;

        [Constructable]
        public SiegeCannon() : this(0)
        {
        }

        [Constructable]
        public SiegeCannon(int facing)
        {
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

            Facing = facing;

            // set the default props
            //Name = "Cannone";
            Weight = 70f;

            // make them siegable by default
            // XmlSiege( hitsmax, resistfire, resistphysical, wood, iron, stone, itemid da distrutto -- 0 = distrutto e basta)
            XmlAttach.AttachTo(this, new XmlSiege(500, 250, 250, 20, 30, 0, 0, 0));

            // and draggable
            XmlAttach.AttachTo(this, new XmlDrag());

            // undo the temporary hue indicator that is set when the xmlsiege attachment is added
            Hue = 0;
        }

        public SiegeCannon(Serial serial)
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

        public override void SpecialEffects(Mobile from, Item Projectile)
        {
            // show the cannon firing animation with explosion sound
            Effects.SendLocationEffect(this, Map, 0x36B0, 16, 1);
            Effects.PlaySound(this, Map, 0x11D);
        }

        public override void UpdateDisplay()
        {
            if (Components != null && Components.Count > 2)
            {
                int z = Components[1].Location.Z;

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
                Weight = 70f;
            }

        }
    }
}
