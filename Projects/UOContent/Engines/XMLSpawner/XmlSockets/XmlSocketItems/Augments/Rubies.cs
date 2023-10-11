using Server.Engines.XmlSpawner2;
using Server.Mobiles;

namespace Server.Items
{
    public class MythicRuby : BaseSocketAugmentation, ISocketRuby
    {
        //Rubino del mito
        //"Creature: +600 Armatura";
        public override int LabelNumber => 504301;
        [Constructable]
        public MythicRuby() : base(0x0F2B)
        {
            Hue = 32;
        }

        public MythicRuby(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 3;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;
        public override int Version => 1;

        public override bool CanAugment(Mobile from, object target)
        {
            return (target is BaseCreature);
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseCreature)
            {
                ((BaseCreature)target).VirtualArmor += 600;
                return true;
            }
            return false;
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseCreature)
            {
                if (version == 0)
                {
                    ((BaseCreature)target).VirtualArmor -= 900;
                }
                else
                {
                    ((BaseCreature)target).VirtualArmor -= 600;
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
                ItemID = 0x0F2B;
                Name = null;
            }
        }
    }

    // --------------------------------------------------
    // Legendary Ruby
    // --------------------------------------------------

    public class LegendaryRuby : BaseSocketAugmentation, ISocketRuby
    {
        //Rubino Leggendario
        //Creature: +400 Armatura
        public override int LabelNumber => 504303;
        [Constructable]
        public LegendaryRuby() : base(0x0F2B)
        {
            Hue = 33;
        }

        public LegendaryRuby(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 2;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;
        public override int Version => 1;

        public override bool CanAugment(Mobile from, object target)
        {
            return (target is BaseCreature);
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseCreature)
            {
                ((BaseCreature)target).VirtualArmor += 400;
                return true;
            }
            return false;
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseCreature)
            {
                if (version == 0)
                {
                    ((BaseCreature)target).VirtualArmor -= 600;
                }
                else
                {
                    ((BaseCreature)target).VirtualArmor -= 400;
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
                ItemID = 0x0F2B;
                Name = null;
            }
        }
    }

    // --------------------------------------------------
    // Ancient Ruby
    // --------------------------------------------------

    public class AncientRuby : BaseSocketAugmentation, ISocketRuby
    {
        //Rubino Antico
        //Creature: +200 Armatura
        public override int LabelNumber => 504305;
        [Constructable]
        public AncientRuby() : base(0x0F2B)
        {
            Hue = 30;
        }

        public AncientRuby(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 1;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;
        public override int Version => 1;

        public override bool CanAugment(Mobile from, object target)
        {
            return (target is BaseCreature);
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseCreature)
            {
                ((BaseCreature)target).VirtualArmor += 200;
                return true;
            }
            return false;
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseCreature)
            {
                if (version == 0)
                {
                    ((BaseCreature)target).VirtualArmor -= 300;
                }
                else
                {
                    ((BaseCreature)target).VirtualArmor -= 200;
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
                ItemID = 0x0F2B;
                Name = null;
            }
        }
    }
}
