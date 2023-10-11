using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Engines.XmlSpawner2
{
    public class XmlLock : XmlSiege, ILockpickable
    {
        public override int LightDamageEffectID => 14201;  // 14201 = sparkle
        public override int MediumDamageEffectID => 14201;
        public override int HeavyDamageEffectID => 14201;

        private Mobile m_Attacher;
        private ILockable m_LockedThing;

        #region ILockPickable Interface
        public int LockLevel { get; set; }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool Locked
        {
            get
            {
                if (m_LockedThing != null)
                {
                    return m_LockedThing.Locked;
                }
                else if (m_LockedThing != null)
                {
                    return m_LockedThing.Locked;
                }

                return false;
            }
            set
            { }
        }
        public Mobile Picker { get => m_Attacher; set { } }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int MaxLockLevel { get; set; }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int RequiredSkill { get; set; }
        #region sub-ILockPickable IPoint2D Interface
        public int X
        {
            get
            {
                if (m_LockedThing != null)
                {
                    return m_LockedThing.Location.X;
                }

                return 0;
            }
        }
        public int Y
        {
            get
            {
                if (m_LockedThing != null)
                {
                    return m_LockedThing.Location.Y;
                }

                return 0;
            }
        }
        #endregion

        public void SendLocalizedMessageTo(Mobile to, int number)
        {
            if (Deleted || !(m_LockedThing is Item item) || !to.CanSee(item))
            {
                return;
            }

            to.Send(new MessageLocalized(item.Serial, item.ItemID, MessageType.Regular, 0x3B2, 3, number, "", ""));
        }

        public void LockPick(Mobile from)
        {
            Delete();
        }
        #endregion

        private uint m_Key;

        [Attachable]
        public XmlLock(Mobile attacher, int locklevel, int maxlocklevel, int requiredskill, int hits) : base(hits)
        {
            m_Attacher = attacher;
            LockLevel = locklevel;
            MaxLockLevel = maxlocklevel;
            RequiredSkill = requiredskill;
            DestroyedItemID = 0;
        }

        public XmlLock(ASerial serial) : base(serial)
        {
        }

        public override void OnAttach()
        {
            if (!(AttachedTo is Item) || m_Attacher == null)
            {
                Delete();
                return;
            }

            StoreOriginalItemID((Item)AttachedTo);

            m_Key = Key.RandomValue();
            if (AttachedTo is ILockable)
            {
                m_LockedThing = (ILockable)AttachedTo;
                m_LockedThing.Locked = true;
                m_LockedThing.KeyValue = m_Key;
                if (m_LockedThing is BaseDoor)
                {
                    Expiration = TimeSpan.FromSeconds(Hits * 100);
                    BaseDoor bd = (BaseDoor)m_LockedThing;
                    bd.Z--;
                    Timer.DelayCall(() => { bd.InvalidateProperties(); bd.Z++; });
                }
                else
                {
                    LockableContainer lc = (LockableContainer)m_LockedThing;
                    lc.ReleaseWorldPackets();
                    Timer.DelayCall(() => { lc.InvalidateProperties(); });
                }
                FinishLock();
            }
            else
            {
                Delete();
                return;
            }
        }

        private void FinishLock()
        {
            m_Attacher.AddToBackpack(new Key(KeyType.Rusty, m_Key) { LootType = LootType.Blessed, QuestItem = true });
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            if (Expiration > TimeSpan.Zero && from != null && from.NetState != null)//Chiusura con lucchetto:\nResistenza Lucchetto ~1_val~\n(Termina il ~2_val~ UTC+1)
            {
                return new LogEntry(1005171, string.Format("{0}\t{1:dd/MM HH:mm:ss}", Hits, ExpirationEnd.AddHours(from.NetState.TimeOffset)));
            }
            //Chiusura con lucchetto:\nResistenza Lucchetto ~1_val~
            return new LogEntry(1005172, Hits.ToString());
        }

        public override void OnDestroyed()
        {
            if (m_LockedThing != null)
            {
                if (DestroyedItemID <= 0 || !m_LockedThing.IsDestroyable)
                {
                    if (AttachedTo is Item item)
                    {
                        item.PublicOverheadMessage(MessageType.Emote, 33, true, "*La serratura si rompe*");
                        Effects.PlaySound(item, item.Map, 0x1FF);
                    }
                    Delete();
                }
                else
                {
                    if (m_LockedThing.Map != null && m_LockedThing.Map != Map.Internal)
                    {
                        Item it = (Item)m_LockedThing;
                        List<Item> movelist = new List<Item>(it.Items);

                        foreach (Item i in movelist)
                        {
                            // spill the contents out onto the ground
                            i.MoveToWorld(it.Location, it.Map);
                        }

                        new Debris().MoveToWorld(it.Location, it.Map);
                        Effects.SendLocationEffect(it, it.Map, 0x36B0, 16, 1);
                        Effects.PlaySound(it, it.Map, 0x11D);
                        // and permanently destroy the container
                        it.Delete();
                    }
                }
            }
            else
            {
                base.OnDestroyed();
            }
        }

        public class Debris : Item
        {
            [Constructable]
            public Debris() : base(Utility.RandomMinMax(3117, 3120))
            {
                Movable = false;
                new InternalTimer(this).Start();
            }

            public Debris(Serial serial) : base(serial)
            {
                new InternalTimer(this).Start();
            }

            public override void Serialize(GenericWriter writer)
            {
                //				base.Serialize( writer );
                //	
                //				writer.Write( (int) 0 ); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                //				base.Deserialize( reader );
                //	
                //				int version = reader.ReadInt();
            }
        }

        private class InternalTimer : Timer
        {
            private Item m_Item;

            public InternalTimer(Item item) : base(TimeSpan.FromSeconds(Utility.RandomMinMax(15, 30)))
            {
                Priority = TimerPriority.OneSecond;

                m_Item = item;
            }

            protected override void OnTick()
            {
                m_Item.Delete();
            }
        }

        public override void ApplyScaledDamage(Mobile from, int firedamage, int physicaldamage, Item virtualitem)
        {
            if (!Enabled || Hits == 0)
            {
                return;
            }

            if (AttachedTo is Item item)
            {
                //resistenza lievemente diminuita
                int firescale = 1000 - ResistFire;
                int physicalscale = 1000 - ResistPhysical;
                if (firescale < 0)
                {
                    firescale = 0;
                }

                if (physicalscale < 0)
                {
                    physicalscale = 0;
                }

                int scaleddamage = (firedamage * firescale + physicaldamage * physicalscale) / 1000;
                // subtract the scaled damage from the current hits
                if ((Hits - scaleddamage) <= 0)
                {
                    item.MobileTrigger(from, false);//for special uses
                    if (scaleddamage >= HitsMax)
                    {
                        DestroyedItemID = 2324;
                    }
                }
                Hits -= scaleddamage;

                if (item is BaseAddon && !item.Visible && ((BaseAddon)item).Components != null && ((BaseAddon)item).Components.Count > 0)
                {
                    foreach (AddonComponent c in ((BaseAddon)item).Components)
                    {
                        if (c != null && c.Visible)
                        {
                            c.PublicOverheadMessage(MessageType.Regular, 33, true, string.Format("{0}", scaleddamage));
                            break;
                        }
                    }
                }
                else
                {
                    item.PublicOverheadMessage(MessageType.Regular, 33, true, string.Format("{0}", scaleddamage));
                }

                if (from != null)
                {
                    from.SendLocalizedMessage(505352, scaleddamage.ToString());// "Hai fatto {0} punti danno.", scaleddamage);
                }

                item.InvalidateProperties();
            }
        }

        public override void AfterRepair()
        {
            if (AttachedTo is Item)
            {
                RestoreOriginalItemID((Item)AttachedTo);
            }

            Delete();
        }

        public override void OnDelete()
        {
            if (m_Attacher != null)
            {
                Key.RemoveKeys(m_Attacher, m_Key);
            }
            if (m_LockedThing != null)
            {
                m_LockedThing.Locked = false;
                m_LockedThing.KeyValue = 0;
            }
            if (m_LockedThing != null)
            {
                m_LockedThing.Locked = false;
                m_LockedThing.KeyValue = 0;
            }
            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(m_Attacher);
            writer.Write(m_LockedThing.Serial);
            writer.Write(m_Key);
            writer.Write(LockLevel);
            writer.Write(MaxLockLevel);
            writer.Write(RequiredSkill);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Attacher = reader.ReadMobile();
            if (version == 0)
            {
                BaseDoor bd = reader.ReadItem<BaseDoor>();
                LockableContainer lc = reader.ReadItem<LockableContainer>();
                if (bd != null)
                {
                    m_LockedThing = bd;
                }
                else if (lc != null)
                {
                    m_LockedThing = lc;
                }
            }
            else
            {
                m_LockedThing = World.FindItem(reader.ReadInt()) as ILockable;
            }
            /*m_LockedThing=reader.ReadItem<BaseDoor>();
m_LockedThing=reader.ReadItem<LockableContainer>();*/
            m_Key = reader.ReadUInt();
            LockLevel = reader.ReadInt();
            MaxLockLevel = reader.ReadInt();
            RequiredSkill = reader.ReadInt();
        }
    }
}