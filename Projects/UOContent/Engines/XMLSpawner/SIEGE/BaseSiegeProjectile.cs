using Server.Engines.XmlSpawner2;
using Server.Regions;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.Items
{

    public class BaseSiegeProjectile : Item, ISiegeProjectile
    {
        private int m_Range;                // max number of tiles it can travel
        private int m_AccuracyBonus;        // adjustment to accuracy
        private int m_FiringSpeed;          // adjustment to time until next shot in seconds*10
        private int m_Area;                 // radius of area damage
        private int m_FireDamage;           // amount of fire damage to the target
        private int m_PhysicalDamage;       // amount of physical damage to the target

        public virtual int AnimationID => 0xE73;
        public virtual int AnimationHue => 0x4EA;

        public virtual double PlayerDamageMultiplier => 1.0;
        public virtual double MobDamageMultiplier => 1.5;  // default damage multiplier for creatures
        public virtual double StructureDamageMultiplier => 2.0;  // default damage multiplier for structures

        private BaseSiegeWeapon m_LoadedWeapon;
        public virtual BaseSiegeWeapon LoadedWeapon
        {
            get => m_LoadedWeapon;
            set => m_LoadedWeapon = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Range
        {
            get => m_Range;
            set { m_Range = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int FiringSpeed
        {
            get => m_FiringSpeed;
            set { m_FiringSpeed = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int AccuracyBonus
        {
            get => m_AccuracyBonus;
            set { m_AccuracyBonus = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Area
        {
            get => m_Area;
            set { m_Area = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int FireDamage
        {
            get => m_FireDamage;
            set { m_FireDamage = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int PhysicalDamage
        {
            get => m_PhysicalDamage;
            set { m_PhysicalDamage = value; InvalidateProperties(); }
        }

        public BaseSiegeProjectile()
            : this(1, 0xE73)
        {
        }

        public BaseSiegeProjectile(int amount)
            : this(amount, 0xE73)
        {
        }

        public BaseSiegeProjectile(int amount, int itemid)
            : base(itemid)
        {
            Weight = 5f;
            Stackable = true;
            Amount = amount;
        }

        public BaseSiegeProjectile(Serial serial)
            : base(serial)
        {
        }

        //		public override void OnAfterDuped(Item newItem)
        //		{
        //
        //			base.OnAfterDuped(newItem);
        //
        //			BaseSiegeProjectile s = newItem as BaseSiegeProjectile;
        //			// dupe the siege projectile props
        //			if (s != null)
        //			{
        //				s.FiringSpeed = FiringSpeed;
        //				s.AccuracyBonus = AccuracyBonus;
        //				s.Area = Area;
        //				s.Range = Range;
        //				s.FireDamage = FireDamage;
        //				s.PhysicalDamage = PhysicalDamage;
        //			}
        //		}

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1061169, Range.ToString()); // range ~1_val~
            list.Add(504478, FiringSpeed.ToString()); // Velocità ~1_val~
            list.Add(504479, AccuracyBonus.ToString());//"Accuratezza\t{0}", AccuracyBonus.ToString()); // ~1_val~: ~2_val~
            list.Add(504480, Area.ToString());//"Area\t{0}",  // ~1_val~: ~2_val~
            list.Add(504481, PhysicalDamage.ToString());//"Danno Fisico\t{0}", PhysicalDamage.ToString()); // ~1_val~: ~2_val~
            list.Add(504482, FireDamage.ToString());//"Danno Fuoco\t{0}", FireDamage.ToString()); // ~1_val~: ~2_val~
        }

        public override void OnDoubleClick(Mobile from)
        {

            // check the range between the player and projectiles
            if ((Parent == null && !from.InRange(Location, GrabRange)) ||
                (RootParent is Mobile && !from.InRange(((Mobile)RootParent).Location, GrabRange)) ||
                (RootParent is Container && !from.InRange(((Container)RootParent).Location, GrabRange))
                )
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            from.Target = new SiegeWeaponTarget(this);
        }

        public void OnHit(Mobile from, ISiegeWeapon weapon, IEntity target, Point3D targetloc)
        {
            if (weapon == null || from == null)
            {
                return;
            }

            // play explosion sound at target

            Effects.PlaySound(targetloc, weapon.Map, 0x11D);

            List<XmlSiege> damagelist = new List<XmlSiege>();

            // deal with the fact that for multis, the targetloc and the actual multi location may differ
            // so deal the multi damage first
            if (target is BaseMulti)
            {
                XmlSiege a = (XmlSiege)XmlAttach.FindAttachment(target, typeof(XmlSiege));

                if (a != null)
                {
                    damagelist.Add(a);
                }
            }

            // apply splash damage to objects with a siege attachment
            IPooledEnumerable<Item> itemlist = from.Map.GetItemsInRange(targetloc, Area);

            if (itemlist != null)
            {
                foreach (Item item in itemlist)
                {
                    if (item == null || item.Deleted)
                    {
                        continue;
                    }

                    XmlSiege a = (XmlSiege)XmlAttach.FindAttachment(item, typeof(XmlSiege));

                    if (a != null && !damagelist.Contains(a))
                    {
                        damagelist.Add(a);
                    }
                    else
                        // if it had no siege attachment and the item is an addoncomponent, then check the parent addon
                        if (item is AddonComponent)
                    {
                        a = (XmlSiege)XmlAttach.FindAttachment(((AddonComponent)item).Addon, typeof(XmlSiege));

                        if (a != null && !damagelist.Contains(a))
                        {
                            damagelist.Add(a);
                        }
                    }
                }
            }


            int scaledfiredamage = (int)(FireDamage * StructureDamageMultiplier * weapon.WeaponDamageFactor);
            int scaledphysicaldamage = (int)(PhysicalDamage * StructureDamageMultiplier * weapon.WeaponDamageFactor);

            foreach (XmlSiege a in damagelist)
            {
                // apply siege damage
                a.ApplyScaledDamage(from, scaledfiredamage, scaledphysicaldamage);
            }

            // apply splash damage to mobiles
            List<Mobile> mobdamage = new List<Mobile>();

            IPooledEnumerable<Mobile> moblist = from.Map.GetMobilesInRange(targetloc, Area);
            if (moblist != null)
            {
                foreach (Mobile m in moblist)
                {
                    if (m == null || m.Deleted || !from.CanBeHarmful(m, false) || m.Region is HouseRegion)
                    {
                        continue;
                    }

                    mobdamage.Add(m);
                }
                moblist.Free();
            }

            int totaldamage = FireDamage + PhysicalDamage;
            if (totaldamage > 0)
            {
                int damage = 0;
                int phys = 100 * PhysicalDamage / totaldamage;
                int fire = 100 * FireDamage / totaldamage;
                foreach (Mobile m in mobdamage)
                {
                    // AOS.Damage( Mobile m, Mobile from, int damage, int phys, int fire, int cold, int pois, int nrgy, bool keepAlive, siegeweapon )
                    if (m.Player)
                    {
                        damage = (int)(totaldamage * PlayerDamageMultiplier * weapon.WeaponDamageFactor);
                        AOS.Damage(m, from, damage, phys, fire, 0, 0, 0, false, 2);
                        from.DoHarmful(m, true);
                    }
                    else
                    {
                        damage = (int)(totaldamage * MobDamageMultiplier * weapon.WeaponDamageFactor);
                        AOS.Damage(m, from, damage, phys, fire, 0, 0, 0, false, 2);
                        from.DoHarmful(m, true);
                    }
                }
            }
            // consume the ammunition
            Consume(1);
            weapon.Projectile = this;
        }

        private class SiegeWeaponTarget : Target
        {
            private BaseSiegeProjectile m_projectile;

            public SiegeWeaponTarget(BaseSiegeProjectile projectile)
                : base(2, true, TargetFlags.None)
            {
                m_projectile = projectile;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == null || m_projectile == null || from.Map == null)
                {
                    return;
                }

                ISiegeWeapon weapon = null;

                if (targeted is ISiegeWeapon)
                {
                    // load the cannon
                    weapon = (ISiegeWeapon)targeted;
                }
                else
                    if (targeted is SiegeComponent)
                {
                    weapon = ((SiegeComponent)targeted).Addon as ISiegeWeapon;
                }

                if (weapon == null || weapon.Map == null)
                {
                    from.SendLocalizedMessage(504472);//"Target non valido");
                    return;
                }

                // load the cannon
                weapon.LoadWeapon(from, m_projectile); ;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(2); // version
                             // version 1
            writer.Write(m_LoadedWeapon);
            // version 0
            writer.Write(m_Range);
            writer.Write(m_AccuracyBonus);
            writer.Write(m_Area);
            writer.Write(m_FireDamage);
            writer.Write(m_PhysicalDamage);
            writer.Write(m_FiringSpeed);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                {
                    m_LoadedWeapon = reader.ReadItem() as BaseSiegeWeapon;
                    m_Range = reader.ReadInt();
                    m_AccuracyBonus = reader.ReadInt();
                    m_Area = reader.ReadInt();
                    m_FireDamage = reader.ReadInt();
                    m_PhysicalDamage = reader.ReadInt();
                    m_FiringSpeed = reader.ReadInt();
                    break;
                }
                case 1:
                {
                    m_LoadedWeapon = reader.ReadItem() as BaseSiegeWeapon;
                    goto case 0;
                }
                case 0:
                {
                    Name = null;
                    m_Range = reader.ReadInt();
                    m_AccuracyBonus = reader.ReadInt();
                    m_Area = reader.ReadInt();
                    m_FireDamage = reader.ReadInt();
                    m_PhysicalDamage = reader.ReadInt();
                    m_FiringSpeed = reader.ReadInt();
                    break;
                }
            }
        }
    }
}
