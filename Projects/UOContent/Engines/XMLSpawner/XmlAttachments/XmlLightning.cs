using Server.Items;
using Server.Spells;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlLightning : XmlAttachment
    {
        private int m_Damage = 0;
        private TimeSpan m_Refractory = TimeSpan.FromSeconds(5);    // 5 seconds default time between activations
        private DateTime m_EndTime;
        private int proximityrange = 1;                 // default movement activation from 5 tiles away

        [CommandProperty(AccessLevel.GameMaster)]
        public int Damage { get => m_Damage; set => m_Damage = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Refractory { get => m_Refractory; set => m_Refractory = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range { get => proximityrange; set => proximityrange = value; }

        private int m_WeaponUses; // default Unlimited weapon uses - zero is default

        [CommandProperty(AccessLevel.GameMaster)]
        public int WeaponUses { get => m_WeaponUses; set => m_WeaponUses = value; }
        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlLightning(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlLightning(int damage)
        {
            m_Damage = damage;
        }

        [Attachable]
        public XmlLightning(int damage, double refractory)
        {
            m_Damage = damage;
            Refractory = TimeSpan.FromSeconds(refractory);
        }

        [Attachable]
        public XmlLightning(int damage, double refractory, double expiresin)
        {
            m_Damage = damage;
            Expiration = TimeSpan.FromMinutes(expiresin);
            Refractory = TimeSpan.FromSeconds(refractory);
        }

        [Attachable]
        public XmlLightning(int damage, double refractory, double expiresin, int weaponuses)
        {
            m_Damage = damage;
            Expiration = TimeSpan.FromMinutes(expiresin);
            Refractory = TimeSpan.FromSeconds(refractory);
            m_WeaponUses = weaponuses;
        }

        // note that this method will be called when attached to either a mobile or a weapon
        // when attached to a weapon, only that weapon will do additional damage
        // when attached to a mobile, any weapon the mobile wields will do additional damage
        public override void OnWeaponHit(Mobile attacker, Mobile defender, BaseWeapon weapon, ref int damageGiven, int originalDamage)
        {
            // if it is still refractory then return
            if (DateTime.UtcNow < m_EndTime)
            {
                return;
            }

            int damage = 0;

            if (m_Damage > 0)
            {
                damage = Utility.Random(m_Damage);
            }

            if (defender != null && attacker != null && damage > 0)
            {
                damage = (int)(damage * Utility.c_BilanciaRess);//bilancia la ress
                defender.BoltEffect(0);

                SpellHelper.Damage(TimeSpan.Zero, defender, attacker, damage, 0, 0, 0, 0, 100);

                if (m_WeaponUses != 0)
                {
                    m_WeaponUses -= 1;
                    if (m_WeaponUses <= 0)
                    {
                        Delete();
                    }

                    return;
                }
                m_EndTime = DateTime.UtcNow + Refractory;
            }
        }

        //public override bool HandlesOnMovement { get { return true; } }

        // restrict the movement detection feature to non-movable items

        public override bool HandlesOnMovement
        {
            get
            {
                if (AttachedTo is Item && !((Item)AttachedTo).Movable)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }



        public override void OnMovement(MovementEventArgs e)
        {
            base.OnMovement(e);

            if (e.Mobile == null || e.Mobile.AccessLevel > AccessLevel.Player || AttachedTo is BaseWeapon)
            {
                return;
            }

            if (AttachedTo is Item && (((Item)AttachedTo).Parent == null) && Utility.InRange(e.Mobile.Location, ((Item)AttachedTo).Location, proximityrange))
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

            writer.Write(2);
            // version 2
            writer.Write(m_WeaponUses);
            // version 1
            writer.Write(proximityrange);
            // version 0
            writer.Write(m_Damage);
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
                case 2:
                    m_WeaponUses = reader.ReadInt();
                    goto case 1;
                case 1:
                    proximityrange = reader.ReadInt();
                    goto case 0;
                case 0:
                    // version 0
                    m_Damage = reader.ReadInt();
                    Refractory = reader.ReadTimeSpan();
                    TimeSpan remaining = reader.ReadTimeSpan();
                    m_EndTime = DateTime.UtcNow + remaining;
                    break;
            }
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            //1005151 -> Danno da energia
            if (Expiration > TimeSpan.Zero)
            {
                if (Refractory > TimeSpan.Zero)//~1_val~ ~2_val~ finisce in ~3_val~ min : ~4_val~ sec tra ogni uso
                {
                    return new LogEntry(LocalizerA(1), string.Format("#{0}\t{1}\t{2:F2}\t{3:F1}", 1005151, m_Damage, Expiration.TotalMinutes, m_Refractory.TotalSeconds));
                }
                else//~1_val~ ~2_val~ finisce in ~3_val~ min
                {
                    return new LogEntry(LocalizerA(2), string.Format("#{0}\t{1}\t{2:F2}", 1005151, m_Damage, Expiration.TotalMinutes));
                }
            }
            else
            {
                if (Refractory > TimeSpan.Zero)//~1_val~ ~2_val~ : ~3_val~ sec tra ogni uso
                {
                    return new LogEntry(LocalizerA(3), string.Format("#{0}\t{1}\t{2:F1}", 1005151, m_Damage, m_Refractory.TotalSeconds));
                }
                else//~1_val~ ~2_val~
                {
                    return new LogEntry(LocalizerA(4), string.Format("#{0}\t{1}", 1005151, m_Damage));
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

            int damage = 0;

            if (m_Damage > 0)
            {
                damage = Utility.RandomMinMax(m_Damage / 3, m_Damage);
            }

            if (damage > 0)
            {
                damage = (int)(damage * Utility.c_BilanciaRess);//bilancia la ress
                m.BoltEffect(0);

                SpellHelper.Damage(TimeSpan.Zero, m, damage, 0, 0, 0, 0, 100);
            }

            m_EndTime = DateTime.UtcNow + Refractory;

        }
    }
}
