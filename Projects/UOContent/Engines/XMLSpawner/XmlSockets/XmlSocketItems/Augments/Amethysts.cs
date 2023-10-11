using Server.Engines.XmlSpawner2;
using Server.Mobiles;

namespace Server.Items
{
    // --------------------------------------------------
    // Legendary Amethyst
    // --------------------------------------------------

    public class MythicAmethyst : BaseSocketAugmentation, ISocketAmethyst
    {
        //Ametista del Mito
        //Creature: +30 Danno max / +900 HP
        public override int LabelNumber => 504307;
        [Constructable]
        public MythicAmethyst() : base(0x0F2E)
        {
            Hue = 11;
        }

        public MythicAmethyst(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 3;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;

        public override bool OnAugment(Mobile from, object target)
        {
            /*if(target is BaseWeapon)
			{
				((BaseWeapon)target).Attributes.WeaponDamage += 17;
			} else
			if(target is BaseShield)
			{
				BaseShield s = target as BaseShield;

				s.Attributes.BonusStr += 9;

			} else
			if(target is BaseArmor)
			{
				((BaseArmor)target).Attributes.DefendChance += 16;
			} else*/

            if (target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;
                bc.DamageMax += 30;
                bc.HitsMax += 900;
                return true;
            }
            return false;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            return target is BaseCreature;//(target is BaseWeapon || target is BaseArmor || target is BaseCreature);
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            /*if(target is BaseWeapon)
			{
				((BaseWeapon)target).Attributes.WeaponDamage -= 17;
			} else
			if(target is BaseShield)
			{
				BaseShield s = target as BaseShield;

				s.Attributes.BonusStr -= 9;

			} else
			if(target is BaseArmor)
			{
				((BaseArmor)target).Attributes.DefendChance -= 16;
			} else*/
            if (target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;
                bc.DamageMax -= 30;
                bc.HitsMax -= 900;
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
                ItemID = 0x0F2E;
                Name = null;
            }
        }
    }

    // --------------------------------------------------
    // Legendary Amethyst
    // --------------------------------------------------

    public class LegendaryAmethyst : BaseSocketAugmentation, ISocketAmethyst
    {
        //Ametista Leggendaria
        //Creature: +20 Danno max / +600 HP
        public override int LabelNumber => 504309;
        [Constructable]
        public LegendaryAmethyst() : base(0x0F2E)
        {
            Hue = 12;
        }

        public LegendaryAmethyst(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 2;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;

        public override bool OnAugment(Mobile from, object target)
        {
            /*if(target is BaseWeapon)
			{
				((BaseWeapon)target).Attributes.WeaponDamage += 10;
			} else
			if(target is BaseShield)
			{
				BaseShield s = target as BaseShield;

				s.Attributes.BonusStr += 5;

			} else
			if(target is BaseArmor)
			{
				((BaseArmor)target).Attributes.DefendChance += 10;
			} else*/
            if (target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;
                bc.DamageMax += 20;
                bc.HitsMax += 600;
                return true;
            }
            return false;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            return target is BaseCreature;//(target is BaseWeapon || target is BaseArmor || target is BaseCreature);
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            /*if(target is BaseWeapon)
			{
				((BaseWeapon)target).Attributes.WeaponDamage -= 10;
			} else
			if(target is BaseShield)
			{
				BaseShield s = target as BaseShield;

				s.Attributes.BonusStr -= 5;

			} else
			if(target is BaseArmor)
			{
				((BaseArmor)target).Attributes.DefendChance -= 10;
			} else*/
            if (target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;
                bc.DamageMax -= 20;
                bc.HitsMax -= 600;
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
                ItemID = 0x0F2E;
                Name = null;
            }
        }
    }

    // --------------------------------------------------
    // Ancient Amethyst
    // --------------------------------------------------

    public class AncientAmethyst : BaseSocketAugmentation, ISocketAmethyst
    {
        //Ametista Antica
        //Creature: +10 Danno max / +300 HP
        public override int LabelNumber => 504311;
        [Constructable]
        public AncientAmethyst() : base(0x0F2E)
        {
            Hue = 15;
        }

        public AncientAmethyst(Serial serial) : base(serial)
        {
        }

        public override int SocketsRequired => 1;
        public override int Icon => 0x9a8;
        public override bool UseGumpArt => true;
        public override int IconXOffset => 15;
        public override int IconYOffset => 15;

        public override bool OnAugment(Mobile from, object target)
        {
            /*if(target is BaseWeapon)
			{
				((BaseWeapon)target).Attributes.WeaponDamage += 4;
			} else
			if(target is BaseShield)
			{
				BaseShield s = target as BaseShield;

				s.Attributes.BonusStr += 2;

			} else
			if(target is BaseArmor)
			{
				((BaseArmor)target).Attributes.DefendChance += 4;
			} else*/
            if (target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;
                bc.DamageMax += 10;
                bc.HitsMax += 300;
                return true;
            }
            return false;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            return target is BaseCreature;//(target is BaseWeapon || target is BaseArmor || target is BaseCreature);
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            /*if(target is BaseWeapon)
			{
				((BaseWeapon)target).Attributes.WeaponDamage -= 4;
			} else
			if(target is BaseShield)
			{
				BaseShield s = target as BaseShield;

				s.Attributes.BonusStr -= 2;

			} else
			if(target is BaseArmor)
			{
				((BaseArmor)target).Attributes.DefendChance -= 4;
			} else*/
            if (target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;
                bc.DamageMax -= 10;
                bc.HitsMax -= 300;
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
                ItemID = 0x0F2E;
                Name = null;
            }
        }
    }
}
