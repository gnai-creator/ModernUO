using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlEnemyMastery : XmlAttachment
    {
        private int m_ShowName;
        private int m_Chance = 20;       // 20% chance by default
        private string m_Enemy;

        [CommandProperty(AccessLevel.GameMaster)]
        public int ShowName
        {
            get => m_ShowName;
            set
            {
                if (value > 0)
                {
                    m_ShowName = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RemainingUses { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Chance { get => m_Chance; set { if (value >= 0 && value <= 100) { m_Chance = value; } } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PercentIncrease { get; set; } = 50;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Enemy
        {
            get => m_Enemy;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                m_Enemy = value;
                // look up the type
                EnemyType = SpawnerType.GetType(m_Enemy);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Type EnemyType { get; private set; }

        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlEnemyMastery(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlEnemyMastery(string enemy)
        {
            Enemy = enemy;
        }

        [Attachable]
        public XmlEnemyMastery(string enemy, int increase)
        {
            PercentIncrease = increase;
            Enemy = enemy;
        }

        [Attachable]
        public XmlEnemyMastery(string enemy, int chance, int increase)
        {
            m_Chance = chance;
            PercentIncrease = increase;
            Enemy = enemy;
        }

        [Attachable]
        public XmlEnemyMastery(string enemy, int chance, int increase, double expiresin)
        {
            m_Chance = chance;
            PercentIncrease = increase;
            Expiration = TimeSpan.FromMinutes(expiresin);
            Enemy = enemy;
        }

        public XmlEnemyMastery(string enemy, int chance, int increase, double expiresin, int nametoshow)
        {
            m_Chance = chance;
            PercentIncrease = increase;
            Expiration = TimeSpan.FromMinutes(expiresin);
            Enemy = Name = enemy;
            ShowName = nametoshow;
        }

        public override void OnBeforeReattach(XmlAttachment old)
        {
            if (RealExpiration != TimeSpan.Zero)
            {
                Expiration = RealExpiration + old.Expiration;
            }
            if(RemainingUses > 0 && old is XmlEnemyMastery xem && xem.RemainingUses > 0)
            {
                RemainingUses += xem.RemainingUses;
            }
        }

        public override void OnAttach()
        {
            base.OnAttach();

            if (AttachedTo is Mobile)
            {
                Mobile m = AttachedTo as Mobile;
                Effects.PlaySound(m, m.Map, 516);
                if (Expiration > TimeSpan.Zero)
                {
                    if (RemainingUses > 0)
                        m.SendLocalizedMessage(1005131, $"{(m_ShowName > 0 ? string.Format("#{0}", m_ShowName) : Enemy)}\t{Expiration.TotalHours}\t{RemainingUses}");//Hai un bonus su ~1_val~ per ~2_val~ ore o ~3_val~ usi rimanenti
                    else
                        m.SendLocalizedMessage(1005132, $"{(m_ShowName > 0 ? string.Format("#{0}", m_ShowName) : Enemy)}\t{Expiration.TotalHours}");//Hai un bonus su ~1_val~ per ~2_val~ ore
                }
                else
                {
                    if (RemainingUses > 0)
                        m.SendLocalizedMessage(1005133, $"{(m_ShowName > 0 ? string.Format("#{0}", m_ShowName) : Enemy)}\t{RemainingUses}");//Hai un bonus su ~1_val~ con ~2_val~ usi rimanenti
                    else
                        m.SendLocalizedMessage(1005134, $"{(m_ShowName > 0 ? string.Format("#{0}", m_ShowName) : Enemy)}");//Hai un bonus su ~1_val~
                }
            }
        }

        // note that this method will be called when attached to either a mobile or a weapon
        // when attached to a weapon, only that weapon will do additional damage
        // when attached to a mobile, any weapon the mobile wields will do additional damage
        public override void OnWeaponHit(Mobile attacker, Mobile defender, BaseWeapon weapon, ref int damageGiven, int originalDamage)
        {
            if (EnemyType == null || defender == null ||
               !EnemyType.IsAssignableFrom(defender.GetType())
               || attacker == null || Utility.Random(100) >= m_Chance)
            {
                return;
            }

            damageGiven += (int)(damageGiven * (PercentIncrease * 0.01));
            if (RemainingUses > 0)
            {
                RemainingUses--;
                if (RemainingUses <= 0)
                    Delete();
            }
        }

        // when attached to a mobile, any spell the mobile launch will do additional damage
        public override void OnSpellDamage(Item augmenter, Mobile caster, Mobile defender, ref int spelldamage, int phys, int fire, int cold, int pois, int nrgy)
        {
            if (EnemyType == null || defender == null ||
               !EnemyType.IsAssignableFrom(defender.GetType())
               || caster == null || Utility.Random(100) >= m_Chance)
            {
                return;
            }

            spelldamage += (int)(spelldamage * (PercentIncrease * 0.01));
            if(RemainingUses > 0)
            {
                RemainingUses--;
                if (RemainingUses <= 0)
                    Delete();
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();

            if (AttachedTo is Mobile)
            {
                Mobile m = AttachedTo as Mobile;
                if (!m.Deleted)
                {
                    Effects.PlaySound(m, m.Map, 958);
                    //m.SendMessage(505353String.Format("Il bonus di danno su {0} svanisce..",Enemy));
                    m.SendLocalizedMessage(1004410, Enemy);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(4);
            // version 4
            writer.Write(RemainingUses);
            // version 3
            writer.Write(m_ShowName);
            // version 0
            writer.Write(PercentIncrease);
            writer.Write(m_Chance);
            writer.Write(m_Enemy);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 4:
                {
                    RemainingUses = reader.ReadInt();
                    goto case 3;
                }
                case 3:
				{
					m_ShowName=reader.ReadInt();
					goto case 0;
				}
                case 2:
                {
                    string s = reader.ReadString();
                    if (!string.IsNullOrEmpty(s))
                    {
                        s = s.ToLower();
                        if (s.Contains("morti") || s.Contains("dead"))
                            m_ShowName = 1004401;
                        else if (s.Contains("demon") || s.Contains("daemon"))
                            m_ShowName = 1004402;
                        else if (s.Contains("misti") || s.Contains("magic"))
                            m_ShowName = 1004403;
                    }
                    goto case 0;
                }
                case 1:
                {
                    reader.ReadBool();
                    goto case 0;
                }
                case 0:
                {
                    PercentIncrease = reader.ReadInt();
                    m_Chance = reader.ReadInt();
                    Enemy = reader.ReadString();
                    break;
                }
            }
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            //1005155 -> Maestria mostri
            if (m_ShowName > 0)
            {
                if (Expiration > TimeSpan.Zero)
                {
                    //~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%, finisce in ~5_val~ ore, ~6_val~ usi rimanenti
                    if(RemainingUses > 0)
                        return new LogEntry(LocalizerB(1), string.Format("#{0}\t{1}\t#{2}\t{3}\t{4:F2}\t{5}", 1005155, PercentIncrease, m_ShowName, m_Chance, Expiration.TotalHours, RemainingUses));
                    //~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%, finisce in ~5_val~ ore
                    return new LogEntry(LocalizerB(2), string.Format("#{0}\t{1}\t#{2}\t{3}\t{4:F2}", 1005155, PercentIncrease, m_ShowName, m_Chance, Expiration.TotalHours));
                }
                else
                {
                    //~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%, ~5_val~ usi rimanenti
                    if (RemainingUses > 0)
                        return new LogEntry(LocalizerB(3), string.Format("#{0}\t{1}\t#{2}\t{3}\t{4}", 1005155, PercentIncrease, m_ShowName, m_Chance, RemainingUses));
                    //~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%
                    return new LogEntry(LocalizerB(4), string.Format("#{0}\t{1}\t#{2}\t{3}", 1005155, PercentIncrease, m_ShowName, m_Chance));
                }
            }
            else if (m_Enemy != null)
            {
                if (Expiration > TimeSpan.Zero)
                {
                    //~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%, finisce in ~5_val~ ore, ~6_val~ usi rimanenti
                    if (RemainingUses > 0)
                        return new LogEntry(LocalizerB(1), string.Format("#{0}\t{1}\t{2}\t{3}\t{4:F2}\t{5}", 1005155, PercentIncrease, m_Enemy, m_Chance, Expiration.TotalHours, RemainingUses));
                    //~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%, finisce in ~5_val~ ore
                    return new LogEntry(LocalizerB(2), string.Format("#{0}\t{1}\t{2}\t{3}\t{4:F2}", 1005155, PercentIncrease, m_Enemy, m_Chance, Expiration.TotalHours));
                }
                else
                {
                    //~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%, ~5_val~ usi rimanenti
                    if (RemainingUses > 0)
                        return new LogEntry(LocalizerB(3), string.Format("#{0}\t{1}\t{2}\t{3}\t{4}", 1005155, PercentIncrease, m_Enemy, m_Chance, RemainingUses));
                    //~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%
                    return new LogEntry(LocalizerB(4), string.Format("#{0}\t{1}\t{2}\t{3}", 1005155, PercentIncrease, m_Enemy, m_Chance));
                }
            }

            return null;
        }
    }
}
