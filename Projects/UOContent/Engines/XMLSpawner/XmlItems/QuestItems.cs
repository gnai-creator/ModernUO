using Server.Engines.XmlSpawner2;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class QuestItemDestructible : Item
    {
        private int m_Probability; // serialize
        private Type m_TriggerWhat = null; // serialize
        public int RegionTriggers { get; set; } // MUST serialize

        [CommandProperty(AccessLevel.GameMaster)]
        public string TriggerObjOpts { get; set; } // serialize

        [CommandProperty(AccessLevel.GameMaster)]
        public string OnDestroy { get; set; } // MUST serialize

        [CommandProperty(AccessLevel.Developer)]
        public bool DebugOpt { get; set; } //temporary, don't serialize

        [Constructable]
        public QuestItemDestructible(int hits, int resistfire, int resistphis, int itemmin, int itemmax, int huemin, int huemax)
            : this(hits, resistfire, resistphis, itemmin, itemmax, huemin, huemax, 0, 0, 0, null, 0)
        {
        }

        [Constructable]
        public QuestItemDestructible(int hits, int resistfire, int resistphis, int itemmin, int itemmax, int huemin, int huemax, int autorepairtime, int destroyedid, int triggerpercent, string triggerwhat, int regionmaxtriggers) : base()
        {
            Movable = false;
            ItemID = Utility.RandomMinMax(itemmin, itemmax);
            Hue = Utility.RandomMinMax(huemin, huemax);
            RegionTriggers = regionmaxtriggers;
            if (hits > 0)
            {
                if (autorepairtime > 0 && destroyedid > 0)
                {
                    XmlAttach.AttachTo(this, new XmlSiege(hits, resistfire, resistphis, 0, 0, 0, destroyedid, autorepairtime));
                }
                else
                {
                    XmlAttach.AttachTo(this, new XmlSiege(hits, resistfire, resistphis, 0, 0, 0, 0));
                }
            }

            if (triggerpercent > 0)
            {
                m_Probability = Math.Min(triggerpercent, 100);
                if (triggerwhat != null)
                {
                    m_TriggerWhat = ScriptCompiler.FindTypeByName(triggerwhat, true);
                }
            }
            else
            {
                m_Probability = 0;
            }
        }

        public QuestItemDestructible(Serial serial) : base(serial)
        {
        }

        public override void MobileTrigger(Mobile m, bool repaired)
        {
            if (m != null && !repaired) //il distruttore dell'item
            {
                object o = this;
                string status_str = "";
                if (m_TriggerWhat != null && m_Probability > Utility.Random(100) && RegionTriggers > 0)
                {
                    o = Activator.CreateInstance(m_TriggerWhat);
                    if (o is ISpawnable)
                    {
                        ((ISpawnable)o).MoveToWorld(Location, Map);
                        BaseXmlSpawner.ApplyObjectStringProperties(null, TriggerObjOpts, o, m, this, out status_str);
                        if (DebugOpt)
                        {
                            m.SendMessage("{0}", status_str);
                            status_str = "";
                        }
                    }

                    --RegionTriggers;
                    List<Item> items = Region.Find(Location, Map).GetItems();
                    int count = items.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        if (items[i].Name == Name && items[i] is QuestItemDestructible)
                        {
                            ((QuestItemDestructible)items[i]).RegionTriggers = RegionTriggers;
                        }
                    }
                }

                if (OnDestroy != null)
                {
                    XmlSpawner.SpawnObject TheSpawn = new XmlSpawner.SpawnObject(null, 0);

                    string substitutedtypeName = BaseXmlSpawner.ApplySubstitution(null, o, m, OnDestroy);
                    string typeName = BaseXmlSpawner.ParseObjectType(substitutedtypeName);

                    if (BaseXmlSpawner.IsTypeOrItemKeyword(typeName))
                    {
                        BaseXmlSpawner.SpawnTypeKeyword(o, TheSpawn, typeName, substitutedtypeName, true, m, m.Location, Map.Internal, out status_str);
                    }
                    if (DebugOpt)
                    {
                        m.SendMessage("{0}", status_str);
                    }
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); //version

            writer.Write(m_Probability);
            if (m_TriggerWhat != null)
            {
                writer.Write(m_TriggerWhat.ToString());
            }
            else
            {
                writer.Write(string.Empty);
            }

            writer.Write(RegionTriggers);
            writer.Write(TriggerObjOpts);
            writer.Write(OnDestroy);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();

            m_Probability = reader.ReadInt();
            string type = reader.ReadString();
            if (!string.IsNullOrEmpty(type))
            {
                m_TriggerWhat = ScriptCompiler.FindTypeByName(type, true);
            }

            RegionTriggers = reader.ReadInt();
            TriggerObjOpts = reader.ReadString();
            OnDestroy = reader.ReadString();
        }
    }

    public class SimpleQuestItem : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanDelete { get; set; }

        [Constructable(AccessLevel.GameMaster)]
        public SimpleQuestItem(string name, int itemid, bool candelete)
        {
            CanDelete = candelete;
            Name = name;
            ItemID = itemid;
        }

        [Constructable(AccessLevel.GameMaster)]
        public SimpleQuestItem(string name, int itemid, bool candelete, Layer layer)
        {
            CanDelete = candelete;
            Name = name;
            ItemID = itemid;
            Layer = layer;
            QuestItem = true;
        }

        public SimpleQuestItem(Serial serial) : base(serial)
        {
        }

        public override bool Nontransferable => true;
        public override void HandleInvalidTransfer(Mobile from)
        {
            if (CanDelete)
            {
                from.SendGump(new Gumps.XmlConfirmDeleteGump(from, this));
            }
            else
            {
                from.SendLocalizedMessage(1049343); // You can only drop quest items into the top-most level of your backpack while you still need them for your quest.
            }
        }

        public override bool DisplayLootType => true;

        public override bool IsStandardLoot()
        {
            return false;
        }

        public override int ItemID
        {
            get => base.ItemID;
            set
            {
                if (value <= 0)
                {
                    Delete();
                }
                else
                {
                    base.ItemID = value;
                }
            }
        }

        public override DeathMoveResult OnParentDeath(Mobile parent)
        {
            if (CanDelete != false && parent.Player)
            {
                return DeathMoveResult.DeleteItem;
            }

            return base.OnParentDeath(parent);
        }

        public override DeathMoveResult OnInventoryDeath(Mobile parent)
        {
            if (CanDelete != false && parent.Player)
            {
                return DeathMoveResult.DeleteItem;
            }

            return base.OnInventoryDeath(parent);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
            writer.Write(CanDelete);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            if (version < 1)
            {
                CanDelete = !QuestItem;
                QuestItem = false;
            }
            else
            {
                CanDelete = reader.ReadBool();
            }
        }
    }
}