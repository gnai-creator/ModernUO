/*using System;
using Server;
using Server.Items;
using Server.Network;
using Server.Mobiles;
using System.Collections;

namespace Server.Engines.XmlSpawner2
{
	public class XmlSocketable : XmlAttachment
	{
		private int m_MaxSockets;
		private SkillName m_RequiredSkill = XmlSockets.DefaultSocketSkill;
		private double m_MinSkillLevel = XmlSockets.DefaultSocketDifficulty;
		private SkillName m_RequiredSkill2 = XmlSockets.DefaultSocketSkill;
		private double m_MinSkillLevel2 = 0;                // second skill requirement turned off by default
		private Type m_RequiredResource = XmlSockets.DefaultSocketResource;
		private int m_ResourceQuantity = XmlSockets.DefaultSocketResourceQuantity;


		[CommandProperty( AccessLevel.GameMaster )]
		public int MaxSockets { get{ return m_MaxSockets; } set { m_MaxSockets = value; InvalidateParentProperties(); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public SkillName RequiredSkill { get{ return m_RequiredSkill; } set { m_RequiredSkill = value; InvalidateParentProperties();} }

		[CommandProperty( AccessLevel.GameMaster )]
		public double MinSkillLevel { get{ return m_MinSkillLevel; } set { m_MinSkillLevel = value; InvalidateParentProperties();} }
		
		[CommandProperty( AccessLevel.GameMaster )]
		public SkillName RequiredSkill2 { get{ return m_RequiredSkill2; } set { m_RequiredSkill2 = value; InvalidateParentProperties();} }

		[CommandProperty( AccessLevel.GameMaster )]
		public double MinSkillLevel2 { get{ return m_MinSkillLevel2; } set { m_MinSkillLevel2 = value; InvalidateParentProperties();} }
		
		[CommandProperty( AccessLevel.GameMaster )]
		public int ResourceQuantity { get{ return m_ResourceQuantity; } set { m_ResourceQuantity = value; InvalidateParentProperties();} }
		
		[CommandProperty( AccessLevel.GameMaster )]
		public Type RequiredResource { get{ return m_RequiredResource; } set { m_RequiredResource = value; InvalidateParentProperties();} }

		// These are the various ways in which the message attachment can be constructed.  
		// These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
		// Other overloads could be defined to handle other types of arguments
	   
		// a serial constructor is REQUIRED
		public XmlSocketable(ASerial serial) : base(serial)
		{
		}

		[Attachable]
		public XmlSocketable(int maxsockets, string skillname, double minskilllevel, string skillname2, double minskilllevel2, string resource, int quantity)
		{
			MaxSockets = maxsockets;

			RequiredResource = SpawnerType.GetType(resource);

			SkillName sn;
			if(Enum.TryParse(skillname, true, out sn))
				RequiredSkill=sn;

			MinSkillLevel = minskilllevel;

			if(Enum.TryParse(skillname2, true, out sn))
				RequiredSkill2=sn;

			MinSkillLevel2 = minskilllevel2;

			ResourceQuantity = quantity;
			InvalidateParentProperties();
		}
		
		[Attachable]
		public XmlSocketable(int maxsockets, string skillname, double minskilllevel, string resource, int quantity)
		{
			MaxSockets = maxsockets;

			RequiredResource = SpawnerType.GetType(resource);

			SkillName sn;
			if(Enum.TryParse(skillname, true, out sn))
				RequiredSkill=sn;

			MinSkillLevel = minskilllevel;
			ResourceQuantity = quantity;
			InvalidateParentProperties();
		}

		[Attachable]
		public XmlSocketable(int maxsockets)
		{
			MaxSockets = maxsockets;
			InvalidateParentProperties();
		}
		
		[Attachable]
		public XmlSocketable()
		{
			MaxSockets = -1;
			InvalidateParentProperties();
		}
		
		public XmlSocketable(int maxsockets, SkillName skillname, double minskilllevel, Type resource, int quantity)
		{
			MaxSockets = maxsockets;
			RequiredResource = resource;
			ResourceQuantity = quantity;
			RequiredSkill = skillname;
			MinSkillLevel = minskilllevel;
			InvalidateParentProperties();
		}
		
		public XmlSocketable(int maxsockets, SkillName skillname, double minskilllevel, SkillName skillname2, double minskilllevel2, Type resource, int quantity)
		{
			MaxSockets = maxsockets;
			RequiredResource = resource;
			ResourceQuantity = quantity;
			RequiredSkill = skillname;
			MinSkillLevel = minskilllevel;
			RequiredSkill2 = skillname2;
			MinSkillLevel2 = minskilllevel2;
			InvalidateParentProperties();
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize(writer);

			writer.Write( (int) 1 );
			// version 1
			writer.Write((int)m_RequiredSkill2);
			writer.Write(m_MinSkillLevel2);
			// version 0
			writer.Write(m_MaxSockets);
			if(m_RequiredResource != null)
				writer.Write(m_RequiredResource.ToString());
			else
				writer.Write((string) null);
			writer.Write(m_ResourceQuantity);
			writer.Write((int)m_RequiredSkill);
			writer.Write(m_MinSkillLevel);

		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();
			switch(version)
			{
				case 1:
					// version 1
					m_RequiredSkill2 = (SkillName)reader.ReadInt();
					m_MinSkillLevel2 = reader.ReadDouble();
					goto case 0;
				case 0:
					// version 0
					m_MaxSockets = reader.ReadInt();
					string resourcetype = reader.ReadString();
					try
					{
						m_RequiredResource = Type.GetType(resourcetype);
					}catch{}
					m_ResourceQuantity = reader.ReadInt();
					m_RequiredSkill = (SkillName)reader.ReadInt();
					m_MinSkillLevel = reader.ReadDouble();
					break;
			}
		}
		
		public override void OnAttach()
		{
			base.OnAttach();
		
			InvalidateParentProperties();
		}

		public override LogEntry OnIdentify(Mobile from)
		{
			string msg = null;
			int nSockets = 0;

			// first see if the target has any existing sockets

			XmlSockets s = XmlAttach.FindAttachment(AttachedTo,typeof(XmlSockets)) as XmlSockets;

			if(s != null)
			{
				// find out how many sockets it has
				nSockets = s.NSockets;
			}

			if(nSockets > MaxSockets) m_MaxSockets = nSockets;

			if(MaxSockets == nSockets)
			{
				// already full so no chance of socketing
				return new LogEntry(LocalizerDef(), "#1005187");//Non puoi aggiungere ulteriori potenziamenti.
			}

			if(MaxSockets > 0)
			{
				msg = String.Format("Sono permessi massimo {0} slot.",MaxSockets);
			}
			else if(MaxSockets == 0)
			{
				return new LogEntry(LocalizerDef(), "#1005188");
			}
			else if(MaxSockets == -1)
			{
				msg = String.Format("Puoi potenziare l'oggetto.");
			}
				
			// compute difficulty based upon existing sockets

			if(from != null)
			{
				if(MinSkillLevel > 0)
					msg += String.Format("\nRichiesto {0} skill in {1} per potenziare.", MinSkillLevel, RequiredSkill);

				if(MinSkillLevel2 > 0)
					msg += String.Format("\ned anche {0} skill in {1}.", MinSkillLevel2, RequiredSkill2);

				if(RequiredResource != null && ResourceQuantity > 0)
					msg += String.Format("\nIl processo richiede {0} {1}.", ResourceQuantity, RequiredResource.Name);

				int success = XmlSockets.ComputeSuccessChance(from, nSockets, MinSkillLevel, RequiredSkill, MinSkillLevel2, RequiredSkill2);

				msg += String.Format("\nChance di potenziamento {0}%\n",success);
			}

			return new LogEntry(LocalizerDef(), msg);
		}
	}
}
*/