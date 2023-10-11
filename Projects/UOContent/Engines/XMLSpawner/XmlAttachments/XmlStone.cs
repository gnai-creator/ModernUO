using Server.Items;
using Server.Spells.Necromancy;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlStone : XmlAttachment
    {
        private int m_Stone = 0;
        private int m_Probability = 100;
        private bool m_RandomSecs = false;
        private TimeSpan m_Refractory = TimeSpan.FromSeconds(5);// 5 seconds default time between activations
        private DateTime m_EndTime;
        private int proximityrange = 1;// default movement activation from 1 tile away

        [CommandProperty(AccessLevel.GameMaster)]
        public int Stone { get => m_Stone; set => m_Stone = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RandomSecs { get => m_RandomSecs; set => m_RandomSecs = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range { get => proximityrange; set => proximityrange = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Probability { get => m_Probability; set => m_Probability = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Refractory { get => m_Refractory; set => m_Refractory = value; }

        private int m_WeaponUses; // default Unlimited weapon uses - zero is default

        [CommandProperty(AccessLevel.GameMaster)]
        public int WeaponUses { get => m_WeaponUses; set => m_WeaponUses = value; }
        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlStone(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlStone(int stone_millisec)
        {
            m_Stone = stone_millisec;
            m_RandomSecs = false;
        }

        [Attachable]
        public XmlStone(int stone_millisec, bool random_secs)
        {
            m_Stone = stone_millisec;
            m_RandomSecs = random_secs;
        }

        [Attachable]
        public XmlStone(int stone_millisec, int percent)
        {
            m_Stone = stone_millisec;
            m_RandomSecs = false;
            if (percent <= 100 && percent > 0)
            {
                m_Probability = percent;
            }
            else
            {
                m_Probability = 100;
            }
        }

        [Attachable]
        public XmlStone(int stone_millisec, int percent, bool random_secs)
        {
            m_Stone = stone_millisec;
            m_RandomSecs = random_secs;
            if (percent <= 100 && percent > 0)
            {
                m_Probability = percent;
            }
            else
            {
                m_Probability = 100;
            }
        }

        [Attachable]
        public XmlStone(int stone_millisec, int percent, double refractory)
        {
            m_Stone = stone_millisec;
            if (percent <= 100 && percent > 0)
            {
                m_Probability = percent;
            }
            else
            {
                m_Probability = 100;
            }

            Refractory = TimeSpan.FromSeconds(refractory);
            m_RandomSecs = false;
        }

        [Attachable]
        public XmlStone(int stone_millisec, int percent, double refractory, bool random_secs)
        {
            m_Stone = stone_millisec;
            if (percent <= 100 && percent > 0)
            {
                m_Probability = percent;
            }
            else
            {
                m_Probability = 100;
            }

            Refractory = TimeSpan.FromSeconds(refractory);
            m_RandomSecs = random_secs;
        }

        [Attachable]
        public XmlStone(int stone_millisec, int percent, double refractory, double expiresin)
        {
            m_Stone = stone_millisec;
            if (percent <= 100 && percent > 0)
            {
                m_Probability = percent;
            }
            else
            {
                m_Probability = 100;
            }

            Expiration = TimeSpan.FromMinutes(expiresin);
            Refractory = TimeSpan.FromSeconds(refractory);
            m_RandomSecs = false;
        }

        [Attachable]
        public XmlStone(int stone_millisec, int percent, double refractory, double expiresin, bool random_secs)
        {
            m_Stone = stone_millisec;
            if (percent <= 100 && percent > 0)
            {
                m_Probability = percent;
            }
            else
            {
                m_Probability = 100;
            }

            Expiration = TimeSpan.FromMinutes(expiresin);
            Refractory = TimeSpan.FromSeconds(refractory);
            m_RandomSecs = random_secs;
        }

        [Attachable]
        public XmlStone(int stone_millisec, int percent, double refractory, double expiresin, int weaponuses)
        {
            m_Stone = stone_millisec;
            if (percent <= 100 && percent > 0)
            {
                m_Probability = percent;
            }
            else
            {
                m_Probability = 100;
            }

            Expiration = TimeSpan.FromMinutes(expiresin);
            Refractory = TimeSpan.FromSeconds(refractory);
            m_WeaponUses = weaponuses;
            m_RandomSecs = false;
        }

        [Attachable]
        public XmlStone(int stone_millisec, int percent, double refractory, double expiresin, int weaponuses, bool random_secs)
        {
            m_Stone = stone_millisec;
            if (percent <= 100 && percent > 0)
            {
                m_Probability = percent;
            }
            else
            {
                m_Probability = 100;
            }

            Expiration = TimeSpan.FromMinutes(expiresin);
            Refractory = TimeSpan.FromSeconds(refractory);
            m_WeaponUses = weaponuses;
            m_RandomSecs = random_secs;
        }

        // note that this method will be called when attached to either a mobile or a weapon
        // when attached to a weapon, only that weapon will do additional damage
        // when attached to a mobile, any weapon the mobile wields will do additional damage
        public override void OnWeaponHit(Mobile attacker, Mobile defender, BaseWeapon weapon, ref int damageGiven, int originalDamage)
        {
            //non ha senso eseguire questo codice se l'arma non ha un effetto minimo
            if (m_Stone == 0)
            {
                Delete();
                return;
            }
            // if it is still refractory then return
            if (DateTime.UtcNow < m_EndTime)
            {
                return;
            }

            int stone_millisec = 0;

            if (m_RandomSecs)
            {
                stone_millisec = Utility.RandomMinMax(m_Stone / 3, m_Stone);
            }
            else
            {
                stone_millisec = m_Stone;
            }

            if (defender != null && attacker != null && stone_millisec > 0)
            {
                bool hittable = true;
                if (m_Probability < 100)
                {
                    hittable = Utility.Random(0, 100) < m_Probability;
                }

                if (hittable)
                {
                    StoneEffect(defender, stone_millisec);
                    if (m_WeaponUses != 0)
                    {
                        m_WeaponUses -= 1;
                        if (m_WeaponUses <= 0)
                        {
                            Delete();
                        }

                        return;
                    }
                }
                m_EndTime = DateTime.UtcNow + Refractory;
            }
        }

        private void StoneEffect(Mobile m, double duration)
        {
            if (m == null)
            {
                return;
            }

            m.FixedParticles(0x374A, 10, 15, 5013, EffectLayer.Waist);
            m.PlaySound(0x204);
            FleshToStoneSpell.StoneEffect(m, duration / 1000.0);
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

            writer.Write(0);
            writer.Write(m_RandomSecs);
            writer.Write(m_Probability);
            writer.Write(m_WeaponUses);
            writer.Write(proximityrange);
            writer.Write(m_Stone);
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
            /*int version =*/
            reader.ReadInt();
            m_RandomSecs = reader.ReadBool();
            m_Probability = reader.ReadInt();
            m_WeaponUses = reader.ReadInt();
            Range = reader.ReadInt();
            m_Stone = reader.ReadInt();
            Refractory = reader.ReadTimeSpan();
            TimeSpan remaining = reader.ReadTimeSpan();
            m_EndTime = DateTime.UtcNow + remaining;
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            //1005159 -> Pietrificazione
            if (Expiration > TimeSpan.Zero)
            {
                if (Refractory > TimeSpan.Zero)
                {
                    return new LogEntry(LocalizerA(1), string.Format("{0}\t{1} msec -\t{2:F2}\t{3:F1}", 1005159, m_Stone, Expiration.TotalMinutes, m_Refractory.TotalSeconds));
                }

                return new LogEntry(LocalizerA(2), string.Format("{0}\t{1} msec -\t{2:F2}", 1005159, m_Stone, Expiration.TotalMinutes));
            }
            else
            {
                if (Refractory > TimeSpan.Zero)
                {
                    return new LogEntry(LocalizerA(3), string.Format("{0}\t{1} msec -\t{2:F1}", 1005159, m_Stone, m_Refractory.TotalSeconds));
                }

                return new LogEntry(LocalizerA(3), string.Format("{0}\t{1} msec", 1005159, m_Stone));
            }
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // announce it to the mob
            if (AttachedTo is Mobile mobile)
            {
                if (m_Stone > 0)
                {
                    mobile.SendLocalizedMessage(505343);// "Hai il potere del tocco pietrificante!");
                }
            }
        }

        public override void OnTrigger(object activator, Mobile m)
        {
            if (m == null)
            {
                return;
            }

            if (m_Stone == 0)
            {
                Delete();
                return;
            }

            // if it is still refractory then return
            if (DateTime.UtcNow < m_EndTime)
            {
                return;
            }

            int stone_millisec;

            if (m_RandomSecs)
            {
                stone_millisec = Utility.Random(m_Stone);
            }
            else
            {
                stone_millisec = m_Stone;
            }

            bool hittable = true;
            if (m_Probability < 100)
            {
                hittable = Utility.Random(0, 100) < m_Probability;
            }

            if (hittable)
            {
                StoneEffect(m, stone_millisec);
                //                if(m_WeaponUses != 0)
                //				{
                //					m_WeaponUses -= 1;
                //					if(m_WeaponUses <= 0 )
                //						this.Delete();
                //					return;
                //				}
            }
            m_EndTime = DateTime.UtcNow + Refractory;
        }
    }
}