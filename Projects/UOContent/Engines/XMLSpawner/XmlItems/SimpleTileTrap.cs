using Server.Mobiles;
using Server.Regions;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class SimpleTileTrap : Item
    {
        private bool m_Pressed;
        private int m_PressSwitchSound = 939;
        private int m_UnPressSwitchSound = -1;
        private Item m_TargetItem0 = null;
        private string m_TargetProperty0 = null;
        private Item m_TargetItem1 = null;
        private string m_TargetProperty1 = null;
        private string m_OnTriggerProperty = null;
        private List<Item> m_DroppedOnThis = new List<Item>();
        private TileTrapRegion m_Region;
        public List<Item> DroppedOnThis
        {
            get => m_DroppedOnThis;
            set => m_DroppedOnThis = value;
        }

        [Constructable]
        public SimpleTileTrap(int normalID, int pressedID) : this()
        {
            ItemID = normalID;
            NormalItemID = normalID;
            PressedItemID = pressedID;
        }

        [Constructable]
        public SimpleTileTrap() : base(7107)
        {
            Name = "A tile trap";
            Movable = false;
            AllowItemPressure = false;
            Timer.DelayCall(TimeSpan.FromMilliseconds(100), PostPlacement);
        }

        private void PostPlacement()
        {
            if (!Deleted && m_Region == null)
            {
                Region reg = Region.Find(Location, Map);
                m_Region = new TileTrapRegion(this, Location, Map, reg);
                m_Region.Register();
            }
        }

        public SimpleTileTrap(Serial serial) : base(serial)
        {
        }

        public void SetPadStatic(bool pressed, Mobile m)
        {
            // BE WARNED, "m" CAN BE NULL!!
            if (pressed && !m_Pressed) //  we can press it only if it is released
            {
                if (m != null && !m.Alive)
                {
                    return;
                }

                m_Pressed = true;
                OnEnter(m);
                if (PressedItemID != 0)
                {
                    ItemID = PressedItemID;
                }
            }
            else if (!pressed && m_Pressed) // we can release it only if it was pressed before
            {
                bool ok = true;
                if (AllowItemPressure && m_DroppedOnThis.Count > 0)
                {
                    ok = false;
                }
                else
                {
                    foreach (Mobile mob in Map.GetMobilesInRange(Location, 0))
                    {
                        if (mob.Alive)
                        {
                            ok = false;
                            break;
                        }
                    }
                }

                if (ok)
                {
                    m_Pressed = false;
                    OnExit(m);
                    if (NormalItemID != 0)
                    {
                        ItemID = NormalItemID;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PressSwitchSound
        {
            get => m_PressSwitchSound;
            set
            {
                m_PressSwitchSound = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UnPressSwitchSound
        {
            get => m_UnPressSwitchSound;
            set
            {
                m_UnPressSwitchSound = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PressedItemID { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public int NormalItemID { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool AllowItemPressure { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Target0Item
        {
            get => m_TargetItem0;
            set { m_TargetItem0 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Target0Property
        {
            get => m_TargetProperty0;
            set { m_TargetProperty0 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Target0ItemName
        {
            get
            {
                if (m_TargetItem0 != null && !m_TargetItem0.Deleted)
                {
                    return m_TargetItem0.Name;
                }
                else
                {
                    return null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Target1Item
        {
            get => m_TargetItem1;
            set { m_TargetItem1 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Target1Property
        {
            get => m_TargetProperty1;
            set { m_TargetProperty1 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Target1ItemName
        {
            get
            {
                if (m_TargetItem1 != null && !m_TargetItem1.Deleted)
                {
                    return m_TargetItem1.Name;
                }
                else
                {
                    return null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string OnTriggerProperty
        {
            get => m_OnTriggerProperty;
            set { m_OnTriggerProperty = value; InvalidateProperties(); }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(3); // version 

            // ver 3
            writer.Write(m_Pressed);
            writer.WriteItemList(m_DroppedOnThis);
            // ver 2
            writer.Write(m_OnTriggerProperty);
            // ver 1
            writer.Write(PressedItemID);
            writer.Write(NormalItemID);
            // ver 0
            writer.Write(m_PressSwitchSound);
            writer.Write(m_TargetItem0);
            writer.Write(m_TargetProperty0);
            writer.Write(m_TargetItem1);
            writer.Write(m_TargetProperty1);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 3:
                {
                    m_Pressed = reader.ReadBool();
                    m_DroppedOnThis = reader.ReadStrongItemList();
                    goto case 2;
                }
                case 2:
                {
                    m_OnTriggerProperty = reader.ReadString();
                    goto case 1;
                }
                case 1:
                {
                    PressedItemID = reader.ReadInt();
                    NormalItemID = reader.ReadInt();
                    goto case 0;
                }
                case 0:
                {
                    m_PressSwitchSound = reader.ReadInt();
                    m_TargetItem0 = reader.ReadItem();
                    m_TargetProperty0 = reader.ReadString();
                    m_TargetItem1 = reader.ReadItem();
                    m_TargetProperty1 = reader.ReadString();
                }
                break;
            }
            Timer.DelayCall(TimeSpan.FromMilliseconds(100), PostPlacement);
        }

        public bool CheckRange(Point3D loc, Point3D oldLoc, int range)
        {
            return CheckRange(loc, range) && !CheckRange(oldLoc, range);
        }

        public bool CheckRange(Point3D loc, int range)
        {
            return ((Z + 8) >= loc.Z && (loc.Z + 16) > Z)
                && Utility.InRange(GetWorldLocation(), loc, range);
        }

        public override void OnDelete()
        {
            if (m_Region != null)
            {
                m_Region.Unregister();
                m_Region = null;
            }
            base.OnDelete();
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
		}*/

        public override void OnRemoved(IEntity parent)
        {
            base.OnRemoved(parent);
            if (m_Region != null)
            {
                m_Region.Unregister();
                m_Region = null;
            }
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);
            if (m_Region != null)
            {
                m_Region.Unregister();
                m_Region = null;
            }
            Timer.DelayCall(TimeSpan.FromMilliseconds(100), PostPlacement);
        }

        public override void OnMapChange()
        {
            base.OnMapChange();
            if (m_Region != null)
            {
                m_Region.Unregister();
                m_Region = null;
            }
            Timer.DelayCall(TimeSpan.FromMilliseconds(100), PostPlacement);
        }

        private void OnEnter(Mobile m)
        {
            if (m_OnTriggerProperty != null)
            {
                XmlSpawner.SpawnObject TheSpawn = new XmlSpawner.SpawnObject(null, 0);

                string substitutedtypeName = BaseXmlSpawner.ApplySubstitution(null, null, null, m_OnTriggerProperty);
                string typeName = BaseXmlSpawner.ParseObjectType(substitutedtypeName);

                if (BaseXmlSpawner.IsTypeOrItemKeyword(typeName))
                {
                    BaseXmlSpawner.SpawnTypeKeyword(this, TheSpawn, typeName, substitutedtypeName, true, m, m.Location, Map.Internal, out _);
                }
            }
            PlaySound(PressSwitchSound);
            BaseXmlSpawner.ApplyObjectStringProperties(null, m_TargetProperty1, m_TargetItem1, m, this, out _);
        }

        private void OnExit(Mobile m)
        {
            BaseXmlSpawner.ApplyObjectStringProperties(null, m_TargetProperty0, m_TargetItem0, m, this, out _);
            PlaySound(UnPressSwitchSound);
        }
    }

    public class TileTrapRegion : BaseRegion
    {
        private SimpleTileTrap m_TileTrap;
        public TileTrapRegion(SimpleTileTrap tiletrap, Point3D p, Map map, Region parent) : base(null, map, parent, new Rectangle3D(p.X, p.Y, p.Z, 1, 1, 10))
        {
            m_TileTrap = tiletrap;
        }

        public override void OnEnter(Mobile m)
        {
            if ((m.Player && m.AccessLevel == AccessLevel.Player))
            {
                m_TileTrap.SetPadStatic(true, m);
            }
        }

        public override void OnExit(Mobile m)
        {
            if ((m.Player && m.AccessLevel == AccessLevel.Player))
            {
                m_TileTrap.SetPadStatic(false, m);
            }
        }

        public override bool OnDecay(Item item)
        {
            if (item.Parent == null && !m_TileTrap.DroppedOnThis.Contains(item))
            {
                m_TileTrap.DroppedOnThis.Add(item);
                m_TileTrap.SetPadStatic(true, null);
            }

            return base.OnDecay(item);
        }

        public override void OnItemRemoved(Item item)
        {
            m_TileTrap.DroppedOnThis.Remove(item);
            if (m_TileTrap.DroppedOnThis.Count < 1)
            {
                m_TileTrap.SetPadStatic(false, null);
            }

            base.OnItemRemoved(item);
        }

        public override void OnLiftItem(Item oldItem, Item newItem)
        {
            m_TileTrap.DroppedOnThis.Remove(oldItem);
            m_TileTrap.DroppedOnThis.Add(newItem);
            base.OnLiftItem(oldItem, newItem);
        }

        public override void OnItemDeleted(Item item)
        {
            m_TileTrap.DroppedOnThis.Remove(item);
            if (m_TileTrap.DroppedOnThis.Count < 1)
            {
                m_TileTrap.SetPadStatic(false, null);
            }

            base.OnItemDeleted(item);
        }

        public override void OnDeath(Mobile m)
        {
            base.OnDeath(m);
            m_TileTrap.SetPadStatic(false, m);
        }

        public override void OnAfterResurrect(Mobile m)
        {
            base.OnAfterResurrect(m);
            m_TileTrap.SetPadStatic(true, m);
        }

        public override bool ShowRegionName => false;
        /*public override void OnDeathMoveTimer(Mobile m)
		{
			if(Parent != null)
				Parent.OnDeathMoveTimer(m);
		}

		public override bool OnMoveInto( Mobile m, Direction d, Point3D newLocation, Point3D oldLocation )
		{
			if(Parent!=null)
				return Parent.OnMoveInto(m, d, newLocation, oldLocation);
			return true;
		}*/
    }
}
