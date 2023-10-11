using Server.Engines.XmlSpawner2;
using Server.Multis;
using Server.Regions;
using Server.Targeting;
using System;

namespace Server.Items
{

    public abstract class BaseSiegeWeapon : BaseAddon, ISiegeWeapon
    {
        public const int MaxStorageAltitude = 10;
        public virtual double WeaponLoadingDelay => 15.0;  // base delay for loading this weapon
        public virtual double WeaponStorageDelay => 15.0;  // base delay for packing away this weapon

        public virtual double DamageReductionWhenDamaged => 0.4;  // scale damage from 40-100% depending on the damage it has taken 
        public virtual double RangeReductionWhenDamaged => 0.7;  // scale range from 70-100% depending on the damage it has taken 

        public virtual int MinTargetRange => 1;  // target must be further away than this
        public virtual int MinStorageRange => 1;  // player must be at least this close to allow packaging
        public virtual int MinFiringRange => 3;  // player must be at least this close to fire the weapon

        public virtual bool Parabola => false;  // whether the weapon needs to consider line of sight when selecting a target

        public virtual int StoredWeaponID => 3644;  // itemid used when the weapon is packed up (crate by default)

        public override BaseAddonDeed Deed => null;

        public abstract void UpdateDisplay();

        public abstract Type[] AllowedProjectiles { get; }

        private int m_Facing;
        private BaseSiegeProjectile m_Projectile;
        private DateTime m_NextFiringTime;
        private bool m_FixedFacing = false;
        private bool m_Draggable = false;
        private bool m_Packable = true;
        public bool Storing = false;

        private XmlSiege m_SiegeAttachment = null;

        private XmlSiege SiegeAttachment
        {
            get
            {
                if (m_SiegeAttachment == null)
                {
                    m_SiegeAttachment = (XmlSiege)XmlAttach.FindAttachment(this, typeof(XmlSiege));
                }
                return m_SiegeAttachment;
            }

        }

        public int Hits => (SiegeAttachment != null) ? SiegeAttachment.Hits : 0;

        public int HitsMax => (SiegeAttachment != null) ? SiegeAttachment.HitsMax : 0;

        // default weapon performance factors.
        // taking damage reduces the multiplier

        // default damage multiplier for the weapon
        public virtual double WeaponDamageFactor
        {
            get
            {
                if (HitsMax > 0)
                {
                    return ((1 - DamageReductionWhenDamaged) * Hits / HitsMax) + DamageReductionWhenDamaged;
                }
                return 1;
            }
        }

        // default range multiplier for the weapon
        public virtual double WeaponRangeFactor
        {
            get
            {
                if (HitsMax > 0)
                {
                    return ((1 - RangeReductionWhenDamaged) * Hits / HitsMax) + RangeReductionWhenDamaged;
                }
                return 1;
            }
        }

        public virtual BaseSiegeProjectile Projectile
        {
            get => m_Projectile;
            set
            {
                m_Projectile = value;
                // invalidate component properties
                if (Components != null)
                {
                    foreach (AddonComponent c in Components)
                    {
                        c.InvalidateProperties();
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool IsDraggable { get => m_Draggable; set => m_Draggable = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsPackable { get => m_Packable; set => m_Packable = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool FixedFacing
        {
            get => m_FixedFacing;
            set => m_FixedFacing = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Facing
        {
            get => m_Facing;
            set
            {
                m_Facing = value;
                if (m_Facing < 0)
                {
                    m_Facing = 3;
                }

                if (m_Facing > 3)
                {
                    m_Facing = 0;
                }

                UpdateDisplay();
                // save the current state of the itemids
                if (SiegeAttachment != null)
                {
                    SiegeAttachment.StoreOriginalItemID(this);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextFiring
        {
            get => m_NextFiringTime - DateTime.UtcNow;
            set => m_NextFiringTime = DateTime.UtcNow + value;
        }

        public DateTime NextFiringTime => m_NextFiringTime;

        public override void OnDelete()
        {
            base.OnDelete();

            if (m_Projectile != null)
            {
                m_Projectile.Delete();
            }
        }

        private bool m_BlockDelete = false;
        public override bool BlockDelete()
        {
            return m_BlockDelete;
        }
        public virtual void StoreWeapon_Callback((Mobile, BaseSiegeWeapon) state)
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

            // make sure that there is only one person nearby
            IPooledEnumerable<Mobile> moblist = from.Map.GetMobilesInRange(from.Location, MinStorageRange);
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
            }
            if (count > 1)
            {
                from.SendLocalizedMessage(504466);//"Le persone che hai attorno ti impediscono un disassemblaggio corretto!");
                weapon.Storing = false;
                return;
            }

            // make sure that the player is still next to the weapon
            bool found = false;
            IPooledEnumerable<Item> ie = from.GetItemsInRange(MinStorageRange);
            foreach (Item it in ie)
            {
                if (Components.Contains(it as AddonComponent))
                {
                    if (from.InRange(it.Z, MaxStorageAltitude))
                    {
                        found = true;
                    }

                    break;
                }
            }
            ie.Free();

            if (!found)
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                from.SendLocalizedMessage(504467, GetNameString());//from.SendMessage("{0} non disassemblata.", weapon.Name);
                weapon.Storing = false;
                return;
            }

            // use the crate itemid while stored
            weapon.ItemID = StoredWeaponID;
            weapon.Visible = true;
            weapon.Movable = true;
            from.AddToBackpack(weapon);

            // remove the components
            m_BlockDelete = true;
            foreach (AddonComponent i in weapon.Components)
            {
                if (i != null)
                {
                    i.Delete();
                }
            }
            weapon.Components.Clear();
            m_BlockDelete = false;

            from.SendLocalizedMessage(504468);//"Disassemblaggio {0} completato.", weapon.Name);
            weapon.Storing = false;
            InvalidateProperties();
        }

        public abstract int BaseLenght { get; }
        public virtual void PlaceWeapon(Mobile from, Point3D location, Map map)
        {
            MoveToWorld(location, map);
            UpdateDisplay();
        }

        public virtual void StoreWeapon(Mobile from)
        {
            if (from == null)
            {
                return;
            }

            IPooledEnumerable<Item> ie = from.GetItemsInRange(MinStorageRange);
            bool found = false;
            foreach (Item it in ie)
            {
                if (Components.Contains(it as AddonComponent))
                {
                    if (from.InRange(it.Z, MaxStorageAltitude))
                    {
                        found = true;
                    }

                    break;
                }
            }
            ie.Free();

            if (!found)
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            if (Storing)
            {
                from.SendLocalizedMessage(504469);//"Stai già disassemblando un'arma!");
                return;
            }

            // 15 second delay to pack up the cannon
            Timer.DelayCall(TimeSpan.FromSeconds(WeaponStorageDelay), StoreWeapon_Callback, (from, this));

            from.SendLocalizedMessage(504470, GetNameString());//"Disassemblo {0}...", Name);
            Storing = true;
        }

        private bool ContainsInterface(Type[] typearray, Type type)
        {
            if (typearray == null || type == null)
            {
                return false;
            }

            foreach (Type t in typearray)
            {
                if (t == type)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CheckAllowedProjectile(Item projectile)
        {
            if (projectile == null || AllowedProjectiles == null)
            {
                return false;
            }

            for (int i = 0; i < AllowedProjectiles.Length; ++i)
            {
                Type t = AllowedProjectiles[i];
                Type pt = projectile.GetType();

                if (t == null || pt == null)
                {
                    continue;
                }

                if (pt.IsSubclassOf(t) || pt.Equals(t) || (t.IsInterface && ContainsInterface(pt.GetInterfaces(), t)))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void LoadWeapon(Mobile from, BaseSiegeProjectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            // can't load destroyed weapons
            if (Hits == 0)
            {
                return;
            }

            // restrict allowed projectiles
            if (!CheckAllowedProjectile(projectile))
            {
                from.SendLocalizedMessage(504471);//"Non può essere usato su quest'arma");
                return;
            }

            if (m_Projectile != null && !m_Projectile.Deleted)
            {
                from.SendLocalizedMessage(504495, m_Projectile.GetNameString());//~1_val~ rimosso.
                m_Projectile.Movable = true;
                from.AddToBackpack(m_Projectile);
                m_Projectile.LoadedWeapon = null;
            }

            if (projectile.Amount > 1)
            {
                //projectile.Amount--;
                //Projectile = projectile.Dupe(1);
                Projectile = Mobile.LiftItemDupe(projectile, projectile.Amount - 1) as BaseSiegeProjectile;
            }
            else
            {
                Projectile = projectile;
            }

            if (m_Projectile != null)
            {
                m_Projectile.Movable = false;
                m_Projectile.Internalize();
                ((ISiegeProjectile)m_Projectile).LoadedWeapon = this;

                from.SendLocalizedMessage(504496, m_Projectile.GetNameString());//~1_val~ caricato.
            }
        }

        public override bool OnDroppedToWorld(Mobile from, Point3D point)
        {
            bool dropped = base.OnDroppedToWorld(from, point);
            if (dropped && (this is SiegeCannon || this is SiegeCatapult) && from.AccessLevel < AccessLevel.GameMaster)
            {
                BaseBoat boat = BaseBoat.FindBoatAt(point, from.Map);

                if (boat != null)
                {
                    from.SendLocalizedMessage(500269); // You cannot build that there.
                    return false;
                }
            }

            if (dropped)
            {
                ItemID = 1;
                Visible = false;
                Movable = false;
                for (int i = 0; i < BaseLenght; ++i)
                {
                    AddComponent(new SiegeComponent(0, Name), 0, 0, 0);
                }
                UpdateDisplay();
                m_NextFiringTime = DateTime.UtcNow + TimeSpan.FromSeconds(10);
                Facing = Direzionatore.TargetDirectionHandle(from, point);
            }
            return dropped;
        }

        public virtual bool HasFiringAngle(IPoint3D t)
        {
            int dy = t.Y - Y;
            int dx = t.X - X;

            switch (Facing)
            {
                case 0:
                    return t.X < X && ((dy <= 0 && -dy <= -dx) || (dy > 0 && dy <= -dx));
                case 1:
                    return t.Y < Y && ((dx <= 0 && -dx <= -dy) || (dx > 0 && dx <= -dy));
                case 2:
                    return t.X > X && ((dy <= 0 && -dy <= dx) || (dy > 0 && dy <= dx));
                case 3:
                    return t.Y > Y && ((dx <= 0 && -dx <= dy) || (dx > 0 && dx <= dy));
            }

            return false;
        }

        public BaseSiegeWeapon()
        {
        }

        public BaseSiegeWeapon(Serial serial) : base(serial)
        {
        }

        public virtual Point3D ProjectileLaunchPoint => (Location);

        public virtual bool AttackTarget(Mobile from, IEntity target, Point3D targetloc, bool parabola)
        {
            if (from == null || from.Map == null || !(m_Projectile is ISiegeProjectile projectile))
            {
                return false;
            }

            if (!HasFiringAngle(targetloc))
            {
                from.SendLocalizedMessage(504483);//"Non hai angolo di fuoco.");
                return false;
            }

            // check the target range
            int distance = (int)XmlSiege.GetDistance(targetloc, Location);

            int projectilerange = (int)(projectile.Range * WeaponRangeFactor);

            if (projectilerange < distance)
            {
                from.SendLocalizedMessage(504484);//"Fuori portata");
                return false;
            }

            if (distance <= MinTargetRange)
            {
                from.SendLocalizedMessage(504485);//"Bersaglio troppo vicino");
                return false;
            }

            // check the target line of sight
            /*int height = 6;
			if (target is Item)
			{
				height = Math.Min(((Item)target).ItemData.Height, 6);
			}
			else if (target is Mobile)
			{
				height = 14;
			}*/

            Point3D adjustedloc = new Point3D(targetloc.X, targetloc.Y, targetloc.Z + 5);
            Point3D startloc = new Point3D(Location.X, Location.Y, Location.Z + 5);

            if (!Map.LineOfSight_Firearm(startloc, adjustedloc, parabola))
            {
                from.SendLocalizedMessage(504486);//"Bersaglio non in vista");
                return false;
            }
            else if (Region.Find(adjustedloc, Map) is HouseRegion && !Map.LineOfSight(this, adjustedloc))
            {
                from.SendLocalizedMessage(504487);//"Bersaglio non valido");
                return false;
            }

            // L'azione leva hiding o altre cazzate tipo attacchi, cast e altre minchieronate da lameroni -.-
            from.RevealingAction();
            from.OnWarmodeChanged();
            // ok, the projectile is being fired
            // calculate attack parameters
            double firingspeedbonus = projectile.FiringSpeed / 10.0;
            double dexbonus = from.Dex / 30.0;
            int weaponskill = (int)from.Skills[SkillName.ArmsLore].Value;

            int accuracybonus = projectile.AccuracyBonus;

            // calculate the cooldown time with dexterity bonus and firing speed bonus on top of the base delay
            double loadingdelay = WeaponLoadingDelay - dexbonus - firingspeedbonus;

            m_NextFiringTime = DateTime.UtcNow + TimeSpan.FromSeconds(loadingdelay);

            // calculate the accuracy based on distance and weapon skill
            int accuracy = weaponskill + accuracybonus;

            if (Utility.Random(distance * 6) > accuracy)
            {
                //play animation and whatever is necessary...
                SpecialEffects(from, m_Projectile);
                // consume the ammunition
                m_Projectile.Consume(1);
                // update the properties display
                Projectile = m_Projectile;
                from.SendLocalizedMessage(504488);//"Bersaglio mancato!");
                return true;
            }

            LaunchProjectile(from, m_Projectile, target, targetloc, TimeSpan.FromSeconds(distance * 0.08));

            return true;
        }

        public virtual void LaunchProjectile(Mobile from, Item projectile, IEntity target, Point3D targetloc, TimeSpan delay)
        {
            if (!(projectile is ISiegeProjectile pitem))
            {
                return;
            }

            int animationid = pitem.AnimationID;
            int animationhue = pitem.AnimationHue;

            // show the projectile moving to the target
            XmlSiege.SendMovingProjectileEffect(this, target, animationid, ProjectileLaunchPoint, targetloc, 7, 0, false, true, animationhue);

            // delayed damage at the target to account for travel distance of the projectile
            Timer.DelayCall(delay, DamageTarget_Callback, (from, this, target, targetloc, projectile));

            return;
        }

        public virtual void SpecialEffects(Mobile from, Item Projectile)
        {
        }

        public virtual void DamageTarget_Callback((Mobile, BaseSiegeWeapon, IEntity, Point3D, Item) args)
        {
            Mobile from = args.Item1;
            BaseSiegeWeapon weapon = args.Item2;
            IEntity target = args.Item3;
            Point3D targetloc = args.Item4;
            Item pitem = args.Item5;


            if (pitem is ISiegeProjectile projectile)
            {
                projectile.OnHit(from, weapon, target, targetloc);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Parent != null)
            {
                return;
            }

            // can't use destroyed weapons
            if (Hits == 0)
            {
                return;
            }

            /*if (!BasePotion.HasFreeHand(from))
			{
				from.SendMessage("Hai le mani occupate!");
				return;
			}*/
            if (from.Combatant != null)
            {
                from.Combatant = null;
            }

            // check the range between the player and weapon
            if (!from.InRange(Location, MinFiringRange) || !from.InRange(Location.Z, MaxStorageAltitude) || from.Map != Map)
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            if (Storing)
            {
                from.SendLocalizedMessage(504489, GetNameString());//"{0} in disassemblaggio.", Name);
                return;
            }

            if (m_Projectile == null || m_Projectile.Deleted)
            {
                from.SendLocalizedMessage(504490, GetNameString());//"{0} senza Proiettile.", Name);
                return;
            }

            // check if the cannon is cool enough to fire
            if (m_NextFiringTime > DateTime.UtcNow)
            {
                from.SendLocalizedMessage(504491);//"Arma non ancora pronta per l'uso!");
                return;
            }

            from.Target = new SiegeTarget(this, from, Parabola, m_Projectile, m_NextFiringTime);
        }

        private class SiegeTarget : Target
        {
            private BaseSiegeWeapon m_weapon;
            private Mobile m_from;
            private bool m_parabola;
            private Item m_projectile;
            private DateTime m_previousfiringtime;

            public SiegeTarget(BaseSiegeWeapon weapon, Mobile from, bool parabola, BaseSiegeProjectile projectile, DateTime previousfiringtime)
                : base(30, true, TargetFlags.Harmful)
            {
                CheckLOS = false;
                m_weapon = weapon;
                m_from = from;
                m_parabola = parabola;
                m_projectile = projectile;
                m_previousfiringtime = previousfiringtime;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == null || m_weapon == null || from.Map == null)
                {
                    return;
                }

                // I giocatori hanno modi incredibili di buggare le cose...doppiocheck OBBLIGATORIO
                // i bug senza sono evidenti.
                if (m_previousfiringtime > DateTime.UtcNow)
                {
                    from.SendLocalizedMessage(504492);//"È ancora troppo caldo, devi aspettare!");
                    return;
                }

                if (m_projectile == null || m_projectile.Deleted)
                {
                    from.SendLocalizedMessage(504493);//"Non puoi usare il cannone senza il proiettile dentro!");
                    return;
                }

                if (!from.InRange(m_weapon.Location, 3) || !from.InRange(m_weapon.Location.Z, MaxStorageAltitude))
                {
                    from.SendLocalizedMessage(504494);//"Sei troppo lontano dall'arma!");
                    return;
                }

                if (from.Combatant != null)
                {
                    from.Combatant = null;
                }

                if (targeted is StaticTarget)
                {
                    int staticid = ((StaticTarget)targeted).ItemID;
                    int staticx = ((StaticTarget)targeted).Location.X;
                    int staticy = ((StaticTarget)targeted).Location.Y;

                    Item multiitem = null;
                    Point3D tileloc = Point3D.Zero;

                    // find the possible multi owner of the static tile
                    foreach (Item item in from.Map.GetItemsInRange(((StaticTarget)targeted).Location, 50))
                    {
                        if (item is BaseMulti)
                        {
                            // search the component list for a match
                            MultiComponentList mcl = ((BaseMulti)item).Components;
                            bool found = false;
                            if (mcl != null && mcl.List != null)
                            {
                                for (int i = 0; i < mcl.List.Length; ++i)
                                {
                                    MultiTileEntry t = mcl.List[i];

                                    int x = t.m_OffsetX + item.X;
                                    int y = t.m_OffsetY + item.Y;
                                    int z = t.m_OffsetZ + item.Z;
                                    int itemID = t.m_ItemID & TileData.MaxItemValue;

                                    if (itemID == staticid && x == staticx && y == staticy)
                                    {
                                        found = true;
                                        tileloc = new Point3D(x, y, z);
                                        break;
                                    }

                                }
                            }

                            if (found)
                            {
                                multiitem = item;
                                break;
                            }
                        }
                    }
                    if (multiitem != null)
                    {
                        //Console.WriteLine("attacking {0} at {1}:{2}", multiitem, tileloc, ((StaticTarget)targeted).Location);
                        // may have to reconsider the use tileloc vs target loc
                        //m_cannon.AttackTarget(from, multiitem, ((StaticTarget)targeted).Location);

                        m_weapon.AttackTarget(from, multiitem, multiitem.Map.GetPoint(targeted, true), m_parabola);
                    }
                }
                else
                    if (targeted is IEntity)
                {
                    // attack the target
                    m_weapon.AttackTarget(from, (IEntity)targeted, ((IEntity)targeted).Location, m_parabola);
                }
                else
                        if (targeted is LandTarget)
                {
                    // attack the target
                    m_weapon.AttackTarget(from, null, ((LandTarget)targeted).Location, m_parabola);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(2); // version
                             // version 1
            writer.Write(m_FixedFacing);
            writer.Write(m_Draggable);
            writer.Write(m_Packable);
            // version 0
            writer.Write(m_Facing);
            writer.WriteItem(m_Projectile);
            writer.Write(m_NextFiringTime);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    goto case 1;
                case 1:
                    m_FixedFacing = reader.ReadBool();
                    m_Draggable = reader.ReadBool();
                    m_Packable = reader.ReadBool();
                    goto case 0;
                case 0:
                    m_Facing = reader.ReadInt();
                    m_Projectile = reader.ReadItem<BaseSiegeProjectile>();
                    m_NextFiringTime = reader.ReadDateTime();
                    break;
            }
            if (version < 2 && Visible)
            {
                Timer.DelayCall(delegate
                                {
                                    if (this != null && !Deleted)
                                    {
                                        m_BlockDelete = true;
                                        foreach (AddonComponent i in Components)
                                        {
                                            if (i != null)
                                            {
                                                i.Delete();
                                            }
                                        }
                                        Components.Clear();
                                        m_BlockDelete = false;
                                    }
                                });
            }
        }
    }
}
