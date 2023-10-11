using Server.Engines.XmlSpawner2;
using Server.Mobiles;

namespace Server.Items
{

    // --------------------------------------------------
    // Mythic Tourmaline
    // --------------------------------------------------

    public class MythicTourmaline : BaseSocketAugmentation, ISocketTourmaline
    {
        //Tormalina del Mito
        //Creature: +30 a tutte le resistenze magiche
        public override int LabelNumber => 504333;
        [Constructable]
        public MythicTourmaline() : base(0xF2D)
        {
            Hue = 1161;
        }

        public MythicTourmaline(Serial serial) : base(serial)
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
                BaseCreature b = (BaseCreature)target;
                b.FireResistSeed += 30;
                b.ColdResistSeed += 30;
                b.PoisonResistSeed += 30;
                b.EnergyResistSeed += 30;
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
                BaseCreature b = (BaseCreature)target;
                b.FireResistSeed -= 30;
                b.ColdResistSeed -= 30;
                b.PoisonResistSeed -= 30;
                b.EnergyResistSeed -= 30;
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
                Name = null;
            }
        }
    }

    // --------------------------------------------------
    // Legendary Tourmaline
    // --------------------------------------------------

    public class LegendaryTourmaline : BaseSocketAugmentation, ISocketTourmaline
    {
        //Tormalina Leggendaria
        //Creature: +20 a tutte le resistenze magiche
        public override int LabelNumber => 504335;
        [Constructable]
        public LegendaryTourmaline() : base(0xF2D)
        {
            Hue = 53;
        }

        public LegendaryTourmaline(Serial serial) : base(serial)
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
                BaseCreature b = (BaseCreature)target;
                b.FireResistSeed += 20;
                b.ColdResistSeed += 20;
                b.PoisonResistSeed += 20;
                b.EnergyResistSeed += 20;
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
                BaseCreature b = (BaseCreature)target;
                b.FireResistSeed -= 20;
                b.ColdResistSeed -= 20;
                b.PoisonResistSeed -= 20;
                b.EnergyResistSeed -= 20;
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
                Name = null;
            }
        }
    }

    // --------------------------------------------------
    // Ancient Tourmaline
    // --------------------------------------------------

    public class AncientTourmaline : BaseSocketAugmentation, ISocketTourmaline
    {
        //Tormalina Antica
        //Creature: +10 a tutte le resistenze magiche
        public override int LabelNumber => 504337;
        [Constructable]
        public AncientTourmaline() : base(0xF2D)
        {
            Hue = 56;
        }

        public AncientTourmaline(Serial serial) : base(serial)
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
                BaseCreature b = (BaseCreature)target;
                b.FireResistSeed += 10;
                b.ColdResistSeed += 10;
                b.PoisonResistSeed += 10;
                b.EnergyResistSeed += 10;
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
                BaseCreature b = (BaseCreature)target;
                b.FireResistSeed -= 10;
                b.ColdResistSeed -= 10;
                b.PoisonResistSeed -= 10;
                b.EnergyResistSeed -= 10;
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
                Name = null;
            }
        }
    }
}
