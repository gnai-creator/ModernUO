using Server.Items;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlLifeDrain : XmlAttachment
    {
        private TimeSpan m_Refractory = TimeSpan.FromSeconds(5);    // 5 seconds default time between activations
        private DateTime m_EndTime;
        private Mobile m_Owner = null;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Drain { get; set; } = 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range { get; set; } = 1;

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Refractory { get => m_Refractory; set => m_Refractory = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WeaponUses { get; set; }
        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlLifeDrain(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlLifeDrain(int drain)
        {
            Drain = drain;
        }

        [Attachable]
        public XmlLifeDrain(int drain, float refractory)
        {
            Drain = drain;
            m_Refractory = TimeSpan.FromSeconds(refractory);
        }

        [Attachable]
        public XmlLifeDrain(int drain, float refractory, float expiresin)
        {
            Drain = drain;
            Expiration = TimeSpan.FromMinutes(expiresin);
            m_Refractory = TimeSpan.FromSeconds(refractory);
        }

        public XmlLifeDrain(int drain, float refractory, float expiresin, string name)
        {
            Drain = drain;
            Expiration = TimeSpan.FromMinutes(expiresin);
            m_Refractory = TimeSpan.FromSeconds(refractory);
            Name = name;
        }

        [Attachable]
        public XmlLifeDrain(int drain, float refractory, float expiresin, int weaponuses)
        {
            Drain = drain;
            Expiration = TimeSpan.FromMinutes(expiresin);
            m_Refractory = TimeSpan.FromSeconds(refractory);
            WeaponUses = weaponuses;
        }

        public XmlLifeDrain(int drain, TimeSpan refractory, TimeSpan expiration, Mobile owner)
        {
            Drain = drain;
            Expiration = expiration;
            m_Refractory = refractory;
            m_Owner = owner;
        }

        // note that this method will be called when attached to either a mobile or a weapon
        // when attached to a weapon, only that weapon will do additional damage
        // when attached to a mobile, any weapon the mobile wields will do additional damage, also every spell casted by that mobile will do the drain if into the refractory
        public override void OnWeaponHit(Mobile attacker, Mobile defender, BaseWeapon weapon, ref int damageGiven, int originalDamage)
        {
            // if it is still refractory then return
            if (DateTime.UtcNow < m_EndTime)
            {
                return;
            }

            int drain = 0;
            if (Drain > 0)
            {
                drain = Utility.RandomMinMax(Drain >> 1, Drain);
                if (drain > damageGiven)
                {
                    drain = Math.Max(drain, damageGiven);
                }
            }
            else if (Drain < 0)
            {
                drain = Utility.RandomMinMax(Drain >> 1, Drain);
            }

            if (defender != null && attacker != null)
            {
                if (drain > 0)
                {
                    defender.Hits -= drain;
                    if (defender.Hits < 0)
                    {
                        defender.Hits = 0;
                    }

                    if (m_Owner == attacker)
                    {
                        attacker.Heal(drain);
                    }
                    else
                    {
                        if (attacker.HitsMax + BasePotion.MaxOver > attacker.Hits + drain)
                            attacker.Hits += drain;
                        else
                            attacker.Hits = attacker.HitsMax + BasePotion.MaxOver;
                    }

                    if (attacker.Hits < 0)
                    {
                        attacker.Hits = 0;
                    }

                    DrainEffect(defender);
                }
                else if (drain < 0)
                {
                    attacker.Hits += drain;
                    if (attacker.Hits < 0)
                    {
                        attacker.Hits = 0;
                    }

                    DrainEffect(attacker);
                }

                if (WeaponUses != 0)
                {
                    WeaponUses -= 1;
                    if (WeaponUses <= 0)
                    {
                        Delete();
                    }

                    return;
                }
                m_EndTime = DateTime.UtcNow + Refractory;
            }
        }

        public override void OnSpellDamage(Item augmenter, Mobile caster, Mobile defender, ref int spelldamage, int phys, int fire, int cold, int pois, int nrgy)
        {
            OnWeaponHit(caster, defender, null, ref spelldamage, spelldamage);
        }

        public static void DrainEffect(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            m.FixedParticles(0x374A, 10, 15, 5013, 0x496, 0, EffectLayer.Waist);
            m.PlaySound(0x231);
            m.SendLocalizedMessage(505287);// "Senti che la tua forza vitale ti abbandona!" );
        }

        public override void OnEquip(Mobile from)
        {
            if (m_Owner != null && m_Owner != from)
            {
                Delete();
            }
        }

        public override bool HandlesOnMovement => true;

        public override void OnMovement(MovementEventArgs e)
        {
            base.OnMovement(e);

            if (e.Mobile == null || e.Mobile.AccessLevel > AccessLevel.Player || AttachedTo is BaseWeapon)
            {
                return;
            }

            if (AttachedTo is Item && (((Item)AttachedTo).Parent == null) && Utility.InRange(e.Mobile.Location, ((Item)AttachedTo).Location, Range))
            {
                OnTrigger(null, e.Mobile);
            }
            else
            {
                return;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(3);
            // version 3
            writer.Write(m_Owner);
            // version 2
            writer.Write(WeaponUses);
            // version 1
            writer.Write(Range);
            // version 0
            writer.Write(Drain);
            writer.Write(m_Refractory);
            if (m_EndTime <= DateTime.UtcNow)
            {
                writer.Write(TimeSpan.Zero);
            }
            else
            {
                writer.Write(m_EndTime.Subtract(DateTime.UtcNow));
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 3:
                    m_Owner = reader.ReadMobile();
                    goto case 2;
                case 2:
                    WeaponUses = reader.ReadInt();
                    goto case 1;
                case 1:
                    // version 1
                    Range = reader.ReadInt();
                    goto case 0;
                case 0:
                    // version 0
                    Drain = reader.ReadInt();
                    Refractory = reader.ReadTimeSpan();
                    TimeSpan remaining = reader.ReadTimeSpan();
                    m_EndTime = DateTime.UtcNow + remaining;
                    break;
            }
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            //1005154 -> Risucchio vitale
            if (m_Owner != null)
            {
                return new LogEntry(LocalizerA(5), string.Format("#{0}\t{1}", 1005154, Drain));
            }
            else if (Expiration > TimeSpan.Zero)
            {
                if (Refractory > TimeSpan.Zero)//~1_val~ ~2_val~ finisce in ~3_val~ min : ~4_val~ sec tra ogni uso
                {
                    return new LogEntry(LocalizerA(1), string.Format("#{0}\t{1}\t{2:F2}\t{3:F1}", 1005154, Drain, Expiration.TotalMinutes, m_Refractory.TotalSeconds));
                }
                else//~1_val~ ~2_val~ finisce in ~3_val~ min
                {
                    return new LogEntry(LocalizerA(2), string.Format("#{0}\t{1}\t{2:F2}", 1005154, Drain, Expiration.TotalMinutes));
                }
            }
            else
            {
                if (Refractory > TimeSpan.Zero)//~1_val~ ~2_val~ : ~3_val~ sec tra ogni uso
                {
                    return new LogEntry(LocalizerA(3), string.Format("#{0}\t{1}\t{2:F1}", 1005154, Drain, m_Refractory.TotalSeconds));
                }
                else//~1_val~ ~2_val~
                {
                    return new LogEntry(LocalizerA(4), string.Format("#{0}\t{1}", 1005154, Drain));
                }
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();
            if (m_Owner != null)
            {
                if (AttachedTo is Item i)
                {
                    if (i.Parent == m_Owner || i.IsChildOf(m_Owner.Backpack))
                    {
                        m_Owner.SendLocalizedMessage(505288);// Un'arma ha esaurito l'effetto del risucchio vitale
                    }
                    i.InvalidateProperties();
                }
            }
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // announce it to the mob
            if (AttachedTo is Mobile mob)
            {
                if (Drain > 0)
                {
                    mob.SendLocalizedMessage(505289);// "Hai il potere del risucchio vitale!");
                }
                else
                {
                    mob.SendLocalizedMessage(505290);//Hai la maledizione del risucchio vitale!
                }
            }
        }

        public override void OnTrigger(object activator, Mobile m)
        {
            if (m == null)
            {
                return;
            }

            // if it is still refractory then return
            if (DateTime.UtcNow < m_EndTime)
            {
                return;
            }

            int drain = 0;

            if (Drain > 0)
            {
                drain = Utility.Random(Drain);
            }

            if (drain > 0)
            {
                m.Hits -= drain;
                if (m.Hits < 0)
                {
                    m.Hits = 0;
                }

                DrainEffect(m);

            }

            m_EndTime = DateTime.UtcNow + Refractory;

        }
    }
}
