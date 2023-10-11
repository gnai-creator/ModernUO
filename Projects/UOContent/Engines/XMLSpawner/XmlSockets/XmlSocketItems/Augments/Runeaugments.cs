using Server.Engines.XmlSpawner2;

namespace Server.Items
{
    public class RunaDiLuce : BaseSocketAugmentation, ISocketRune
    {
        //Runa della Luce
        //Armi e Scudi: Luce Permanente
        public override int LabelNumber => 504327;
        [Constructable]
        public RunaDiLuce() : base(0x1f14)
        {
            Hue = 289;
        }

        public RunaDiLuce(Serial serial) : base(serial)
        {
        }

        public override int IconXOffset => 5;
        public override int IconYOffset => 20;

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is IDurability)
            {
                IDurability id = (IDurability)target;

                if (id.MaxHitPoints == 0 && id.HitPoints == 0)
                {
                    from.SendLocalizedMessage(504292);//"Gli oggetti indistruttibili non possono avere applicata tale runa");
                }
                else if (target is BaseWeapon)
                {
                    ((BaseWeapon)target).Attributes.NightSight = 1;
                    from.SendLocalizedMessage(504293);//"Il bersaglio brilla di luce propria ora!");
                    return true;
                }
                else if (target is BaseShield)
                {
                    ((BaseShield)target).Attributes.NightSight = 1;
                    from.SendLocalizedMessage(504293);//"Il bersaglio brilla di luce propria ora!");
                    return true;
                }
            }

            return false;
        }


        public override bool CanAugment(Mobile from, object target)
        {
            if (target is BaseWeapon || target is BaseShield)
            {
                return true;
            }

            return false;
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

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseWeapon)
            {
                ((BaseWeapon)target).Attributes.NightSight = 0;
                from.SendLocalizedMessage(504291);//"L'oggetto ha smesso di brillare.");
            }
            else if (target is BaseShield)
            {
                ((BaseShield)target).Attributes.NightSight = 0;
                from.SendLocalizedMessage(504291);//"L'oggetto ha smesso di brillare.");
            }
            else
            {
                return false;
            }

            return true;
        }

        public override bool CanRecover(Mobile from, object target, int version)
        {
            return true;
        }
    }
    // ---------------------------------------------------
    // Tyr rune
    // ---------------------------------------------------

    /*public class TyrRune : BaseSocketAugmentation
    {
    
        [Constructable]
        public TyrRune() : base(0x1f14)
        {
            Name = "Tyr Rune";
            Hue = 289;
        }

        public override int IconXOffset { get { return 5;} }

        public override int IconYOffset { get { return 20;} }

        public TyrRune( Serial serial ) : base( serial )
		{
		}

        public override string OnIdentify(Mobile from)
        {

            return "Arma: Attacco Speciale colpo Staminante\nScudo: Difesa speciale Staminante";
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if(target is BaseWeapon)
            {
                // adds the custom attack attachment
                XmlCustomAttacks.AddAttack(target, XmlCustomAttacks.SpecialAttacks.Staminante);

                from.SendMessage("Il bersaglio ha acquisito il colpo staminante");
                return true;
            } else
            if(target is BaseShield)
            {
                // adds the custom defense attachment
                XmlCustomDefenses.AddDefense(target, XmlCustomDefenses.SpecialDefenses.StamDrain);

                from.SendMessage("Il bersaglio possiede la difesa staminante");
                return true;
            }

            return false;
        }


        public override bool CanAugment(Mobile from, object target)
        {
            if(target is BaseWeapon || target is BaseArmor)
            {
                return true;
            }

            return false;
        }
        

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
    }
    
    // ---------------------------------------------------
    // Ahm rune
    // ---------------------------------------------------

    public class AhmRune : BaseSocketAugmentation
    {

        [Constructable]
        public AhmRune() : base(0x1f14)
        {
            Name = "Ahm Rune";
            Hue = 289;
        }

        public AhmRune( Serial serial ) : base( serial )
		{
		}

        public override int IconXOffset { get { return 5;} }

        public override int IconYOffset { get { return 20;} }

        public override string OnIdentify(Mobile from)
        {
            return "Weapons: Special attack Puff Of Smoke\nShields: Special defense Puff Of Smoke";
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if(target is BaseWeapon)
            {
                XmlCustomAttacks.AddAttack(target, XmlCustomAttacks.SpecialAttacks.ColpoOmbra);

                from.SendMessage("The target has gained the special attack Puff Of Smoke");
                return true;
            } else
            if(target is BaseShield)
            {
                XmlCustomDefenses.AddDefense(target, XmlCustomDefenses.SpecialDefenses.PuffOfSmoke);

                from.SendMessage("The target has gained the special defense Puff Of Smoke");
                return true;
            }

            return false;
        }

        
        public override bool CanAugment(Mobile from, object target)
        {
            if(target is BaseWeapon || target is BaseShield)
            {
                return true;
            }
            
            return false;
        }
        
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
    }
    
    // ---------------------------------------------------
    // Mor rune
    // ---------------------------------------------------

    public class MorRune : BaseSocketAugmentation
    {

        [Constructable]
        public MorRune() : base(0x1f14)
        {
            Name = "Mor Rune";
            Hue = 289;
        }

        public MorRune( Serial serial ) : base( serial )
		{
		}

        public override int IconXOffset { get { return 5;} }

        public override int IconYOffset { get { return 20;} }

        public override string OnIdentify(Mobile from)
        {
            return "Weapons: Special attack Mind Drain\nShields: Special defense Mind Drain";
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if(target is BaseWeapon)
            {
                XmlCustomAttacks.AddAttack(target, XmlCustomAttacks.SpecialAttacks.MindDrain);

                from.SendMessage("The target has gained the special attack Mind Drain");
                return true;
            } else
            if(target is BaseShield)
            {
                XmlCustomDefenses.AddDefense(target, XmlCustomDefenses.SpecialDefenses.MindDrain);

                from.SendMessage("The target has gained the special defense Mind Drain");
                return true;
            }

            return false;
        }

        
        public override bool CanAugment(Mobile from, object target)
        {
            if(target is BaseWeapon || target is BaseShield)
            {
                return true;
            }
            
            return false;
        }
        
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
    }
    
    // ---------------------------------------------------
    // Mef rune
    // ---------------------------------------------------

    public class MefRune : BaseSocketAugmentation
    {

        [Constructable]
        public MefRune() : base(0x1f14)
        {
            Name = "Mef Rune";
            Hue = 289;
        }

        public MefRune( Serial serial ) : base( serial )
		{
		}

        public override int IconXOffset { get { return 5;} }

        public override int IconYOffset { get { return 20;} }

        public override string OnIdentify(Mobile from)
        {
            return "Weapons: Special attack Gift of Health\nShields: Special defense Gift of Health";
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if(target is BaseWeapon)
            {
                XmlCustomAttacks.AddAttack(target, XmlCustomAttacks.SpecialAttacks.GiftOfHealth);

                from.SendMessage("The target has gained the special attack Gift of Health");
                return true;
            } else
            if(target is BaseShield)
            {
                XmlCustomDefenses.AddDefense(target, XmlCustomDefenses.SpecialDefenses.GiftOfHealth);

                from.SendMessage("The target has gained the special defense Gift of Health");
                return true;
            }

            return false;
        }

        
        public override bool CanAugment(Mobile from, object target)
        {
            if(target is BaseWeapon || target is BaseShield)
            {
                return true;
            }
            
            return false;
        }
        
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
    }
    
    // ---------------------------------------------------
    // Ylm rune
    // ---------------------------------------------------

    public class YlmRune : BaseSocketAugmentation
    {

        [Constructable]
        public YlmRune() : base(0x1f14)
        {
            Name = "Ylm Rune";
            Hue = 289;
        }

        public YlmRune( Serial serial ) : base( serial )
		{
		}

        public override int IconXOffset { get { return 5;} }

        public override int IconYOffset { get { return 20;} }

        public override string OnIdentify(Mobile from)
        {
            return "Weapons: Special attack Vortex Strike\nShields: Special defense Spike Shield";
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if(target is BaseWeapon)
            {
                XmlCustomAttacks.AddAttack(target, XmlCustomAttacks.SpecialAttacks.VortexStrike);

                from.SendMessage("The target has gained the special attack Vortex Strike");
                return true;
            } else
            if(target is BaseShield)
            {
                XmlCustomDefenses.AddDefense(target, XmlCustomDefenses.SpecialDefenses.SpikeShield);

                from.SendMessage("The target has gained the special defense Spike Shield");
                return true;
            }

            return false;
        }

        
        public override bool CanAugment(Mobile from, object target)
        {
            if(target is BaseWeapon || target is BaseShield)
            {
                return true;
            }
            
            return false;
        }
        
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
    }
    
    // ---------------------------------------------------
    // Kot rune
    // ---------------------------------------------------

    public class KotRune : BaseSocketAugmentation
    {

        [Constructable]
        public KotRune() : base(0x1f14)
        {
            Name = "Kot Rune";
            Hue = 289;
        }

        public KotRune( Serial serial ) : base( serial )
		{
		}

        public override int IconXOffset { get { return 5;} }

        public override int IconYOffset { get { return 20;} }

        public override string OnIdentify(Mobile from)
        {
            return "Weapons: Special attack Paralyzing Fear\nShields: Special defense Paralyzing Fear";
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if(target is BaseWeapon)
            {
                XmlCustomAttacks.AddAttack(target, XmlCustomAttacks.SpecialAttacks.ParalyzingFear);

                from.SendMessage("The target has gained the special attack Paralyzing Fear");
                return true;
            } else
            if(target is BaseShield)
            {
                XmlCustomDefenses.AddDefense(target, XmlCustomDefenses.SpecialDefenses.ParalyzingFear);

                from.SendMessage("The target has gained the special defense Paralyzing Fear");
                return true;
            }

            return false;
        }

        
        public override bool CanAugment(Mobile from, object target)
        {
            if(target is BaseWeapon || target is BaseShield)
            {
                return true;
            }
            
            return false;
        }
        
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
    }
    
    // ---------------------------------------------------
    // Jor rune
    // ---------------------------------------------------

    public class JorRune : BaseSocketAugmentation
    {

        [Constructable]
        public JorRune() : base(0x1f14)
        {
            Name = "Jor Rune";
            Hue = 289;
        }

        public JorRune( Serial serial ) : base( serial )
		{
		}

        public override int IconXOffset { get { return 5;} }

        public override int IconYOffset { get { return 20;} }

        public override string OnIdentify(Mobile from)
        {
            return "Weapons: Special attack Triple Slash";
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if(target is BaseWeapon)
            {
                XmlCustomAttacks.AddAttack(target, XmlCustomAttacks.SpecialAttacks.TripleSlash);

                from.SendMessage("The target has gained the special attack Triple Slash");
                return true;
            }

            return false;
        }

        
        public override bool CanAugment(Mobile from, object target)
        {
            if(target is BaseWeapon)
            {
                return true;
            }
            
            return false;
        }
        
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
    }*/
}
