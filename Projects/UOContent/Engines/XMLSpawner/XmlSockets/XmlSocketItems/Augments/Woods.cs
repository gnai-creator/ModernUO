using Server.Engines.XmlSpawner2;

namespace Server.Items
{

    // ---------------------------------------------------
    // Mythic wood
    // ---------------------------------------------------

    public class MythicWood : BaseSocketAugmentation, ISocketWood
    {
        //Legno del Mito
        //Armatura: +15 Lumberjacking
        public override int LabelNumber => 504339;
        [Constructable]
        public MythicWood() : base(0x0F90)
        {
            Hue = 11;
        }

        public override int SocketsRequired => 3;
        public override int IconXOffset => 5;
        public override int IconYOffset => 20;

        public MythicWood(Serial serial) : base(serial)
        {
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseArmor)
            {
                BaseArmor a = (BaseArmor)target;
                // find a free slot
                for (int i = 0; i < 5; ++i)
                {
                    if (a.SkillBonuses.GetBonus(i) == 0)
                    {
                        a.SkillBonuses.SetValues(i, SkillName.Lumberjacking, 15.0f);
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseArmor)
            {
                BaseArmor a = (BaseArmor)target;
                for (int i = 0; i < 5; ++i)
                {
                    if (a.SkillBonuses.GetSkill(i) == SkillName.Lumberjacking)
                    {
                        if (a.SkillBonuses.GetBonus(i) == 15.0)
                        {
                            a.SkillBonuses.SetValues(i, SkillName.Alchemy, 0.0f);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool CanRecover(Mobile from, object target, int version)
        {
            return true;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            if (target is BaseArmor)
            {
                return true;
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    // ---------------------------------------------------
    // Legendary wood
    // ---------------------------------------------------

    public class LegendaryWood : BaseSocketAugmentation, ISocketWood
    {
        //Legno Leggendario
        //Armatura: +10 Lumberjacking
        public override int LabelNumber => 504341;
        [Constructable]
        public LegendaryWood() : base(0x0F90)
        {
            Hue = 12;
        }

        public override int SocketsRequired => 2;
        public override int IconXOffset => 5;
        public override int IconYOffset => 20;

        public LegendaryWood(Serial serial) : base(serial)
        {
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseArmor)
            {
                BaseArmor a = (BaseArmor)target;
                // find a free slot
                for (int i = 0; i < 5; ++i)
                {
                    if (a.SkillBonuses.GetBonus(i) == 0)
                    {
                        a.SkillBonuses.SetValues(i, SkillName.Lumberjacking, 10.0f);
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseArmor)
            {
                BaseArmor a = (BaseArmor)target;
                for (int i = 0; i < 5; ++i)
                {
                    if (a.SkillBonuses.GetSkill(i) == SkillName.Lumberjacking)
                    {
                        if (a.SkillBonuses.GetBonus(i) == 10.0)
                        {
                            a.SkillBonuses.SetValues(i, SkillName.Alchemy, 0.0f);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool CanRecover(Mobile from, object target, int version)
        {
            return true;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            if (target is BaseArmor)
            {
                return true;
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }


    // ---------------------------------------------------
    // Ancient wood
    // ---------------------------------------------------

    public class AncientWood : BaseSocketAugmentation, ISocketWood
    {
        //504343
        //Armatura: +5 Lumberjacking
        public override int LabelNumber => 504343;
        [Constructable]
        public AncientWood() : base(0x0F90)
        {
            Hue = 15;
        }

        public override int IconXOffset => 5;
        public override int IconYOffset => 20;

        public AncientWood(Serial serial) : base(serial)
        {
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseArmor)
            {
                BaseArmor a = (BaseArmor)target;
                // find a free slot
                for (int i = 0; i < 5; ++i)
                {
                    if (a.SkillBonuses.GetBonus(i) == 0)
                    {
                        a.SkillBonuses.SetValues(i, SkillName.Lumberjacking, 5.0f);
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseArmor)
            {
                BaseArmor a = (BaseArmor)target;
                for (int i = 0; i < 5; ++i)
                {
                    if (a.SkillBonuses.GetSkill(i) == SkillName.Lumberjacking)
                    {
                        if (a.SkillBonuses.GetBonus(i) == 5.0)
                        {
                            a.SkillBonuses.SetValues(i, SkillName.Alchemy, 0.0f);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool CanRecover(Mobile from, object target, int version)
        {
            return true;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            if (target is BaseArmor)
            {
                return true;
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}
