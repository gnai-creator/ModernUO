using Server.Engines.XmlSpawner2;
using Server.Mobiles;

namespace Server.Items
{
    // --------------------------------------------------
    // Mythic Diamond
    // --------------------------------------------------

    public class MythicDiamond : BaseSocketAugmentation, ISocketDiamond
    {
        //Diamante del Mito
        //Creature: +120 Forza
        public override int LabelNumber => 504315;
        [Constructable]
        public MythicDiamond() : base(0x0F30)
        {
            Hue = 1153;
        }

        public MythicDiamond(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 3;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseCreature)
            {
                ((BaseCreature)target).RawStr += 120;
                return true;
            }
            return false;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            return (target is BaseCreature);
        }
        public override int Version => 1;
        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseCreature)
            {
                if (version == 1)
                {
                    ((BaseCreature)target).RawStr -= 120;
                }
                else
                {
                    ((BaseCreature)target).RawStr -= 60;
                }

                return true;
            }
            return false;
        }

        public override bool CanRecover(Mobile from, object target, int version)
        {
            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            if (version < 1)
            {
                ItemID = 0x0F30;
                Name = null;
            }
        }
    }

    // --------------------------------------------------
    // Legendary Diamond
    // --------------------------------------------------

    public class LegendaryDiamond : BaseSocketAugmentation, ISocketDiamond
    {
        //Diamante Leggendario
        //Creature: +80 Forza
        public override int LabelNumber => 504317;
        [Constructable]
        public LegendaryDiamond() : base(0x0F30)
        {
            Hue = 1150;
        }

        public LegendaryDiamond(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 2;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseCreature)
            {
                ((BaseCreature)target).RawStr += 80;
                return true;
            }
            return false;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            return (target is BaseCreature);
        }
        public override int Version => 1;
        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseCreature)
            {
                if (version == 1)
                {
                    ((BaseCreature)target).RawStr -= 80;
                }
                else
                {
                    ((BaseCreature)target).RawStr -= 40;
                }

                return true;
            }
            return false;
        }

        public override bool CanRecover(Mobile from, object target, int version)
        {
            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            if (version < 1)
            {
                ItemID = 0x0F30;
                Name = null;
            }
        }
    }

    // --------------------------------------------------
    // Ancient Diamond
    // --------------------------------------------------

    public class AncientDiamond : BaseSocketAugmentation, ISocketDiamond
    {
        //Diamante Antico
        //Creature: +40 Forza
        public override int LabelNumber => 504319;
        [Constructable]
        public AncientDiamond() : base(0x0F30)
        {
            Hue = 1151;
        }

        public AncientDiamond(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 1;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseCreature)
            {
                ((BaseCreature)target).RawStr += 40;
                return true;
            }
            return false;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            return (target is BaseCreature);
        }
        public override int Version => 1;
        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseCreature)
            {
                if (version == 1)
                {
                    ((BaseCreature)target).RawStr -= 40;
                }
                else
                {
                    ((BaseCreature)target).RawStr -= 20;
                }

                return true;
            }
            return false;
        }

        public override bool CanRecover(Mobile from, object target, int version)
        {
            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            if (version < 1)
            {
                ItemID = 0x0F30;
                Name = null;
            }
        }
    }
}
