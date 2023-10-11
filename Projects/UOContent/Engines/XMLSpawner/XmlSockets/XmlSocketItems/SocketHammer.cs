using Server.ContextMenus;
using Server.Engines.XmlSpawner2;
using System.Collections.Generic;

namespace Server.Items
{
    public interface ISocketConsumer
    {
        bool IsChildOf(IEntity o);
        int UsesRemaining { get; set; }
        IEntity Parent { get; }
    }

    [FlipableAttribute(0x13E4, 0x13E3)]
    public class SocketHammer : Item, ISocketConsumer
    {
        public override int LabelNumber => 504259;
        private int m_UsesRemaining;
        private bool m_Incastonare;
        public bool Incastonare
        {
            get => m_Incastonare;
            set
            {
                m_Incastonare = value;
                if (m_Incastonare)
                {
                    Hue = 0x55;
                }
                else
                {
                    Hue = 0x22;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Developer)]
        public int UsesRemaining
        {
            get => m_UsesRemaining;
            set { m_UsesRemaining = value; InvalidateProperties(); }
        }

        [Constructable(AccessLevel.Developer)]
        public SocketHammer() : this(Utility.RandomMinMax(60, 100))
        {
        }

        [Constructable(AccessLevel.Developer)]
        public SocketHammer(int nuses) : base(0x13E4)
        {
            Hue = 0x55;
            UsesRemaining = nuses;
            Layer = Layer.OneHanded;
            m_Incastonare = true;
        }

        public SocketHammer(Serial serial) : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_UsesRemaining >= 0)
            {
                list.Add(1060584, m_UsesRemaining.ToString()); // uses remaining: ~1_val~
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.Backpack == null)
            {
                return;
            }

            if (IsChildOf(from.Backpack) || Parent == from)
            {
                if (UsesRemaining <= 0)
                {
                    from.SendLocalizedMessage(504256);//"Rompi il martello per l'eccessiva usura!");
                    Delete();
                    return;
                }
                Item equip = from.FindItemOnLayer(Layer.OneHanded);
                if (equip != null && equip != this)
                {
                    from.PlaceInBackpack(equip);
                    equip.InvalidateProperties();
                }
                equip = from.FindItemOnLayer(Layer.TwoHanded);

                if (equip != null && equip != this && !(equip is BaseShield))
                {
                    from.PlaceInBackpack(equip);
                    equip.InvalidateProperties();
                }
                from.EquipItem(this);

                if (m_Incastonare)
                {
                    from.Target = new XmlSockets.AddSocketToTarget(0, this);
                }
                else
                {
                    from.Target = new XmlSockets.RecoverAugmentationFromTarget(this);
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            list.Add(new InserisciSlot(from, this));
            list.Add(new TogliRuna(from, this));
            base.GetContextMenuEntries(from, list);
        }

        private class InserisciSlot : ContextMenuEntry
        {
            private Mobile m_Mobile;
            private SocketHammer m_Hammer;

            public InserisciSlot(Mobile m, SocketHammer hammer) : base(5100)
            {
                m_Mobile = m;
                m_Hammer = hammer;
                Enabled = !hammer.m_Incastonare;
            }

            public override void OnClick()
            {
                if (m_Mobile == null || m_Mobile.Deleted || m_Mobile.Backpack == null || m_Hammer == null)
                {
                    return;
                }

                if (m_Hammer.IsChildOf(m_Mobile.Backpack) || m_Hammer.Parent == m_Mobile)
                {
                    m_Hammer.Incastonare = true;
                }
                else
                {
                    m_Mobile.SendLocalizedMessage(504255);//"L'incastonatore deve trovarsi nel tuo zaino.");
                }
            }
        }

        private class TogliRuna : ContextMenuEntry
        {
            private Mobile m_Mobile;
            private SocketHammer m_Hammer;

            public TogliRuna(Mobile m, SocketHammer hammer) : base(6142)
            {
                m_Mobile = m;
                m_Hammer = hammer;
                Enabled = hammer.m_Incastonare;
            }

            public override void OnClick()
            {
                if (m_Mobile == null || m_Mobile.Deleted || m_Mobile.Backpack == null || m_Hammer == null)
                {
                    return;
                }

                if (m_Hammer.IsChildOf(m_Mobile.Backpack) || m_Hammer.Parent == m_Mobile)
                {
                    m_Hammer.Incastonare = false;
                }
                else
                {
                    m_Mobile.SendLocalizedMessage(504255);//"L'incastonatore deve trovarsi nel tuo zaino.");
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
            writer.Write(m_UsesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_UsesRemaining = reader.ReadInt();
            Incastonare = true;
            if (version < 1)
            {
                Name = null;
            }
        }
    }
}
