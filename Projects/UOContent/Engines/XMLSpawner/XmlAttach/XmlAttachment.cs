using Server.Items;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Engines.XmlSpawner2
{
    public abstract class XmlAttachment// : IXmlAttachment
    {
        private string m_Name;

        private IEntity m_AttachedTo;
        private string m_AttachedBy;
        private AttachmentTimer m_ExpirationTimer;

        private TimeSpan m_Expiration = TimeSpan.Zero;     // no expiration by default

        private static int s_TypeA_Pos = 1005134;
        private static int s_TypeB_Pos = 1005070;
        private static int s_TypeC_Pos = 1005160;
        private static int s_TypeDef = -1;
        private static int[] s_TypeDef_Loc =
        {
            1005041,
            1005042,
            1005043,
            1005044,
            1005045,
            1005046,
            1005047,
            1005048
        };

        // ----------------------------------------------
        // Public properties
        // ----------------------------------------------

        /// <summary>
        /// ritorna un numero di localizzazione che accetta come parametro una stringa costruibile a piacere
        /// </summary>
        /// <returns>numero di localizzazione</returns>
        protected static int LocalizerDef()
        {
            s_TypeDef = (s_TypeDef + 1) % 8;
            return s_TypeDef_Loc[s_TypeDef];
        }

        /// <summary>
        /// 1 -> ~1_val~ ~2_val~ finisce in ~3_val~ min : ~4_val~ sec tra ogni uso<br></br>
        /// 2 -> ~1_val~ ~2_val~ finisce in ~3_val~ min<br></br>
        /// 3 -> ~1_val~ ~2_val~ : ~3_val~ sec tra ogni uso<br></br>
        /// 4 -> ~1_val~ ~2_val~<br></br>
        /// 5 -> ~1_val~ (1/~2_val~)
        /// </summary>
        /// <param name="num">indica il tipo di cliloc emesso come da tabella sommario, valore massimo consentito è il 5 (minimo 1), valori diversi da questi creano localizzazioni sballate</param>
        /// <returns>cliloc emesso come da tabella sommario</returns>
        protected static int LocalizerA(int num)
        {
            s_TypeA_Pos += 5;
            if (s_TypeA_Pos >= 1005149)
                s_TypeA_Pos = 1005134;
            return s_TypeA_Pos + num;
        }

        /// <summary>
        /// 1 -> ~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%, finisce in ~5_val~ ore, ~6_val~ usi rimanenti<br></br>
        /// 2 -> ~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%, finisce in ~5_val~ ore<br></br>
        /// 3 -> ~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%, ~5_val~ usi rimanenti<br></br>
        /// 4 -> ~1_val~ : +~2_val~% danno vs ~3_val~, ~4_val~%
        /// </summary>
        /// <param name="num">indica il tipo di cliloc emesso come da tabella sommario, valore massimo consentito è il 4 (minimo 1), valori diversi da questi creano localizzazioni sballate</param>
        /// <returns>cliloc emesso come da tabella sommario</returns>
        protected static int LocalizerB(int num)
        {
            s_TypeB_Pos += 4;
            if (s_TypeB_Pos >= 1005078)
                s_TypeB_Pos = 1005070;
            return s_TypeB_Pos + num;
        }

        /// <summary>
        /// 1 -> ~1_val~ dura ~2_val~ secondi - ~3_val~ secondi tra ogni uso : ~4_val~ usi rimasti<br></br>
        /// 2 -> ~1_val~ dura ~2_val~ secondi - ~3_val~ secondi tra ogni uso<br></br>
        /// 3 -> ~1_val~ dura ~2_val~ secondi : ~3_val~ usi rimasti<br></br>
        /// 4 -> ~1_val~ dura ~2_val~ secondi<br></br>
        /// </summary>
        /// <param name="num">indica il tipo di cliloc emesso come da tabella sommario, valore massimo consentito è il 4 (minimo 1), valori diversi da questi creano localizzazioni sballate</param>
        /// <returns>cliloc emesso come da tabella sommario</returns>
        protected static int LocalizerC(int num)
        {
            s_TypeC_Pos += 4;
            if (s_TypeC_Pos >= 1005168)
                s_TypeC_Pos = 1005160;
            return s_TypeC_Pos + num;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime CreationTime { get; private set; }

        public bool Deleted { get; private set; }

        public bool DoDelete { get => false; set { if (value) { Delete(); } } }

        [CommandProperty(AccessLevel.Administrator)]
        public bool NoOverridesDelete
        {
            get => false;
            set
            {
                if (value == true)
                {
                    if (Deleted)
                    {
                        return;
                    }

                    Deleted = true;

                    if (m_ExpirationTimer != null)
                    {
                        m_ExpirationTimer.Stop();
                    }

                    // dereference the attachment object
                    AttachedTo = null;
                    OwnedBy = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SerialValue => Serial.Value;

        public ASerial Serial { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Expiration
        {
            get
            {
                // if the expiration timer is running then return the remaining time
                if (m_ExpirationTimer != null)
                {
                    return ExpirationEnd - DateTime.UtcNow;
                }
                else
                {
                    return m_Expiration;
                }
            }
            set
            {
                m_Expiration = value;
                // if it is already attached to something then set the expiration timer
                if (m_AttachedTo != null)
                {
                    DoTimer(m_Expiration);
                }
            }
        }

        public TimeSpan RealExpiration => m_Expiration;

        public DateTime ExpirationEnd { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool CanActivateInBackpack => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool CanActivateEquipped => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool CanActivateInWorld => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool HandlesOnSpeech => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool HandlesOnMovement => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool HandlesOnKill => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool HandlesOnKilled => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool HandlesOnSkillUse => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string Name { get => m_Name; set => m_Name = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual IEntity Attached => m_AttachedTo;

        public virtual IEntity AttachedTo { get => m_AttachedTo; set => m_AttachedTo = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string AttachedBy => m_AttachedBy;

        public IEntity OwnedBy { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public IEntity Owner => OwnedBy;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual object GenericInternal => null;

        public override string ToString()
        {
            return string.Format("0x{0:X} \"{1}\" AttachedTo -> {2}", Serial.Value, GetType().Name, m_AttachedTo != null ? m_AttachedTo.ToString() : "(-NULL-)");
        }

        // ----------------------------------------------
        // Private methods
        // ----------------------------------------------
        private void DoTimer(TimeSpan delay)
        {
            bool restart = false;
            if (m_ExpirationTimer != null)
            {
                m_ExpirationTimer.Stop();
                restart = true;
            }

            if (!restart || delay > TimeSpan.Zero)
            {
                ExpirationEnd = DateTime.UtcNow + delay;
                m_ExpirationTimer = new AttachmentTimer(this, delay);
                m_ExpirationTimer.Start();
            }
            else
            {
                ExpirationEnd = DateTime.MinValue;
                m_ExpirationTimer = null;
            }
        }

        // a timer that can be implement limited lifetime attachments
        private class AttachmentTimer : Timer
        {
            private XmlAttachment m_Attachment;

            public AttachmentTimer(XmlAttachment attachment, TimeSpan delay)
                : base(delay)
            {
                Priority = TimerPriority.OneSecond;

                m_Attachment = attachment;
            }

            protected override void OnTick()
            {
                if (m_Attachment.OnExpiration())
                {
                    m_Attachment.Delete();
                }
                else
                {
                    m_Attachment.m_Expiration = TimeSpan.Zero;
                    m_Attachment.m_ExpirationTimer?.Stop();
                }
            }
        }

        // ----------------------------------------------
        // Constructors
        // ----------------------------------------------
        public XmlAttachment()
        {
            CreationTime = DateTime.UtcNow;

            // get the next unique serial id
            Serial = ASerial.NewSerial();

            // register the attachment in the serial keyed hashtable
            XmlAttach.HashSerial(Serial, this);
        }

        // needed for deserialization
        public XmlAttachment(ASerial serial)
        {
            Serial = serial;
        }

        // ----------------------------------------------
        // Public methods
        // ----------------------------------------------

        public static void Initialize()
        {
            XmlAttach.CleanUp();
        }

        public virtual bool CanEquip(Mobile from)
        {
            return true;
        }

        public virtual bool OnExpiration()
        {
            return true;
        }

        public virtual void OnEquip(Mobile from)
        {
        }

        public virtual void OnRemoved(IEntity parent)
        {
        }

        public virtual void OnAttach()
        {
            // start up the expiration timer on attachment
            if (m_Expiration > TimeSpan.Zero)
            {
                DoTimer(m_Expiration);
            }
        }

        public virtual void OnBeforeReattach(XmlAttachment old)
        {
        }

        public virtual void OnReattach()
        {
        }

        public virtual void OnUse(Mobile from)
        {
        }

        public virtual void OnUser(object target)
        {
        }

        public virtual bool BlockDefaultOnUse(Mobile from, object target)
        {
            return false;
        }

        public virtual bool OnDragLift(Mobile from, Item item)
        {
            return true;
        }

        public void SetAttachedBy(string name)
        {
            m_AttachedBy = name;
        }

        public virtual void OnSpeech(SpeechEventArgs args)
        {
        }

        public virtual void OnMovement(MovementEventArgs args)
        {
        }

        public virtual void OnKill(Mobile killed, Mobile killer)
        {
        }

        public virtual void OnBeforeKill(Mobile killed, Mobile killer)
        {
        }

        public virtual void OnKilled(Mobile killed, Mobile killer, bool last)
        {
        }

        public virtual void OnBeforeKilled(Mobile killed, Mobile killer)
        {
        }

        public virtual void OnSkillUse(Mobile m, Skill skill, bool success)
        {
        }

        public virtual void OnSpellDamage(Item augmenter, Mobile caster, Mobile defender, ref int spelldamage, int phys, int fire, int cold, int pois, int nrgy)
        {
        }

        public virtual void OnHitBySpell(Mobile caster, Mobile defender, ref int spelldamage, int phys, int fire, int cold, int pois, int nrgy)
        {
        }

        public virtual void OnWeaponHit(Mobile attacker, Mobile defender, BaseWeapon weapon, ref int damageGiven, int originalDamage)
        {
        }

        public virtual int OnArmorHit(Mobile attacker, Mobile defender, Item armor, BaseWeapon weapon, int damageGiven)
        {
            return 0;
        }

        public virtual void ShieldDamageMod(Mobile attacker, BaseShield shield, ref float bonus)
        {
        }

        public virtual LogEntry OnIdentify(Mobile from)
        {
            return null;
        }

        public virtual LogEntry DisplayedProperties(Mobile from)
        {
            return OnIdentify(from);
        }

        public virtual void AddProperties(ObjectPropertyList list)
        {
        }

        public void InvalidateParentProperties()
        {
            if (AttachedTo is Item item)
            {
                item.InvalidateProperties();
            }
        }

        public void SafeItemDelete(Item item)
        {
            Timer.DelayCall(TimeSpan.Zero, DeleteItemCallback, item);

        }

        public void DeleteItemCallback(Item item)
        {
            // delete the item
            item?.Delete();
        }

        public void SafeMobileDelete(Mobile mob)
        {
            Timer.DelayCall(TimeSpan.Zero, DeleteMobileCallback, mob);

        }

        public void DeleteMobileCallback(Mobile mob)
        {
            // delete the mobile
            mob?.Delete();
        }

        public void Delete()
        {
            if (World.DecayLock)
            {
                AddToPostLoadDelete(this);
                return;
            }
            else if (Deleted)
            {
                return;
            }

            Deleted = true;

            if (m_ExpirationTimer != null)
            {
                m_ExpirationTimer.Stop();
            }

            OnDelete();

            if(AttachedTo != null && XmlAttach.EntityAttachments.TryGetValue(AttachedTo, out List<XmlAttachment> alist) && alist != null)
            {
                //we remove any and all reference of this instance
                while (alist.Remove(this)){}
            }
            // dereference the attachment object
            AttachedTo = null;
            OwnedBy = null;
        }

        public virtual void OnDelete()
        {
        }

        public virtual void OnTrigger(object activator, Mobile from)
        {
        }

        public virtual void Serialize(GenericWriter writer)
        {
            writer.Write(2);
            // version 2
            writer.Write(m_AttachedBy);
            // version 1
            if (OwnedBy is Item)
            {
                writer.Write(0);
                writer.Write((Item)OwnedBy);
            }
            else
                if (OwnedBy is Mobile)
            {
                writer.Write(1);
                writer.Write((Mobile)OwnedBy);
            }
            else
            {
                writer.Write(-1);
            }

            // version 0
            writer.Write(Name);
            // if there are any active timers, then serialize
            writer.Write(m_Expiration);
            if (m_ExpirationTimer != null)
            {
                writer.Write(ExpirationEnd - DateTime.UtcNow);
            }
            else
            {
                writer.Write(TimeSpan.Zero);
            }
            writer.Write(CreationTime);
        }

        public virtual void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    m_AttachedBy = reader.ReadString();
                    goto case 1;
                case 1:
                    int owned = reader.ReadInt();
                    if (owned == 0)
                    {
                        OwnedBy = reader.ReadItem();
                    }
                    else if (owned == 1)
                    {
                        OwnedBy = reader.ReadMobile();
                    }
                    else
                    {
                        OwnedBy = null;
                    }

                    goto case 0;
                case 0:
                    // version 0
                    Name = reader.ReadString();
                    m_Expiration = reader.ReadTimeSpan();
                    TimeSpan remaining = reader.ReadTimeSpan();

                    if (remaining > TimeSpan.Zero)
                    {
                        DoTimer(remaining);
                    }
                    else if (m_Expiration != TimeSpan.Zero && remaining <= TimeSpan.Zero)
                    {
                        AddToPostLoadDelete(this);
                    }

                    CreationTime = reader.ReadDateTime();
                    break;
            }
        }

        private static List<XmlAttachment> AttachmentsToDelete = new List<XmlAttachment>();
        protected static void AddToPostLoadDelete(XmlAttachment a)
        {
            AttachmentsToDelete?.Add(a);
        }

        internal static void PostLoadChecks()
        {
            if (AttachmentsToDelete != null)
            {
                for (int i = AttachmentsToDelete.Count - 1; i >= 0; --i)
                {
                    XmlAttachment a = AttachmentsToDelete[i];
                    if (a != null && !a.Deleted)
                    {
                        if (!a.UnsavedAttach)
                            Console.WriteLine($"Cancello attachment {a}{(a.m_Expiration != TimeSpan.Zero ? " già scaduto" : "")}");
                        a.Delete();
                    }
                }
            }
            AttachmentsToDelete = null;
        }

        protected virtual bool UnsavedAttach => false;

        internal virtual string EntryString { get; }
    }
}
