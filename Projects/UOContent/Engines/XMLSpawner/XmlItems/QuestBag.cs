namespace Server.Items
{
    public class QuestBag : Container
    {
        public override int LabelNumber => 504525;
        [Constructable]
        public QuestBag() : base(0xE76)
        {
            Hue = 1161;
            Weight = 0.0f;
        }

        public QuestBag(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Parent != from.Backpack)//ammesso uso solo nel backpack, non in sottocontenitori
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }
            base.OnDoubleClick(from);
        }

        public override bool DisplaysContent => Parent is Backpack && base.DisplaysContent;

        public override bool Nontransferable => true;
        public override bool CanRenameContainer => false;
        public override void HandleInvalidTransfer(Mobile from)
        {
            from.SendGump(new Gumps.XmlConfirmDeleteGump(from, this));
            //base.HandleInvalidTransfer(from);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            if (version < 1)
            {
                Name = null;
            }

            if (Layer == Layer.Auction)
            {
                Delete();
            }
        }

        public override bool OnStackAttempt(Mobile from, Item stack, Item dropped)
        {
            from.SendLocalizedMessage(504526);// "Questa borsa non può contenere oggetti." ); // That must be in your pack for you to use it.
            return false;
        }

        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            from.SendLocalizedMessage(504526);// "Questa borsa non può contenere oggetti." ); // That must be in your pack for you to use it.
            return false;
        }

        public override bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return false;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            from.SendLocalizedMessage(504526);// "Questa borsa non può contenere oggetti." ); // That must be in your pack for you to use it.
            return false;
        }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            return false;
        }
    }
}