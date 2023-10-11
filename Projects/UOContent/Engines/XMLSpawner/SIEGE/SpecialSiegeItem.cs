using Server.Engines.XmlSpawner2;
using Server.Mobiles;
using System;

namespace Server.Items
{
    public class SpecialSiegeItem : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public string DestroyChkCondition { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string DestOkCheckAction { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string DestFailCheckAction { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string DeleteChecks { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string DelFailCheckAction { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string DelOkCheckAction { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string RepairChkCondition { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string RepairOkAction { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string RepairFailAction { get; set; }

        [Constructable(AccessLevel.GameMaster)]
        public SpecialSiegeItem(int itemID) : base(itemID)
        {
            //this item is not destructable, so only the del checks are possible!
        }

        [Constructable(AccessLevel.GameMaster)]
        public SpecialSiegeItem(int itemID, int hits, int fireres, int phisicres, int destroyitemID, double autorepairtime_mins) : base(itemID)
        {
            XmlAttach.AttachTo(this, new XmlSiege(hits, fireres, phisicres, 0, 0, 0, destroyitemID, autorepairtime_mins));
        }

        public SpecialSiegeItem(Serial serial) : base(serial)
        {
        }

        public override void MobileTrigger(Mobile m, bool repaired)
        {
            if (this == null || Deleted)
            {
                return;
            }

            if (!repaired)
            {
                if (m != null)
                {
                    if (CheckCondition(m, DestroyChkCondition))
                    {
                        ExecuteActions(m, DestOkCheckAction);
                    }
                    else
                    {
                        ExecuteActions(m, DestFailCheckAction);
                    }
                }
            }
            else
            {
                if (CheckCondition(m, RepairChkCondition))
                {
                    ExecuteActions(m, RepairOkAction);
                }
                else
                {
                    ExecuteActions(m, RepairFailAction);
                }
            }
        }

        private void ExecuteActions(Mobile m, string actions)
        {
            if (string.IsNullOrEmpty(actions))
            {
                return;
            }

            string[] args = actions.Split(';');

            for (int j = 0; j < args.Length; j++)
            {
                ExecuteAction(m, args[j]);
            }
        }

        private void ExecuteAction(Mobile mob, string action)
        {
            if (action == null || action.Length <= 0)
            {
                return;
            }

            string status_str = null;
            Server.Mobiles.XmlSpawner.SpawnObject TheSpawn = new Server.Mobiles.XmlSpawner.SpawnObject(null, 0)
            {
                TypeName = action
            };
            string substitutedtypeName = BaseXmlSpawner.ApplySubstitution(null, this, mob, action);
            string typeName = BaseXmlSpawner.ParseObjectType(substitutedtypeName);
            //new Point3D(0, 0, 0);
            Map map = null;
            Point3D loc;
            if (Parent == null)
            {
                loc = Location;
                map = Map;
            }
            else
            {
                loc = RootParent.Location;
                map = RootParent.Map;
            }

            if (BaseXmlSpawner.IsTypeOrItemKeyword(typeName))
            {
                BaseXmlSpawner.SpawnTypeKeyword(this, TheSpawn, typeName, substitutedtypeName, true, mob, loc, map, out status_str);
            }
            else
            {
                // its a regular type descriptor so find out what it is
                Type type = SpawnerType.GetType(typeName);
                try
                {
                    string[] arglist = BaseXmlSpawner.ParseString(substitutedtypeName, 3, BaseXmlSpawner.SlashDelim);
                    object o = Server.Mobiles.XmlSpawner.CreateObject(type, arglist[0]);

                    if (o == null)
                    {
                        status_str = "invalid type specification: " + arglist[0];
                    }
                    else if (o is Mobile)
                    {
                        Mobile m = (Mobile)o;
                        if (m is BaseCreature)
                        {
                            BaseCreature c = (BaseCreature)m;
                            c.Home = loc; // Spawners location is the home point
                        }

                        m.Location = loc;
                        m.Map = map;

                        BaseXmlSpawner.ApplyObjectStringProperties(null, substitutedtypeName, m, mob, this, out status_str);
                    }
                    else if (o is Item)
                    {
                        Item item = (Item)o;
                        BaseXmlSpawner.AddSpawnItem(null, this, TheSpawn, item, loc, map, mob, false, substitutedtypeName, out status_str);
                    }
                }
                catch { }
            }

            ReportError(mob, status_str);
        }

        private void ReportError(Mobile mob, string status_str)
        {
            if (status_str != null && mob != null && !mob.Deleted && mob.AccessLevel > AccessLevel.Player)
            {
                mob.SendMessage(33, string.Format("{0}:{1}", Name, status_str));
            }
        }

        private bool CheckCondition(Mobile from, string condition)
        {
            // test the condition if there is one
            if (!string.IsNullOrEmpty(condition))
            {
                return BaseXmlSpawner.CheckPropertyString(null, this, condition, from, out _);
            }
            return true;
        }

        public override bool BlockDelete()
        {
            // test the condition if there is one
            if (!string.IsNullOrEmpty(DeleteChecks))
            {
                if (BaseXmlSpawner.CheckPropertyString(null, this, DeleteChecks, null, out _))
                {
                    ExecuteActions(null, DelOkCheckAction);
                }
                else
                {
                    ExecuteActions(null, DelFailCheckAction);
                }
            }
            return base.BlockDelete();
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            DestroyChkCondition = reader.ReadString();
            DestOkCheckAction = reader.ReadString();
            DestFailCheckAction = reader.ReadString();
            DeleteChecks = reader.ReadString();
            DelOkCheckAction = reader.ReadString();
            DelFailCheckAction = reader.ReadString();
            if (version > 0)
            {
                RepairChkCondition = reader.ReadString();
                RepairOkAction = reader.ReadString();
                RepairFailAction = reader.ReadString();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
            writer.Write(DestroyChkCondition);
            writer.Write(DestOkCheckAction);
            writer.Write(DestFailCheckAction);
            writer.Write(DeleteChecks);
            writer.Write(DelOkCheckAction);
            writer.Write(DelFailCheckAction);
            writer.Write(RepairChkCondition);
            writer.Write(RepairOkAction);
            writer.Write(RepairFailAction);
        }
    }
}