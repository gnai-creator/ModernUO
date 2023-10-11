using Server.Engines.XmlSpawner2;
using Server.Mobiles;

namespace Server.Items
{
    // --------------------------------------------------
    // Mythic Emerald
    // --------------------------------------------------

    public class MythicEmerald : BaseSocketAugmentation, ISocketEmerald
    {
        //Smeraldo del Mito
        //Creature: +120 Dex
        public override int LabelNumber => 504321;
        [Constructable]
        public MythicEmerald() : base(0x0F2F)
        {
            Hue = 1267;
        }

        public MythicEmerald(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 3;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;
        public override int Version => 1;

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseCreature)
            {
                ((BaseCreature)target).RawDex += 120;
                return true;
            }
            return false;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            return (target is BaseCreature);
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseCreature)
            {
                if (version == 1)
                {
                    ((BaseCreature)target).RawDex -= 120;
                }
                else
                {
                    ((BaseCreature)target).RawDex -= 60;
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
                ItemID = 0x0F2F;
                Name = null;
            }
        }
    }

    // --------------------------------------------------
    // Legendary Emerald
    // --------------------------------------------------

    public class LegendaryEmerald : BaseSocketAugmentation, ISocketEmerald
    {
        //Smeraldo Leggendario
        //Creature: +80 Dex
        public override int LabelNumber => 504323;
        [Constructable]
        public LegendaryEmerald() : base(0x0F2F)
        {
            Hue = 1268;
        }

        public LegendaryEmerald(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 2;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;
        public override int Version => 1;

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseCreature)
            {
                ((BaseCreature)target).RawDex += 80;
                return true;
            }
            return false;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            return (target is BaseCreature);
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseCreature)
            {
                if (version == 1)
                {
                    ((BaseCreature)target).RawDex -= 80;
                }
                else
                {
                    ((BaseCreature)target).RawDex -= 40;
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
                ItemID = 0x0F2F;
                Name = null;
            }
        }
    }

    // --------------------------------------------------
    // Ancient Emerald
    // --------------------------------------------------

    public class AncientEmerald : BaseSocketAugmentation, ISocketEmerald
    {
        //Smeraldo Antico
        //Creature: +40 Dex
        public override int LabelNumber => 504325;
        [Constructable]
        public AncientEmerald() : base(0x0F2F)
        {
            Hue = 76;
        }

        public AncientEmerald(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 1;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;
        public override int Version => 1;

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseCreature)
            {
                ((BaseCreature)target).RawDex += 40;
                return true;
            }
            return false;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            return (target is BaseCreature);
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseCreature)
            {
                if (version == 1)
                {
                    ((BaseCreature)target).RawDex -= 40;
                }
                else
                {
                    ((BaseCreature)target).RawDex -= 20;
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
                ItemID = 0x0F2F;
                Name = null;
            }
        }
    }
}
