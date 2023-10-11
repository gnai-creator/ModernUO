using Server.Items;
using System.Collections.Generic;

namespace Server.Engines.XmlSpawner2
{
    public class XmlMajorItems : XmlAttachment
    {
        private List<Item> m_MajorItems = new List<Item>();
        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlMajorItems(ASerial serial) : base(serial)
        {
        }

        public XmlMajorItems()
        {
        }

        public void AddItemToMajor(Mobile major, List<Item> toadd)
        {
            if (major == null || major.Deleted)
            {
                return;
            }

            if (AttachedTo is TownStone)
            {
                TownStone stone = (TownStone)AttachedTo;
                if (stone.Town != null)
                {
                    if (stone.Town.Leader != major || major.Backpack == null || major.Backpack.Deleted)
                    {
                        return;
                    }

                    for (int i = m_MajorItems.Count - 1; i >= 0; --i)
                    {
                        m_MajorItems[i].Delete();
                    }
                    m_MajorItems = toadd;
                    foreach (Item item in m_MajorItems)
                    {
                        major.Backpack.AddItem(item);
                    }
                    return;
                }
            }
            Delete();
        }

        public override void OnAttach()
        {
            base.OnAttach();

            if (!(AttachedTo is TownStone))
            {
                Delete();
            }
        }

        public override void OnDelete()
        {
            for (int i = m_MajorItems.Count - 1; i >= 0; --i)
            {
                if (m_MajorItems[i] != null && !m_MajorItems[i].Deleted)
                {
                    m_MajorItems[i].Delete();
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.WriteItemList(m_MajorItems);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            m_MajorItems = reader.ReadStrongItemList();
        }
    }
}