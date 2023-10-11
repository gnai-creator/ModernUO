/*using System;
using System.Collections.Generic;

using Server;
using Server.Mobiles;
using Server.Regions;

namespace Server.Items
{
	public class XmlGenericTrap : Item
	{
		private bool m_Active;
		private int m_SwitchSound = 939;
		private string m_OnTriggerProperty;

		[Constructable]
		public SimpleTileTrap(int normalID, int activeID) : this()
		{
			ItemID=normalID;
			NormalItemID=normalID;
			ActiveItemID=activeID;
		}

		[Constructable]
		public SimpleTileTrap() : base( 7107 )
		{
			Name = "A tile trap";
			Movable = false;
		}

		public SimpleTileTrap( Serial serial ) : base( serial )
		{
		}

		public void SetPadStatic(bool activated, Mobile m)
		{
			// BE WARNED, "m" CAN BE NULL!!
			if(activated && !m_Active) //  we can activate it only if it is unactivated
			{
				if(m!=null && !m.Alive)
					return;
				m_Active=true;
				OnEnter( m );
				if(ActiveItemID!=0)
					ItemID=ActiveItemID;
			}
			else if(!activated && m_Active) // we can release it only if it was pressed before
			{
				bool ok=true;
				if(AllowItemPressure && m_DroppedOnThis.Count>0)
				{
					ok=false;
				}
				else foreach(Mobile mob in Map.GetMobilesInRange(this.Location, 0))
				{
					if(mob.Alive)
					{
						ok=false;
						break;
					}
				}
				if(ok)
				{
					m_Active=false;
					OnExit(m);
					if(NormalItemID!=0)
						ItemID=NormalItemID;
				}
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int SwitchSound
		{
			get{ return m_SwitchSound; }
			set 
			{
				m_SwitchSound = value;
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int ActiveItemID { get; set; }
		[CommandProperty( AccessLevel.GameMaster )]
		public int NormalItemID { get; set; }

		[CommandProperty( AccessLevel.GameMaster )]
		public string OnTriggerProperty
		{
			get{ return m_OnTriggerProperty; }
			set{ m_OnTriggerProperty = value; InvalidateProperties(); }
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 3 ); // version 

			// ver 3
			writer.Write( m_Active );
			writer.WriteItemList( m_DroppedOnThis );
			// ver 2
			writer.Write( m_OnTriggerProperty );
			// ver 1
			writer.Write( this.ActiveItemID );
			writer.Write( this.NormalItemID );
			// ver 0
			writer.Write( this.m_SwitchSound );
			writer.Write( this.m_TargetItem0 );
			writer.Write( this.m_TargetProperty0 );
			writer.Write( this.m_TargetItem1 );
			writer.Write( this.m_TargetProperty1 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
			switch ( version )
			{
				case 3:
				{
					m_Active = reader.ReadBool();
					m_DroppedOnThis=reader.ReadStrongItemList();
					goto case 2;
				}
				case 2:
				{
					m_OnTriggerProperty=reader.ReadString();
					goto case 1;
				}
				case 1:
				{
					ActiveItemID=reader.ReadInt();
					NormalItemID=reader.ReadInt();
					goto case 0;
				}
				case 0:
				{
					this.m_SwitchSound = reader.ReadInt();
					this.m_TargetItem0 = reader.ReadItem();
					this.m_TargetProperty0 = reader.ReadString();
					this.m_TargetItem1 = reader.ReadItem();
					this.m_TargetProperty1 = reader.ReadString();
				}
				break;
			}
			Timer.DelayCall(
				delegate
				{
					if(this!=null && !this.Deleted)
					{
						m_Region = new TileTrapRegion(this, Location, Map);
						m_Region.Register();
					}
				});
		}

		public bool CheckRange( Point3D loc, Point3D oldLoc, int range )
		{
			return CheckRange( loc, range ) && !CheckRange( oldLoc, range );
		}

		public bool CheckRange( Point3D loc, int range )
		{
			return ( (this.Z + 8) >= loc.Z && (loc.Z + 16) > this.Z )
				&& Utility.InRange( GetWorldLocation(), loc, range );
		}

		/*public override bool HandlesOnMovement{ get{ return true; } } // Tell the core that we implement OnMovement

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			base.OnMovement( m, oldLocation );

			if ( m.Location == oldLocation )
				return;

			if( ( m.Player && m.AccessLevel == AccessLevel.Player ) )
			{
				if ( CheckRange( m.Location, oldLocation, 0 ) )
				{
					SetPadStatic(true, m);
				}
				else if ( oldLocation == this.Location )
				{
					SetPadStatic(false, m);
				}
			}
		}

		private void OnEnter( Mobile m )
		{
			string status_str;
			if(m_OnTriggerProperty!=null)
			{
				XmlSpawner.SpawnObject TheSpawn = new XmlSpawner.SpawnObject(null, 0);
				
				string substitutedtypeName = BaseXmlSpawner.ApplySubstitution(null, null, null, m_OnTriggerProperty);
				string typeName = BaseXmlSpawner.ParseObjectType(substitutedtypeName);
				
				if (BaseXmlSpawner.IsTypeOrItemKeyword(typeName))
				{
					BaseXmlSpawner.SpawnTypeKeyword(null, TheSpawn, typeName, substitutedtypeName, true, m, m.Location, Map.Internal, out status_str);
				}
			}
			this.PlaySound(SwitchSound);
			BaseXmlSpawner.ApplyObjectStringProperties(null, m_TargetProperty1, m_TargetItem1, m, this, out status_str);
		}

		private void OnExit( Mobile m )
		{
			string status_str;
			BaseXmlSpawner.ApplyObjectStringProperties(null, m_TargetProperty0, m_TargetItem0, m, this, out status_str);
		}
	}
}*/