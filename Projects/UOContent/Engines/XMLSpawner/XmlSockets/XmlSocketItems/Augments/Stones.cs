using Server.Commands;
using Server.Engines.XmlSpawner2;
using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.Items
{
    public class BondingStone : BaseSocketAugmentation, ISocketStone
    {
        //Bonding Stone
        //Creature: Bond<BR>Durata: 
        public override int LabelNumber => 504329;
        [Constructable]
        public BondingStone() : this(200.0)
        {
            Hue = 35;
        }

        public BondingStone(double hours) : base(0xFCC)
        {
            Hue = 35;
            m_Time = hours;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.Target = new InternalTarget(this);
                from.SendLocalizedMessage(504294);//"Seleziona delle exp per ricaricare la pietra! (ATTENZIONE: vengono consumate tutte le fiale del mucchio!)");
            }
            else
            {
                from.SendLocalizedMessage(504295);//"Deve trovarsi nel tuo zaino.");
            }
        }

        private class InternalTarget : Target
        {
            private BondingStone m_Stone;

            public InternalTarget(BondingStone stone) : base(3, false, TargetFlags.None)
            {
                m_Stone = stone;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Stone == null || from == null || !m_Stone.IsChildOf(from.Backpack))
                {
                    return;
                }

                if (targeted is Exp xp)
                {
                    if (xp.IsChildOf(from.Backpack) && !xp.Deleted)
                    {
                        int count = xp.Amount;
                        xp.Consume(count);
                        m_Stone.m_Time += count * 1.2;
                        m_Stone.InvalidateProperties();
                    }
                    else
                    {
                        from.SendLocalizedMessage(504296);//"Le fiale devono trovarsi nel tuo zaino");
                    }
                }
                else
                {
                    from.SendLocalizedMessage(504290);//Può essere ripristinata solo selezionando delle fiale
                }
            }
        }

        private double m_Time;

        [CommandProperty(AccessLevel.Developer)]
        public double RemainingTime { get => m_Time; set => m_Time = value; }

        public override int SocketsRequired => 1;
        public override int IconXOffset => 5;
        public override int IconYOffset => 20;

        public BondingStone(Serial serial) : base(serial)
        {
        }

        public override string OnIdentify(Mobile from)
        {
            if (from is BaseCreature)
            {
                if (XmlAttach.FindAttachment(from, typeof(XmlBond)) is XmlBond bond)
                {
                    m_Time = bond.Remaining.TotalHours;
                }
                else
                {
                    m_Time = 0;
                }
            }
            return string.Format(" {0}:{1:D2}", ((int)m_Time).ToString(), (int)((m_Time - ((int)m_Time)) * 60));

        }

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseCreature bc)
            {
                CommandLogging.WriteLine(from, "{0} Monta BondingStone ({1} Ore) su animale {2}", CommandLogging.Format(from), RemainingTime, bc);
                return (XmlAttach.FindAttachment(bc, typeof(XmlBond)) == null && XmlAttach.AttachTo((BaseCreature)target, new XmlBond(TimeSpan.FromHours(m_Time))));

            }

            return false;
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseCreature bc)
            {
                if (XmlAttach.FindAttachment(bc, typeof(XmlBond)) is XmlBond a && !a.Deleted)
                {
                    m_Time = a.Remaining.TotalHours;
                    //se "a" è uguale a NULL sai che botto che facciamo? XD
                    CommandLogging.WriteLine(from, "{0} Rimuove BondingStone ({1} Ore) da animale {2}", CommandLogging.Format(from), m_Time, target);
                    a.Delete();
                }
                else
                {
                    CommandLogging.WriteLine(from, "Errore in OnRecover per {0} che rimuove BondingStone ({1} Ore diventano -> 0) da animale {2}", CommandLogging.Format(from), m_Time, target);
                    m_Time = 0;
                }
                return true;
            }
            return false;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            if (target is BaseCreature)
            {
                if (m_Time >= 1)
                {
                    return true;

                }
                else if (m_Time == 0)
                {
                    from.SendLocalizedMessage(504258);//"Per poter applicare una bonding, questa deve avere ALMENO 1 ora residua, inoltre l'uso di bug è punibile con sanzioni pesanti (ed una stone a zero ore suppone che tu volessi shrinkare l'animale gratuitamente), il tuo tentativo è stato loggato!");
                    Console.WriteLine("Il pg {0} ha tentato di sfruttare un bug (accu {1})", from, (from.Account != null && from.Account.Username != null ? from.Account.Username : "NULL"));
                }
                else
                {
                    from.SendLocalizedMessage(504257);//"Per poter applicare una bonding, questa deve avere ALMENO 1 ora residua!");
                }
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

            writer.Write(4);
            writer.Write(m_Time);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            if (version < 4)
            {
                Name = null;
                if (version < 3)
                {
                    m_Time = reader.ReadDouble();
                    if (m_Time < 200.0)
                    {
                        m_Time = 200.0;
                    }
                }
                else
                {
                    m_Time = reader.ReadDouble();
                }
            }
            else
            {
                m_Time = reader.ReadDouble();
            }
        }
    }

    public class RegenerationStone : BaseSocketAugmentation, ISocketStone
    {
        //Stone della Rigenerazione
        //Creature: Rigenerazione HP Aumentata
        public override int LabelNumber => 504331;
        [Constructable]
        public RegenerationStone() : base(0x1779)
        {
            Hue = 25;
        }

        public override int SocketsRequired => 1;
        public override int IconXOffset => 5;
        public override int IconYOffset => 20;

        public RegenerationStone(Serial serial) : base(serial)
        {
        }

        public override bool OnAugment(Mobile from, object target)
        {
            if (target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;

                if (bc.HealDelay <= 0)
                {
                    bc.HealDelay = 10;
                }

                bc.HealEffect += 20;

                return true;
            }

            return false;
        }

        public override bool OnRecover(Mobile from, object target, int version)
        {
            if (target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;
                if (bc.HealDelay == 10)
                {
                    bc.HealDelay = 0;
                }

                bc.HealEffect -= 20;

                return true;
            }
            return false;
        }

        public override bool CanAugment(Mobile from, object target)
        {
            return target is BaseCreature;
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

    /*public class GlimmeringGranite : BaseSocketAugmentation
	{

		[Constructable]
		public GlimmeringGranite() : base(0x1779)
		{
			Name = "Glimmering Granite";
			Hue = 15;
		}

		public override int IconXOffset { get { return 5;} }

		public override int IconYOffset { get { return 20;} }

		public GlimmeringGranite( Serial serial ) : base( serial )
		{
		}

		public override string OnIdentify(Mobile from)
		{

			return "Armor, Jewelry: +5 Alchemy";
		}

		public override bool OnAugment(Mobile from, object target)
		{
			if(target is BaseArmor)
			{
				BaseArmor a = target as BaseArmor;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Alchemy, 5.0 );
						break;
					}
				}
				return true;
			} else
			if(target is BaseJewel)
			{
				BaseJewel a = target as BaseJewel;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Alchemy, 5.0 );
						break;
					}
				}
				return true;
			}

			return false;
		}


		public override bool CanAugment(Mobile from, object target)
		{
			if(target is BaseArmor || target is BaseJewel)
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

	public class GlimmeringClay : BaseSocketAugmentation
	{

		[Constructable]
		public GlimmeringClay() : base(0x1779)
		{
			Name = "Glimmering Clay";
			Hue = 25;
		}

		public override int IconXOffset { get { return 5;} }

		public override int IconYOffset { get { return 20;} }

		public GlimmeringClay( Serial serial ) : base( serial )
		{
		}

		public override string OnIdentify(Mobile from)
		{

			return "Armor, Jewelry: +5 Anatomy";
		}

		public override bool OnAugment(Mobile from, object target)
		{
			if(target is BaseArmor)
			{
				BaseArmor a = target as BaseArmor;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Anatomy, 5.0 );
						break;
					}
				}
				return true;
			} else
			if(target is BaseJewel)
			{
				BaseJewel a = target as BaseJewel;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Anatomy, 5.0 );
						break;
					}
				}
				return true;
			}

			return false;
		}


		public override bool CanAugment(Mobile from, object target)
		{
			if(target is BaseArmor || target is BaseJewel)
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

	public class GlimmeringGypsum : BaseSocketAugmentation
	{

		[Constructable]
		public GlimmeringGypsum() : base(0x1779)
		{
			Name = "Glimmering Gypsum";
			Hue = 45;
		}

		public override int IconXOffset { get { return 5;} }

		public override int IconYOffset { get { return 20;} }

		public GlimmeringGypsum( Serial serial ) : base( serial )
		{
		}

		public override string OnIdentify(Mobile from)
		{

			return "Armor, Jewelry: +5 ItemID";
		}

		public override bool OnAugment(Mobile from, object target)
		{
			if(target is BaseArmor)
			{
				BaseArmor a = target as BaseArmor;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.ItemID, 5.0 );
						break;
					}
				}
				return true;
			} else
			if(target is BaseJewel)
			{
				BaseJewel a = target as BaseJewel;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.ItemID, 5.0 );
						break;
					}
				}
				return true;
			}

			return false;
		}


		public override bool CanAugment(Mobile from, object target)
		{
			if(target is BaseArmor || target is BaseJewel)
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
	
	public class GlimmeringIronOre : BaseSocketAugmentation
	{

		[Constructable]
		public GlimmeringIronOre() : base(0x1779)
		{
			Name = "Glimmering Iron Ore";
			Hue = 55;
		}

		public override int IconXOffset { get { return 5;} }

		public override int IconYOffset { get { return 20;} }

		public GlimmeringIronOre( Serial serial ) : base( serial )
		{
		}

		public override string OnIdentify(Mobile from)
		{

			return "Armor, Jewelry: +5 ArmsLore";
		}

		public override bool OnAugment(Mobile from, object target)
		{
			if(target is BaseArmor)
			{
				BaseArmor a = target as BaseArmor;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.ArmsLore, 5.0 );
						break;
					}
				}
				return true;
			} else
			if(target is BaseJewel)
			{
				BaseJewel a = target as BaseJewel;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.ArmsLore, 5.0 );
						break;
					}
				}
				return true;
			}

			return false;
		}


		public override bool CanAugment(Mobile from, object target)
		{
			if(target is BaseArmor || target is BaseJewel)
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
	
	 public class GlimmeringOnyx : BaseSocketAugmentation
	{

		[Constructable]
		public GlimmeringOnyx() : base(0x1779)
		{
			Name = "Glimmering Onyx";
			Hue = 2;
		}

		public override int IconXOffset { get { return 5;} }

		public override int IconYOffset { get { return 20;} }

		public GlimmeringOnyx( Serial serial ) : base( serial )
		{
		}

		public override string OnIdentify(Mobile from)
		{

			return "Armor, Jewelry: +5 Parry";
		}

		public override bool OnAugment(Mobile from, object target)
		{
			if(target is BaseArmor)
			{
				BaseArmor a = target as BaseArmor;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Parry, 5.0 );
						break;
					}
				}
				return true;
			} else
			if(target is BaseJewel)
			{
				BaseJewel a = target as BaseJewel;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Parry, 5.0 );
						break;
					}
				}
				return true;
			}

			return false;
		}


		public override bool CanAugment(Mobile from, object target)
		{
			if(target is BaseArmor || target is BaseJewel)
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
	
	public class GlimmeringMarble : BaseSocketAugmentation
	{

		[Constructable]
		public GlimmeringMarble() : base(0x1779)
		{
			Name = "Glimmering Marble";
			Hue = 85;
		}

		public override int IconXOffset { get { return 5;} }

		public override int IconYOffset { get { return 20;} }

		public GlimmeringMarble( Serial serial ) : base( serial )
		{
		}

		public override string OnIdentify(Mobile from)
		{

			return "Armor, Jewelry: +5 Blacksmith";
		}

		public override bool OnAugment(Mobile from, object target)
		{
			if(target is BaseArmor)
			{
				BaseArmor a = target as BaseArmor;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Blacksmith, 5.0 );
						break;
					}
				}
				return true;
			} else
			if(target is BaseJewel)
			{
				BaseJewel a = target as BaseJewel;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Blacksmith, 5.0 );
						break;
					}
				}
				return true;
			}

			return false;
		}


		public override bool CanAugment(Mobile from, object target)
		{
			if(target is BaseArmor || target is BaseJewel)
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

	public class GlimmeringPetrifiedWood : BaseSocketAugmentation
	{

		[Constructable]
		public GlimmeringPetrifiedWood() : base(0x1779)
		{
			Name = "Glimmering Petrified wood";
			Hue = 85;
		}

		public override int IconXOffset { get { return 5;} }

		public override int IconYOffset { get { return 20;} }

		public GlimmeringPetrifiedWood( Serial serial ) : base( serial )
		{
		}

		public override string OnIdentify(Mobile from)
		{

			return "Armor, Jewelry: +5 Fletching";
		}

		public override bool OnAugment(Mobile from, object target)
		{
			if(target is BaseArmor)
			{
				BaseArmor a = target as BaseArmor;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Fletching, 5.0 );
						break;
					}
				}
				return true;
			} else
			if(target is BaseJewel)
			{
				BaseJewel a = target as BaseJewel;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Fletching, 5.0 );
						break;
					}
				}
				return true;
			}

			return false;
		}


		public override bool CanAugment(Mobile from, object target)
		{
			if(target is BaseArmor || target is BaseJewel)
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
	
	public class GlimmeringLimestone : BaseSocketAugmentation
	{

		[Constructable]
		public GlimmeringLimestone() : base(0x1779)
		{
			Name = "Glimmering Limestone";
			Hue = 85;
		}

		public override int IconXOffset { get { return 5;} }

		public override int IconYOffset { get { return 20;} }

		public GlimmeringLimestone( Serial serial ) : base( serial )
		{
		}

		public override string OnIdentify(Mobile from)
		{

			return "Armor, Jewelry: +5 Peacemaking";
		}

		public override bool OnAugment(Mobile from, object target)
		{
			if(target is BaseArmor)
			{
				BaseArmor a = target as BaseArmor;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Peacemaking, 5.0 );
						break;
					}
				}
				return true;
			} else
			if(target is BaseJewel)
			{
				BaseJewel a = target as BaseJewel;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Peacemaking, 5.0 );
						break;
					}
				}
				return true;
			}

			return false;
		}


		public override bool CanAugment(Mobile from, object target)
		{
			if(target is BaseArmor || target is BaseJewel)
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
	
	public class GlimmeringBloodrock : BaseSocketAugmentation
	{

		[Constructable]
		public GlimmeringBloodrock() : base(0x1779)
		{
			Name = "Glimmering Bloodrock";
			Hue = 85;
		}

		public override int IconXOffset { get { return 5;} }

		public override int IconYOffset { get { return 20;} }

		public GlimmeringBloodrock( Serial serial ) : base( serial )
		{
		}

		public override string OnIdentify(Mobile from)
		{

			return "Armor, Jewelry: +5 Healing";
		}

		public override bool OnAugment(Mobile from, object target)
		{
			if(target is BaseArmor)
			{
				BaseArmor a = target as BaseArmor;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Healing, 5.0 );
						break;
					}
				}
				return true;
			} else
			if(target is BaseJewel)
			{
				BaseJewel a = target as BaseJewel;
				// find a free slot
				for(int i =0; i < 5; ++i)
				{
					if(a.SkillBonuses.GetBonus(i) == 0)
					{
						a.SkillBonuses.SetValues( i, SkillName.Healing, 5.0 );
						break;
					}
				}
				return true;
			}

			return false;
		}


		public override bool CanAugment(Mobile from, object target)
		{
			if(target is BaseArmor || target is BaseJewel)
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
	*/
}
