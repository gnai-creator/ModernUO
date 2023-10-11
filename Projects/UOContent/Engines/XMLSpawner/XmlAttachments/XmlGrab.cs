using Server.Engines.XmlSpawner2;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Commands
{
    public class XmlGrab : XmlAttachment
    {
        public static void Configure()
        {
            string[] names = Enum.GetNames(typeof(LootMaterial));
            s_CheckToLoot = new ulong[names.Length];
            s_ClilocNames = new int[names.Length];//viene più grande di uno, il nome NONE non viene inizializzato
            s_CheckToLoot[0] = 0;
            ulong u = 1;
            for (int i = 1, cliloc = 504562; i < names.Length; ++i, cliloc++)
            {
                s_CachedValues[names[i]] = (int)Enum.Parse(typeof(LootMaterial), names[i]);
                s_CheckToLoot[i] = u;
                s_ClilocNames[i] = cliloc;
                u <<= 1;//left shift bit
            }
        }

        private enum LootMaterial : int
        {
            None = 0,//bit 0
            Gold = 1,//bit 1
            Exp = 2,//bit 2
            ExpNewbie = 3,//bit 4
            Vial,//etc
            Arrow,
            Bolt,
            Bandages,
            Token,
            BlackPearl,
            Bloodmoss,
            Garlic,
            Ginseng,
            MandrakeRoot,
            Nightshade,
            SpidersSilk,
            SulfurousAsh,
            BatWing,
            Blackmoor,
            Bloodspawn,
            Brimstone,
            DaemonBone,
            DeadWood,
            DragonsBlood,
            ExecutionesCap,
            EyeOfNewt,
            GraveDust,
            Obsidian,
            PigIron,
            Pumice,
            VolcanicAsh,
            WyrmsHeart,
            VialOfBlood,
            ScagliaDiSerpente,
            ScagliaDiDrago,
            NoxCrystal,
            DaemonBlood,
            Feather
        }

        private static Dictionary<string, int> s_CachedValues = new Dictionary<string, int>();
        private static ulong[] s_CheckToLoot;
        private static int[] s_ClilocNames;
        private static Dictionary<Mobile, LootTimer> m_Timers = new Dictionary<Mobile, LootTimer>();

        private ulong m_ToLootItems;
        private Container m_Destination = null;
        public XmlGrab() : base()
        {
        }

        public XmlGrab(ASerial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
            writer.WriteItem(m_Destination);
            writer.Write(m_ToLootItems);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Destination = reader.ReadItem<Container>();
            m_ToLootItems = reader.ReadULong();
            if (version < 1)
            {
                m_ToLootItems = 0;
            }
        }

        [Usage("Loot (OPTIONAL: true to select items to take - false for container choice)")]
        [Description("It allows you to empty the body of a monster, the monster must close to you (2 tiles maximum)...first you have to select a target container! (your backpack or a sub-container, if not set it will be required as the first target), using the word TRUE after the command, this will show an object type selection gump! if instead the word FALSE is used, you will be asked to select the destination bag (if already set)")]
        public static void Loot_OnCommand(CommandEventArgs e)
        {
            PostCommand(e.Mobile as PlayerMobile, e.GetString(0));
        }

        private static void PostCommand(PlayerMobile pm, string s)
        {
            if (pm == null || pm.Backpack == null)
            {
                return;
            }

            if (!(XmlAttach.FindAttachment(pm, typeof(XmlGrab)) is XmlGrab lootopts) || lootopts.Deleted)
            {
                lootopts = new XmlGrab();
                XmlAttach.AttachTo(pm, lootopts);
                //e.Mobile.SendGump( new LootGump(pm, lootopts));
            }
            if (lootopts.m_Destination == null || (!lootopts.m_Destination.IsChildOf(pm.Backpack) && lootopts.m_Destination != pm.Backpack))
            {
                //scelta contenitore di destinazione se non presente o non impostato
                pm.SendLocalizedMessage(1004472);//Devi prima impostare il contenitore di destinazione! Sceglilo ora!
                pm.Target = new DestinationTarget(lootopts, s);
            }
            else
            {
                if (!string.IsNullOrEmpty(s))
                {
                    s = s.ToLower(Core.Culture);
                    if (s.Contains("false"))
                    {
                        pm.SendLocalizedMessage(1004472);//Devi prima impostare il contenitore di destinazione! Sceglilo ora!
                        pm.Target = new DestinationTarget(lootopts, string.Empty);
                    }
                    else if (s.Contains("true"))
                    {
                        pm.SendGump(new LootGump(lootopts));
                    }
                    else
                    {
                        pm.SendLocalizedMessage(1004470);//Scrivendo il comando [loot da solo verrà richiesto di lootare il corpo della vittima. [loot true, invece, permette di scegliere cosa prendere.
                    }
                }
                /*else if(lootopts.m_Types.Count<1)
				{
					pm.SendLocalizedMessage(1004473);//Non hai impostato alcun oggetto da prendere. Sceglili ora!
					pm.SendGump( new LootGump(pm, lootopts));
				}*/
                else if (lootopts.m_ToLootItems == 0)
                {
                    pm.SendLocalizedMessage(1004503);//Non hai impostato alcun oggetto da prendere, usa il comando [loot true per impostarli!
                }
                else
                {
                    pm.Target = new LootTarget(lootopts);
                }
            }
        }

        private class DestinationTarget : Target
        {
            private XmlGrab m_LootOpts;
            private string m_S;
            public DestinationTarget(XmlGrab lootopts, string s) : base(0, false, TargetFlags.None)
            {
                m_LootOpts = lootopts;
                m_S = s;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_LootOpts == null || from == null || m_LootOpts.Deleted || from.Backpack == null)
                {
                    return;
                }

                if (targeted is Container)
                {
                    Container c = (Container)targeted;
                    if (c.IsChildOf(from.Backpack) || c == from.Backpack)
                    {
                        m_LootOpts.m_Destination = c;
                        from.SendLocalizedMessage(1004471);//Contenitore di destinazione impostato...
                        PostCommand(from as PlayerMobile, m_S);
                    }
                    else
                    {
                        from.SendLocalizedMessage(500212);//Non puoi usare quel contenitore!
                    }
                }
                else
                {
                    from.SendLocalizedMessage(501712);//"Non è un contenitore!");
                }
            }
        }

        private class LootTarget : Target
        {
            private XmlGrab m_Dest;
            public LootTarget(XmlGrab dest) : base(2, false, TargetFlags.None)
            {
                m_Dest = dest;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Corpse)
                {
                    Corpse c = (Corpse)targeted;
                    if (c.Owner is PlayerMobile)
                    {
                        from.SendLocalizedMessage(1004500);//"Non puoi usarlo sui corpi dei giocatori");
                    }
                    else
                    {
                        if (c.CheckLoot(from, null))
                        {
                            if (m_Timers.TryGetValue(from, out LootTimer t) && t != null)
                            {
                                t.Stop();
                            }

                            m_Timers[from] = t = new LootTimer(from, c, m_Dest);
                            t.Start();
                        }
                        else
                        {
                            from.SendLocalizedMessage(1004501);//"Non puoi svuotare quel corpo");
                        }
                    }
                }
            }
        }

        private class LootTimer : Timer
        {
            private Mobile m_Owner;
            private Corpse m_ToLoot;
            private XmlGrab m_Cont;
            private DateTime m_MoveTime;
            private Point3D m_Loc;
            private Map m_Map;
            public LootTimer(Mobile m, Corpse c, XmlGrab cont) : base(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(750))
            {
                m_Owner = m;
                m_ToLoot = c;
                m_MoveTime = m.LastMoveTime;
                m_Loc = c.GetWorldLocation();
                m_Map = c.Map;
                m_Cont = cont;
            }

            protected override void OnTick()
            {
                if (m_Owner == null || m_Cont == null)
                {
                    if (m_Owner != null)
                    {
                        m_Timers.Remove(m_Owner);
                    }

                    Stop();
                    return;
                }
                else if (m_Owner.Alive && m_Owner.LastMoveTime == m_MoveTime && m_Owner.NetState != null
                   && m_Owner.NetState.Running && (m_Cont.m_Destination.IsChildOf(m_Owner.Backpack) || m_Cont.m_Destination == m_Owner.Backpack)
                   && m_ToLoot != null && !m_ToLoot.Deleted)
                {
                    bool stop = false;
                    /*IPooledEnumerable<NetState> states = m_Map.GetClientsInRange(m_Loc, 2);
					foreach(NetState ns in states)
					{
						if(ns.Mobile!=m_Owner)
						{
							stop=true;
							break;
						}
					}*/
                    if (!stop)
                    {
                        int count = m_ToLoot.Items.Count;
                        Item item = null;
                        while (count > 0 && !stop)
                        {
                            item = m_ToLoot.Items[count - 1];
                            count--;
                            if (s_CachedValues.TryGetValue(item.GetType().Name, out int itemval) && (m_Cont.m_ToLootItems & s_CheckToLoot[itemval]) != 0 && item.Movable && !item.Nontransferable && item.Visible)
                            {
                                stop = true;
                            }
                        }
                        if (stop && item != null)
                        {
                            if (m_Cont.m_Destination.TryDropItem(m_Owner, item, true))
                            {
                                return;
                            }
                            else
                            {
                                m_Owner.SendLocalizedMessage(500720, "", 0x22);//Non hai abbastanza spazio nello zaino!
                            }
                        }
                    }
                }
                m_Owner.SendLocalizedMessage(1004502);//"Hai smesso di svuotare il contenitore");
                m_Timers.Remove(m_Owner);
                Stop();
            }
        }

        private class LootGump : Gump
        {
            private XmlGrab m_LootOpts;
            public LootGump(XmlGrab lootopts) : base(0, 0)
            {
                m_LootOpts = lootopts;
                Closable = true;
                Disposable = true;
                Dragable = true;
                Resizable = false;
                AddPage(0);
                int maxx = 30 + (int)(Math.Ceiling((s_CachedValues.Count) / 19.0)) * 180, maxy = 80 + 25 * Math.Min(19, s_CachedValues.Count), i = 0;
                AddBackground(0, 0, maxx, maxy, 9270);
                AddImageTiled(40, 40, maxx - 80, 2, 96);
                AddHtmlLocalized(0, 20, maxx, 20, 1004499, 0x564B, false, false);
                //int x=30, y=50;
                foreach (KeyValuePair<string, int> kvp in s_CachedValues)
                {
                    int val = kvp.Value;
                    maxx = 20 + (i / 19) * 180;
                    maxy = 50 + (i % 19) * 25;
                    AddCheck(maxx, maxy, 1150, 1153, (s_CheckToLoot[val] & m_LootOpts.m_ToLootItems) != 0, val);
                    AddHtmlLocalized(maxx + 34, maxy, 146, 25, s_ClilocNames[val], 0x6F6E, false, false);
                    ++i;
                }
                AddButton(maxx + 40, maxy + 30, 247, 248, 1, GumpButtonType.Reply, 0);
            }

            public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
            {
                if (info == null || info.Switches == null || m_LootOpts == null || m_LootOpts.Deleted)
                {
                    return;
                }

                switch (info.ButtonID)
                {
                    case 0: // Closed or Cancel
                    {
                        return;
                    }
                    default:
                    {
                        // OK button
                        if (info.ButtonID == 1)
                        {
                            m_LootOpts.m_ToLootItems = 0;
                            for (int i = 0; i < info.Switches.Length; ++i)
                            {
                                int scelta = info.Switches[i];
                                if (scelta < 1 || scelta >= s_CheckToLoot.Length)
                                {
                                    continue;
                                }

                                m_LootOpts.m_ToLootItems += s_CheckToLoot[scelta];
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
