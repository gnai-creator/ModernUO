#define CLIENT6017

using Server.Engines.XmlSpawner2;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

/*
** XmlQuestHolder class
**
**
** Version 1.0
** updated 9/17/04
** - based on the XmlQuestToken class, but derived from the Container instead of Item class in order to support reward holding and display
*/
namespace Server.Items
{
    public abstract class XmlQuestHolder : Container, IXmlQuest
    {
        //        public const PlayerFlag CarriedXmlQuestFlag = (PlayerFlag)0x00100000;
        public bool HasCollect { get; set; } = false;
        private double m_ExpirationDuration;
        private string m_Objective1;
        private string m_Objective2;
        private string m_Objective3;
        private string m_Objective4;
        private string m_Objective5;
        private bool m_Completed1 = false;
        private bool m_Completed2 = false;
        private bool m_Completed3 = false;
        private bool m_Completed4 = false;
        private bool m_Completed5 = false;
        private string m_TitleString;
        private string m_SkillTrigger = null;
        private bool m_Repeatable = true;
        private TimeSpan m_NextRepeatable;
        private Item m_RewardItem;
        private XmlAttachment m_RewardAttachment;
        private int m_RewardAttachmentSerialNumber;
        private string m_status_str;
        public static int JournalNotifyColor = 0;
        public static int JournalEchoColor = 6;

        public XmlQuestHolder(Serial serial)
            : base(serial)
        {
        }

        public XmlQuestHolder()
            : this(3643)
        {
        }

        public XmlQuestHolder(int itemID)
            : base(itemID)
        {
            Weight = 0.0f;
            Hue = 500;
            //LootType = LootType.Blessed;
            TimeCreated = DateTime.UtcNow;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(9); // version
                             // version 8
            writer.Write(RewardAttachDescript);
            // version 7
            writer.Write(RewardAction);
            // version 6
            if (Journal == null || Journal.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(Journal.Count);
                foreach (XmlQuest.JournalEntry e in Journal)
                {
                    writer.Write(e.EntryID);
                    writer.Write(e.EntryText);
                }
            }
            // version 5
            writer.Write(m_Repeatable);
            // version 4
            writer.Write(Difficulty);
            // version 3
            writer.Write(AttachmentString);
            // version 2
            writer.Write(m_NextRepeatable);
            // version 1
            if (m_RewardAttachment != null)
            {
                writer.Write(m_RewardAttachment.Serial.Value);
            }
            else
            {
                writer.Write(0);
            }
            // version 0
            writer.Write(ReturnContainer);
            writer.Write(m_RewardItem);
            writer.Write(AutoReward);
            writer.Write(CanSeeReward);
            writer.Write(PlayerMade);
            writer.Write(Creator);
            writer.Write(Description1);
            writer.Write(Description2);
            writer.Write(Description3);
            writer.Write(Description4);
            writer.Write(Description5);
            writer.Write(Owner);
            writer.Write(RewardString);
            writer.Write(ConfigFile);
            writer.Write(NoteString);    // moved from the QuestNote class
            writer.Write(m_TitleString);   // moved from the QuestNote class
            writer.Write(PartyEnabled);
            writer.Write(PartyRange);
            writer.Write(State1);
            writer.Write(State2);
            writer.Write(State3);
            writer.Write(State4);
            writer.Write(State5);
            writer.Write(m_ExpirationDuration);
            writer.Write(TimeCreated);
            writer.Write(m_Objective1);
            writer.Write(m_Objective2);
            writer.Write(m_Objective3);
            writer.Write(m_Objective4);
            writer.Write(m_Objective5);
            writer.Write(m_Completed1);
            writer.Write(m_Completed2);
            writer.Write(m_Completed3);
            writer.Write(m_Completed4);
            writer.Write(m_Completed5);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (item == this)
            {
                return base.CheckLift(from, item, ref reject);
            }
            reject = LRReason.CannotLift;
            return false;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 9:
                case 8:
                {
                    RewardAttachDescript = reader.ReadString();
                    goto case 7;
                }
                case 7:
                {
                    RewardAction = reader.ReadString();
                    goto case 6;
                }
                case 6:
                {
                    int nentries = reader.ReadInt();

                    if (nentries > 0)
                    {
                        Journal = new List<XmlQuest.JournalEntry>();
                        for (int i = 0; i < nentries; ++i)
                        {
                            string entryID = reader.ReadString();
                            string entryText = reader.ReadString();
                            Journal.Add(new XmlQuest.JournalEntry(entryID, entryText));
                        }
                    }

                    goto case 5;
                }
                case 5:
                {
                    m_Repeatable = reader.ReadBool();

                    goto case 4;
                }
                case 4:
                {
                    Difficulty = reader.ReadInt();

                    goto case 3;
                }
                case 3:
                {
                    AttachmentString = reader.ReadString();

                    goto case 2;
                }
                case 2:
                {
                    m_NextRepeatable = reader.ReadTimeSpan();

                    goto case 1;
                }
                case 1:
                {
                    m_RewardAttachmentSerialNumber = reader.ReadInt();

                    goto case 0;
                }
                case 0:
                {
                    ReturnContainer = reader.ReadItem<Container>();
                    m_RewardItem = reader.ReadItem();
                    AutoReward = reader.ReadBool();
                    CanSeeReward = reader.ReadBool();
                    PlayerMade = reader.ReadBool();
                    Creator = reader.ReadMobile<PlayerMobile>();
                    Description1 = reader.ReadString();
                    Description2 = reader.ReadString();
                    Description3 = reader.ReadString();
                    Description4 = reader.ReadString();
                    Description5 = reader.ReadString();
                    Owner = reader.ReadMobile<PlayerMobile>();
                    RewardString = reader.ReadString();
                    ConfigFile = reader.ReadString();
                    NoteString = reader.ReadString();
                    m_TitleString = reader.ReadString();
                    PartyEnabled = reader.ReadBool();
                    PartyRange = reader.ReadInt();
                    State1 = reader.ReadString();
                    State2 = reader.ReadString();
                    State3 = reader.ReadString();
                    State4 = reader.ReadString();
                    State5 = reader.ReadString();
                    Expiration = reader.ReadDouble();
                    TimeCreated = reader.ReadDateTime();
                    m_Objective1 = reader.ReadString();
                    m_Objective2 = reader.ReadString();
                    m_Objective3 = reader.ReadString();
                    m_Objective4 = reader.ReadString();
                    m_Objective5 = reader.ReadString();
                    m_Completed1 = reader.ReadBool();
                    m_Completed2 = reader.ReadBool();
                    m_Completed3 = reader.ReadBool();
                    m_Completed4 = reader.ReadBool();
                    m_Completed5 = reader.ReadBool();
                }
                break;
            }
            if (version < 9)
            {
                Timer.DelayCall(PostLoadCheck);
            }
        }

        private void PostLoadCheck()
        {
            if (!IsValid)
            {
                Delete();
            }
        }

        private static Container PlaceHolderItem = null;

        public static void Initialize()
        {
            // create a temporary placeholder item used to force allocation empty Items lists used to hold hidden rewards.
            PlaceHolderItem = new Container(1);

            foreach (Item item in World.Items.Values)
            {
                if (item is XmlQuestHolder t)
                {
                    t.UpdateWeight();

                    t.RestoreRewardAttachment();
                }
            }

            // remove the temporary placeholder item
            PlaceHolderItem.Delete();
        }

        private void HideRewards()
        {
            if (m_RewardItem != null)
            {
                // remove the item from the containers item list
                if (Items.Contains(m_RewardItem))
                {
                    Items.Remove(m_RewardItem);
                }
            }
        }

        private void UnHideRewards()
        {
            if (m_RewardItem == null)
            {
                return;
            }

            Container tmpitem = null;

            if (Items == Item.EmptyItems)
            {
                tmpitem = PlaceHolderItem;

                if (tmpitem == null || tmpitem.Deleted)
                {
                    tmpitem = new Container(1);
                }

                // need to get it to allocate a new list by adding an item
                DropItem(tmpitem);
            }

            if (!Items.Contains(m_RewardItem))
            {
                m_RewardItem.Parent = this;
                m_RewardItem.Map = Map;

                // restore the item to the containers item list
                Items.Add(m_RewardItem);
            }

            // remove the placeholder
            if (tmpitem != null && Items.Contains(tmpitem))
            {
                Items.Remove(tmpitem);
                tmpitem.Map = Map.Internal;
            }

            if (tmpitem != null && tmpitem != PlaceHolderItem)
            {
                tmpitem.Delete();
            }
        }

        public override bool CheckItemUse(Mobile from, Item item)
        {
            return item is Container && base.CheckItemUse(from, item) && (CanSeeReward || item == this);
        }

        public override void DisplayTo(Mobile to)
        {
            if (to == null)
            {
                return;
            }

            // add the reward item back into the container list for display
            UnHideRewards();

            to.Send(new ContainerDisplay(this));

            //if (to.NetState != null && to.NetState.ContainerGridLines)
            to.Send(new ContainerContent6017(to, this));
            /*else
				to.Send(new ContainerContent(to, this));*/

            List<Item> items = Items;

            for (int i = 0; i < items.Count; ++i)
            {
                to.Send(items[i].OPLPacket);
            }
            // move the reward item out of container to protect it from use
            HideRewards();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            list.Add(Name);
            if (LootType == LootType.Blessed)
            {
                list.Add(1038021);
            }

            if (RootParent is PlayerVendor pv)
            {
                pv.GetChildProperties(list, this);
            }
            else if (PlayerMade && Owner != null)
            {
                list.Add(1050044, "{0}\t{1}", TotalItems, TotalWeight); // ~1_COUNT~items,~2_WEIGHT~stones
            }
        }

        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            return false;
        }

        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            return false;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            return false;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            return false;
        }

        public override bool CheckTarget(Mobile from, Server.Targeting.Target targ, object targeted)
        {
            if (from.AccessLevel == AccessLevel.Player)
            {
                return false;
            }

            return true;
        }


        public override void OnDoubleClick(Mobile from)
        {
            if (IsValid)
            {
                if (from is PlayerMobile pm)
                {
                    if (PlayerMade && (from == Creator) && (from == Owner))
                    {
                        from.SendGump(new XmlPlayerQuestGump(pm, this));
                    }
                }
            }
            else
            {
                Delete();
            }
        }

        public override bool OnDroppedToWorld(Mobile from, Point3D point)
        {
            if (!QuestItem)
            {
                from.SendGump(new XmlConfirmDeleteGump(from, this));
            }

            return false;
        }

        public override bool OnStackAttempt(Mobile from, Item stack, Item dropped)
        {
            return false;
        }

        public override void OnDelete()
        {

            // remove any temporary quest attachments associated with this quest and quest owner
            XmlQuest.RemoveTemporaryQuestObjects(Owner, Name);

            base.OnDelete();

            // remove any reward items that might be attached to this
            ReturnReward();

            // determine whether the owner needs to be flagged with a quest attachment indicating completion of this quest
            QuestCompletionAttachment();


            CheckOwnerFlag();
        }

        public override void OnItemLifted(Mobile from, Item item)
        {
            base.OnItemLifted(from, item);

            if (from is PlayerMobile pm)
            {
                if (PlayerMade && Owner != null && Owner == Creator)
                {
                    LootType = LootType.Regular;
                }
                else if (Owner == null)
                {
                    Owner = pm;

                    LootType = LootType.Blessed;
                    // flag the owner as carrying a questtoken
                    Owner.SetFlag(XmlQuest.CarriedXmlQuestFlag, true);
                }
            }
        }

        public override void OnAdded(IEntity target)
        {
            base.OnAdded(target);

            if (target != null && target is Container cont)
            {
                // find the parent of the container
                // note, the only valid additions are to the player pack or a questbook.  Anything else is invalid.  
                // This is to avoid exploits involving storage or transfer of questtokens
                // make an exception for playermade quests that can be put on playervendors
                IEntity parentOfTarget = cont.Parent;

                // if this is a QuestBook then allow additions if it is in a players pack or it is a player quest
                if (parentOfTarget != null && parentOfTarget is Container parent && target is XmlQuestBook)
                {
                    parentOfTarget = parent.Parent;
                }

                // check to see if it can be added.
                // allow playermade quests to be placed in playervendors or in xmlquestbooks that are in the world (supports the playerquestboards)
                if (PlayerMade && (((parentOfTarget != null) && parentOfTarget is PlayerVendor) ||
                    ((parentOfTarget == null) && target is XmlQuestBook)))
                {
                    CheckOwnerFlag();

                    Owner = null;

                    LootType = LootType.Regular;
                }
                else if (parentOfTarget != null && parentOfTarget is PlayerMobile pm)
                {
                    if (PlayerMade && Owner != null && (Owner == Creator || Creator == null))
                    {
                        // check the old owner
                        CheckOwnerFlag();

                        Owner = parentOfTarget as PlayerMobile;

                        // first owner will become creator by default
                        if (Creator == null)
                        {
                            Creator = Owner;
                        }

                        LootType = LootType.Blessed;

                        // flag the new owner as carrying a questtoken
                        Owner.SetFlag(XmlQuest.CarriedXmlQuestFlag, true);
                    }
                    else
                    {
                        if (Owner == null)
                        {
                            Owner = parentOfTarget as PlayerMobile;

                            LootType = LootType.Blessed;

                            // flag the owner as carrying a questtoken
                            Owner.SetFlag(XmlQuest.CarriedXmlQuestFlag, true);
                        }
                        else if (pm != Owner || target is BankBox)
                        {
                            // tried to give it to another player or placed it in the players bankbox. try to return it to the owners pack
                            Owner.AddToBackpack(this);
                        }
                    }
                }
                else
                {
                    if (Owner != null)
                    {
                        // try to return it to the owners pack
                        Owner.AddToBackpack(this);
                    }
                    // allow placement into containers in the world, npcs or drop on their corpses when owner is null
                    else if (!(parentOfTarget is Mobile) && !(target is Corpse) && parentOfTarget != null)
                    {

                        // invalidate the token

                        CheckOwnerFlag();

                        Invalidate();
                    }
                }
            }
        }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            return false;
        }
        public List<XmlQuest.JournalEntry> Journal { get; set; }
        private static char[] colondelim = new char[1] { ':' };

        [CommandProperty(AccessLevel.GameMaster)]
        public string EchoAddJournalEntry
        {
            set =>
                // notify and echo journal text
                VerboseAddJournalEntry(value, true, true);
        }

        public string NotifyAddJournalEntry
        {
            set =>
                // notify
                VerboseAddJournalEntry(value, true, false);
        }

        public string AddJournalEntry
        {
            set =>
                // silent
                VerboseAddJournalEntry(value, false, false);
        }

        private void VerboseAddJournalEntry(string entrystring, bool notify, bool echo)
        {
            if (entrystring == null)
            {
                return;
            }

            // parse the value
            string[] args = entrystring.Split(colondelim, 2);

            if (args == null)
            {
                return;
            }

            string entryID = null;
            string entryText = null;
            if (args.Length > 0)
            {
                entryID = args[0].Trim();
            }

            if (entryID == null || entryID.Length == 0)
            {
                return;
            }

            if (args.Length > 1)
            {
                entryText = args[1].Trim();
            }

            // allocate a new journal if none exists
            if (Journal == null)
            {
                Journal = new List<XmlQuest.JournalEntry>();
            }

            // go through the existing journal to find a matching ID
            XmlQuest.JournalEntry foundEntry = null;

            foreach (XmlQuest.JournalEntry e in Journal)
            {
                if (e.EntryID == entryID)
                {
                    foundEntry = e;
                    break;
                }
            }

            if (foundEntry != null)
            {
                // modify an existing entry
                if (entryText == null || entryText.Length == 0)
                {
                    // delete the entry
                    Journal.Remove(foundEntry);
                }
                else
                {
                    // just replace the text
                    foundEntry.EntryText = entryText;

                    if (RootParent is Mobile holder)
                    {
                        if (notify)
                        {
                            // notify the player holding the questholder                       
                            holder.SendLocalizedMessage(505339, $"{entryID}\t{Name}", JournalNotifyColor);//, "Modificato il giornale '{0}' nella quest '{1}'.", entryID, Name);
                        }
                        if (echo)
                        {
                            // echo the journal text to the player holding the questholder                       
                            holder.SendMessage(JournalEchoColor, "{0}", entryText);
                        }
                    }
                }
            }
            else
            {
                // add a new entry
                if (entryText != null && entryText.Length != 0)
                {
                    // add the new entry
                    Journal.Add(new XmlQuest.JournalEntry(entryID, entryText));

                    if (RootParent is Mobile holder)
                    {
                        if (notify)
                        {
                            // notify the player holding the questholder                       
                            holder.SendLocalizedMessage(505338, $"{entryID}\t{Name}", JournalNotifyColor);//JournalNotifyColor, "Aggiunto il giornale '{0}' nella quest '{1}'.", entryID, Name);
                        }
                        if (echo)
                        {
                            // echo the journal text to the player holding the questholder                       
                            holder.SendMessage(JournalEchoColor, "{0}", entryText);
                        }
                    }
                }
            }
        }



        private void QuestCompletionAttachment()
        {
            bool complete = IsCompleted;

            // is this quest repeatable
            if ((!Repeatable || NextRepeatable > TimeSpan.Zero) && complete)
            {
                double expiresin = Repeatable ? NextRepeatable.TotalMinutes : 0;

                // then add an attachment indicating that it has already been done
                XmlAttach.AttachTo(Owner, new XmlQuestAttachment(Name, expiresin));
            }

            // have quest points been enabled?
            if (XmlQuest.QuestPointsEnabled && complete && !PlayerMade)
            {
                XmlQuestPoints.GiveQuestPoints(Owner, this);
            }
        }

        private void PackItem(Item item)
        {
            if (item != null)
            {
                DropItem(item);
            }

            PackItemsMovable(this, false);

            // make sure the weight and gold of the questtoken is updated to reflect the weight of added rewards in playermade quests to avoid
            // exploits where quests are used as zero weight containers

            UpdateWeight();
        }

        public int HashCode => Utility.GetHashCode32(m_Objective1 + m_Objective2 + m_Objective3 + m_Objective4 + m_Objective5);

        private void CalculateWeight(Container target)
        {
            int gold = 0;
            int weight = 0;
            int nitems = 0;

            foreach (Item i in target.Items)
            {
                if (i is Container cont)
                {
                    CalculateWeight(cont);
                    weight += i.TotalWeight + (int)i.Weight;
                    gold += i.TotalGold;
                    nitems += i.TotalItems + 1;
                }
                else
                {
                    // make sure gold amount is consistent with totalgold
                    if (i is Gold)
                    {
                        UpdateTotal(i, TotalType.Gold, i.Amount);
                    }
                    weight += (int)(i.Weight * i.Amount);
                    gold += i.TotalGold;
                    nitems += 1;
                }
            }

            UpdateTotal(target, TotalType.Weight, weight);
            UpdateTotal(target, TotalType.Gold, gold);
            UpdateTotal(target, TotalType.Items, nitems);
        }


        private void UpdateWeight()
        {
            UnHideRewards();

            // update the container totals
            UpdateTotals();

            // and the parent totals
            if (RootParent is Mobile rp)
            {
                rp.UpdateTotals();
            }

            // hide the reward item
            HideRewards();
        }

        public override int GetTotal(TotalType type)
        {
            return 0;
        }

        private void ReturnReward()
        {
            if (m_RewardItem != null)
            {
                CheckRewardItem();

                // if this was player made, then return the item to the creator
                if (PlayerMade && (Creator != null) && !Creator.Deleted)
                {
                    m_RewardItem.Movable = true;

                    // make sure all of the items in the pack are movable as well
                    PackItemsMovable(this, true);

                    bool returned = false;

                    if ((ReturnContainer != null) && !ReturnContainer.Deleted)
                    {
                        returned = ReturnContainer.TryDropItem(Creator, m_RewardItem, false);
                        //ReturnContainer.DropItem(m_RewardItem);
                    }
                    if (!returned)
                    {

                        returned = Creator.AddToBackpack(m_RewardItem);
                    }

                    if (returned)
                    {
                        Creator.SendLocalizedMessage(505336, $"{m_RewardItem.GetType().Name}\t{Name}");// "La tua ricompensa {0} è stata presa dalla quest {1}", m_RewardItem.GetType().Name, Name);
                                                                                                       //AddMobileWeight(Creator, m_RewardItem);
                    }
                    else
                    {
                        Creator.SendLocalizedMessage(505335, $"{m_RewardItem.GetType().Name}\t{Name}");// "Tentativo di presa ricompensa {0} dalla quest {1} : contenitore pieno.", );
                                                                                                       //Creator.SendMessage("Tentativo di presa ricompensa {0} dalla quest {1} : contenitore pieno.", m_RewardItem.GetType().Name, Name);
                    }
                }
                else
                {
                    // just delete it
                    m_RewardItem.Delete();

                }
                m_RewardItem = null;
                UpdateWeight();
            }
            if (m_RewardAttachment != null)
            {
                // delete any remaining attachments
                m_RewardAttachment.Delete();
            }

        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlayerMobile Owner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public new string Name
        {
            get
            {
                if (PlayerMade)
                {
                    return "PQ: " + base.Name;
                }
                else
                {
                    return base.Name;
                }
            }
            set
            {
                base.Name = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlayerMobile Creator { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Difficulty { get; set; } = 1;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Status
        {
            get => m_status_str;
            set => m_status_str = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string NoteString { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AutoReward { get; set; } = false;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanSeeReward { get; set; } = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PlayerMade { get; set; } = false;

        [CommandProperty(AccessLevel.GameMaster)]
        public Container ReturnContainer { get; set; }

        private void PackItemsMovable(Container pack, bool canmove)
        {
            if (pack == null)
            {
                return;
            }

            UnHideRewards();
            Item[] itemlist = pack.FindItemsByType(typeof(Item), true);
            if (itemlist != null)
            {
                for (int i = 0; i < itemlist.Length; ++i)
                {
                    itemlist[i].Movable = canmove;
                }
            }

        }

        private void RestoreRewardAttachment()
        {
            m_RewardAttachment = XmlAttach.FindAttachmentBySerial(m_RewardAttachmentSerialNumber);
        }

        public XmlAttachment RewardAttachment
        {
            get
            {
                // if the reward item is not set, and the reward string is specified, then use the reward string to construct and assign the
                // reward item
                // dont allow player made quests to use the rewardstring creation feature
                if (m_RewardAttachment != null && m_RewardAttachment.Deleted)
                {
                    m_RewardAttachment = null;
                }

                if ((m_RewardAttachment == null || m_RewardAttachment.Deleted) &&
                    (AttachmentString != null) && !PlayerMade)
                {
                    object o = XmlQuest.CreateItem(this, AttachmentString, out m_status_str, typeof(XmlAttachment));
                    if (o is Item it)
                    {
                        it.Delete();
                    }
                    else if (o is XmlAttachment xa)
                    {
                        m_RewardAttachment = xa;
                        m_RewardAttachment.OwnedBy = this;
                    }
                }

                return m_RewardAttachment;
            }
            set
            {
                // get rid of any existing attachment
                if (m_RewardAttachment != null && !m_RewardAttachment.Deleted)
                {
                    m_RewardAttachment.Delete();
                }

                m_RewardAttachment = value;
            }
        }

        public string RewardAttachDescript { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item RewardItem
        {
            get
            {
                // if the reward item is not set, and the reward string is specified, then use the reward string to construct and assign the
                // reward item
                // dont allow player made quests to use the rewardstring creation feature
                if ((m_RewardItem == null || m_RewardItem.Deleted) &&
                    !string.IsNullOrEmpty(RewardString) && !PlayerMade)
                {
                    object o = XmlQuest.CreateItem(this, RewardString.TrimStart('@'), out m_status_str, typeof(Item));
                    if (o is Item it)
                    {
                        m_RewardItem = it;
                        PackItem(m_RewardItem);
                    }
                    else if (o is XmlAttachment xa)
                    {
                        xa.Delete();
                    }
                }

                return m_RewardItem;
            }
            set
            {
                // get rid of any existing reward item if it has been assigned
                if (m_RewardItem != null && !m_RewardItem.Deleted)
                {
                    ReturnReward();
                }

                // and assign the new item
                m_RewardItem = value;

                /*
				// is this currently carried by a mobile?
				if(m_RewardItem.RootParent != null && m_RewardItem.RootParent is Mobile)
				{
					// if so then remove it
					((Mobile)(m_RewardItem.RootParent)).RemoveItem(m_RewardItem);

				}
				*/

                // and put it in the pack
                if (m_RewardItem != null && !m_RewardItem.Deleted)
                {
                    PackItem(m_RewardItem);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TitleString
        {
            get => m_TitleString;
            set { m_TitleString = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string RewardAction { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string RewardString { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string AttachmentString { get; set; }


        [CommandProperty(AccessLevel.GameMaster)]
        public string ConfigFile { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool LoadConfig
        {
            get => false;
            set
            {
                if (value == true)
                {
                    LoadXmlConfig(ConfigFile);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PartyEnabled { get; set; } = false;
        [CommandProperty(AccessLevel.GameMaster)]
        public int PartyRange { get; set; } = -1;
        [CommandProperty(AccessLevel.GameMaster)]
        public string State1 { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string State2 { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string State3 { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string State4 { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string State5 { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Description1 { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Description2 { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Description3 { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Description4 { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Description5 { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Objective1
        {
            get => m_Objective1;
            set
            {
                if (value == m_Objective1)
                {
                    return;
                }

                PlayerMobile owner = (Parent as XmlQuestBook)?.Owner;
                if (owner != null)
                {
                    if(!XmlQuestBook.Hashes.TryGetValue(owner, out var hashes))
                    {
                        XmlQuestBook.Hashes[owner] = new HashSet<int>();
                    }
                    hashes.Remove(HashCode);
                    m_Objective1 = value;
                    hashes.Add(HashCode);
                }
                else
                    m_Objective1 = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Objective2
        {
            get => m_Objective2;
            set
            {
                if (value == m_Objective2)
                {
                    return;
                }

                PlayerMobile owner = (Parent as XmlQuestBook)?.Owner;
                if (owner != null)
                {
                    if (!XmlQuestBook.Hashes.TryGetValue(owner, out var hashes))
                    {
                        XmlQuestBook.Hashes[owner] = new HashSet<int>();
                    }
                    hashes.Remove(HashCode);
                    m_Objective2 = value;
                    hashes.Add(HashCode);
                }
                else
                    m_Objective2 = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Objective3
        {
            get => m_Objective3;
            set
            {
                if (value == m_Objective3)
                {
                    return;
                }

                PlayerMobile owner = (Parent as XmlQuestBook)?.Owner;
                if (owner != null)
                {
                    if (!XmlQuestBook.Hashes.TryGetValue(owner, out var hashes))
                    {
                        XmlQuestBook.Hashes[owner] = new HashSet<int>();
                    }
                    hashes.Remove(HashCode);
                    m_Objective3 = value;
                    hashes.Add(HashCode);
                }
                else
                    m_Objective3 = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Objective4
        {
            get => m_Objective4;
            set
            {
                if (value == m_Objective4)
                {
                    return;
                }

                PlayerMobile owner = (Parent as XmlQuestBook)?.Owner;
                if (owner != null)
                {
                    if (!XmlQuestBook.Hashes.TryGetValue(owner, out var hashes))
                    {
                        XmlQuestBook.Hashes[owner] = new HashSet<int>();
                    }
                    hashes.Remove(HashCode);
                    m_Objective4 = value;
                    hashes.Add(HashCode);
                }
                else
                    m_Objective4 = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Objective5
        {
            get => m_Objective5;
            set
            {
                if (value == m_Objective5)
                {
                    return;
                }

                PlayerMobile owner = (Parent as XmlQuestBook)?.Owner;
                if (owner != null)
                {
                    if (!XmlQuestBook.Hashes.TryGetValue(owner, out var hashes))
                    {
                        XmlQuestBook.Hashes[owner] = new HashSet<int>();
                    }
                    hashes.Remove(HashCode);
                    m_Objective5 = value;
                    hashes.Add(HashCode);
                }
                else
                    m_Objective5 = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Completed1
        {
            get => IsValid && m_Completed1;
            set
            {
                m_Completed1 = value;
                CheckAutoReward();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Completed2
        {
            get => IsValid && m_Completed2;
            set
            {
                m_Completed2 = value;
                CheckAutoReward();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Completed3
        {
            get => IsValid && m_Completed3;
            set
            {
                m_Completed3 = value;
                CheckAutoReward();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Completed4
        {
            get => IsValid && m_Completed4;
            set
            {
                m_Completed4 = value;
                CheckAutoReward();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Completed5
        {
            get => IsValid && m_Completed5;
            set
            {
                m_Completed5 = value;
                CheckAutoReward();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TimeCreated { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Expiration
        {
            get => m_ExpirationDuration;
            set
            {
                // cap the max value at 100 years
                if (value > 876000)
                {
                    m_ExpirationDuration = 876000;
                }
                else
                {
                    m_ExpirationDuration = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan ExpiresIn
        {
            get
            {
                if (m_ExpirationDuration > 0)
                {
                    // if this is a player created quest, then refresh the expiration time until it is in someone elses possession
                    /*
					 if(PlayerMade && ((Owner == Creator) || (Owner == null)))
					 {
						 m_TimeCreated = DateTime.UtcNow;
					 }
					 */
                    return (TimeCreated + TimeSpan.FromHours(m_ExpirationDuration) - DateTime.UtcNow);
                }
                else
                {
                    return TimeSpan.FromHours(0);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool IsExpired
        {
            get
            {
                if (((m_ExpirationDuration > 0) && (ExpiresIn <= TimeSpan.FromHours(0))))
                {

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Repeatable
        {
            get => m_Repeatable;
            set => m_Repeatable = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual TimeSpan NextRepeatable
        {
            get => m_NextRepeatable;
            set => m_NextRepeatable = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool AlreadyDone
        {
            get
            {
                // look for a quest attachment with the current quest name
                if (XmlAttach.FindAttachment(Owner, typeof(XmlQuestAttachment), Name) == null)
                {
                    return false;
                }

                return true;
            }
        }

        public virtual LogEntry ExpirationString
        {
            get
            {
                if (AlreadyDone)
                {
                    return new LogEntry(504980, string.Empty);//"Già Completata";
                }
                else if (m_ExpirationDuration <= 0)
                {
                    return new LogEntry(504981, string.Empty);//"Non Scade";
                }
                else if (IsExpired)
                {
                    return new LogEntry(504982, string.Empty);//"Scaduta";
                }
                else
                {
                    TimeSpan ts = ExpiresIn;

                    int days = (int)ts.TotalDays;
                    int hours = (int)(ts - TimeSpan.FromDays(days)).TotalHours;
                    int minutes = (int)(ts - TimeSpan.FromHours(hours)).TotalMinutes;
                    int seconds = (int)(ts - TimeSpan.FromMinutes(minutes)).TotalSeconds;

                    if (days > 0)
                    {
                        return new LogEntry(504970, string.Format("{0}\t#{1}\t{2}\t#{3}", days, (days == 1 ? 504972 : 504973), hours, (hours == 1 ? 504974 : 504975)));//String.Format("Scadrà in {0} giorni {1} ore", days, hours);
                    }
                    else if (hours > 0)
                    {
                        return new LogEntry(504970, string.Format("{0}\t#{1}\t{2}\t#{3}", hours, (hours == 1 ? 504974 : 504975), minutes, (minutes == 1 ? 504976 : 504977)));//String.Format("Scadrà in {0} ore {1} minuti", hours, minutes);
                    }
                    else
                    {
                        return new LogEntry(504970, string.Format("{0}\t#{1}\t{2}\t#{3}", minutes, (minutes == 1 ? 504976 : 504977), seconds, (seconds == 1 ? 504978 : 504979)));//String.Format("Scadrà in {0} minuti {1} sec", minutes, seconds);
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool IsValid
        {
            get
            {
                if (IsExpired)
                {
                    // eliminate reward definitions
                    RewardString = null;
                    AttachmentString = null;

                    // return any reward items
                    ReturnReward();

                    return false;
                }
                else if (AlreadyDone)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public override bool CanRenameContainer => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool IsCompleted
        {
            get
            {
                if (IsValid &&
                    (m_Completed1 || string.IsNullOrEmpty(m_Objective1)) &&
                    (m_Completed2 || string.IsNullOrEmpty(m_Objective2)) &&
                    (m_Completed3 || string.IsNullOrEmpty(m_Objective3)) &&
                    (m_Completed4 || string.IsNullOrEmpty(m_Objective4)) &&
                    (m_Completed5 || string.IsNullOrEmpty(m_Objective5)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool DoneLevel1
        {
            get
            {
                bool[] bools = new[] { m_Completed1, m_Completed2, m_Completed3, m_Completed4, m_Completed5 };

                return IsValid && (bools.Count(boolean => boolean == true) >= 1);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool DoneLevel2
        {
            get
            {
                bool[] bools = new[] { m_Completed1, m_Completed2, m_Completed3, m_Completed4, m_Completed5 };

                return IsValid && (bools.Count(boolean => boolean == true) >= 2);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool DoneLevel3
        {
            get
            {
                bool[] bools = new[] { m_Completed1, m_Completed2, m_Completed3, m_Completed4, m_Completed5 };

                return IsValid && (bools.Count(boolean => boolean == true) >= 3);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool DoneLevel4
        {
            get
            {
                bool[] bools = new[] { m_Completed1, m_Completed2, m_Completed3, m_Completed4, m_Completed5 };

                return IsValid && (bools.Count(boolean => boolean == true) >= 4);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool DoneLevel5
        {
            get
            {
                bool[] bools = new[] { m_Completed1, m_Completed2, m_Completed3, m_Completed4, m_Completed5 };

                return IsValid && (bools.Count(boolean => boolean == true) >= 5);
            }
        }

        public Container Pack => this;

        // this is the handler for skill use
        // not yet implemented, just a hook for now
        public void OnSkillUse(Mobile m, Skill skill, bool success)
        {
            if (m == Owner && IsValid)
            {
                //m_skillTriggerActivated  = false;

                // do a location test for the skill use
                /*
				if ( !Utility.InRange( m.Location, this.Location, m_ProximityRange ) )
					return;
				*/
                int testskill = -1;

                // check the skill trigger conditions, Skillname,min,max
                try
                {
                    testskill = (int)Enum.Parse(typeof(SkillName), m_SkillTrigger);
                }
                catch { }

                if (m_SkillTrigger != null && (int)skill.SkillName == testskill)
                {
                    // have a skill trigger so flag it and test it
                    //m_skillTriggerActivated  = true;
                }

            }
        }

        public bool HandlesOnSkillUse => (IsValid && m_SkillTrigger != null && m_SkillTrigger.Length > 0);

        private void CheckOwnerFlag()
        {
            if (Owner != null && !Owner.Deleted)
            {
                // need to check to see if any other questtoken items are owned
                // search the Owners top level pack for an xmlquest
                List<Item> list = XmlQuest.FindXmlQuest(Owner);

                if (list == null || list.Count == 0)
                {

                    // if none remain then flag the ower as having none
                    Owner.SetFlag(XmlQuest.CarriedXmlQuestFlag, false);
                }
            }


        }

        public virtual void Invalidate()
        {
            //Hue = 32;
            //LootType = LootType.Regular;
            if (Owner != null)
            {
                Owner.SendLocalizedMessage(505334, Name);// String.Format("Quest invalidata - '{0}' rimossa", Name));
            }
            Delete();
        }

        public void CheckRewardItem()
        {
            // go through all reward items and delete anything that is movable.  This blocks any exploits where players might
            // try to add items themselves
            if (m_RewardItem != null && !m_RewardItem.Deleted && m_RewardItem is Container cont)
            {
                foreach (Item i in cont.FindItemsByType(typeof(Item), true))
                {
                    if (i.Movable)
                    {
                        i.Delete();
                    }
                }
            }

        }

        public void CheckAutoReward()
        {
            if (!Deleted && AutoReward && IsCompleted && Owner != null &&
                ((RewardItem != null && !m_RewardItem.Deleted) || (RewardAttachment != null && !m_RewardAttachment.Deleted) || RewardAction != null))
            {
                if (RewardItem != null)
                {
                    // make sure nothing has been added to the pack other than the original reward items
                    CheckRewardItem();

                    m_RewardItem.Movable = true;

                    // make sure all of the items in the pack are movable as well
                    PackItemsMovable(this, true);

                    Owner.AddToBackpack(m_RewardItem);
                    //AddMobileWeight(Owner,m_RewardItem);

                    m_RewardItem = null;
                }

                if (RewardAttachment != null)
                {
                    Timer.DelayCall(AttachToCallback, (Owner, m_RewardAttachment));

                    m_RewardAttachment = null;
                }

                if (RewardAction != null)
                {
                    BaseXmlSpawner.ExecuteActions(Owner, Owner, RewardAction);
                }

                Owner.SendLocalizedMessage(505333, Name);// String.Format("{0} completata. Ricevi la ricompensa!", Name));
                Delete();
            }
        }

        public void AttachToCallback((PlayerMobile, XmlAttachment) state)
        {
            if (state.Item1 != null)
                XmlAttach.AttachTo(state.Item1, state.Item2);
        }

        private const string XmlTableName = "Properties";
        private const string XmlDataSetName = "XmlQuestHolder";

        public void LoadXmlConfig(string filename)
        {
            if (filename == null || filename.Length <= 0)
            {
                return;
            }
            // Check if the file exists
            if (System.IO.File.Exists(filename) == true)
            {
                FileStream fs = null;
                try
                {
                    fs = File.Open(filename, FileMode.Open, FileAccess.Read);
                }
                catch { }

                if (fs == null)
                {
                    Status = string.Format("Unable to open {0} for loading", filename);
                    return;
                }

                // Create the data set
                DataSet ds = new DataSet(XmlDataSetName);

                // Read in the file
                //ds.ReadXml( e.Arguments[0].ToString() );
                bool fileerror = false;
                try
                {
                    ds.ReadXml(fs);
                }
                catch { fileerror = true; }

                // close the file
                fs.Close();
                if (fileerror)
                {
                    Console.WriteLine("XmlQuestHolder: Error in XML config file '{0}'", filename);
                    return;
                }
                // Check that at least a single table was loaded
                if (ds.Tables != null && ds.Tables.Count > 0)
                {
                    if (ds.Tables[XmlTableName] != null && ds.Tables[XmlTableName].Rows.Count > 0)
                    {
                        foreach (DataRow dr in ds.Tables[XmlTableName].Rows)
                        {
                            bool valid_entry;
                            string strEntry = null;
                            bool boolEntry = true;
                            double doubleEntry = 0;
                            int intEntry = 0;
                            TimeSpan timespanEntry = TimeSpan.Zero;

                            valid_entry = true;
                            try { strEntry = (string)dr["Name"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Name = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Title"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                TitleString = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Note"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                NoteString = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Reward"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                RewardString = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Attachment"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                AttachmentString = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Objective1"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Objective1 = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Objective2"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Objective2 = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Objective3"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Objective3 = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Objective4"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Objective4 = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Objective5"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Objective5 = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Description1"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Description1 = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Description2"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Description2 = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Description3"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Description3 = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Description4"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Description4 = strEntry;
                            }

                            valid_entry = true;
                            strEntry = null;
                            try { strEntry = (string)dr["Description5"]; }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Description5 = strEntry;
                            }

                            valid_entry = true;
                            boolEntry = false;
                            try { boolEntry = bool.Parse((string)dr["PartyEnabled"]); }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                PartyEnabled = boolEntry;
                            }

                            valid_entry = true;
                            boolEntry = false;
                            try { boolEntry = bool.Parse((string)dr["AutoReward"]); }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                AutoReward = boolEntry;
                            }

                            valid_entry = true;
                            boolEntry = true;
                            try { boolEntry = bool.Parse((string)dr["CanSeeReward"]); }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                CanSeeReward = boolEntry;
                            }

                            valid_entry = true;
                            boolEntry = true;
                            try { boolEntry = bool.Parse((string)dr["Repeatable"]); }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                m_Repeatable = boolEntry;
                            }

                            valid_entry = true;
                            timespanEntry = TimeSpan.Zero;
                            try { timespanEntry = TimeSpan.Parse((string)dr["NextRepeatable"]); }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                m_NextRepeatable = timespanEntry;
                            }

                            valid_entry = true;
                            boolEntry = false;
                            try { boolEntry = bool.Parse((string)dr["PlayerMade"]); }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                PlayerMade = boolEntry;
                            }

                            valid_entry = true;
                            intEntry = 0;
                            try { intEntry = int.Parse((string)dr["PartyRange"]); }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                PartyRange = intEntry;
                            }

                            valid_entry = true;
                            doubleEntry = 0;
                            try { doubleEntry = double.Parse((string)dr["Expiration"]); }
                            catch { valid_entry = false; }
                            if (valid_entry)
                            {
                                Expiration = doubleEntry;
                            }
                        }
                    }
                }
            }
        }
    }
}
