using Server.Gumps;
using Server.Mobiles;
using System;
using System.Collections.Generic;


/*
** XmlQuestBook class
**
*/
namespace Server.Items
{
    [Flipable(0x1E5E, 0x1E5F)]
    public class PlayerQuestBoard : XmlQuestBook
    {
        public override bool IsDecoContainer => false;

        public PlayerQuestBoard(Serial serial) : base(serial)
        {
        }

        [Constructable]
        public PlayerQuestBoard() : base(0x1e5e)
        {
            Movable = false;
            Name = "Player Quest Board";
            LiftOverride = true;    // allow players to store books in it
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    public class XmlQuestBook : Container, IDyable, IXmlQuestBook
    {
        public override int LabelNumber => 500059;

        [CommandProperty(AccessLevel.GameMaster)]
        public PlayerMobile Owner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Locked { get; set; }

        public XmlQuestBook(Serial serial) : base(serial)
        {
        }

        [Constructable]
        public XmlQuestBook(int itemid) : this()
        {
            ItemID = itemid;
            QuestItem = true;
        }

        [Constructable]
        public XmlQuestBook() : base(0x2259)
        {
            //LootType = LootType.Blessed;
            Hue = 0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm)
            {
                if (pm.AccessLevel >= AccessLevel.GameMaster)
                {
                    base.OnDoubleClick(from);
                }

                pm.SendGump(new XmlQuestBookGump(pm, this));
            }
        }

        public override bool CanRenameContainer => false;

        public override void RemoveItem(Item item)
        {
            if (item is IXmlQuest iq)
            {
                if (iq.Owner != null && Hashes.TryGetValue(iq.Owner, out var hashes))
                {
                    hashes.Remove(iq.HashCode);
                }
            }

            base.RemoveItem(item);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is IXmlQuest iq && !Locked)
            {
                if (iq.IsValid)
                {
                    if(iq.Owner != from)
                    {
                        from.SendLocalizedMessage(501648);
                        Console.WriteLine("Inserimento quest con owner differente dal proprio: {0}", dropped);
                        return false;
                    }
                    if(!Hashes.TryGetValue(from, out var hashes))
                    {
                        Hashes[from] = hashes = new HashSet<int>();
                    }
                    if (!hashes.Add(iq.HashCode))
                    {
                        from.SendLocalizedMessage(500076);
                        Console.WriteLine("Inserimento quest già presente: {0}", dropped);
                        return false;
                    }
                    return base.OnDragDrop(from, dropped);
                }
                else
                {
                    from.SendLocalizedMessage(500077);
                    dropped.Delete();
                }
            }
            return false;
        }

        public override void DisplayTo(Mobile to)
        {
            if (to.AccessLevel >= AccessLevel.GameMaster)
            {
                base.DisplayTo(to);
            }
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
            {
                return false;
            }
            else if (RootParent is Mobile && from != RootParent)
            {
                return false;
            }

            Hue = sender.DyedHue;

            return true;
        }

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
            if (Owner != null)
            {
                Owner.SendMessage(string.Format("{0} Quests invalidated - '{1}' removed", TotalItems, Name));
            }
            Delete();
        }

        public override void OnItemLifted(Mobile from, Item item)
        {
            base.OnItemLifted(from, item);

            if (from is PlayerMobile pm && Owner == null)
            {
                Owner = pm;
                LootType = LootType.Blessed;
                // flag the owner as carrying a questtoken assuming the book contains quests and then confirm it with CheckOwnerFlag
                Owner.SetFlag(XmlQuest.CarriedXmlQuestFlag, true);
                CheckOwnerFlag();
            }
        }

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            if (parent != null && parent is Container cont)
            {
                // find the parent of the container
                // note, the only valid additions are to the player pack.  Anything else is invalid.  This is to avoid exploits involving storage or transfer of questtokens
                IEntity from = cont.Parent;

                // check to see if it can be added
                if (from != null && from is PlayerMobile pm)
                {
                    // if it was not owned then allow it to go anywhere
                    if (Owner == null)
                    {
                        Owner = pm;

                        LootType = LootType.Blessed;
                        // could also bless all of the quests inside as well but not actually necessary since blessed containers retain their
                        // contents whether blessed or not, and when dropped the questtokens will be blessed

                        // flag the owner as carrying a questtoken
                        Owner.SetFlag(XmlQuest.CarriedXmlQuestFlag, true);
                        CheckOwnerFlag();
                    }
                    else if (pm != Owner || parent is BankBox)
                    {
                        // tried to give it to another player or placed it in the players bankbox. try to return it to the owners pack
                        Owner.AddToBackpack(this);
                    }
                }
                else
                {
                    if (Owner != null)
                    {
                        // try to return it to the owners pack
                        Owner.AddToBackpack(this);
                    }
                    // allow placement into npcs or drop on their corpses when owner is null
                    else if (!(from is Mobile) && !(parent is Corpse))
                    {
                        // in principle this should never be reached

                        // invalidate the token

                        CheckOwnerFlag();

                        Invalidate();
                    }
                }
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();

            CheckOwnerFlag();
        }

        public override bool OnDroppedToWorld(Mobile from, Point3D point)
        {
            /*bool returnvalue = base.OnDroppedToWorld(from,point);*/

            if (!QuestItem)
            {
                from.SendGump(new XmlConfirmDeleteGump(from, this));
            }

            //CheckOwnerFlag();

            //Invalidate();
            return false;
            //return returnvalue;
        }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(2); // version

            writer.Write(Owner);
            writer.Write(Locked);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            Owner = reader.ReadMobile<PlayerMobile>();
            Locked = reader.ReadBool();
            if (version > 1)
            {
                if (Items.Count > 0)
                {
                    if(!Hashes.TryGetValue(Owner, out var hashes))
                    {
                        Hashes[Owner] = hashes = new HashSet<int>();
                    }
                    foreach (Item it in Items)
                    {
                        if (it is IXmlQuest iq)
                        {
                            hashes.Add(iq.HashCode);
                        }
                    }
                }
            }
            else
            {
                Name = null;
                Timer.DelayCall(PostLoadCheck);
            }
        }
        public static Dictionary<Mobile, HashSet<int>> Hashes { get; } = new Dictionary<Mobile, HashSet<int>>();

        private void PostLoadCheck()
        {
            if(Owner == null)
            {
                Console.WriteLine($"Rimosso libro delle quest: {this} - Rootparent {RootParent}");
                Delete();
                return;
            }
            List<Item> toremove = new List<Item>();
            foreach (Item it in Items)
            {
                if (it is IXmlQuest iq)
                {
                    if (iq.IsValid)
                    {
                        if(!Hashes.TryGetValue(Owner, out var hashes))
                        {
                            Hashes[Owner] = hashes = new HashSet<int>();
                        }
                        if (!hashes.Add(iq.HashCode))
                        {
                            toremove.Add(it);
                        }
                    }
                    else
                    {
                        toremove.Add(it);
                    }
                }
                else
                {
                    toremove.Add(it);
                }
            }
            for (int i = toremove.Count - 1; i >= 0; --i)
            {
                toremove[i].Delete();
            }
        }

        public override bool OnStackAttempt(Mobile from, Item stack, Item dropped)
        {
            from.SendLocalizedMessage(1005106);//Oggetto non valido
            return false;
        }

        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            if (dropped is IXmlQuest)
            {
                return base.TryDropItem(from, dropped, sendFullMessage);
            }

            from.SendLocalizedMessage(1005106);//Oggetto non valido
            return false;
        }

        public override bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            from.SendLocalizedMessage(1005106);//Oggetto non valido
            return false;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            from.SendLocalizedMessage(1005106);//Oggetto non valido
            return false;
        }
    }
}
