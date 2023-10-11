using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Engines.XmlSpawner2
{
    public class XmlBlessItem : XmlAttachment
    {
        /*public static void Configure()
		{
			EventSink.WorldLoad += new WorldLoadEventHandler(Load);
		}*/
        public const int c_BlessMin = 40;
        public static bool NotRepairable(Item it, bool willbless)
        {
            if (!willbless)
            {
                if (it is BaseWeapon)
                {
                    return NotRepairable((BaseWeapon)it);
                }
                else if (it is BaseClothing)
                {
                    return NotRepairable((BaseClothing)it);
                }
                else if (it is BaseArmor)
                {
                    return NotRepairable((BaseArmor)it);
                }
                else if (it is BaseJewel)
                {
                    return NotRepairable((BaseJewel)it);
                }
                else
                    return false;
            }
            else
            {
                return false;
            }
        }
        public static bool NotRepairable(BaseWeapon bw)
        {
            return (bw.LootType != LootType.Regular && (bw.Resource != CraftResource.Iron || bw.DamageLevel != WeaponDamageLevel.Regular || bw.Attributes.HasBonuses || bw.SkillBonuses.HasBonuses || bw.AosElementDamages.HasBonuses));
        }
        public static bool NotRepairable(BaseClothing bc)
        {
            return (bc.LootType != LootType.Regular && ((bc.Resource != CraftResource.None && bc.Resource != CraftResource.RegularLeather && bc.Resource != CraftResource.LightFur && bc.Resource != CraftResource.DarkFur) || bc.Attributes.HasBonuses || bc.ClothingAttributes.HasBonuses || bc.SkillBonuses.HasBonuses || bc.Resistances.HasBonuses));
        }
        public static bool NotRepairable(BaseArmor ba)
        {
            return (ba.LootType != LootType.Regular && ((ba.Resource != CraftResource.Iron && ba.Resource != CraftResource.RegularLeather && (ba.MaterialType != ArmorMaterialType.Studded || ba.Layer == Layer.Gloves)) || ba.ProtectionLevel != ArmorProtectionLevel.Regular || ba.Attributes.HasBonuses || ba.ArmorAttributes.HasBonuses || ba.SkillBonuses.HasBonuses));
        }
        public static bool NotRepairable(BaseJewel bj)
        {
            return (bj.LootType != LootType.Regular && (bj.Resource != CraftResource.Iron || bj.Attributes.HasBonuses || bj.SkillBonuses.HasBonuses || bj.Resistances.HasBonuses));
        }

        public static int AugmentMaxHitpoints(Item it, bool willbless = false)
        {
            return c_BlessMin * (it.Consecrated ? 2 : 1) * (NotRepairable(it, willbless) ? 1 : 10);
        }

        [CommandProperty(AccessLevel.GameMaster, true)]
        public string BlesserAccount { get; private set; }

        // a serial constructor is REQUIRED
        public XmlBlessItem(ASerial serial) : base(serial)
        {
        }

        public XmlBlessItem(Mobile blesser)
        {
            if (blesser != null && blesser.Account != null)
            {
                BlesserAccount = blesser.Account.Username;
            }
            else
            {
                Delete();
            }
        }

        private XmlBlessItem()
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(4);
            // version 0
            writer.Write(BlesserAccount);
        }

        //public static List<XmlBlessItem> s_List = new List<XmlBlessItem>();
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            // version 0
            BlesserAccount = reader.ReadString();
            /*if (version < 4)
            {
                s_List.Add(this);
            }*/
        }
        /*public static void Load()
		{
			Timer.DelayCall(TimeSpan.FromSeconds(30), PostLoad);
		}
		public static void PostLoad()
		{
			int cnt=0;
			foreach(XmlBlessItem s in s_List)
			{
				Item it=s.AttachedTo as Item;
				if(it!=null)
				{
					if(it is IWearableDurability)
					{
						IWearableDurability iwd = (IWearableDurability)s.AttachedTo;
						iwd.HitPoints=iwd.MaxHitPoints=AugmentMaxHitpoints(it);
						cnt++;
					}
				}
			}
			Console.WriteLine("Completato con {0} oggetti ripristinati!",cnt);
		}*/
        /*public static void Recupero_OnCommand( CommandEventArgs e ) 
		{
			using( StreamWriter op = new StreamWriter( "attachments.txt", true ) )
			{
				foreach(XmlBlessItem xb in s_List)
				{
					if(xb.AttachedTo is Item)
					{
						Item it=(Item)xb.AttachedTo;
						op.WriteLine("{0},{1}",it.Serial.Value,xb.BlesserAccount);
					}
				}
			}
		}
		public static void RecuperoLoad_OnCommand( CommandEventArgs e ) 
		{
			int cnt=0;
			using( StreamReader op = new StreamReader( "attachments.txt" ) )
			{
				string str=op.ReadLine();
				while(str!=null)
				{
					string[] args=str.Split(',');
					if(args.Length>1)
					{
						int i;
						if(int.TryParse(args[0], out i))
						{
							Item it=World.FindItem(i);
							if(it!=null)
							{
								if(!string.IsNullOrEmpty(args[1]))
									XmlAttach.AttachTo(it, new XmlBlessItem(){BlesserAccount=args[1]});
								else
									XmlAttach.AttachTo(it, new XmlBlessItem(){BlesserAccount=""});
								if(it is IWearableDurability)
								{
									IWearableDurability iwd = (IWearableDurability)it;
									iwd.HitPoints=iwd.MaxHitPoints=AugmentMaxHitpoints(it);
									cnt++;
								}
							}
						}
					}
					str=op.ReadLine();
				}
			}
			Console.WriteLine("Recuperati {0} item con xmlblessitem", cnt);
		}*/

        public override LogEntry OnIdentify(Mobile from)
        {
            if (from == null || from.AccessLevel == AccessLevel.Player)
            {
                return null;
            }

            return new LogEntry(1005169, BlesserAccount);
        }

        public override void OnDelete()
        {
            // remove the mod
            if (AttachedTo is Item it)
            {
                it.LootType = LootType.Regular;
            }

            base.OnDelete();
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // apply the mod
            if (AttachedTo is Item it)
            {
                it.LootType = LootType.Blessed;
                if (it is IWearableDurability iwd)
                {
                    int max = AugmentMaxHitpoints(it);
                    iwd.MaxHitPoints = Math.Min(iwd.MaxHitPoints, max);
                    iwd.HitPoints = Math.Min(Math.Min(iwd.MaxHitPoints, iwd.HitPoints), max);
                }
            }
            else
            {
                Delete();
            }
        }
    }
}