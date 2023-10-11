using Server.Items;
using Server.Regions;
using Server.Spells;
using Server.Towns;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class HandSiegeAttack : XmlAttachment
    {
        private double m_DamageScaleFactor = 1.2; // multiplier of weapon min/max damage used to calculate siege damage.
        private Item m_AttackTarget = null;    // target of the attack
        private Item m_VirtualItem;
        private InternalTimer m_Timer;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item AttackTarget
        {
            get => m_AttackTarget;
            set
            {
                m_AttackTarget = value;

                if (m_AttackTarget != null)
                {
                    if (AttachedTo is BaseWeapon weapon)
                    {
                        //l'arma deve essere equippata!
                        if (weapon.Parent is Mobile m)
                        {
                            TimeSpan basedelay = weapon.GetDelay(m);
                            DoTimer(basedelay, true);
                        }
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseWeapon Weapon { get; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public HandSiegeAttack(ASerial serial)
            : base(serial)
        {
        }

        [Attachable]
        public HandSiegeAttack(BaseWeapon weapon)
        {
            Weapon = weapon;
            if (weapon.Type == WeaponType.Ranged)
            {
                m_DamageScaleFactor = 0.8;
            }
            else if (weapon.Type == WeaponType.Bashing)
            {
                m_DamageScaleFactor = 1.4;
            }
            else if (weapon.Type == WeaponType.Fists)
            {
                m_DamageScaleFactor = 0.2;
            }
        }

        public override void OnReattach()
        {
            base.OnReattach();
            if (AttachedTo != Weapon)
            {
                Delete();
            }
        }

        public override string Name
        {
            get
            {
                if (m_Timer != null && m_Timer.Running)
                {
                    return "1";
                }

                return null;
            }
            set
            {
                if (m_Timer != null)
                {
                    m_Timer.Stop();
                }
            }
        }

        private const WeaponType c_NonCarvingWeapon = WeaponType.Bashing | WeaponType.Fists | WeaponType.Ranged | WeaponType.Staff | WeaponType.FireWeapon;
        public static bool SelectTarget(Mobile from, BaseWeapon weapon, Item targeted)
        {
            if (from == null || weapon == null || targeted == null)
            {
                return true;
            }
            else if (targeted is Corpse c)
            {
                if (!c.Carved && (c_NonCarvingWeapon & weapon.Type) == 0 && c.CanCarve() && (c.Owner == null || Notoriety.Compute(from, c.Owner) != Notoriety.Ally))
                {
                    c.Carve(from, weapon);
                    c.Open(from);
                    return true;
                }
                return false;
            }

            // does this weapon have a HandSiegeAttack attachment on it already?

            if (!(XmlAttach.FindAttachment(weapon, typeof(HandSiegeAttack)) is HandSiegeAttack a) || a.Deleted)
            {
                a = new HandSiegeAttack(weapon);
                XmlAttach.AttachTo(weapon, a);
            }
            FinalizeTarget(from, a, targeted);
            return false;
        }

        public static void FinalizeTarget(Mobile from, HandSiegeAttack attachment, Item targeted)
        {
            if (targeted is AddonComponent addon)
            {
                // if the addon doesnt have an xmlsiege attachment, then attack the addon
                if (!(XmlAttach.FindAttachment(targeted, typeof(XmlSiege)) is XmlSiege a) || a.Deleted)
                {
                    attachment.BeginAttackTarget(from, addon.Addon, addon);//.Location);
                }
                else
                {
                    attachment.BeginAttackTarget(from, addon, addon);//.Location);
                }
            }
            else
            {
                attachment.BeginAttackTarget(from, targeted, targeted);//.Location);
            }
        }

        public void BeginAttackTarget(Mobile from, Item target, Item virtualitem)//Point3D targetloc)
        {
            if (from == null || target == null)
            {
                return;
            }

            // check the target line of sight
            Point3D adjustedloc = new Point3D(virtualitem.X, virtualitem.Y, virtualitem.Z + target.ItemData.Height);
            Point3D fromloc = new Point3D(from.Location.X, from.Location.Y, from.Location.Z + 14);

            if (!from.Map.LineOfSight(fromloc, adjustedloc))
            {
                from.SendLocalizedMessage(504486);//"Non vedi il bersaglio.");
                return;
            }
            m_VirtualItem = virtualitem;

            int distance = (int)XmlSiege.GetDistance(from.Location, adjustedloc);

            if (distance <= Weapon.MaxRange)
            {
                if ((target.ItemID == 0x6f5 || target.ItemID == 0x6f6) && !from.Criminal)
                {
                    if (Region.Find(adjustedloc, target.Map) is TownRegion reg && !reg.Disabled)
                    {
                        if (reg.OwnerTown != null && from.Town != reg.OwnerTown)
                        {
                            if (!reg.OwnerTown.IsWar((Town)from.Town))
                            {
                                from.Criminal = true;
                            }
                        }
                    }
                }
                from.Combatant = null;
                AttackTarget = target;
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public override void Serialize(GenericWriter writer)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            Delete();
        }

        protected override bool UnsavedAttach => true;

        public override void OnAttach()
        {
            base.OnAttach();
            if (AttachedTo != Weapon)
            {
                Delete();
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();

            if (m_Timer != null)
            {
                m_Timer.Stop();
            }
        }

        #region TIMER
        public void DoTimer(TimeSpan delay, bool wait)
        {
            // is there a timer already running?  Then let it finish
            if (m_Timer != null)
            {
                if (m_Timer.Running && wait)
                {
                    return;
                }
                else
                {
                    m_Timer.Stop();
                }
            }

            m_Timer = new InternalTimer(this, delay);
            m_Timer.Start();
        }

        // added the duration timer that begins on spawning
        private class InternalTimer : Timer
        {
            private HandSiegeAttack m_attachment;

            public InternalTimer(HandSiegeAttack attachment, TimeSpan delay)
                : base(delay)
            {
                Priority = TimerPriority.FiftyMS;
                m_attachment = attachment;
            }

            protected override void OnTick()
            {
                if (m_attachment == null)
                {
                    return;
                }

                BaseWeapon weapon = m_attachment.Weapon;
                Item target = m_attachment.AttackTarget;

                if (weapon == null || weapon.Deleted || target == null || target.Deleted)
                {
                    Stop();
                    return;
                }
                //if siege item destroyed or if deleted just stop - continue to attack on normal items, so it gives illusion everything is destroyable
                XmlAttachment a = XmlAttach.FindAttachment(target, typeof(XmlSiege), null);
                if (a != null && (a.Deleted || !a.CanEquip(null)))
                {
                    Stop();
                    return;
                }

                // the weapon must be equipped

                if (!(weapon.Parent is Mobile attacker) || attacker.Deleted)
                {
                    Stop();
                    return;
                }

                // the attacker cannot be fighting or casting
                TimeSpan delay = weapon.GetDelay(attacker);
                if (attacker.LastActionTime.Add(delay) > DateTime.UtcNow || attacker.Spell != null || attacker.Combatant != null || !attacker.InLOS(target))
                {
                    Stop();
                    return;
                }

                //Map of target and attacker
                Map attackermap = attacker.Map;
                Map targetmap = target.Map;

                if (targetmap == null || targetmap == Map.Internal || attackermap == null || attackermap == Map.Internal || targetmap != attackermap)
                {
                    // if the attacker or target has an invalid map, then stop
                    Stop();
                    return;
                }


                if (!attacker.InRange(target.Location, Core.GlobalMaxUpdateRange))
                {
                    // control if it's in range for attack else stops
                    Stop();
                }
                else if (!attacker.InRange(m_attachment.m_VirtualItem ?? m_attachment.m_AttackTarget, weapon.MaxRange))
                {
                    //not in the weapon range, postpone it
                    m_attachment.DoTimer(delay, false);
                }
                else
                {
                    if (weapon is BaseRanged br)
                    {
                        if (attacker.Player)
                        {
                            Container pack = attacker.Backpack;
                            if (((!(attacker.FindItemOnLayer(Layer.Cloak) is Faretra Faretra) || !Faretra.ConsumeTotal(br.AmmoType, 1)) && (pack == null || !pack.ConsumeTotal(br.AmmoType, 1))))
                            {
                                Stop();
                                return;
                            }
                        }
                        attacker.MovingEffect(target, br.EffectID, 18, 1, false, false);
                    }
                    // attack the target
                    // Animate( int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay )
                    int action = weapon.animation(attacker.Mounted); // 1-H bash animation, 29=2-H mounted
                    int sound = weapon.HitSound;

                    // attack animation
                    //attacker.OnWarmodeChanged();
                    attacker.RevealingAction();
                    SpellHelper.Turn(attacker, target);
                    if (sound > 0)
                    {
                        attacker.PlaySound(sound);
                    }

                    attacker.Animate(action, 7, 1, true, false, 0);

                    // calculate the siege damage based on the weapon min/max damage and the overall damage scale factor
                    int basedamage = (int)(Utility.RandomMinMax(weapon.MinDamage, weapon.MaxDamage) * m_attachment.m_DamageScaleFactor);
                    // reduce the actual delay by the weapon speed
                    //double basedelay -= weapon.Speed*0.1;

                    //if (basedelay < 1) basedelay = 1;
                    if (basedamage < 1)
                    {
                        basedamage = 1;
                    }

                    // apply siege damage, all physical
                    XmlSiege.Attack(attacker, target, 0, basedamage, m_attachment.m_VirtualItem);

                    // prepare for the next attack
                    m_attachment.DoTimer(delay, false);
                }
            }
        }
        #endregion
    }
}
