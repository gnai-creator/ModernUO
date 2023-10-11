namespace Server.Items
{
    public abstract class SiegeCannonball : BaseSiegeProjectile
    {
        public SiegeCannonball()
            : this(1)
        {
        }

        public SiegeCannonball(int amount)
            : base(amount, 0xE73)
        {
            Stackable = true;
            Amount = amount;
            Weight = 5f;
        }

        public SiegeCannonball(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            /*int version =*/
            reader.ReadInt();
        }
    }

    public class LightCannonball : SiegeCannonball
    {
        public override int LabelNumber => 504473;

        [Constructable]
        public LightCannonball()
            : this(1)
        {
        }

        [Constructable]
        public LightCannonball(int amount)
            : base(amount)
        {
            Range = 20;
            Area = 1;
            AccuracyBonus = 10;
            PhysicalDamage = 65;
            FireDamage = 15;
            FiringSpeed = 35;
        }

        public LightCannonball(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            /*int version =*/
            reader.ReadInt();
        }
        /*
                public override Item Dupe(int amount)
                {
                    LightCannonball s = new LightCannonball(amount);

                    return this.Dupe(s, amount);
                }
         * */
    }

    public class IronCannonball : SiegeCannonball
    {
        public override int LabelNumber => 504474;

        [Constructable]
        public IronCannonball()
            : this(1)
        {
        }

        [Constructable]
        public IronCannonball(int amount)
            : base(amount)
        {
            Range = 17;
            Area = 1;
            AccuracyBonus = 0;
            PhysicalDamage = 80;
            FireDamage = 20;
            FiringSpeed = 25;
        }

        public IronCannonball(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            /*int version =*/
            reader.ReadInt();
        }
        /*
                public override Item Dupe(int amount)
                {
                    IronCannonball s = new IronCannonball(amount);

                    return this.Dupe(s, amount);
                }
         * */
    }

    public class ExplodingCannonball : SiegeCannonball
    {
        public override int LabelNumber => 504475;

        [Constructable]
        public ExplodingCannonball()
            : this(1)
        {
        }

        [Constructable]
        public ExplodingCannonball(int amount)
            : base(amount)
        {
            Range = 14;
            Area = 2;
            AccuracyBonus = -10;
            PhysicalDamage = 30;
            FireDamage = 60;
            FiringSpeed = 20;
            Hue = 46;
        }

        public ExplodingCannonball(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            /*int version =*/
            reader.ReadInt();
        }
        /*
                public override Item Dupe(int amount)
                {
                    ExplodingCannonball s = new ExplodingCannonball(amount);

                    return this.Dupe(s, amount);
                }
         * */
    }

    public class FieryCannonball : SiegeCannonball
    {
        public override int LabelNumber => 504476;

        // use a fireball animation when fired
        public override int AnimationID => 0x36D4;
        public override int AnimationHue => 0;

        [Constructable]
        public FieryCannonball()
            : this(1)
        {
        }

        [Constructable]
        public FieryCannonball(int amount)
            : base(amount)
        {
            Range = 12;
            Area = 3;
            AccuracyBonus = -20;
            PhysicalDamage = 30;
            FireDamage = 70;
            FiringSpeed = 10;
            Hue = 33;
        }

        public FieryCannonball(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            /*int version =*/
            reader.ReadInt();
        }
        /*
                public override Item Dupe(int amount)
                {
                    FieryCannonball s = new FieryCannonball(amount);

                    return this.Dupe(s, amount);
                }
         * */
    }

    public class GrapeShot : SiegeCannonball
    {
        // only does damage to mobiles
        public override double StructureDamageMultiplier => 0.0;  //  damage multiplier for structures
        public override int LabelNumber => 504477;

        [Constructable]
        public GrapeShot()
            : this(1)
        {
        }

        public override int ItemID => ItemID = 0xE74;

        [Constructable]
        public GrapeShot(int amount)
            : base(amount)
        {
            Range = 22;
            Area = 3;
            AccuracyBonus = -10;
            PhysicalDamage = 25;
            FireDamage = 25;
            FiringSpeed = 35;
        }

        public GrapeShot(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            /*int version =*/
            reader.ReadInt();
        }
    }
}
