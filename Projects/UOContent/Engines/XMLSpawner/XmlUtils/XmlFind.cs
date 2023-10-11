using Server.Commands;
using Server.Commands.Generic;
using Server.Engines.XmlSpawner2;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
//using Amib.Threading;

/*
** XmlFind
** utility for locating objects in the world.
** ArteGordon
** original version 1.0
** 4/13/04
**
*/

namespace Server.Mobiles
{
    public interface ISpawnObjectFinderList// : IEntity
    {
        void ISpawnObjectDoGump(Mobile to);
        List<ISpawnObjectFinder> i_SpawnObjects { get; }
        string status_str { get; }
    }
    public interface ISpawnObjectFinder
    {
        string TypeName { get; }
    }

    public class XmlFindGump : Gump
    {
        private static List<Thread> m_XmlGenericThreads = new List<Thread>();

        public class XmlFindThread
        {
            private DateTime m_Started;
            private SearchCriteria m_SearchCriteria;
            private Mobile m_From;
            private string m_commandstring;
            public bool Completed { get; set; }

            public XmlFindThread(Mobile from, SearchCriteria criteria, string commandstring)
            {
                m_Started = DateTime.UtcNow;
                m_SearchCriteria = criteria;
                m_From = from;
                m_commandstring = commandstring;
                Completed = false;
            }

            public void XmlFindThreadMain()
            {
                if (m_From == null)
                {
                    return;
                }

                List<SearchEntry> results = Search(m_SearchCriteria, out string status_str);
                for (int i = results.Count - 1; i >= 0; --i)
                {
                    var se = results[i];
                    if ((se.Object is Item it && it.RootParent is PlayerMobile pm && pm.AccessLevel > m_From.AccessLevel && !pm.VisibilityList.Contains(m_From)) || (se.Object is PlayerMobile gpm && gpm.AccessLevel > m_From.AccessLevel && !gpm.VisibilityList.Contains(m_From)))
                    {
                        results.RemoveAt(i);
                        continue;
                    }
                }

                CommandLogging.WriteLine(m_From, $"{m_From.AccessLevel} {CommandLogging.FormatMobComplete(m_From)} Risultati: {results.Count} - {m_SearchCriteria}");
                XmlFindGump gump = new XmlFindGump(m_From, m_From.Location, m_From.Map, true, true, false,

                    m_SearchCriteria,

                    results, -1, 0, null, m_commandstring,
                    false, false, false, false, false, false, false, 0, 0);
                if (m_Started < DateTime.UtcNow)
                {
                    m_From.SendMessage("Ricerca completata in: {0:F2} secondi", DateTime.UtcNow.Subtract(m_Started).TotalSeconds);
                }

                if (XmlAttach.FindAttachment(m_From, typeof(XmlFindAttachment)) is XmlFindAttachment xf && !xf.Deleted)
                {
                    xf.AddResults(gump);
                }
                else
                {
                    xf = new XmlFindAttachment();
                    XmlAttach.AttachTo(m_From, xf);
                    xf.AddResults(gump);
                }

                // display the updated gump synched with the main server thread
                m_XmlGenericThreads.Remove(Thread.CurrentThread);
                Timer.DelayCall(TimeSpan.Zero, GumpDisplayCallback, (m_From, gump, status_str));
            }

            public static void GumpDisplayCallback((Mobile, XmlFindGump, string) state)
            {
                Mobile from = state.Item1;
                XmlFindGump gump = state.Item2;
                string status_str = state.Item3;

                if (from != null && !from.Deleted)
                {
                    from.SendGump(gump);
                    if (status_str != null)
                    {
                        from.SendMessage(33, "XmlFind: {0}", status_str);
                    }
                }
            }
        }

        private const int MaxEntries = 20;
        private const int MaxEntriesPerPage = 20;

        public class SearchEntry
        {
            public bool Selected;
            public object Object;

            public SearchEntry(object o)
            {
                Object = o;
            }
        }

        public class SearchCriteria
        {
            public bool Dosearchtype;
            public bool Dosearchname;
            public bool Dosearchrange;
            public bool Dosearchregion;
            public bool Dosearchspawnentry;
            public bool Dosearchspawntype;
            public bool Dosearchcondition;
            public bool Dosearchfel;
            public bool Dosearchtram;
            public bool Dosearchilsh;
            public bool Dosearchint;
            public bool Dosearchnull;
            public bool Dosearcherr;
            public bool DosearchdungSemiC;
            public bool DosearchdungC;
            public bool Dosearcheventi;
            public bool Dosearchage;
            public bool Dosearchwithattach;
            public bool Dosearchattach;
            public bool Dohidevalidint = false;
            public bool Searchagedirection;
            public bool Searchattachornot;
            public double Searchage;
            public int Searchrange;
            public string Searchregion;
            public string Searchcondition;
            public string Searchtype;
            public string Searchattachtype;
            public string Searchname;
            public string Searchspawnentry;
            public Mobile From;
            public Map Currentmap;
            public Point3D Currentloc;

            public SearchCriteria(bool dotype, bool doname, bool dorange, bool doregion, bool doentry, bool doentrytype, bool docondition, bool dofel, bool dotram,
                bool doilsh, bool semichiusi, bool chiusi, bool eventi, bool doint, bool donull, bool doerr, bool doage, bool dowithattach, bool doattach, bool dohidevalid,
                bool agedirection, double age, int range, string region, string condition, string type, string attachtype, string name, string entry, bool searchattachornot,
                Mobile from)
            {
                Dosearchtype = dotype;
                Dosearchname = doname;
                Dosearchrange = dorange;
                Dosearchregion = doregion;
                Dosearchspawnentry = doentry;
                Dosearchspawntype = doentrytype;
                Dosearchcondition = docondition;
                Dosearchfel = dofel;
                Dosearchtram = dotram;
                Dosearchilsh = doilsh;
                DosearchdungSemiC = semichiusi;
                DosearchdungC = chiusi;
                Dosearcheventi = eventi;
                Dosearchint = doint;
                Dosearchnull = donull;
                Dosearcherr = doerr;
                Dosearchage = doage;
                Dosearchwithattach = dowithattach;
                Dosearchattach = doattach;
                Dohidevalidint = dohidevalid;
                Searchagedirection = agedirection;
                Searchage = age;
                Searchrange = range;
                Searchregion = region;
                Searchcondition = condition;
                Searchtype = type;
                Searchattachtype = attachtype;
                Searchname = name;
                Searchspawnentry = entry;
                Searchattachornot = searchattachornot;
                From = from;
            }

            public SearchCriteria()
            {
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Ricerca: ");
                if (Dosearchtype && Searchtype != null)
                {
                    sb.AppendFormat("Type={0} - ", string.IsNullOrWhiteSpace(Searchtype) ? "(-EMPTY-)" : Searchtype);
                }

                if (Dosearchwithattach)
                {
                    sb.AppendFormat("{0} Attachment - ", Searchattachornot ? "Con" : "Senza");
                }

                if (Dosearchcondition && !string.IsNullOrWhiteSpace(Searchcondition))
                {
                    sb.AppendFormat("PropTest='{0}' - ", Searchcondition);
                }

                if (Dosearchname)
                {
                    sb.AppendFormat("Nome='{0}' - ", (Searchname == null ? "(-NULL-)" : string.IsNullOrWhiteSpace(Searchname) ? "(-EMPTY-)" : Searchname));
                }

                if (Dosearchattach)
                {
                    sb.AppendFormat("AttachmentType='{0}' - ", string.IsNullOrWhiteSpace(Searchattachtype) ? "(-EMPTY-)" : Searchattachtype);
                }

                if (Dosearchspawnentry || Dosearchspawntype || Dosearcherr)
                {
                    sb.AppendFormat("SearchSpawn{0}{1}{2}='{3}' - ", (Dosearchspawnentry ? "Entry" : ""), (Dosearchspawntype ? "Type" : ""), (Dosearcherr ? "Err" : ""), (string.IsNullOrWhiteSpace(Searchspawnentry) ? "(-EMPTY-)" : Searchspawnentry));
                }

                sb.Append("Ricerche su Mappe: ");
                sb.AppendFormat("{0}{1}{2}{3}{4}{5}{6}{7}", (Dosearchint ? "Internal " : ""), (Dosearchnull ? "Null " : ""), (Dosearchfel ? "Fellucca " : ""), (Dosearchtram ? "Trammel " : ""), (Dosearchilsh ? "Ilshenar " : ""), (DosearchdungSemiC ? "Dung_Semi-Chiusi " : ""), (DosearchdungC ? "Dung_Chiusi " : ""), (Dosearcheventi ? "Eventi " : ""));
                sb.Append("- ");
                if (Dohidevalidint)
                {
                    sb.AppendFormat("Hide valid Internal - ");
                }

                if (Dosearchregion && !string.IsNullOrWhiteSpace(Searchregion))
                {
                    sb.AppendFormat("SearchRegion='{0}' - ", Searchregion);
                }

                if (Dosearchage && Searchage > 0)
                {
                    sb.AppendFormat("SearchAge='{0}{1}' - ", (Searchagedirection ? ">" : "<"), Searchage);
                }

                if (Dosearchrange && Searchrange >= 0)
                {
                    sb.AppendFormat("SearchRange={0} - ", Searchrange);
                }

                return sb.ToString();
            }
        }

        private SearchCriteria m_SearchCriteria;
        private bool Sorttype;
        private bool Sortrange;
        private bool Sortname;
        private bool Sortmap;
        private bool Sortselect;
        private bool Sortowner;
        private Mobile m_From;
        private Point3D StartingLoc;
        private Map StartingMap;
        private bool m_ShowExtension;
        private bool Descendingsort;
        private int Selected;
        private int DisplayFrom;
        private string SaveFilename;
        private string CommandString;

        private bool SelectAll = false;

        private List<SearchEntry> m_SearchList;

        public static void Initialize()
        {
            CommandSystem.Register("XmlFind", AccessLevel.GameMaster, new CommandEventHandler(XmlFind_OnCommand));
            EventSink.WorldPreSave += new WorldPreSaveEventHandler(PreSave);
            EventSink.WorldPostSave += new WorldPostSaveEventHandler(PostSave);
            //			SmartThread = new SmartThreadPool();
            //			SmartThread.Name = "XmlFind Thread";
        }

        private static void PreSave(WorldSaveEventArgs e)
        {
            foreach (Thread t in m_XmlGenericThreads)
            {
#pragma warning disable 618
                if (t.ThreadState == ThreadState.Running)
                {
                    t.Suspend();
                }
#pragma warning restore 618
            }
        }

        private static void PostSave()
        {
            foreach (Thread t in m_XmlGenericThreads)
            {
#pragma warning disable 618
                if (t.ThreadState == ThreadState.Suspended)
                {
                    t.Resume();
                }
#pragma warning restore 618
            }
        }

        private static bool TestRange(object o, int range, Map currentmap, Point3D currentloc)
        {
            if (range < 0)
            {
                return true;
            }

            if (o is Item item)
            {
                if (item.Map != currentmap)
                {
                    return false;
                }

                // is the item in a container?
                // if so, then check the range of the parent rather than the item
                Point3D loc = item.GetWorldLocation();

                return Utility.InRange(currentloc, loc, range);

            }
            else if (o is Mobile mob)
            {
                if (mob.Map == currentmap)
                {
                    return (Utility.InRange(currentloc, mob.Location, range));
                }
                else if (mob.LogoutMap != null && mob.LogoutMap == currentmap)
                {
                    return (Utility.InRange(currentloc, mob.LogoutLocation, range));
                }
            }
            else if(o is Targeting.StaticTarget st)
            {
                return Utility.InRange(currentloc, st.Location, range);
            }
            else if(o is Targeting.LandTarget lt)
            {
                return Utility.InRange(currentloc, lt.Location, range);
            }
            return false;
        }

        private static bool TestRegion(object o, string regionname, Map map = null)
        {
            if (regionname == null)
            {
                return false;
            }

            if (o is Item item)
            {
                // is the item in a container?
                // if so, then check the region of the parent rather than the item
                Point3D loc = item.Location;
                map = item.Map;
                if (item.Parent != null && item.RootParent != null)
                {
                    loc = item.RootParent.Location;
                    map = item.RootParent.Map;
                }
                if (map == null)
                {
                    return false;
                }

                if (map.Regions.TryGetValue(regionname, out Region r) && r != null)
                {
                    return (r.Contains(loc));
                }

                return false;
            }
            else if (o is Mobile mob)
            {
                bool internalmap = false;
                map = mob.Map;
                if (map == Map.Internal && mob.LogoutMap != null)
                {
                    map = mob.LogoutMap;
                    internalmap = true;
                }
                if (map == null)
                {
                    return false;
                }

                if (map.Regions.TryGetValue(regionname, out Region r) && r != null)
                {
                    return r.Contains((internalmap ? mob.LogoutLocation : mob.Location));
                }

                return false;
            }
            else if (map != null)
            {
                if (o is Targeting.StaticTarget st)
                {
                    if (map.Regions.TryGetValue(regionname, out Region r) && r != null)
                    {
                        return r.Contains(st.Location);
                    }
                }
                else if (o is Targeting.LandTarget lt)
                {
                    if (map.Regions.TryGetValue(regionname, out Region r) && r != null)
                    {
                        return r.Contains(lt.Location);
                    }
                }
            }
            return false;
        }

        private static bool TestAttach(Item i)
        {
            List<XmlAttachment> xls = XmlAttach.FindAttachments(i);
            if (xls != null && xls.Count > 0)
            {
                return true;
            }

            return false;
        }

        private static bool TestAttach(Mobile m)
        {
            List<XmlAttachment> xls = XmlAttach.FindAttachments(m);
            if (xls != null && xls.Count > 0)
            {
                return true;
            }

            return false;
        }

        private static bool TestAge(Mobile mob, double age, bool direction)
        {
            if (age <= 0)
            {
                return true;
            }

            if (direction)
            {
                // true means allow only mobs greater than the age
                if ((DateTime.UtcNow - mob.CreationTime) > TimeSpan.FromHours(age))
                {
                    return true;
                }
            }
            else
            {
                // false means allow only mobs less than the age
                if ((DateTime.UtcNow - mob.CreationTime) < TimeSpan.FromHours(age))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TestAge(Item it, double age, bool direction)
        {
            if (age <= 0)
            {
                return true;
            }

            if (direction)
            {
                // true means allow only mobs greater than the age
                if ((DateTime.UtcNow - it.LastMoved) > TimeSpan.FromHours(age))
                {
                    return true;
                }
            }
            else
            {
                // false means allow only mobs less than the age
                if ((DateTime.UtcNow - it.LastMoved) < TimeSpan.FromHours(age))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TestAge(XmlAttachment xa, double age, bool direction)
        {
            if (age <= 0)
            {
                return true;
            }

            if (direction)
            {
                // true means allow only mobs greater than the age
                if ((DateTime.UtcNow - xa.CreationTime) > TimeSpan.FromHours(age))
                {
                    return true;
                }
            }
            else
            {
                // false means allow only mobs less than the age
                if ((DateTime.UtcNow - xa.CreationTime) < TimeSpan.FromHours(age))
                {
                    return true;
                }
            }

            return false;
        }

        private static void IgnoreManagedInternal(object i, ref List<object> ignoreList)
        {

            // ignore valid internalized commodity deed items
            if (i is CommodityDeed)
            {
                CommodityDeed deed = (CommodityDeed)i;

                if (deed.Commodity != null && deed.Commodity.Map == Map.Internal)
                {
                    ignoreList.Add(deed.Commodity);
                }
            }

            // ignore valid internalized keyring keys
            if (i is KeyRing)
            {
                KeyRing keyring = (KeyRing)i;

                if (keyring.Keys != null)
                {
                    foreach (Key k in keyring.Keys)
                    {
                        ignoreList.Add(k);
                    }
                }
            }

            // ignore valid internalized relocatable house items
            if (i is BaseHouse)
            {
                BaseHouse house = (BaseHouse)i;

                foreach (RelocatedEntity relEntity in house.RelocatedEntities)
                {
                    if (relEntity.Entity is Item)
                    {
                        ignoreList.Add(relEntity.Entity);
                    }
                }

                foreach (VendorInventory inventory in house.VendorInventories)
                {
                    foreach (Item subItem in inventory.Items)
                    {
                        ignoreList.Add(subItem);
                    }
                }
            }
        }

        // test for valid items/mobs on the internal map
        private static bool TestValidInternal(object o)
        {
            if (o is Mobile m)
            {
                if (m.Map != Map.Internal || m.Account != null ||
                    (m is IMount im && im.Rider != null) ||
                    (m is BaseCreature bc && bc.IsStabled))
                {
                    return true;
                }
            }
            else if (o is Item i)
            {
                // note, in order to test for a vendors display container that contains valid internal map items 
                // we need to see if we have a DisplayCache type container.  Unfortunately, DisplayCache
                // is a private class declared in GenericBuyInfo and so cannot be tested for here. 
                // To get around that we just check the declaring type.
                if (i.Map != Map.Internal || i.Parent != null || i is Fists || i is MountItem || i is EffectItem || i.HeldBy != null ||
                    i is MovingCrate || (i.GetType().DeclaringType == typeof(GenericBuyInfo)))
                {
                    return true;
                }
            }

            return false;
        }

        private static Type StaticTarget = typeof(Targeting.StaticTarget);
        private static Type LandTarget = typeof(Targeting.LandTarget);

        public static List<SearchEntry> Search(SearchCriteria criteria, out string status_str)
        {
            status_str = null;
            List<SearchEntry> newarray = new List<SearchEntry>();
            List<object> ignoreList = new List<object>();
            string searchname = criteria.Searchname?.ToLower(Core.Culture), searchentry = criteria.Dosearchtype && string.IsNullOrEmpty(criteria.Searchspawnentry) ? null : criteria.Searchspawnentry?.ToLower(Core.Culture);

            if (criteria == null)
            {
                status_str = "Empty search criteria";
                return newarray;
            }

            Type targetType = null;
            Type targetattachType = null;

            /*Map tokunomap = null;
			try
			{
				tokunomap = Map.Parse("Tokuno");
			}
			catch { }*/

            /*Map termurmap = null;
			try
			{
				termurmap = Map.Parse("TerMur");
			}
			catch { }*/

            Map dungsemichiusi = Map.DungeonSemiChiusi;//.Parse("Dungeon semi-chiusi");

            Map dungchiusi = Map.DungeonChiusi;//.Parse("Dungeon chiusi");
            
            Map eventi = Map.Eventi;//.Parse("Eventi");
            
            // if the type is specified then get the search type
            if (criteria.Dosearchtype && criteria.Searchtype != null)
            {
                targetType = SpawnerType.GetType(criteria.Searchtype);
                if (targetType == null)
                {
                    status_str = "Invalid type: " + criteria.Searchtype;
                    return newarray;
                }
            }

            // if the attachment type is specified then get the search type
            if (criteria.Dosearchattach && criteria.Searchattachtype != null && criteria.Searchattachtype.Length > 0)
            {
                targetattachType = SpawnerType.GetType(criteria.Searchattachtype);
                if (targetattachType == null)
                {
                    status_str = "Invalid type: " + criteria.Searchattachtype;
                    return newarray;
                }
            }

            // do the search through items

            if (!criteria.Dosearchattach)
            {
                // make a copy so that we dont get enumeration errors if World.Items.Values changes while searching
                //List<Item> itemarray = null;

                //ConcurrentQueue<Item> itemvalues = new ConcurrentQueue<Item>(World.Items.Values);

                /*lock (itemvalues.SyncRoot)
				{
					try
					{
						itemarray = new List<Item>(itemvalues);
					}
					catch (SystemException e) { status_str = "Unable to search World.Items: " + e.Message; }
				}*/
                if (targetType == StaticTarget || targetType == LandTarget)
                {
                    if(!criteria.Dosearchrange || criteria.Searchrange < 0 || criteria.Searchrange > 100)
                    {
                        status_str = $"That type requires a SEARCH RANGE between 0 to 100: {criteria.Searchtype}";
                        return newarray;
                    }
                    else
                    {
                        if (targetType == StaticTarget)
                        {
                            IPooledEnumerable<Targeting.StaticTarget> pool = criteria.Currentmap.GetStaticTargetsInBounds(new Rectangle2D(new Point2D(criteria.Currentloc.m_X - criteria.Searchrange, criteria.Currentloc.m_Y - criteria.Searchrange), new Point2D(criteria.Currentloc.m_X + criteria.Searchrange, criteria.Currentloc.m_Y + criteria.Searchrange)));
                            foreach(var targ in pool)
                            {
                                if (TestRange(targ, criteria.Searchrange, criteria.Currentmap, criteria.Currentloc))
                                {
                                    if (criteria.Dosearchname)
                                    {
                                        if (searchname != null && (targ.Name == null || targ.Name.ToLower(Core.Culture).IndexOf(searchname) <= -1))
                                        {
                                            continue;
                                        }
                                    }
                                    if (criteria.Dosearchregion)
                                    {
                                        if (!TestRegion(targ, criteria.Searchregion, criteria.Currentmap))
                                        {
                                            continue;
                                        }
                                    }
                                    // check for condition
                                    if (criteria.Dosearchcondition)
                                    {
                                        // check the property test
                                        if (criteria.Searchcondition == null || !BaseXmlSpawner.CheckPropertyString(null, targ, criteria.Searchcondition, null, out status_str))
                                        {
                                            continue;
                                        }
                                    }
                                    newarray.Add(new SearchEntry(targ));
                                }
                            }
                            pool.Free();
                        }
                        else if (targetType == LandTarget)
                        {
                            IPooledEnumerable<Targeting.LandTarget> pool = criteria.Currentmap.GetLandTargetsInBounds(new Rectangle2D(new Point2D(criteria.Currentloc.m_X - criteria.Searchrange, criteria.Currentloc.m_Y - criteria.Searchrange), new Point2D(criteria.Currentloc.m_X + criteria.Searchrange, criteria.Currentloc.m_Y + criteria.Searchrange)));
                            foreach (var targ in pool)
                            {
                                if (TestRange(targ, criteria.Searchrange, criteria.Currentmap, criteria.Currentloc))
                                {
                                    if (criteria.Dosearchname)
                                    {
                                        if (searchname != null && (targ.Name == null || targ.Name.ToLower(Core.Culture).IndexOf(searchname) <= -1))
                                        {
                                            continue;
                                        }
                                    }
                                    if (criteria.Dosearchregion)
                                    {
                                        if (!TestRegion(targ, criteria.Searchregion, criteria.Currentmap))
                                        {
                                            continue;
                                        }
                                    }
                                    // check for condition
                                    if (criteria.Dosearchcondition)
                                    {
                                        // check the property test
                                        if (criteria.Searchcondition == null || !BaseXmlSpawner.CheckPropertyString(null, targ, criteria.Searchcondition, null, out status_str))
                                        {
                                            continue;
                                        }
                                    }
                                    newarray.Add(new SearchEntry(targ));
                                }
                            }
                            pool.Free();
                        }
                    }
                }
                else
                {
                    foreach (Item i in World.Items.Values)
                    {
                        bool hasentry = false;
                        bool hasmap = false;
                        bool hasattach = false;

                        if (i == null || i.Deleted)
                        {
                            continue;
                        }

                        // this will deal with items that are not on the internal map but hold valid internal items
                        if (criteria.Dohidevalidint && i.Map != Map.Internal && i.Map != null)
                        {
                            IgnoreManagedInternal(i, ref ignoreList);
                        }

                        // check for map
                        if ((i.Map == Map.Felucca && criteria.Dosearchfel) || (i.Map == Map.Trammel && criteria.Dosearchtram) ||
                            (i.Map == Map.Ilshenar && criteria.Dosearchilsh) || (i.Map == Map.Internal && criteria.Dosearchint) ||
                            (i.Map == null && criteria.Dosearchnull))
                        {
                            hasmap = true;
                        }

                        if (dungsemichiusi != null && i.Map == dungsemichiusi && criteria.DosearchdungSemiC)
                        {
                            hasmap = true;
                        }

                        if (dungchiusi != null && i.Map == dungchiusi && criteria.DosearchdungC)
                        {
                            hasmap = true;
                        }

                        if (eventi != null && i.Map == eventi && criteria.Dosearcheventi)
                        {
                            hasmap = true;
                        }

                        if (!hasmap)
                        {
                            continue;
                        }

                        // check for type
                        if (criteria.Dosearchtype)
                        {
                            if (!i.GetType().IsSubclassOf(targetType) && !i.GetType().Equals(targetType))
                            {
                                continue;
                            }
                        }

                        // check for name
                        if (criteria.Dosearchname)
                        {
                            if (searchname != null && (i.Name == null || i.Name.ToLower(Core.Culture).IndexOf(searchname) <= -1))
                            {
                                continue;
                            }
                        }

                        // check for valid internal map items
                        if (criteria.Dohidevalidint)
                        {
                            if (TestValidInternal(i))
                            {
                                // this will deal with items that are on the internal map and hold valid internal items
                                IgnoreManagedInternal(i, ref ignoreList);
                                continue;
                            }
                        }

                        // check for age
                        if (criteria.Dosearchage)
                        {
                            if (!TestAge(i, criteria.Searchage, criteria.Searchagedirection))
                            {
                                continue;
                            }
                        }

                        // check for range
                        if (criteria.Dosearchrange)
                        {
                            if (!TestRange(i, criteria.Searchrange, criteria.Currentmap, criteria.Currentloc))
                            {
                                continue;
                            }
                        }

                        // check for region
                        if (criteria.Dosearchregion)
                        {
                            if (!TestRegion(i, criteria.Searchregion))
                            {
                                continue;
                            }
                        }

                        // check for attachments
                        if (criteria.Dosearchwithattach)
                        {
                            if (TestAttach(i))
                            {
                                hasattach = true;
                            }

                            if ((!criteria.Searchattachornot && hasattach) || (criteria.Searchattachornot && !hasattach))
                            {
                                continue;
                            }
                        }

                        // check for condition
                        if (criteria.Dosearchcondition)
                        {
                            // check the property test
                            if (criteria.Searchcondition == null || !BaseXmlSpawner.CheckPropertyString(null, i, criteria.Searchcondition, null, out status_str))
                            {
                                continue;
                            }
                        }

                        // check for entry
                        if (searchentry != null && (criteria.Dosearchspawnentry || criteria.Dosearchspawntype))
                        {
                            // see what kind of spawner it is
                            if (i is ISpawnObjectFinderList)
                            {
                                Type targetentrytype = null;
                                if (criteria.Dosearchspawntype)
                                {
                                    targetentrytype = SpawnerType.GetType(searchentry);
                                    if (targetentrytype == null)
                                    {
                                        continue;
                                    }
                                }
                                // search the entries of the spawner
                                foreach (ISpawnObjectFinder so in ((ISpawnObjectFinderList)i).i_SpawnObjects)
                                {
                                    if (criteria.Dosearchspawntype)
                                    {
                                        // search by entry type
                                        Type type = null;

                                        if (so.TypeName != null)
                                        {
                                            string[] args = so.TypeName.Split('/');
                                            string typestr = null;
                                            if (args != null && args.Length > 0)
                                            {
                                                typestr = args[0];
                                            }

                                            type = SpawnerType.GetType(typestr);
                                        }

                                        if (type != null && (type == targetentrytype || type.IsSubclassOf(targetentrytype)))
                                        {
                                            hasentry = true;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        // search by entry string
                                        if (so.TypeName != null && so.TypeName.ToLower(Core.Culture).IndexOf(searchentry) >= 0)
                                        {
                                            hasentry = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        if ((criteria.Dosearchspawnentry || criteria.Dosearchspawntype) && !hasentry)
                        {
                            continue;
                        }

                        // check for err
                        if (criteria.Dosearcherr)
                        {
                            // see what kind of spawner it is
                            if (i is ISpawnObjectFinderList sofl)
                            {
                                // check the status of the spawner
                                if (sofl.status_str == null)
                                {
                                    continue;
                                }
                            }
                            else if (i.Spawner is ISpawnObjectFinderList sofls)
                            {
                                if (sofls.status_str == null)
                                {
                                    continue;
                                }
                            }
                        }

                        // satisfied all conditions so add it
                        newarray.Add(new SearchEntry(i));
                    }

                    // do the search through mobiles
                    // make a copy so that we dont get enumeration errors if World.Mobiles.Values changes while searching
                    /*List<Mobile> mobilearray = null;
                    ICollection mobilevalues = World.Mobiles.Values;
                    lock (mobilevalues.SyncRoot)
                    {
                        try
                        {
                            mobilearray = new List<Mobile>(mobilevalues);
                        }
                        catch (SystemException e) { status_str = "Unable to search World.Mobiles: " + e.Message; }
                    }*/

                    foreach (Mobile i in World.Mobiles.Values)
                    {
                        bool hasentry = false;
                        bool hasmap = false;
                        bool hasattach = false;

                        if (i == null || i.Deleted)
                        {
                            continue;
                        }

                        // check for map
                        if ((i.Map == Map.Felucca && criteria.Dosearchfel) || (i.Map == Map.Trammel && criteria.Dosearchtram) ||
                            (i.Map == Map.Ilshenar && criteria.Dosearchilsh) || (i.Map == Map.Internal && criteria.Dosearchint) ||
                            (i.Map == null && criteria.Dosearchnull))
                        {
                            hasmap = true;
                        }

                        if (dungsemichiusi != null && i.Map == dungsemichiusi && criteria.DosearchdungSemiC)
                        {
                            hasmap = true;
                        }

                        if (dungchiusi != null && i.Map == dungchiusi && criteria.DosearchdungC)
                        {
                            hasmap = true;
                        }

                        if (eventi != null && i.Map == eventi && criteria.Dosearcheventi)
                        {
                            hasmap = true;
                        }

                        if (!hasmap)
                        {
                            continue;
                        }

                        // check for range
                        if (criteria.Dosearchrange)
                        {
                            if (!TestRange(i, criteria.Searchrange, criteria.Currentmap, criteria.Currentloc))
                            {
                                continue;
                            }
                        }

                        // check for region
                        if (criteria.Dosearchregion)
                        {
                            if (!TestRegion(i, criteria.Searchregion))
                            {
                                continue;
                            }
                        }

                        // check for valid internal map mobiles
                        if (criteria.Dohidevalidint)
                        {
                            if (TestValidInternal(i))
                            {
                                continue;
                            }
                        }

                        // check for age
                        if (criteria.Dosearchage)
                        {
                            if (!TestAge(i, criteria.Searchage, criteria.Searchagedirection))
                            {
                                continue;
                            }
                        }

                        // check for type
                        if (criteria.Dosearchtype)
                        {
                            if (!i.GetType().IsSubclassOf(targetType) && !i.GetType().Equals(targetType))
                            {
                                continue;
                            }
                        }

                        // check for name
                        if (criteria.Dosearchname)
                        {
                            string rname = i.GetRawNameFor(criteria.From);
                            if (searchname != null && (rname == null || rname.ToLower(Core.Culture).IndexOf(searchname) <= -1))
                            {
                                continue;
                            }
                        }

                        // check for attachments
                        if (criteria.Dosearchwithattach)
                        {
                            if (TestAttach(i))
                            {
                                hasattach = true;
                            }

                            if ((!criteria.Searchattachornot && hasattach) || (criteria.Searchattachornot && !hasattach))
                            {
                                continue;
                            }
                        }

                        // check for condition
                        if (criteria.Dosearchcondition)
                        {
                            if (criteria.Searchcondition != null)
                            {
                                // check the property test
                                if (!BaseXmlSpawner.CheckPropertyString(null, i, criteria.Searchcondition, null, out status_str))
                                {
                                    continue;
                                }
                            }
                        }

                        // check for entry (actually only for XMLQuestNPC and talking creatures...
                        // check for entry
                        if (searchentry != null && (criteria.Dosearchspawnentry || criteria.Dosearchspawntype))
                        {
                            if (i is ISpawnObjectFinderList list)
                            {
                                Type targetentrytype = null;
                                if (criteria.Dosearchspawntype)
                                {
                                    targetentrytype = SpawnerType.GetType(searchentry);
                                    if (targetentrytype == null)
                                    {
                                        continue;
                                    }
                                }
                                // search the entries of the spawner
                                foreach (ISpawnObjectFinder so in list.i_SpawnObjects)
                                {
                                    if (criteria.Dosearchspawntype)
                                    {
                                        // search by entry type
                                        Type type = null;

                                        if (so.TypeName != null)
                                        {
                                            string[] args = so.TypeName.Split('/', ';');
                                            if (args != null)
                                            {
                                                for (int x = Math.Min(5, args.Length - 1); x >= 0 && type == null; --x)
                                                {
                                                    type = SpawnerType.GetType(args[x].Trim());
                                                }
                                            }
                                        }

                                        if (type != null && (type == targetentrytype || type.IsSubclassOf(targetentrytype)))
                                        {
                                            hasentry = true;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        // search by entry string
                                        if (so.TypeName != null && so.TypeName.ToLower(Core.Culture).IndexOf(searchentry) >= 0)
                                        {
                                            hasentry = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        if ((criteria.Dosearchspawnentry || criteria.Dosearchspawntype) && !hasentry)
                        {
                            continue;
                        }

                        // check for err
                        if (criteria.Dosearcherr)
                        {
                            // see what kind of spawner it is
                            if (i is ISpawnObjectFinderList)
                            {
                                // check the status of the spawner
                                if (((ISpawnObjectFinderList)i).status_str == null)
                                {
                                    continue;
                                }
                            }
                            else if (i.Spawner is ISpawnObjectFinderList)
                            {
                                if (((ISpawnObjectFinderList)i.Spawner).status_str == null)
                                {
                                    continue;
                                }
                            }
                        }

                        // passed all conditions so add it to the list
                        newarray.Add(new SearchEntry(i));
                    }
                }
            }

            // need to keep track of valid internalized XmlSaveItem items
            if (criteria.Dohidevalidint)
            {
                foreach (XmlAttachment i in XmlAttach.Values)
                {
                    if (i is XmlSaveItem s)
                    {
                        if (s.Container != null)
                        {
                            ignoreList.Add(s.Container);
                        }
                    }
                }
            }

            if (criteria.Dosearchattach)
            {
                List<XmlAttachment> foundtypes = new List<XmlAttachment>();
                foreach (XmlAttachment i in XmlAttach.Values)
                {
                    // check for type
                    if (i != null && !i.Deleted && (targetattachType == null || i.GetType().IsSubclassOf(targetattachType) || i.GetType().Equals(targetattachType)))
                    {
                        foundtypes.Add(i);
                    }
                }

                foreach (XmlAttachment xa in foundtypes)
                {
                    bool hasmap = false;
                    // check for age
                    if (criteria.Dosearchage)
                    {
                        if (!TestAge(xa, criteria.Searchage, criteria.Searchagedirection))
                        {
                            continue;
                        }
                    }

                    string es = xa.EntryString;

                    // check for name
                    if (criteria.Dosearchname)
                    {
                        if (searchname != null && (xa.Name == null || xa.Name.ToLower(Core.Culture).IndexOf(searchname) <= -1))
                        {
                            continue;
                        }
                    }
                    if (xa.AttachedTo is ISpawnable i && !i.Deleted)
                    {
                        // check for map
                        if ((i.Map == Map.Felucca && criteria.Dosearchfel) || (i.Map == Map.Trammel && criteria.Dosearchtram) ||
                            (i.Map == Map.Ilshenar && criteria.Dosearchilsh) || (i.Map == Map.Internal && criteria.Dosearchint) ||
                            (i.Map == null && criteria.Dosearchnull))
                        {
                            hasmap = true;
                        }

                        if (dungsemichiusi != null && i.Map == dungsemichiusi && criteria.DosearchdungSemiC)
                        {
                            hasmap = true;
                        }

                        if (dungchiusi != null && i.Map == dungchiusi && criteria.DosearchdungC)
                        {
                            hasmap = true;
                        }

                        if (eventi != null && i.Map == eventi && criteria.Dosearcheventi)
                        {
                            hasmap = true;
                        }

                        if (!hasmap)
                        {
                            continue;
                        }

                        // check for type for the object to who this is attached
                        if (criteria.Dosearchtype)
                        {
                            if (!i.GetType().IsSubclassOf(targetType) && !i.GetType().Equals(targetType))
                            {
                                continue;
                            }
                        }

                        // check for range for the object to who this is attached
                        if (criteria.Dosearchrange)
                        {
                            if (!TestRange(i, criteria.Searchrange, criteria.Currentmap, criteria.Currentloc))
                            {
                                continue;
                            }
                        }

                        // check for region for the object to who this is attached
                        if (criteria.Dosearchregion)
                        {
                            if (!TestRegion(i, criteria.Searchregion))
                            {
                                continue;
                            }
                        }

                        if (criteria.Dosearcherr)
                        {
                            if (i.Spawner is XmlSpawner)
                            {
                                if (((XmlSpawner)i.Spawner).status_str == null)
                                {
                                    continue;
                                }
                            }
                        }

                        // check for condition
                        if (criteria.Dosearchcondition)
                        {
                            if (criteria.Searchcondition != null)
                            {
                                // check the property test
                                if (!BaseXmlSpawner.CheckPropertyString(null, xa, criteria.Searchcondition, null, out status_str))
                                {
                                    continue;
                                }
                            }
                        }

                        if (criteria.Dosearchspawnentry && searchentry != null)
                        {
                            if (string.IsNullOrEmpty(es) || es.ToLower(Core.Culture).IndexOf(searchentry) < 0)
                            {
                                continue;
                            }
                        }
                    }
                    // passed all conditions so add it to the list
                    newarray.Add(new SearchEntry(xa));
                }
            }

            List<SearchEntry> removelist = new List<SearchEntry>();
            for (int i = 0; i < ignoreList.Count; ++i)
            {
                foreach (SearchEntry se in newarray)
                {
                    if (se.Object == ignoreList[i])
                    {
                        removelist.Add(se);
                        break;
                    }
                }
            }

            foreach (SearchEntry se in removelist)
            {
                newarray.Remove(se);
            }

            return newarray;
        }

        [Usage("XmlFind [objecttype] [range]")]
        [Description("Trova un oggetto nel mondo - al momento è possibile recuperare anche 3 ricerche dallo storico, inserendo un numero tra 1 e 3 al posto di [objecttype] e [range], dove 3 è la ricerca più vecchia, 1 la più nuova - esempio xmlfind 1 - vi mostra la ricerca nuova")]
        public static void XmlFind_OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
            {
                return;
            }

            string typename = "Xmlspawner";
            int range = -1;
            bool dorange = false;

            if (e.Arguments.Length > 0)
            {
                if (e.Arguments[0].Length == 1)
                {
                    if (int.TryParse(e.Arguments[0], out range))
                    {
                        Mobile m = e.Mobile;
                        typename = null;
                        if (range > 0 && range < 4)
                        {
                            if (!(XmlAttach.FindAttachment(m, typeof(XmlFindAttachment)) is XmlFindAttachment xf) || xf.Deleted)
                            {
                                xf = new XmlFindAttachment();
                                XmlAttach.AttachTo(m, xf);
                            }
                            XmlFindGump xg = xf.GetResult(range);
                            if (xg != null)
                            {
                                m.SendGump(xg);
                            }
                        }
                        else
                        {
                            m.SendMessage("Al momento è possibile recuperare massimo 3 ricerche dallo storico, quindi inserire un numero tra 1 e 3, dove 3 è la ricerca più vecchia, 1 la più nuova!");
                        }
                    }
                    else
                    {
                        range = -1;
                        typename = e.Arguments[0];
                    }
                }
                else
                {
                    typename = e.Arguments[0];
                }
            }

            if (typename != null)
            {
                if (e.Arguments.Length > 1)
                {
                    dorange = true;
                    try
                    {
                        range = int.Parse(e.Arguments[1]);
                    }
                    catch
                    {
                        dorange = false;
                        e.Mobile.SendMessage("Invalid range argument {0}", e.Arguments[1]);
                    }
                }

                e.Mobile.SendGump(new XmlFindGump(e.Mobile, e.Mobile.Location, e.Mobile.Map, typename, range, dorange, 0, 0));
            }
        }

        public XmlFindGump(Mobile from, Point3D startloc, Map startmap, int x, int y)
            : this(from, startloc, startmap, null, x, y)
        {
        }

        public XmlFindGump(Mobile from, Point3D startloc, Map startmap, string type, int x, int y)
            : this(from, startloc, startmap, type, -1, false, x, y)
        {
        }

        public XmlFindGump(Mobile from, Point3D startloc, Map startmap, string type, int range, bool dorange, int x, int y)
            : this(from, startloc, startmap, true, false, false,

            new SearchCriteria(
            true, // dotype
            false, // doname
            dorange, // dorange
            false, // doregion
            false, // doentry
            false, // doentrytype
            false, // docondition
            true, // dofel
            true, // dotram
            true, // doilsh
            true, // dungeon semi chiusi
            true, // dungeon chiusi
            true, // mappa eventi (green acres, etc)
            false, // doint
            false, // donull
            false, // doerr
            false, // doage
            false, // dowithattach
            false, // doattach
            false, // dohidevalid
            true, // agedirection
            0, // age
            range, // range
            null, // region
            null, // condition
            type, // type
            null, // attachtype
            null, // name
            null, // entry
            true, // search attachment or not *default true
            from//who is using this criteria
            ),

            null, -1, 0, null, null,
            false, false, false, false, false, false, false, x, y)
        {
        }

        public XmlFindGump(Mobile from, Point3D startloc, Map startmap, bool firststart, bool extension, bool descend, SearchCriteria criteria, List<SearchEntry> searchlist, int selected, int displayfrom, string savefilename,
            string commandstring, bool sorttype, bool sortname, bool sortrange, bool sortmap, bool sortselect, bool sortowner, bool selectall, int X, int Y)
            : base(X, Y)
        {
            if (from == null || from.Deleted)
            {
                return;
            }

            StartingMap = startmap;
            StartingLoc = startloc;
            m_From = from;
            if (firststart)
            {
                StartingMap = from.Map;
                StartingLoc = from.Location;
            }

            SaveFilename = savefilename;
            CommandString = commandstring;
            SelectAll = selectall;
            Sorttype = sorttype;
            Sortname = sortname;
            Sortrange = sortrange;
            Sortmap = sortmap;
            Sortselect = sortselect;
            Sortowner = sortowner;
            DisplayFrom = displayfrom;
            Selected = selected;
            m_ShowExtension = extension;
            Descendingsort = descend;

            m_SearchCriteria = criteria;

            if (m_SearchCriteria == null)
            {
                m_SearchCriteria = new SearchCriteria();
            }

            m_SearchList = searchlist;

            // prepare the page
            int height = 540;
            int y;

            AddPage(0);
            if (m_ShowExtension)
            {
                AddBackground(0, 0, 835, height, 5054);
                AddAlphaRegion(0, 0, 835, height);
            }
            else
            {
                AddBackground(0, 0, 210, height, 5054);
                AddAlphaRegion(0, 0, 210, height);
            }


            // Close button
            //AddButton( 5, 450, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0 );
            //AddLabel( 38, 450, 0x384, "Close" );

            // add the SubSearch button
            //AddButton( 90, 160, 0xFA8, 0xFAA, 4, GumpButtonType.Reply, 0 );
            //AddLabel( 128, 160, 0x384, "SubSearch" );

            // ----------------
            // SORT section
            // ----------------
            y = 5;
            // add the Sort button
            AddButton(5, y, 0xFAB, 0xFAD, 700, GumpButtonType.Reply, 0);
            AddLabel(38, y, 0x384, "Sort");

            // add the sort direction button
            if (Descendingsort)
            {
                AddButton(95, y + 3, 0x15E2, 0x15E6, 701, GumpButtonType.Reply, 0);
                AddLabel(115, y, 0x384, "descend");
            }
            else
            {
                AddButton(95, y + 3, 0x15E0, 0x15E4, 701, GumpButtonType.Reply, 0);
                AddLabel(115, y, 0x384, "ascend");
            }
            y += 22;
            // add the Sort on type toggle
            AddRadio(5, y, 0xD2, 0xD3, Sorttype, 0);
            AddLabel(28, y, 0x384, "type");

            // add the Sort on name toggle
            AddRadio(95, y, 0xD2, 0xD3, Sortname, 1);
            AddLabel(118, y, 0x384, "name");

            y += 20;
            // add the Sort on range toggle
            AddRadio(5, y, 0xD2, 0xD3, Sortrange, 2);
            AddLabel(28, y, 0x384, "range");

            // add the Sort on map toggle
            AddRadio(95, y, 0xD2, 0xD3, Sortmap, 4);
            AddLabel(118, y, 0x384, "map");

            y += 20;
            // add the Sort on selected toggle
            AddRadio(5, y, 0xD2, 0xD3, Sortselect, 5);
            AddLabel(28, y, 0x384, "select");

            // add the Sort on owner toggle
            AddRadio(95, y, 0xD2, 0xD3, Sortowner, 6);
            AddLabel(118, y, 0x384, "owner");

            // ----------------
            // SEARCH section
            // ----------------
            y = 85;
            // add the Search button
            AddButton(5, y, 0xFA8, 0xFAA, 3, GumpButtonType.Reply, 0);
            AddLabel(38, y, 0x384, "Search");

            y += 20;
            // add the map buttons
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchint, 312);
            AddLabel(28, y, 0x384, "Int");
            AddCheck(95, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchnull, 314);
            AddLabel(118, y, 0x384, "Null");

            y += 20;
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchfel, 308);
            AddLabel(28, y, 0x384, "Felucca");
            AddCheck(95, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchtram, 309);
            AddLabel(118, y, 0x384, "Trammel");

            y += 20;
            //AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchmal, 310);
            //AddLabel(28, y, 0x384, "Mal");
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchilsh, 311);
            AddLabel(28, y, 0x384, "Ilshenar");

            y += 20;
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.DosearchdungSemiC, 328);
            AddLabel(28, y, 0x384, "Dung Semi Chiusi");
            y += 20;
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.DosearchdungC, 329);
            AddLabel(28, y, 0x384, "Dung Chiusi");
            y += 20;
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearcheventi, 330);
            AddLabel(28, y, 0x384, "Eventi");
            /*y += 20;
			AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchtok, 318);
			AddLabel(28, y, 0x384, "Tok");

			AddCheck(75, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchter, 327);
			AddLabel(98, y, 0x384, "Ter");*/

            y += 20;
            // add the hide valid internal map button
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dohidevalidint, 316);
            AddLabel(28, y, 0x384, "Hide valid internal");

            // ----------------
            // FILTER section
            // ----------------
            y = height - 295;

            // add the search region entry
            AddLabel(28, y, 0x384, "region");
            AddImageTiled(70, y, 108, 19, 0xBBC);
            AddTextEntry(70, y, 250, 19, 0, 106, m_SearchCriteria.Searchregion);
            // add the toggle to enable search region
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchregion, 319);

            y += 20;
            // add the search age entry
            AddLabel(28, y, 0x384, "age");
            //AddImageTiled( 80, 220, 50, 23, 0x52 );
            AddImageTiled(70, y, 85, 19, 0xBBC);
            AddTextEntry(70, y, 85, 19, 0, 105, m_SearchCriteria.Searchage.ToString());
            AddLabel(157, y, 0x384, "Hrs");
            // add the toggle to enable search age
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchage, 303);
            // add the toggle to set the search age test direction
            AddCheck(50, y + 2, 0x1467, 0x1468, m_SearchCriteria.Searchagedirection, 302);

            y += 20;
            // add the search range entry
            AddLabel(28, y, 0x384, "range");
            AddImageTiled(70, y, 85, 19, 0xBBC);
            AddTextEntry(70, y, 85, 19, 0, 100, m_SearchCriteria.Searchrange.ToString());
            // add the toggle to enable search range
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchrange, 304);

            y += 20;
            // add the search type entry
            AddLabel(28, y, 0x384, "type");
            // add the toggle to enable search by type
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchtype, 305);
            //AddImageTiled( 5, 285, 135, 23, 0x52 );
            AddImageTiled(6, y + 20, 172, 19, 0xBBC);
            AddTextEntry(6, y + 20, 250, 19, 0, 101, m_SearchCriteria.Searchtype);

            // add the search for attachments button
            AddLabel(120, y, 0x384, "attach");
            // add the toggle to set the search with or without attachment
            AddCheck(85, y + 4, 0x938, 0x939, m_SearchCriteria.Searchattachornot, 331);
            // add the toggle to enable search by attachment
            AddCheck(97, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchwithattach, 317);

            y += 41;
            // add the search condition entry
            AddLabel(28, y, 0x384, "property test");
            // add the toggle to enable search by condition
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchcondition, 315);
            //AddImageTiled( 5, 285, 135, 23, 0x52 );
            AddImageTiled(6, y + 20, 172, 19, 0xBBC);
            AddTextEntry(6, y + 20, 500, 19, 0, 104, m_SearchCriteria.Searchcondition);

            y += 41;
            // add the search name entry
            AddLabel(28, y, 0x384, "name");
            // add the toggle to enable search by name
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchname, 306);
            //AddImageTiled( 5, 350, 135, 23, 0x52 );
            AddImageTiled(6, y + 20, 172, 19, 0xBBC);
            AddTextEntry(6, y + 20, 250, 19, 0, 102, m_SearchCriteria.Searchname);

            y += 41;
            // add the search attachment type entry
            AddLabel(28, y, 0x384, "attachment type");
            // add the toggle to enable search by attachment type
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchattach, 325);
            //AddImageTiled( 5, 285, 135, 23, 0x52 );
            AddImageTiled(6, y + 20, 172, 19, 0xBBC);
            AddTextEntry(6, y + 20, 250, 19, 0, 125, m_SearchCriteria.Searchattachtype);

            y += 41;
            // add the search spawner entries
            AddLabel(28, y, 0x384, "entry");
            // add the toggle to enable search spawner entries
            AddCheck(5, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchspawnentry, 307);

            // add the search spawner entries by type
            AddLabel(88, y, 0x384, "type");
            // add the toggle to enable search spawner entry types
            AddCheck(65, y, 0xD2, 0xD3, m_SearchCriteria.Dosearchspawntype, 326);

            //AddImageTiled( 5, 415, 135, 23, 0x52 );
            AddImageTiled(6, y + 20, 172, 19, 0xBBC);
            AddTextEntry(6, y + 20, 250, 19, 0, 103, m_SearchCriteria.Searchspawnentry);

            // add the search spawner errors
            AddLabel(140, y, 0x384, "err");
            // add the toggle to enable search spawner entries
            AddCheck(117, y, 0xD2, 0xD3, m_SearchCriteria.Dosearcherr, 313);

            // add the Show Map button
            //AddButton( 5, 450, 0xFAB, 0xFAD, 150, GumpButtonType.Reply, 0 );
            //AddLabel( 38, 450, 0x384, "Map" );

            // ----------------
            // CONTROL section
            // ----------------

            y = height - 25;
            // add the Return button
            if (m_From.AccessLevel >= XmlSpawner.DiskAccessLevel)
            {
                AddButton(72, y, 0xFAE, 0xFAF, 155, GumpButtonType.Reply, 0);
                AddLabel(105, y, 0x384, "Return");
            }

            y = height - 25;
            // add the Bring button
            if (m_From.AccessLevel >= XmlSpawner.DiskAccessLevel)
            {
                AddButton(5, y, 0xFAE, 0xFAF, 154, GumpButtonType.Reply, 0);
                AddLabel(38, y, 0x384, "Bring");
            }


            // add gump extension button
            if (m_ShowExtension)
            {
                AddButton(812, y + 5, 0x15E3, 0x15E7, 200, GumpButtonType.Reply, 0);
            }
            else
            {
                AddButton(190, y + 5, 0x15E1, 0x15E5, 200, GumpButtonType.Reply, 0);
            }

            if (m_ShowExtension)
            {
                AddLabel(178, 5, 0x384, "Gump");
                AddLabel(213, 5, 0x384, "Prop");
                AddLabel(244, 5, 0x384, "Goto");
                AddLabel(275, 5, 0x384, "Att");

                AddLabel(305, 5, 0x384, "Name");
                AddLabel(425, 5, 0x384, "Type");
                AddLabel(515, 5, 0x384, "Location");
                AddLabel(633, 5, 0x384, "Map");
                AddLabel(710, 5, 0x384, "Owner");

                if (m_From.AccessLevel >= XmlSpawner.DiskAccessLevel)
                {
                    // add the Delete button
                    AddButton(190, y, 0xFB1, 0xFB3, 156, GumpButtonType.Reply, 0);
                    AddLabel(223, height - 25, 0x384, "Delete");

                    // add the Reset button
                    AddButton(270, y, 0xFA2, 0xFA3, 157, GumpButtonType.Reply, 0);
                    AddLabel(303, y, 0x384, "Reset");

                    // add the Respawn button
                    AddButton(350, y, 0xFA8, 0xFAA, 158, GumpButtonType.Reply, 0);
                    AddLabel(383, y, 0x384, "Respawn");

                    // add the xmlsave entry
                    AddButton(190, y - 25, 0xFA8, 0xFAA, 159, GumpButtonType.Reply, 0);
                    AddLabel(223, y - 25, 0x384, "Save to file:");

                    AddImageTiled(305, y - 25, 180, 19, 0xBBC);
                    AddTextEntry(305, y - 25, 180, 19, 0, 300, SaveFilename);
                }

                // add the commandstring entry
                AddButton(510, y - 25, 0xFA8, 0xFAA, 160, GumpButtonType.Reply, 0);
                AddLabel(543, y - 25, 0x384, "Command:");

                AddImageTiled(600, y - 25, 180, 19, 0xBBC);
                AddTextEntry(600, y - 25, 180, 19, 0, 301, CommandString);


                // add the page buttons
                for (int i = 0; i < MaxEntries / MaxEntriesPerPage; ++i)
                {
                    //AddButton( 38+i*30, 365, 2206, 2206, 0, GumpButtonType.Page, 1+i );
                    AddButton(458 + i * 25, height - 25, 0x8B1 + i, 0x8B1 + i, 0, GumpButtonType.Page, 1 + i);
                }

                // add the advance pageblock buttons
                AddButton(455 + 25 * (MaxEntries / MaxEntriesPerPage), height - 25, 0x15E1, 0x15E5, 201, GumpButtonType.Reply, 0); // block forward
                AddButton(435, height - 25, 0x15E3, 0x15E7, 202, GumpButtonType.Reply, 0); // block backward

                // add the displayfrom entry
                AddLabel(500, y, 0x384, "Display");
                AddImageTiled(540, y, 60, 21, 0xBBC);
                AddTextEntry(541, y, 60, 21, 0, 400, DisplayFrom.ToString());
                AddButton(600, y, 0xFAB, 0xFAD, 9998, GumpButtonType.Reply, 0);

                // display the item list
                if (m_SearchList != null)
                {
                    AddLabel(220, y - 45, 68, string.Format("Found {0} items/mobs/land/static", m_SearchList.Count));
                    AddLabel(440, y - 45, 68, string.Format("Displaying {0}-{1}", DisplayFrom,
                        (DisplayFrom + MaxEntries < m_SearchList.Count ? DisplayFrom + MaxEntries : m_SearchList.Count)));
                    // count the number of selected objects
                    int count = 0;
                    foreach (SearchEntry e in m_SearchList)
                    {
                        if (e.Selected)
                        {
                            ++count;
                        }
                    }
                    AddLabel(640, y - 45, 33, string.Format("Selected {0}", count));
                }

                // display the select-all-displayed toggle
                AddButton(812, 5, 0xD2, 0xD3, 3999, GumpButtonType.Reply, 0);

                AddLabel(690, y, 0x384, "Select All");
                // display the select-all toggle
                AddButton(750, y, (SelectAll ? 0xD3 : 0xD2), (SelectAll ? 0xD2 : 0xD3), 3998, GumpButtonType.Reply, 0);

                for (int i = 0; i < MaxEntries; ++i)
                {
                    int index = i + DisplayFrom;
                    if (m_SearchList == null || index >= m_SearchList.Count)
                    {
                        break;
                    }

                    SearchEntry e = m_SearchList[index];

                    int page = i / MaxEntriesPerPage;

                    if (i % MaxEntriesPerPage == 0)
                    {
                        AddPage(page + 1);
                        // add highlighted page button
                        //AddImageTiled( 235+page*25, 448, 25, 25, 0xBBC );
                        //AddImage( 238+page*25, 450, 0x8B1+page );
                    }

                    // background for search results area
                    //AddImageTiled(295, 22 * (i % MaxEntriesPerPage) + 30, 516, 23, 0x52);
                    //AddImageTiled(296, 22 * (i % MaxEntriesPerPage) + 31, 514, 21, 0xBBC);

                    object o = e.Object;

                    // add the Goto button for each entry
                    AddButton(246, 22 * (i % MaxEntriesPerPage) + 30, 0xFAE, 0xFAF, 1000 + i, GumpButtonType.Reply, 0);

                    // add the Gump button for spawner entries
                    if (o is ISpawnObjectFinderList)// || o is Spawner)
                    {
                        AddButton(182, 22 * (i % MaxEntriesPerPage) + 30, 0xFBD, 0xFBE, 2000 + i, GumpButtonType.Reply, 0);
                    }

                    // add the Props button for each entry
                    AddButton(214, 22 * (i % MaxEntriesPerPage) + 30, 0xFAB, 0xFAD, 3000 + i, GumpButtonType.Reply, 0);

                    string namestr = null;
                    string typestr = null;
                    string locstr = null;
                    string mapstr = null;
                    string ownstr = null;
                    int texthue = 0;

                    if (o is Item item)
                    {
                        // change the color if it is in a container
                        namestr = item.Name;
                        string str = item.GetType().ToString();
                        if (str != null)
                        {
                            string[] arglist = str.Split('.');
                            typestr = arglist[arglist.Length - 1];
                        }
                        // check for in container
                        // if so then display parent loc
                        // change the color for container held items
                        if (item.Parent != null)
                        {
                            if (item.RootParent is Mobile m)
                            {
                                if (m.Player)
                                {
                                    texthue = 44;
                                }
                                else
                                {
                                    texthue = 24;
                                }

                                locstr = m.Location.ToString();
                                ownstr = m.GetRawNameFor(m_From);
                            }
                            else if (item.RootParent is Container c)
                            {
                                texthue = 5;
                                locstr = c.Location.ToString();
                                if (c.Name != null)
                                {
                                    ownstr = c.Name;
                                }
                                else
                                {
                                    ownstr = c.ItemData.Name;
                                }
                            }

                        }
                        else
                        {
                            locstr = item.Location.ToString();
                        }

                        if (item.Deleted)
                        {
                            mapstr = "Deleted";
                        }
                        else
                            if (item.Map != null)
                        {
                            mapstr = item.Map.ToString();
                        }
                    }
                    else if (o is Mobile mob)
                    {
                        // change the color if it is in a container
                        namestr = mob.GetRawNameFor(m_From);//.Name;
                        string str = mob.GetType().ToString();
                        if (str != null)
                        {
                            string[] arglist = str.Split('.');
                            typestr = arglist[arglist.Length - 1];
                        }
                        locstr = mob.Location.ToString();
                        if (mob.Deleted)
                        {
                            mapstr = "Deleted";
                        }
                        else
                            if (mob.Map != null)
                        {
                            mapstr = mob.Map.ToString();
                        }
                    }
                    else if (o is XmlAttachment a)
                    {
                        // change the color
                        namestr = a.Name;

                        string str = a.GetType().ToString();
                        if (a.Owner is Mobile m)
                        {
                            ownstr = m.GetRawNameFor(m_From);
                            if (m.Player)
                            {
                                texthue = 44;
                            }
                            else
                            {
                                texthue = 24;
                            }
                        }
                        else if (a.Owner is Item)
                        {
                            item = (Item)a.Owner;
                            texthue = 50;
                            if (item.Name != null)
                            {
                                ownstr = item.Name;
                            }
                            else
                            {
                                ownstr = item.ItemData.Name;
                            }
                        }
                        if (str != null)
                        {
                            string[] arglist = str.Split('.');
                            typestr = arglist[arglist.Length - 1];
                        }
                        if (a.AttachedTo is Mobile)
                        {
                            m = (Mobile)a.AttachedTo;
                            locstr = m.Location.ToString();
                            if (m.Map != null)
                            {
                                mapstr = m.Map.ToString();
                            }
                        }
                        if (a.AttachedTo is Item)
                        {
                            item = (Item)a.AttachedTo;
                            if (item.Map != null)
                            {
                                mapstr = item.Map.ToString();
                            }

                            if (item.Parent != null)
                            {
                                if (item.RootParent is Mobile mobile)
                                {
                                    locstr = mobile.Location.ToString();

                                }
                                else
                                    if (item.RootParent is Item itm)
                                {
                                    locstr = itm.Location.ToString();
                                }
                            }
                            else
                            {
                                locstr = item.Location.ToString();
                            }
                        }
                        if (a.Deleted)
                        {
                            mapstr = "Deleted";
                        }
                    }
                    else if (o is Targeting.StaticTarget st)
                    {
                        namestr = st.Name;
                        string str = StaticTarget.ToString();
                        if (str != null)
                        {
                            string[] arglist = str.Split('.');
                            typestr = arglist[arglist.Length - 1];
                        }
                        locstr = st.Location.ToString();
                        mapstr = criteria.Currentmap.ToString();
                    }
                    else if (o is Targeting.LandTarget lt)
                    {
                        namestr = lt.Name;
                        string str = LandTarget.ToString();
                        if (str != null)
                        {
                            string[] arglist = str.Split('.');
                            typestr = arglist[arglist.Length - 1];
                        }
                        locstr = lt.Location.ToString();
                        mapstr = criteria.Currentmap.ToString();
                    }

                    if (e.Selected)
                    {
                        texthue = 33;
                    }

                    if (i == Selected)
                    {
                        texthue = 68;
                    }

                    bool att = XmlAttach.HasAttachments(o as IEntity);
                    // display the name
                    AddImageTiled(275, 22 * (i % MaxEntriesPerPage) + 31, 135, 21, 0xBBC);
                    //AddLabelCropped(295, 22 * (i % MaxEntriesPerPage) + 31, 120, 21, texthue, namestr);
                    AddLabel(att ? 285 : 276, 22 * (i % MaxEntriesPerPage) + 31, texthue, namestr);

                    // display the attachment button if it has attachments
                    if (att)
                    {
                        AddButton(275, 22 * (i % MaxEntriesPerPage) + 35, 2103, 2103, 5000 + i, GumpButtonType.Reply, 0);
                    }

                    // display the type
                    AddImageTiled(411, 22 * (i % MaxEntriesPerPage) + 31, 93, 21, 0xBBC);
                    //AddLabelCropped(419, 22 * (i % MaxEntriesPerPage) + 31, 91, 21, texthue, typestr);
                    AddLabel(412, 22 * (i % MaxEntriesPerPage) + 31, texthue, typestr);
                    // display the loc
                    AddImageTiled(505, 22 * (i % MaxEntriesPerPage) + 31, 114, 21, 0xBBC);
                    AddLabel(506, 22 * (i % MaxEntriesPerPage) + 31, texthue, locstr);
                    // display the map
                    AddImageTiled(620, 22 * (i % MaxEntriesPerPage) + 31, 74, 21, 0xBBC);
                    AddLabel(621, 22 * (i % MaxEntriesPerPage) + 31, texthue, mapstr);
                    // display the owner
                    AddImageTiled(695, 22 * (i % MaxEntriesPerPage) + 31, 115, 21, 0xBBC);
                    AddLabelCropped(696, 22 * (i % MaxEntriesPerPage) + 31, 112, 21, texthue, ownstr);

                    // display the selection button

                    AddButton(812, 22 * (i % MaxEntriesPerPage) + 32, (e.Selected ? 0xD3 : 0xD2), (e.Selected ? 0xD2 : 0xD3), 4000 + i, GumpButtonType.Reply, 0);

                }
            }
        }

        private void DoGoTo(int index)
        {
            if (m_From == null || m_From.Deleted)
            {
                return;
            }

            if (m_SearchList != null && index < m_SearchList.Count)
            {
                object o = m_SearchList[index].Object;
                if (o is Item item)
                {
                    CommandHandlers.GoToItem(m_From, item, true);
                    //					Point3D itemloc;
                    //					if (item.Parent != null)
                    //					{
                    //						if (item.RootParent is Mobile)
                    //						{
                    //							itemloc = ((Mobile)(item.RootParent)).Location;
                    //						}
                    //						else
                    //							if (item.RootParent is Container)
                    //							{
                    //								itemloc = ((Container)(item.RootParent)).Location;
                    //							}
                    //							else
                    //							{
                    //								return;
                    //							}
                    //					}
                    //					else
                    //					{
                    //						itemloc = item.Location;
                    //					}
                    //					if (item == null || item.Deleted || item.Map == null || item.Map == Map.Internal) return;
                    //					m_From.Location = itemloc;
                    //					m_From.Map = item.Map;
                }
                else if (o is Mobile mob)
                {
                    CommandHandlers.GoToMobile(m_From, mob, true);
                    //						if (mob == null || mob.Deleted || mob.Map == null || mob.Map == Map.Internal)) return;
                    //						m_From.Map = mob.Map;
                    //						m_From.Location = mob.Location;
                }
                else if (o is XmlAttachment a)
                {
                    if (a == null || a.Deleted)
                    {
                        return;
                    }

                    if (a.AttachedTo is Mobile amob)
                    {
                        CommandHandlers.GoToMobile(m_From, amob, true);
                        //								if (mob == null || mob.Deleted || mob.Map == null || mob.Map == Map.Internal) return;
                        //								m_From.Location = mob.Location;
                        //								m_From.Map = mob.Map;
                    }
                    else if (a.AttachedTo is Item aitem)
                    {
                        CommandHandlers.GoToItem(m_From, aitem, true);
                        //									Point3D itemloc;
                        //									if (item.Parent != null)
                        //									{
                        //										if (item.RootParent is Mobile)
                        //										{
                        //											itemloc = ((Mobile)(item.RootParent)).Location;
                        //										}
                        //										else
                        //											if (item.RootParent is Container)
                        //											{
                        //												itemloc = ((Container)(item.RootParent)).Location;
                        //											}
                        //											else
                        //											{
                        //												return;
                        //											}
                        //									}
                        //									else
                        //									{
                        //										itemloc = item.Location;
                        //									}
                        //									if (item == null || item.Deleted || item.Map == null || item.Map == Map.Internal) return;
                        //									m_From.Location = itemloc;
                        //									m_From.Map = item.Map;
                    }
                }
                else if(o is Targeting.StaticTarget st)
                {
                    CommandHandlers.GoToLocation(m_From, new Point3D(st.Location.X, st.Location.Y, st.TrueZ), m_SearchCriteria.Currentmap, true);
                }
                else if (o is Targeting.LandTarget lt)
                {
                    CommandHandlers.GoToLocation(m_From, lt.Location, m_SearchCriteria.Currentmap, true);
                }
            }
        }

        private void DoShowGump(int index)
        {
            if (m_From == null || m_From.Deleted)
            {
                return;
            }

            if (m_SearchList != null && index < m_SearchList.Count)
            {
                object o = m_SearchList[index].Object;
                if (o is ISpawnObjectFinderList x && o is IEntity ie)
                {
                    // dont open anything with a null map null item or deleted
                    if (x == null || ie.Deleted || ie.Map == null || ie.Map == Map.Internal)
                    {
                        return;
                    }

                    x.ISpawnObjectDoGump(m_From);
                }
            }
        }

        private void DoShowProps(int index)
        {
            if (m_From == null || m_From.Deleted)
            {
                return;
            }

            if (m_SearchList != null && index < m_SearchList.Count)
            {
                object o = m_SearchList[index].Object;
                if (o is Item it)
                {
                    if (it == null || it.Deleted || !it.CanTarget /*|| x.Map == null*/)
                    {
                        return;
                    }

                    m_From.SendGump(new PropertiesGump(m_From, o));
                }
                else if (o is Mobile mob)
                {
                    if (mob == null || mob.Deleted /*|| x.Map == null*/)
                    {
                        return;
                    }

                    m_From.SendGump(new PropertiesGump(m_From, o));
                }
                else if (o is XmlAttachment x)
                {
                    if (x == null || x.Deleted /*|| x.Map == null*/)
                    {
                        return;
                    }

                    m_From.SendGump(new PropertiesGump(m_From, o));
                }
                else if(o is Targeting.StaticTarget || o is Targeting.LandTarget)
                {
                    m_From.SendGump(new PropertiesGump(m_From, o));
                }
            }
        }

        private void SortFindList()
        {
            if (m_SearchList != null && m_SearchList.Count > 0)
            {
                if (Sorttype)
                {
                    m_SearchList.Sort(new ListTypeSorter(Descendingsort));
                }
                else if (Sortname)
                {
                    m_SearchList.Sort(new ListNameSorter(Descendingsort));
                }
                else if (Sortmap)
                {
                    m_SearchList.Sort(new ListMapSorter(Descendingsort));
                }
                else if (Sortrange)
                {
                    m_SearchList.Sort(new ListRangeSorter(m_From, Descendingsort));
                }
                else if (Sortselect)
                {
                    m_SearchList.Sort(new ListSelectSorter(m_From, Descendingsort));
                }
                else if (Sortowner)
                {
                    m_SearchList.Sort(new ListOwnerSorter(Descendingsort));
                }
            }
        }

        private class ListTypeSorter : IComparer<SearchEntry>
        {
            private bool Dsort;

            public ListTypeSorter(bool descend)
                : base()
            {
                Dsort = descend;
            }

            public int Compare(SearchEntry e1, SearchEntry e2)
            {
                object x = e1.Object;
                object y = e2.Object;

                string xstr = null;
                string ystr = null;
                string str = null;
                if (x is Item)
                {
                    str = ((Item)x).GetType().ToString();
                }
                else if (x is Mobile)
                {
                    str = ((Mobile)x).GetType().ToString();
                }
                if (str != null)
                {
                    string[] arglist = str.Split('.');
                    xstr = arglist[arglist.Length - 1];
                }

                str = null;
                if (y is Item)
                {
                    str = ((Item)y).GetType().ToString();
                }
                else if (y is Mobile)
                {
                    str = ((Mobile)y).GetType().ToString();
                }
                if (str != null)
                {
                    string[] arglist = str.Split('.');
                    ystr = arglist[arglist.Length - 1];
                }
                if (Dsort)
                {
                    return string.Compare(ystr, xstr, true);
                }
                else
                {
                    return string.Compare(xstr, ystr, true);
                }
            }
        }

        private class ListNameSorter : IComparer<SearchEntry>
        {
            private bool Dsort;

            public ListNameSorter(bool descend)
                : base()
            {
                Dsort = descend;
            }

            public int Compare(SearchEntry e1, SearchEntry e2)
            {
                object x = e1.Object;
                object y = e2.Object;

                string xstr = null;
                string ystr = null;

                if (x is Item)
                {
                    xstr = ((Item)x).Name;
                }
                else if (x is Mobile)
                {
                    xstr = ((Mobile)x).RawName;
                }

                if (y is Item)
                {
                    ystr = ((Item)y).Name;
                }
                else if (y is Mobile)
                {
                    ystr = ((Mobile)y).RawName;
                }
                if (Dsort)
                {
                    return string.Compare(ystr, xstr, true);
                }
                else
                {
                    return string.Compare(xstr, ystr, true);
                }
            }
        }

        private class ListMapSorter : IComparer<SearchEntry>
        {
            private bool Dsort;

            public ListMapSorter(bool descend)
                : base()
            {
                Dsort = descend;
            }

            public int Compare(SearchEntry e1, SearchEntry e2)
            {
                object x = e1.Object;
                object y = e2.Object;

                string xstr = null;
                string ystr = null;

                if (x is Item)
                {
                    if (((Item)x).Map != null)
                    {
                        xstr = ((Item)x).Map.ToString();
                    }
                }
                else if (x is Mobile)
                {
                    if (((Mobile)x).Map != null)
                    {
                        xstr = ((Mobile)x).Map.ToString();
                    }
                }

                if (y is Item)
                {
                    if (((Item)y).Map != null)
                    {
                        ystr = ((Item)y).Map.ToString();
                    }
                }
                else if (y is Mobile)
                {
                    if (((Mobile)y).Map != null)
                    {
                        ystr = ((Mobile)y).Map.ToString();
                    }
                }
                if (Dsort)
                {
                    return string.Compare(ystr, xstr, true);
                }
                else
                {
                    return string.Compare(xstr, ystr, true);
                }
            }
        }

        private class ListRangeSorter : IComparer<SearchEntry>
        {
            private Mobile From;
            private bool Dsort;

            public ListRangeSorter(Mobile from, bool descend)
                : base()
            {
                From = from;
                Dsort = descend;
            }

            public int Compare(SearchEntry e1, SearchEntry e2)
            {
                object x = e1.Object;
                object y = e2.Object;

                Map xmap = null;
                Map ymap = null;
                Point3D xloc = new Point3D(0, 0, 0);
                Point3D yloc = new Point3D(0, 0, 0);

                if (From == null || From.Deleted)
                {
                    return 0;
                }

                if (x is Item)
                {
                    xmap = ((Item)x).Map;
                    xloc = ((Item)x).Location;
                }
                else if (x is Mobile)
                {
                    xmap = ((Mobile)x).Map;
                    xloc = ((Mobile)x).Location;
                }

                if (y is Item)
                {
                    ymap = ((Item)y).Map;
                    yloc = ((Item)y).Location;
                }
                else if (y is Mobile)
                {
                    ymap = ((Mobile)y).Map;
                    yloc = ((Mobile)y).Location;
                }

                if (xmap != From.Map && ymap != From.Map)
                {
                    return 0;
                }

                if (Dsort)
                {
                    if (xmap == From.Map && ymap != From.Map)
                    {
                        return 1;
                    }

                    if (xmap != From.Map && ymap == From.Map)
                    {
                        return -1;
                    }

                    return From.GetDistanceToSqrt(yloc).CompareTo(From.GetDistanceToSqrt(xloc));
                }
                else
                {
                    if (xmap == From.Map && ymap != From.Map)
                    {
                        return -1;
                    }

                    if (xmap != From.Map && ymap == From.Map)
                    {
                        return 1;
                    }

                    return From.GetDistanceToSqrt(xloc).CompareTo(From.GetDistanceToSqrt(yloc));
                }
            }
        }

        private class ListSelectSorter : IComparer<SearchEntry>
        {
            private Mobile From;
            private bool Dsort;

            public ListSelectSorter(Mobile from, bool descend)
                : base()
            {
                From = from;
                Dsort = descend;
            }

            public int Compare(SearchEntry e1, SearchEntry e2)
            {
                int x = e1.Selected ? 1 : 0;
                int y = e2.Selected ? 1 : 0;

                if (Dsort)
                {
                    return x - y;
                }
                else
                {
                    return y - x;
                }
            }
        }

        private class ListOwnerSorter : IComparer<SearchEntry>
        {
            private bool Dsort;

            public ListOwnerSorter(bool descend)
                : base()
            {
                Dsort = descend;
            }

            public int Compare(SearchEntry e1, SearchEntry e2)
            {
                object x = e1.Object;
                object y = e2.Object;

                string xstr = null;
                string ystr = null;

                if (x is Item xit)
                {
                    if (xit.RootParent is Item rp)
                    {
                        xstr = rp.Name;
                    }
                    else if (xit.RootParent is Mobile mrp)
                    {
                        xstr = mrp.RawName;
                    }
                }

                if (y is Item yit)
                {
                    if (yit.RootParent is Item rp)
                    {
                        ystr = rp.Name;
                    }
                    else if (yit.RootParent is Mobile mrp)
                    {
                        ystr = mrp.RawName;
                    }
                }

                if (Dsort)
                {
                    return string.Compare(ystr, xstr, true);
                }
                else
                {
                    return string.Compare(xstr, ystr, true);
                }
            }
        }

        private void Refresh(NetState state)
        {
            state.Mobile.SendGump(new XmlFindGump(m_From, StartingLoc, StartingMap, false, m_ShowExtension, Descendingsort, m_SearchCriteria, m_SearchList, Selected, DisplayFrom, SaveFilename,
                CommandString, Sorttype, Sortname, Sortrange,
                Sortmap, Sortselect, Sortowner, SelectAll, X, Y));
        }

        private void ResetList()
        {
            if (m_SearchList == null)
            {
                return;
            }

            for (int i = 0; i < m_SearchList.Count; ++i)
            {
                SearchEntry e = m_SearchList[i];

                if (e.Selected)
                {
                    object o = e.Object;

                    if (o is XmlSpawner)
                    {
                        ((XmlSpawner)o).DoReset = true;
                    }
                }
            }
        }

        private void RespawnList()
        {
            if (m_SearchList == null)
            {
                return;
            }

            for (int i = 0; i < m_SearchList.Count; ++i)
            {
                SearchEntry e = m_SearchList[i];

                if (e.Selected)
                {
                    object o = e.Object;

                    if (o is XmlSpawner)
                    {
                        ((XmlSpawner)o).DoRespawn = true;
                    }
                }
            }
        }

        private void SaveList(Mobile from, string filename)
        {
            if (m_SearchList == null)
            {
                return;
            }

            string dirname;
            if (System.IO.Directory.Exists(XmlSpawner.XmlSpawnDir) && filename != null && !filename.StartsWith("/") && !filename.StartsWith("\\"))
            {
                // put it in the defaults directory if it exists
                dirname = string.Format("{0}/{1}", XmlSpawner.XmlSpawnDir, filename);
            }
            else
            {
                // otherwise just put it in the main installation dir
                dirname = filename;
            }


            List<XmlSpawner> savelist = new List<XmlSpawner>();

            for (int i = 0; i < m_SearchList.Count; ++i)
            {
                SearchEntry e = m_SearchList[i];

                if (e.Selected)
                {
                    object o = e.Object;

                    if (o is XmlSpawner)
                    {
                        // add it to the saves list
                        savelist.Add((XmlSpawner)o);
                    }
                }
            }

            // write out the spawners to a file
            XmlSpawner.SaveSpawnList(from, savelist, dirname, false, true);
        }

        private void ExecuteCommand(Mobile from, string text)
        {
            if (m_SearchList == null)
            {
                return;
            }

            List<object> executelist = new List<object>();

            for (int i = 0; i < m_SearchList.Count; ++i)
            {
                SearchEntry e = m_SearchList[i];

                if (e.Selected)
                {
                    object o = e.Object;

                    // add it to the execute list
                    executelist.Add(o);
                }
            }

            // lookup the command
            // and execute it
            if (text != null && text.Length > 0)
            {
                int indexOf = text.IndexOf(' ');

                string command;
                string[] args;
                string argString;

                if (indexOf >= 0)
                {
                    argString = text.Substring(indexOf + 1);

                    command = text.Substring(0, indexOf);
                    args = CommandSystem.Split(argString);
                }
                else
                {
                    argString = "";
                    command = text.ToLower(Core.Culture);
                    args = new string[0];
                }

                if (args != null && args.Length >= 1)
                {
                    /*string[] cargs = new string[args.Length - 1];
					for (int i = 0; i < args.Length - 1; ++i)
						cargs[i] = args[i + 1];*/

                    CommandEventArgs e = new CommandEventArgs(from, command, argString, args);

                    foreach (BaseCommand c in TargetCommands.AllCommands)
                    {
                        // find the matching command
                        for (int i = 0; i < c.Commands.Length; ++i)
                        {
                            if (c.Commands[i].ToLower(Core.Culture) == command.ToLower(Core.Culture))
                            {
                                bool flushToLog = false;

                                // execute the command on the objects in the list

                                if (executelist.Count > 20)
                                {
                                    CommandLogging.Enabled = false;
                                }

                                c.ExecuteList(e, executelist);

                                if (executelist.Count > 20)
                                {
                                    flushToLog = true;
                                    CommandLogging.Enabled = true;
                                }

                                c.Flush(from, flushToLog);
                                return;
                            }
                        }
                    }
                    from.SendMessage("Invalid command: {0}", args[0]);
                }
                else
                {
                    from.SendMessage("Uso: comando proprietà [valore]");
                }
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info == null || state == null || state.Mobile == null || m_SearchCriteria == null)
            {
                return;
            }

            int radiostate = -1;
            if (info.Switches.Length > 0)
            {
                radiostate = info.Switches[0];
            }

            // read the text entries for the search criteria
            TextRelay tr = info.GetTextEntry(105);        // range info
            if (tr != null && !string.IsNullOrEmpty(tr.Text))
            {
                Utility.ToDouble(tr.Text);
            }
            else
            {
                m_SearchCriteria.Searchage = 0;
            }

            // read the text entries for the search criteria
            tr = info.GetTextEntry(100);        // range info
            if (tr != null && !string.IsNullOrEmpty(tr.Text))
            {
                m_SearchCriteria.Searchrange = Utility.ToInt32(tr.Text, -1);
            }
            else
            {
                m_SearchCriteria.Searchrange = -1;
            }

            tr = info.GetTextEntry(101);        // type info
            if (tr != null)
            {
                m_SearchCriteria.Searchtype = tr.Text;
            }

            tr = info.GetTextEntry(102);        // name info
            if (tr != null)
            {
                m_SearchCriteria.Searchname = tr.Text;
            }

            tr = info.GetTextEntry(125);        // attachment type info
            if (tr != null)
            {
                m_SearchCriteria.Searchattachtype = tr.Text;
            }

            tr = info.GetTextEntry(103);        // entry info
            if (tr != null)
            {
                m_SearchCriteria.Searchspawnentry = tr.Text;
            }

            tr = info.GetTextEntry(104);        // condition info
            if (tr != null)
            {
                m_SearchCriteria.Searchcondition = tr.Text;
            }

            tr = info.GetTextEntry(106);        // region info
            if (tr != null)
            {
                m_SearchCriteria.Searchregion = tr.Text;
            }

            tr = info.GetTextEntry(400);        // displayfrom info
            if(tr != null && !string.IsNullOrEmpty(tr.Text))
            {
                DisplayFrom = Utility.ToInt32(tr.Text);
            }

            tr = info.GetTextEntry(300);        // savefilename info
            if (tr != null)
            {
                SaveFilename = tr.Text;
            }

            tr = info.GetTextEntry(301);        // commandstring info
            if (tr != null)
            {
                CommandString = tr.Text;
            }


            // check all of the check boxes
            m_SearchCriteria.Searchagedirection = info.IsSwitched(302);
            m_SearchCriteria.Dosearchage = info.IsSwitched(303);
            m_SearchCriteria.Dosearchrange = info.IsSwitched(304);
            m_SearchCriteria.Dosearchtype = info.IsSwitched(305);
            m_SearchCriteria.Dosearchname = info.IsSwitched(306);
            m_SearchCriteria.Dosearchspawnentry = info.IsSwitched(307);
            m_SearchCriteria.Dosearchspawntype = info.IsSwitched(326);
            m_SearchCriteria.Dosearcherr = info.IsSwitched(313);
            m_SearchCriteria.Dosearchcondition = info.IsSwitched(315);

            m_SearchCriteria.Dosearchint = info.IsSwitched(312);
            m_SearchCriteria.Dosearchfel = info.IsSwitched(308);
            m_SearchCriteria.Dosearchtram = info.IsSwitched(309);
            //m_SearchCriteria.Dosearchmal = info.IsSwitched(310);
            m_SearchCriteria.Dosearchilsh = info.IsSwitched(311);
            /*m_SearchCriteria.Dosearchtok = info.IsSwitched(318);
			m_SearchCriteria.Dosearchter = info.IsSwitched(327);*/
            m_SearchCriteria.DosearchdungSemiC = info.IsSwitched(328);
            m_SearchCriteria.DosearchdungC = info.IsSwitched(329);
            m_SearchCriteria.Dosearcheventi = info.IsSwitched(330);
            m_SearchCriteria.Dosearchnull = info.IsSwitched(314);

            m_SearchCriteria.Dohidevalidint = info.IsSwitched(316);
            m_SearchCriteria.Dosearchwithattach = info.IsSwitched(317);
            m_SearchCriteria.Dosearchattach = info.IsSwitched(325);
            m_SearchCriteria.Searchattachornot = info.IsSwitched(331);
            m_SearchCriteria.Dosearchregion = info.IsSwitched(319);

            switch (info.ButtonID)
            {

                case 0: // Close
                {
                    return;
                }
                case 3: // Search
                {
                    // clear any selection
                    Selected = -1;

                    // reset displayfrom
                    DisplayFrom = 0;

                    // do the search
                    m_SearchCriteria.Currentloc = state.Mobile.Location;
                    m_SearchCriteria.Currentmap = state.Mobile.Map;

                    //IWorkItemResult work = SmartThread.QueueWorkItem(new XmlFindThread(state.Mobile, m_SearchCriteria, CommandString, ilist, mlist).XmlFindThreadMain);
                    XmlFindThread tobj = new XmlFindThread(state.Mobile, m_SearchCriteria, CommandString);
                    Thread find = new Thread(new ThreadStart(tobj.XmlFindThreadMain))
                    {
                        Name = "XmlFind Thread",
                        Priority = ThreadPriority.BelowNormal
                    };
                    m_XmlGenericThreads.Add(find);
                    find.Start();

                    // turn on gump extension
                    m_ShowExtension = true;
                    return;
                }
                /*case 4: // SubSearch
					{
						Console.WriteLine("Is this SEARCH really used? subsearch CASE in XMLFIND");
						// do the search
						string status_str="subsearch used, but empty";
						//m_SearchList = Search(m_SearchCriteria, out status_str);
						break;
					}*/
                case 150: // Open the map gump
                {
                    break;
                }
                case 154: // Bring all selected objects to the current location
                {
                    if (state.Mobile.AccessLevel < XmlSpawner.DiskAccessLevel)
                    {
                        state.Mobile.SendMessage("Your accesslevel is not sufficient to use this functionality");
                        return;
                    }
                    Refresh(state);

                    state.Mobile.SendGump(new XmlConfirmBringGump(state.Mobile, m_SearchList));
                    return;
                }
                case 155: // Return the player to the starting loc
                {
                    if (state.Mobile.AccessLevel < XmlSpawner.DiskAccessLevel)
                    {
                        state.Mobile.SendMessage("Your accesslevel is not sufficient to use this functionality");
                        return;
                    }
                    m_From.Location = StartingLoc;
                    m_From.Map = StartingMap;
                    break;
                }
                case 156: // Delete selected items
                {
                    if (state.Mobile.AccessLevel < XmlSpawner.DiskAccessLevel)
                    {
                        state.Mobile.SendMessage("Your accesslevel is not sufficient to use this functionality");
                        return;
                    }
                    Refresh(state);

                    state.Mobile.SendGump(new XmlConfirmDeleteGump(state.Mobile, m_SearchList));
                    return;
                }
                case 157: // Reset selected items
                {
                    if (state.Mobile.AccessLevel < XmlSpawner.DiskAccessLevel)
                    {
                        state.Mobile.SendMessage("Your accesslevel is not sufficient to use this functionality");
                        return;
                    }
                    ResetList();
                    break;
                }
                case 158: // Respawn selected items
                {
                    if (state.Mobile.AccessLevel < XmlSpawner.DiskAccessLevel)
                    {
                        state.Mobile.SendMessage("Your accesslevel is not sufficient to use this functionality");
                        return;
                    }
                    RespawnList();
                    break;
                }
                case 159: // xmlsave selected spawners
                {
                    if (state.Mobile.AccessLevel < XmlSpawner.DiskAccessLevel)
                    {
                        state.Mobile.SendMessage("Your accesslevel is not sufficient to use this functionality");
                        return;
                    }
                    SaveList(state.Mobile, SaveFilename);
                    break;
                }
                case 160: // execute the command on the selected items
                {
                    ExecuteCommand(state.Mobile, CommandString);
                    break;
                }
                case 200: // gump extension
                {
                    m_ShowExtension = !m_ShowExtension;
                    break;
                }
                case 201: // forward block
                {
                    if (m_SearchList != null && DisplayFrom + MaxEntries < m_SearchList.Count)
                    {
                        DisplayFrom += MaxEntries;
                        // clear any selection
                        Selected = -1;
                    }
                    break;
                }
                case 202: // backward block
                {

                    DisplayFrom -= MaxEntries;
                    if (DisplayFrom < 0)
                    {
                        DisplayFrom = 0;
                    }
                    // clear any selection
                    Selected = -1;
                    break;
                }

                case 700: // Sort
                {
                    // clear any selection
                    Selected = -1;

                    Sorttype = false;
                    Sortname = false;
                    Sortrange = false;
                    Sortmap = false;
                    Sortselect = false;
                    Sortowner = false;
                    // read the toggle switches that determine the sort
                    if (radiostate == 0) // sort by type
                    {
                        Sorttype = true;
                    }
                    else if (radiostate == 1) // sort by name
                    {
                        Sortname = true;
                    }
                    else if (radiostate == 2) // sort by range
                    {
                        Sortrange = true;
                    }
                    else if (radiostate == 4) // sort by entry
                    {
                        Sortmap = true;
                    }
                    else if (radiostate == 5) // sort by selected
                    {
                        Sortselect = true;
                    }
                    else if (radiostate == 6)
                    {
                        Sortowner = true;
                    }

                    SortFindList();
                    break;
                }
                case 701: // descending sort
                {
                    Descendingsort = !Descendingsort;
                    break;
                }
                case 9998:  // refresh the gump
                {
                    // clear any selection
                    Selected = -1;
                    break;
                }
                default:
                {

                    if (info.ButtonID >= 1000 && info.ButtonID < 1000 + MaxEntries)
                    {
                        // flag the entry selected
                        Selected = info.ButtonID - 1000;
                        // then go to it
                        DoGoTo(info.ButtonID - 1000 + DisplayFrom);
                    }
                    if (info.ButtonID >= 2000 && info.ButtonID < 2000 + MaxEntries)
                    {
                        // flag the entry selected
                        Selected = info.ButtonID - 2000;
                        // then open the gump
                        Refresh(state);
                        DoShowGump(info.ButtonID - 2000 + DisplayFrom);
                        return;
                    }
                    if (info.ButtonID >= 3000 && info.ButtonID < 3000 + MaxEntries)
                    {
                        Selected = info.ButtonID - 3000;
                        // Show the props window
                        Refresh(state);
                        DoShowProps(info.ButtonID - 3000 + DisplayFrom);
                        return;
                    }
                    else
                        if (info.ButtonID == 3998)
                    {
                        SelectAll = !SelectAll;

                        if (m_SearchList != null)
                        {
                            foreach (SearchEntry e in m_SearchList)
                            {
                                e.Selected = SelectAll;
                            }
                        }
                    }
                    if (info.ButtonID == 3999)
                    {

                        // toggle selection of everything currently displayed
                        if (m_SearchList != null)
                        {
                            for (int i = 0; i < MaxEntries; ++i)
                            {
                                if (i + DisplayFrom < m_SearchList.Count)
                                {
                                    SearchEntry e = m_SearchList[i + DisplayFrom];

                                    e.Selected = !e.Selected;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                    if (info.ButtonID >= 4000 && info.ButtonID < 4000 + MaxEntries)
                    {
                        int i = info.ButtonID - 4000;

                        if (m_SearchList != null && i >= 0 && m_SearchList.Count > i + DisplayFrom)
                        {
                            SearchEntry e = m_SearchList[i + DisplayFrom];

                            e.Selected = !e.Selected;
                        }
                    }
                    if (info.ButtonID >= 5000 && info.ButtonID < 5000 + MaxEntries)
                    {
                        int i = info.ButtonID - 5000;

                        if (m_SearchList != null && i >= 0 && m_SearchList.Count > i + DisplayFrom)
                        {
                            SearchEntry e = m_SearchList[i + DisplayFrom];

                            state.Mobile.CloseGump(typeof(XmlGetAttGump));
                            state.Mobile.SendGump(new XmlGetAttGump(state.Mobile, e.Object as IEntity, 10, 10));
                        }
                    }
                    break;
                }
            }
            // Create a new gump
            //m_Spawner.OnDoubleClick( state.Mobile);
            Refresh(state);
        }

        public class XmlConfirmBringGump : Gump
        {
            private List<SearchEntry> SearchList;

            private Mobile From;

            public XmlConfirmBringGump(Mobile from, List<SearchEntry> searchlist)
                : base(0, 0)
            {
                SearchList = searchlist;

                From = from;
                Closable = false;
                Dragable = true;
                AddPage(0);
                AddBackground(10, 200, 200, 130, 5054);
                int count = 0;

                if (SearchList != null)
                {
                    for (int i = 0; i < SearchList.Count; ++i)
                    {
                        if (SearchList[i].Selected)
                        {
                            ++count;
                        }
                    }
                }

                AddLabel(20, 225, 33, string.Format("Bring {0} objects to you?", count));
                AddRadio(35, 255, 9721, 9724, false, 1); // accept/yes radio
                AddRadio(135, 255, 9721, 9724, true, 2); // decline/no radio
                AddHtmlLocalized(72, 255, 200, 30, 1049016, 0x7fff, false, false); // Yes
                AddHtmlLocalized(172, 255, 200, 30, 1049017, 0x7fff, false, false); // No
                AddButton(80, 289, 2130, 2129, 3, GumpButtonType.Reply, 0); // Okay button

            }
            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (info == null || state == null || state.Mobile == null)
                {
                    return;
                }

                int radiostate = -1;

                Point3D myloc = state.Mobile.Location;
                Map mymap = state.Mobile.Map;

                if (info.Switches.Length > 0)
                {
                    radiostate = info.Switches[0];
                }
                switch (info.ButtonID)
                {

                    default:
                    {
                        if (radiostate == 1 && SearchList != null)
                        {    // accept
                            for (int i = 0; i < SearchList.Count; ++i)
                            {
                                SearchEntry e = SearchList[i];

                                if (e.Selected)
                                {
                                    object o = e.Object;

                                    if (o is Item)
                                    {

                                        ((Item)o).MoveToWorld(myloc, mymap);

                                    }
                                    else
                                        if (o is Mobile)
                                    {

                                        ((Mobile)o).MoveToWorld(myloc, mymap);

                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        public class XmlConfirmDeleteGump : Gump
        {
            private List<SearchEntry> SearchList;

            private Mobile From;

            public XmlConfirmDeleteGump(Mobile from, List<SearchEntry> searchlist)
                : base(0, 0)
            {
                SearchList = searchlist;

                From = from;
                Closable = false;
                Dragable = true;
                AddPage(0);
                AddBackground(10, 200, 200, 130, 5054);
                int count = 0;

                if (SearchList != null)
                {
                    for (int i = 0; i < SearchList.Count; ++i)
                    {
                        if (SearchList[i].Selected)
                        {
                            ++count;
                        }
                    }
                }

                AddLabel(20, 225, 33, string.Format("Delete {0} objects?", count));
                AddRadio(35, 255, 9721, 9724, false, 1); // accept/yes radio
                AddRadio(135, 255, 9721, 9724, true, 2); // decline/no radio
                AddHtmlLocalized(72, 255, 200, 30, 1049016, 0x7fff, false, false); // Yes
                AddHtmlLocalized(172, 255, 200, 30, 1049017, 0x7fff, false, false); // No
                AddButton(80, 289, 2130, 2129, 3, GumpButtonType.Reply, 0); // Okay button

            }
            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (info == null || state == null || state.Mobile == null)
                {
                    return;
                }

                int radiostate = -1;
                if (info.Switches.Length > 0)
                {
                    radiostate = info.Switches[0];
                }
                switch (info.ButtonID)
                {
                    default:
                    {
                        if (radiostate == 1 && SearchList != null)
                        {// accept
                            bool landstatic = false;
                            StringBuilder sb = new StringBuilder();
                            int deleted = 0;
                            for (int i = 0; i < SearchList.Count; ++i)
                            {
                                SearchEntry e = SearchList[i];

                                if (e.Selected)
                                {
                                    object o = e.Object;

                                    if (o is Item it)
                                    {
                                        sb.Append(it.ToString());
                                        sb.Append(" - ");
                                        it.Delete();
                                        deleted++;
                                    }
                                    else if (o is Mobile mob)
                                    {
                                        if (!mob.Player)
                                        {
                                            sb.Append(mob.ToString());
                                            sb.Append(" - ");
                                            mob.Delete();
                                            deleted++;
                                        }
                                    }
                                    else if (o is XmlAttachment xa)
                                    {
                                        sb.Append(xa.ToString());
                                        sb.Append(" - ");
                                        xa.Delete();
                                        deleted++;
                                    }
                                    else if (o is Targeting.StaticTarget st)
                                    {
                                        landstatic = true;
                                        sb.Append($"{st.ItemID} ({st.Location})");
                                        sb.Append(" - ");
                                        new UltimaLive.DeleteStatic(st.Map.MapID, st).DoOperation();
                                        deleted++;
                                    }
                                    else if (o is Targeting.LandTarget)
                                    {
                                        //land can't be removed, only morphed
                                        return;
                                    }
                                }
                            }
                            if (deleted > 0)
                            {
                                if (landstatic)
                                {
                                    CommandLogging.WriteLine(From, "{0} {1} Deleting {2} Lands/Statics, IDs: {3}", From.AccessLevel, CommandLogging.Format(From), deleted, sb.ToString());
                                }
                                else
                                {
                                    CommandLogging.WriteLine(From, "{0} {1} Deleting {2} objects, serials: {3}", From.AccessLevel, CommandLogging.Format(From), deleted, sb.ToString());
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
