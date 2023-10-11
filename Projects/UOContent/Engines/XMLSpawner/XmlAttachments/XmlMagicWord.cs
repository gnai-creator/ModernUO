using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlMagicWord : XmlAttachment
    {
        private string Word;
        private TimeSpan Duration = TimeSpan.FromSeconds(60.0);     // 30 sec default duration for effects
        private int Charges = 1;                        // single use by default, note a value of zero or less means unlimited use
        private TimeSpan Refractory = TimeSpan.Zero;    // no refractory period
        private DateTime m_EndTime = DateTime.MinValue;

        // static list used for random word assignment
        private static string[] keywordlist = new string[] { "Shoda", "Malik", "Lepto", "Velas", "Tarda", "Marda", "Vas Malik", "Nartor", "Santor" };

        // note that support for player identification requires modification of the identification skill (see the installation notes for details)
        private bool m_Identified = false;  // optional identification flag that can suppress application of the mod until identified when applied to items

        private bool m_RequireIdentification = false;  // by default no identification is required for the mod to be activatable

        // this property can be set allowing individual items to determine whether they must be identified for the mod to be activatable
        public bool RequireIdentification { get => m_RequireIdentification; set => m_RequireIdentification = value; }

        // a serial constructor is REQUIRED
        public XmlMagicWord(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlMagicWord()
        {
            Word = keywordlist[Utility.Random(keywordlist.Length)];
            Name = Word;
        }

        [Attachable]
        public XmlMagicWord(string word)
        {
            Word = word;
            Name = word;
        }

        [Attachable]
        public XmlMagicWord(string word, double duration)
        {
            Name = word;
            Word = word;
            Duration = TimeSpan.FromSeconds(duration);
        }

        [Attachable]
        public XmlMagicWord(string word, double duration, double refractory)
        {
            Name = word;
            Word = word;
            Duration = TimeSpan.FromSeconds(duration);
            Refractory = TimeSpan.FromSeconds(refractory);
        }

        [Attachable]
        public XmlMagicWord(string word, double duration, double refractory, int charges)
        {
            Name = word;
            Word = word;
            Duration = TimeSpan.FromSeconds(duration);
            Refractory = TimeSpan.FromSeconds(refractory);
            Charges = charges;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(Word);
            writer.Write(Charges);
            writer.Write(Duration);
            writer.Write(Refractory);
            if (m_EndTime <= DateTime.UtcNow)
            {
                writer.Write(TimeSpan.Zero);
            }
            else
            {
                writer.Write(m_EndTime.Subtract(DateTime.UtcNow));
            }

            writer.Write(m_RequireIdentification);
            writer.Write(m_Identified);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            // version 0
            Word = reader.ReadString();
            Charges = reader.ReadInt();
            Duration = reader.ReadTimeSpan();
            Refractory = reader.ReadTimeSpan();
            TimeSpan remaining = reader.ReadTimeSpan();
            m_EndTime = DateTime.UtcNow + remaining;
            m_RequireIdentification = reader.ReadBool();
            m_Identified = reader.ReadBool();
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            // can force identification before the skill mods can be applied
            if (from != null && from.AccessLevel == AccessLevel.Player)
            {
                m_Identified = true;
            }

            if (RequireIdentification && !m_Identified)
            {
                return null;
            }

            if (Refractory > TimeSpan.Zero)
            {
                if (Charges > 0)
                {
                    return new LogEntry(LocalizerC(1), string.Format("{0}\t{1:F1}\t{2:F1}\t{3}", Word, Duration.TotalSeconds, Refractory.TotalSeconds, Charges));
                }

                return new LogEntry(LocalizerC(2), string.Format("{0}\t{1:F1}\t{2:F1}", Word, Duration.TotalSeconds, Refractory.TotalSeconds));
            }
            else
            {
                if (Charges > 0)
                {
                    return new LogEntry(LocalizerC(3), string.Format("{0}\t{1:F1}\t{2}", Word, Duration.TotalSeconds, Charges));
                }

                return new LogEntry(LocalizerC(4), string.Format("{0}\t{1:F1}", Word, Duration.TotalSeconds));
            }
        }

        // by overriding these properties armor and weapons can be restricted to trigger on speech only when equipped and not when in the pack or in the world
        public override bool CanActivateInBackpack
        {
            get
            {
                if (AttachedTo is BaseWeapon || AttachedTo is BaseArmor)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public override bool CanActivateInWorld
        {
            get
            {
                if (AttachedTo is BaseWeapon || AttachedTo is BaseArmor)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public override bool HandlesOnSpeech => true;

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            if (e.Mobile == null || e.Mobile.AccessLevel > AccessLevel.Player)
            {
                return;
            }

            // dont respond to other players speech if this is attached to a mob
            if (AttachedTo is Mobile && (Mobile)AttachedTo != e.Mobile)
            {
                return;
            }

            if (e.Speech == Word)
            {
                OnTrigger(null, e.Mobile);
            }
        }

        public void Hide_Callback(Mobile m)
        {
            if (m != null && !m.Deleted)
                m.Hidden = true;
        }

        public override void OnTrigger(object activator, Mobile m)
        {
            if (m == null || Word == null || (RequireIdentification && !m_Identified))
            {
                return;
            }

            if (DateTime.UtcNow < m_EndTime)
            {
                return;
            }

            string msgstr = "Attivo il potere di " + Word;

            // assign powers to certain words
            switch (Word)
            {
                case "Shoda":
                    m.AddStatMod(new StatMod(StatType.Int, "Shoda", 20, Duration));
                    m.SendMessage("Senti il potere scorrere nella tua mente!");
                    break;
                case "Malik":
                    m.AddStatMod(new StatMod(StatType.Str, "Malik", 20, Duration));
                    m.SendMessage("Senti la tua forza aumentare!");
                    break;
                case "Lepto":
                    m.AddStatMod(new StatMod(StatType.Dex, "Lepto", 20, Duration));
                    m.SendMessage("Ti senti più veloce!");
                    break;
                case "Velas":
                    Timer.DelayCall(TimeSpan.Zero, Hide_Callback, m);
                    m.SendMessage("Diventi invisibile!");
                    break;
                case "Tarda":
                    m.AddSkillMod(new TimedSkillMod(SkillName.Tactics, true, 20, Duration));
                    m.SendMessage("Senti che le tue abilità combattive sono aumentate!");
                    break;
                case "Marda":
                    m.AddSkillMod(new TimedSkillMod(SkillName.Magery, true, 20, Duration));
                    m.SendMessage("Senti il tuo potere magico aumentare!");
                    break;
                case "Vas Malik":
                    m.AddStatMod(new StatMod(StatType.Str, "Vas Malik", 40, Duration));
                    m.SendMessage("Ti senti veramente forte!");
                    break;
                case "Nartor":
                    BaseCreature b = new Drake();
                    b.MoveToWorld(m.Location, m.Map);
                    b.Owners.Add(m);
                    b.SetControlMaster(m);
                    if (b.Controlled)
                    {
                        m.SendMessage("Diventi padrone della bestia!");
                    }

                    break;
                case "Santor":
                    b = new Horse();
                    b.MoveToWorld(m.Location, m.Map);
                    b.Owners.Add(m);
                    b.SetControlMaster(m);
                    if (b.Controlled)
                    {
                        m.SendMessage("Diventi padrone dell'animale!");
                    }

                    break;
                default:
                    m.SendMessage("Nessun effetto.");
                    break;
            }

            // display activation effects
            Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042);
            Effects.PlaySound(m, m.Map, 0x201);

            // display a message over the item it was attached to
            if (AttachedTo is Item)
            {
                ((Item)AttachedTo).PublicOverheadMessage(MessageType.Regular, 0x3B2, true, msgstr);
            }

            Charges--;

            // remove the attachment after the charges run out
            if (Charges == 0)
            {
                Delete();
            }
            else
            {
                m_EndTime = DateTime.UtcNow + Refractory;
            }
        }
    }
}
