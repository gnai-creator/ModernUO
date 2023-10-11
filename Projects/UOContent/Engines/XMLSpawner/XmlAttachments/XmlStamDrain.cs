using Server.Items;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlStamDrain : XmlAttachment
    {
        private int m_Drain = 0;
        private TimeSpan m_Refractory = TimeSpan.FromSeconds(5); // 5 seconds default time between activations
        private DateTime m_EndTime;
        private int proximityrange = 1; // default movement activation from 5 tiles away

        [CommandProperty(AccessLevel.GameMaster)]
        public int Drain { get => m_Drain; set => m_Drain = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range { get => proximityrange; set => proximityrange = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Refractory { get => m_Refractory; set => m_Refractory = value; }

        private int m_WeaponUses; // zero = Unlimited weapon uses - zero is default

        [CommandProperty(AccessLevel.GameMaster)]
        public int WeaponUses { get => m_WeaponUses; set => m_WeaponUses = value; }
        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlStamDrain(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlStamDrain(int drain)
        {
            m_Drain = drain;
        }

        [Attachable]
        public XmlStamDrain(int drain, double refractory)
        {
            m_Drain = drain;
            Refractory = TimeSpan.FromSeconds(refractory);

        }

        [Attachable]
        public XmlStamDrain(int drain, double refractory, double expiresin)
        {
            m_Drain = drain;
            Expiration = TimeSpan.FromMinutes(expiresin);
            Refractory = TimeSpan.FromSeconds(refractory);
        }

        [Attachable]
        public XmlStamDrain(int drain, double refractory, double expiresin, int weaponuses)
        {
            m_Drain = drain;
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

            int drain = 0;

            if (m_Drain > 0)
            {
                drain = Utility.Random(m_Drain);
            }

            if (defender != null && attacker != null && drain > 0)
            {
                defender.Stam -= drain;
                if (defender.Stam < 0)
                {
                    defender.Stam = 0;
                }

                attacker.Stam += drain;
                if (attacker.Stam < 0)
                {
                    attacker.Stam = 0;
                }

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

        public override void OnSpellDamage(Item augmenter, Mobile caster, Mobile defender, ref int spelldamage, int phys, int fire, int cold, int pois, int nrgy)
        {
            OnWeaponHit(caster, defender, null, ref spelldamage, spelldamage);
        }

        public override bool HandlesOnMovement => true;

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
            writer.Write(m_Drain);
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
                    // version 1
                    Range = reader.ReadInt();
                    goto case 0;
                case 0:
                    // version 0
                    m_Drain = reader.ReadInt();
                    Refractory = reader.ReadTimeSpan();
                    TimeSpan remaining = reader.ReadTimeSpan();
                    m_EndTime = DateTime.UtcNow + remaining;
                    break;
            }
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            //1005158 -> Risucchio stamina
            if (Expiration > TimeSpan.Zero)
            {
                if (Refractory > TimeSpan.Zero)
                {
                    return new LogEntry(LocalizerA(1), string.Format("#{0}\t{1}\t{2:F2}\t{3:F1}", 1005158, m_Drain, Expiration.TotalMinutes, m_Refractory.TotalSeconds));
                }

                return new LogEntry(LocalizerA(2), string.Format("#{0}\t{1}\t{2:F2}", 1005158, m_Drain, Expiration.TotalMinutes));
            }
            else
            {
                if (Refractory > TimeSpan.Zero)
                {
                    return new LogEntry(LocalizerA(3), string.Format("#{0}\t{1}\t{2:F1}", 1005158, m_Drain, m_Refractory.TotalSeconds));
                }

                return new LogEntry(LocalizerA(4), string.Format("#{0}\t{1}", 1005158, m_Drain));
            }
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // announce it to the mob
            if (AttachedTo is Mobile)
            {
                if (m_Drain > 0)
                {
                    ((Mobile)AttachedTo).SendLocalizedMessage(505345);// "Hai il potere del risucchio stamina!");
                }
                else
                {
                    ((Mobile)AttachedTo).SendLocalizedMessage(505344);// "Hai la maledizione del risucchio stamina!");
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

            if (m_Drain > 0)
            {
                drain = Utility.Random(m_Drain);
            }

            if (drain > 0)
            {
                m.Stam -= drain;
                if (m.Stam < 0)
                {
                    m.Stam = 0;
                }
            }

            m_EndTime = DateTime.UtcNow + Refractory;

        }
    }
}
