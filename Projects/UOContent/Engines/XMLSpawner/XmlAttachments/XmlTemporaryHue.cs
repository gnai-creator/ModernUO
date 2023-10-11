using System;
using System.Collections.Generic;

namespace Server.Engines.XmlSpawner2
{
    public class XmlTemporaryHue : XmlAttachment
    {
        private int m_Originalhue;
        private int m_Hue;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Hue { get => m_Hue; set => m_Hue = value; }


        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlTemporaryHue(ASerial serial) : base(serial)
        {
        }

        public XmlTemporaryHue(string name, int hue, TimeSpan duration)
        {
            Name = name;
            m_Hue = hue;
            Expiration = duration;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(m_Originalhue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            // version 0
            m_Originalhue = reader.ReadInt();
            Timer.DelayCall(Delete);
        }

        public override void OnDelete()
        {
            base.OnDelete();

            // remove the mod
            if (AttachedTo is Mobile)
            {
                ((Mobile)AttachedTo).Hue = m_Originalhue;
            }
            else if (AttachedTo is Item)
            {
                ((Item)AttachedTo).Hue = m_Originalhue;
            }
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // apply the mod
            if (AttachedTo is Mobile)
            {
                Mobile m = AttachedTo as Mobile;
                m_Originalhue = m.TrueHue;
                List<XmlAttachment> temphues = XmlAttach.FindAttachments(m, typeof(XmlTemporaryHue));
                if (temphues != null)
                {
                    temphues.Remove(this);
                    foreach (XmlTemporaryHue hues in temphues)
                    {
                        m_Originalhue = hues.m_Originalhue;
                        break;
                    }
                }
                m.Hue = m_Hue;
            }
            else if (AttachedTo is Item)
            {
                Item i = AttachedTo as Item;
                m_Originalhue = i.Hue;
                List<XmlAttachment> temphues = XmlAttach.FindAttachments(i, typeof(XmlTemporaryHue));
                if (temphues != null)
                {
                    temphues.Remove(this);
                    foreach (XmlTemporaryHue hues in temphues)
                    {
                        m_Originalhue = hues.m_Originalhue;
                        break;
                    }
                }
                i.Hue = m_Hue;
            }
            else
            {
                Delete();
            }
        }

        public override void OnBeforeReattach(XmlAttachment old)
        {
            base.OnBeforeReattach(old);

            if (old != null && !old.Deleted && old is XmlTemporaryHue)
            {
                XmlTemporaryHue x = (XmlTemporaryHue)old;
                if (AttachedTo is Mobile)
                {
                    Mobile m = AttachedTo as Mobile;
                    m_Originalhue = x.m_Originalhue;
                    m.Hue = m_Hue;
                }
                else if (AttachedTo is Item)
                {
                    Item i = AttachedTo as Item;
                    m_Originalhue = x.m_Originalhue;
                    i.Hue = m_Hue;
                }
            }
        }
    }
}
