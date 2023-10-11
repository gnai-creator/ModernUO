using Server.Accounting;
using Server.Engines.PartySystem;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Targeting;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Engines.XmlSpawner2
{
    public class XmlPoints : XmlAttachment
    {
        private PlayerMobile m_Owner;
        internal Account OwnerAccount
        {
            get
            {
                return m_Owner?.Account;
            }
        }

        internal const int STARTING_POINTS = 100;  // 100 default starting points
        private List<KillEntry> KillList = new List<KillEntry>();
        private DateTime m_LastDecay;
        public DateTime m_CancelEnd;
        public CancelTimer m_CancelTimer;

        public Point3D m_StartingLoc;
        public Map m_StartingMap;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Points { get; set; } = STARTING_POINTS;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Rank { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DeltaRank { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime WhenRanked { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Credits { get; set; } = 0;

        //[CommandProperty(AccessLevel.GameMaster)]
        //public bool Broadcast { get; set; } = false;

        //[CommandProperty(AccessLevel.GameMaster)]
        //public bool ReceiveBroadcasts { get; set; } = false;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Kills { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Deaths { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastKill { get; private set; } = DateTime.MinValue;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastDeath { get; private set; } = DateTime.MinValue;

        public bool HasChallenge { get { return ((Challenger != null && !Challenger.Deleted) || (ChallengeGame != null && !ChallengeGame.Deleted)); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlayerMobile Challenger { get; set; }

        public BaseChallengeGame ChallengeGame { get; set; }

        public BaseChallengeGame ChallengeSetup { get; set; }

        public static bool TeleportOnDuel { get; } = true;                          // are players automatically teleported to and from the specified dueling location

        private static TimeSpan m_DeathDelay = TimeSpan.FromSeconds(180);    // 180 seconds default min time between deaths for point loss
        internal const int KILL_DELAY = 2600;//calculated in seconds for the killdelay, 43 minutes max - 21 minutes min 

        // set these scalings to determine points gained/lost based on the difference in points between the killer and the killed
        // default is set to 10% of the point difference (0.1).  Note, regardless of the scaling at least 1 point will be gained/lost per kill (original was 5%)
        private static double m_WinScale = 0;//0.05;                               // set to zero for no scaling for points gained for killing (fixed 1 point per kill)
        private static double m_LoseScale = 0;//0.05;                              // set to zero for no scaling for points lost when killed (fixed 1 point per death)
        private static double m_CreditScale = 0;//0.05;

        // admin control of pvp-kill broadcasts. If false then no broadcasting. If true, then broadcasting is controlled by the player settings
        //private static bool m_SystemBroadcast = true;

        private static TimeSpan m_PointsDecayTime { get; } = TimeSpan.FromDays(5);      // default time interval for automatic point loss for no pvp activity

        // set m_PointsDecay to zero to disable the automatic points decay feature
        private const int POINTS_DECAY = 10;                                  // default point loss if no kills are made within the PointsDecayTime

        // set m_CancelTimeout to determine how long it takes to cancel a challenge after it is requested
        public static TimeSpan CancelTimeout = TimeSpan.FromMinutes(5);

        public static bool AllowWithinGuildTownPoints = true;              // allow within-guild-town challenge duels for points.

        public static bool UnrestrictedChallenges = false;              // allow the normal waiting time between kills for points to be overridden for challenges

        // allows players to be autores'd following 1-on-1 duels
        // Team Challenge type matches handle their own autores behavior
        public static bool AutoResAfterDuel { get; } = true;

        private static bool m_GainHonorFromDuel { get; } = false;
        private static bool m_LogKills { get; } = true;            // log all kills that award points to the kills.log file

        private static Dictionary<Mobile, RankEntry> UnRankedEntries { get; } = new Dictionary<Mobile, RankEntry>();
        private static List<RankEntry> RankList = new List<RankEntry>();
        private static bool needsupdate = true;

        public static DuelLocationEntry[] DuelLocations = new DuelLocationEntry[]
        {
            new DuelLocationEntry("Jhelom Fighting Pit", 1398, 3742, -21, Map.Felucca, 14),
			//new DuelLocationEntry("Luna Grand Arena", 940, 637, -90, Map.Malas, 4),
		};

        public class DuelLocationEntry
        {
            public Point3D DuelLocation;
            public Map DuelMap;
            public int DuelRange;
            public string Name;

            public DuelLocationEntry(string name, int X, int Y, int Z, Map map, int range)
            {
                Name = name;
                DuelLocation = new Point3D(X, Y, Z);
                DuelMap = map;
                DuelRange = range;
            }
        }

        public static bool DuelLocationAvailable(DuelLocationEntry duelloc)
        {
            // check to see whether there are any players at the location
            if (duelloc == null || duelloc.DuelMap == null) return true;

            int duelrange = duelloc.DuelRange;

            if (duelloc.DuelRange <= 0) duelrange = 16;

            foreach (Mobile m in duelloc.DuelMap.GetMobilesInRange(duelloc.DuelLocation, duelrange))
            {
                if (m.Player)
                    return false;
            }

            return true;
        }

        public static bool CheckCombat(Mobile m)
        {
            return (m != null && (m.Aggressors.Count > 0 || m.Aggressed.Count > 0));
        }
        
        private class KillEntry
        {
            public PlayerMobile Killed;
            public DateTime WhenKilled;
            public DateTime NextKill;

            public KillEntry(PlayerMobile m, DateTime w, DateTime d)
            {
                Killed = m;
                WhenKilled = w;
                NextKill = d;
            }

            public KillEntry(PlayerMobile m, DateTime w, TimeSpan d)
            {
                Killed = m;
                WhenKilled = w;
                NextKill = w + d;
            }
        }

        public class RankEntry : IComparable<RankEntry>
        {
            public PlayerMobile Killer;
            public int Rank;
            public XmlPoints PointsAttachment;

            public RankEntry(PlayerMobile m, XmlPoints attachment)
            {
                Killer = m;
                PointsAttachment = attachment;
            }

            public int CompareTo(RankEntry p)
            {
                if (p.PointsAttachment == null || PointsAttachment == null) return 0;

                // break points ties with kills (more kills means higher rank)
                if (p.PointsAttachment.Points - PointsAttachment.Points == 0)
                {
                    // if kills are the same then compare deaths (fewer deaths means higher rank)
                    if (p.PointsAttachment.Kills - PointsAttachment.Kills == 0)
                    {
                        // if deaths are the same then use previous ranks
                        if (p.PointsAttachment.Deaths - PointsAttachment.Deaths == 0)
                        {
                            return p.PointsAttachment.Rank - PointsAttachment.Rank;
                        }

                        return PointsAttachment.Deaths - p.PointsAttachment.Deaths;
                    }

                    return p.PointsAttachment.Kills - PointsAttachment.Kills;
                }

                return p.PointsAttachment.Points - PointsAttachment.Points;
            }
        }

        private static bool SameGuildPartyOrTown(PlayerMobile killed, PlayerMobile killer)
        {
            Account kdacc = killed.Account, kracc = killer.Account;
            if (kdacc != null  && kracc != null)
            {
                bool sameguildortown = killed.Party is Party pd && pd.Active && pd.Contains(killer);
                for (int i = kdacc.Length - 1; i >= 0 && !sameguildortown; --i)
                {
                    if (kdacc[i] is PlayerMobile pmkd && (pmkd.Guild != null || pmkd.Town != null))
                    {
                        for (int ii = kracc.Length - 1; ii >= 0 && !sameguildortown; --ii)
                        {
                            if (kracc[ii] is PlayerMobile pmkr)
                            {
                                sameguildortown = (pmkr.Guild != null && pmkr.Guild.IsAlly(pmkd.Guild)) || (pmkr.Town != null && (pmkr.Town == pmkd.Town || pmkr.Town.IsAlly(pmkd.Town)));
                            }
                        }
                    }
                }
                return sameguildortown;
            }
            else
                return (killer.Guild != null && killer.Guild == killed.Guild) || (killer.Town != null && killer.Town == killed.Town) || (killed.Party is Party pd && pd.Active && pd.Contains(killer));
        }

        private static void RefreshRankList()
        {
            if (needsupdate)
            {
                RankList = new List<RankEntry>(UnRankedEntries.Values);
                RankList.Sort();

                int rank = 0;
                //int prevpoints = 0;
                for (int i = 0; i < RankList.Count; ++i)
                {
                    RankEntry p = RankList[i];
                    // bump the rank for every successive player in the list.  Players with the same points total will be
                    // ordered by kills
                    rank++;
                    p.Rank = rank;
                }
                needsupdate = false;
            }
        }

        public static int GetRanking(Mobile m)
        {
            if (m == null)
                return 0;

            RefreshRankList();
            // go through the sorted list and calculate rank
            if (UnRankedEntries.TryGetValue(m, out RankEntry p))
                return p.Rank;

            // rank 0 means unranked
            return 0;
        }

        private static void UpdateRanking(PlayerMobile m, XmlPoints attachment)
        {
            if (m == null)
                return;

            needsupdate = true;
            if (m.AccessLevel > AccessLevel.Player || m.Niubbo)
            {
                UnRankedEntries.Remove(m);
            }
            else
            {
                // flag the rank list for updating on the next attempt to retrieve a rank
                if (!UnRankedEntries.TryGetValue(m, out RankEntry p) || p.PointsAttachment != attachment)
                {
                    UnRankedEntries[m] = new RankEntry(m, attachment);
                }
            }

            // if points statistics are being displayed in player name properties, then update them
            m.InvalidateProperties();
        }

        public static int GetCredits(Mobile m)
        {
            if (XmlAttach.FindAttachment(m, typeof(XmlPoints)) is XmlPoints xp)
            {
                return xp.Credits;
            }

            return 0;
        }

        public static int GetPoints(Mobile m)
        {
            if (XmlAttach.FindAttachment(m, typeof(XmlPoints)) is XmlPoints xp)
            {
                return xp.Points;
            }

            return 0;
        }

        public static bool HasCredits(Mobile m, int credits, int minpoints)
        {
            if (m == null || m.Deleted)
            {
                return false;
            }

            if (XmlAttach.FindAttachment(m, typeof(XmlPoints)) is XmlPoints p)
            {
                if (p.Credits >= credits && (minpoints <= 0 || p.Points - STARTING_POINTS >= minpoints))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TakeCredits(Mobile m, int credits)
        {
            if (m == null || m.Deleted) return false;

            if (XmlAttach.FindAttachment(m, typeof(XmlPoints)) is XmlPoints x)
            {
                if (x.Credits >= credits)
                {
                    x.Credits -= credits;
                    return true;
                }
            }

            return false;
        }

        //public static void BroadcastCombatResult(Mobile killer, Mobile killed)
        //{
        //    foreach (NetState state in NetState.Instances)
        //    {
        //        Mobile m = state.Mobile;
        //
        //        if (m != null)
        //        {
        //            // check to see if they have a points attachment with ReceiveBroadcasts enabled
        //            if (XmlAttach.FindAttachment(m, typeof(XmlPoints)) is XmlPoints x)
        //            {
        //                if (!x.ReceiveBroadcasts)
        //                    return;
        //            }
        //
        //            m.SendLocalizedMessage(505724, $"{killer.GetNameFor(m)}\t{killed.GetNameFor(m)}", 0x482);
        //        }
        //    }
        //}

        //public static void BroadcastMessage ( AccessLevel ac, int hue, int cliloc, string args )
		//{
        //    foreach ( NetState state in NetState.Instances )
		//	{
		//		Mobile m = state.Mobile;
        //
		//		if ( m != null && m.AccessLevel >= ac )
		//		{
        //            // check to see if they have a points attachment with ReceiveBroadcasts enabled
        //            if (XmlAttach.FindAttachment(m, typeof(XmlPoints)) is XmlPoints x)
        //            {
        //                if (!x.ReceiveBroadcasts)
        //                    return;
        //            }
        //
        //            m.SendLocalizedMessage(cliloc, args, hue);
		//		}
		//	}
		//}

        //public static void EventSink_Speech( SpeechEventArgs args )
		//{
        //    if (!(args.Mobile is PlayerMobile from) || from.Map == null) return;
        //
        //    if (args.Speech != null && args.Speech.ToLower() == "showpoints")
		//		ShowPointsOverhead(from);
        //
		//	if(args.Speech != null && args.Speech.ToLower() == "i wish to duel")
		//		from.Target = new ChallengeTarget(from);
		//}
        //
		//public static void ShowPointsOverhead( Mobile from )
		//{
		//	if(from == null) return;
        //
		//	from.PublicOverheadMessage( MessageType.Regular, 0x3B2, false, GetPoints(from).ToString());
		//}

        public static void Configure()
        {
            //check ranks after load, so we can get a count of invalid attachments (deleted players) and get rid of them instantaneously
            EventSink.WorldPostLoad += new WorldPostLoadEventHandler(PostLoadHandler);
        }

        public new static void Initialize()
        {
            //Register our speech handler
            //EventSink.Speech += new SpeechEventHandler( EventSink_Speech );

            //CommandSystem.Register( "Challenge", AccessLevel.Player, new CommandEventHandler( Challenge_OnCommand ) );
			//CommandSystem.Register( "LMSChallenge", AccessLevel.Player, new CommandEventHandler( LMSChallenge_OnCommand ) );
			//CommandSystem.Register( "TeamLMSChallenge", AccessLevel.Player, new CommandEventHandler( TeamLMSChallenge_OnCommand ) );
			//CommandSystem.Register( "Deathmatch", AccessLevel.Player, new CommandEventHandler( Deathmatch_OnCommand ) );
			//CommandSystem.Register( "TeamDeathmatch", AccessLevel.Player, new CommandEventHandler( TeamDeathmatch_OnCommand ) );
			//CommandSystem.Register( "DeathBall", AccessLevel.Player, new CommandEventHandler( DeathBall_OnCommand ) );
			//CommandSystem.Register( "KingOfTheHill", AccessLevel.Player, new CommandEventHandler( KingOfTheHill_OnCommand ) );
			//CommandSystem.Register( "TeamDeathBall", AccessLevel.Player, new CommandEventHandler( TeamDeathBall_OnCommand ) );
			//CommandSystem.Register( "TeamKotH", AccessLevel.Player, new CommandEventHandler( TeamKotH_OnCommand ) );
			//CommandSystem.Register( "CTFChallenge", AccessLevel.Player, new CommandEventHandler( CTFChallenge_OnCommand ) );
			//CommandSystem.Register( "SystemBroadcastKills", AccessLevel.GameMaster, new CommandEventHandler( SystemBroadcastKills_OnCommand ) );
			//CommandSystem.Register( "SeeKills", AccessLevel.Player, new CommandEventHandler( SeeKills_OnCommand ) );
			//CommandSystem.Register( "BroadcastKills", AccessLevel.Player, new CommandEventHandler( BroadcastKills_OnCommand ) );
			//CommandSystem.Register( "TopPlayers", AccessLevel.Player, new CommandEventHandler( TopPlayers_OnCommand ) );

            foreach (Item i in World.Items.Values)
            {
                if (i is BaseChallengeGame bcg && !bcg.GameCompleted)
                {
                    // find the region it is in
                    // is this in a challenge game region?
                    Region r = Region.Find(bcg.Location, bcg.Map);
                    if (r is ChallengeGameRegion cgr)
                    {
                        cgr.ChallengeGame = bcg;
                    }
                }
            }
        }

        
		//[Usage( "SeeKills [true/false]" )]
		//[Description( "Determines whether a player sees others pvp broadcast results." )]
		//public static void SeeKills_OnCommand( CommandEventArgs e )
		//{
        //
        //    if (XmlAttach.FindAttachment(e.Mobile, typeof(XmlPoints)) is XmlPoints xp)
        //    {
        //        if (e.Arguments.Length > 0)
        //        {
        //            if (bool.TryParse(e.Arguments[0], out bool b))
        //                xp.ReceiveBroadcasts = b;
        //        }
        //        e.Mobile.SendMessage("SeeKills is {0}", xp.ReceiveBroadcasts);
        //    }
        //}
        //
		//[Usage( "BroadcastKills [true/false]" )]
		//[Description( "Determines whether pvp results will be broadcast.  The killers (winner) flag setting is used. " )]
		//public static void BroadcastKills_OnCommand( CommandEventArgs e )
		//{
        //    if (XmlAttach.FindAttachment(e.Mobile, typeof(XmlPoints)) is XmlPoints xp)
        //    {
		//		if(e.Arguments.Length > 0)
		//		{
        //            if (e.Arguments.Length > 0)
        //            {
        //                if (bool.TryParse(e.Arguments[0], out bool b))
        //                    xp.Broadcast = b;
        //            }
        //        }
        //
		//		e.Mobile.SendMessage("BroadcastKills is {0}", xp.Broadcast);
		//	}
		//}
        //
		//[Usage( "SystemBroadcastKills [true/false]" )]
		//[Description( "GM override of broadcasting of pvp results.  False means no broadcasting. True means players settings are used." )]
		//public static void SystemBroadcastKills_OnCommand( CommandEventArgs e )
		//{
		//	if(e.Arguments.Length > 0)
		//	{
        //        if (bool.TryParse(e.Arguments[0], out bool b))
        //            m_SystemBroadcast = b;
		//	} 
        //
		//	e.Mobile.SendMessage("SystemBroadcastKills is {0}.", m_SystemBroadcast);
		//}
        //
		//[Usage( "TopPlayers" )]
		//[Description( "Displays the top players in points" )]
		//public static void TopPlayers_OnCommand( CommandEventArgs e )
		//{
        //    Mobile m = e.Mobile;
        //
        //    if (m.FindGump(typeof(TopPlayersGump)) is TopPlayersGump g)
        //    {
        //        g = new TopPlayersGump(m, g.TownFilter, g.GuildFilter, g.NameFilter);
        //        m.CloseGump(typeof(TopPlayersGump));
        //    }
        //    else
        //        g = new TopPlayersGump(m);
        //    m.SendGump(g);
		//}

        public static bool AreChallengers(Mobile from, Mobile target)
        {
            if (from != null && target != null && XmlAttach.FindAttachment(from, typeof(XmlPoints)) is XmlPoints afrom && XmlAttach.FindAttachment(target, typeof(XmlPoints)) is XmlPoints atarget && !afrom.Deleted && !atarget.Deleted)
            {
                return afrom.Challenger == target && atarget.Challenger == from || (afrom.ChallengeGame != null && !afrom.ChallengeGame.Deleted && atarget.ChallengeGame == afrom.ChallengeGame && afrom.ChallengeGame.AreChallengers(from, target));
            }
            return false;
        }

        public static bool AreInAnyGame(XmlPoints atarget)
        {
            // get the challenge game info from the points attachment
            if (atarget.ChallengeGame != null && !atarget.ChallengeGame.Deleted)
            {
                return atarget.ChallengeGame.AreInGame(atarget.m_Owner);
            }

            return false;

        }

        public static bool AreInSameGame(XmlPoints afrom, XmlPoints atarget)
        {
            if (afrom.m_Owner == null || atarget.m_Owner == null) return false;

            // check the team challenge status
            if (afrom.ChallengeGame != null && !afrom.ChallengeGame.Deleted && afrom.ChallengeGame == atarget.ChallengeGame)
            {
                return afrom.ChallengeGame.AreInGame(atarget.m_Owner) && afrom.ChallengeGame.AreInGame(atarget.m_Owner);
            }

            return false;
        }

        public static bool AreTeamMembers(XmlPoints afrom, XmlPoints atarget)
        {
            // check the team challenge status
            if (afrom.ChallengeGame != null && !afrom.ChallengeGame.Deleted && afrom.ChallengeGame == atarget.ChallengeGame)
            {
                return afrom.ChallengeGame.AreTeamMembers(afrom.m_Owner, atarget.m_Owner);
            }

            return false;
        }

        public static bool InsuranceIsFree(XmlPoints from, XmlPoints awardto)
        {
            if (from == null || awardto == null) return false;

            // check the team challenge status
            if (from.ChallengeGame != null && !from.ChallengeGame.Deleted && from.ChallengeGame == awardto.ChallengeGame)
            {
                return from.ChallengeGame.InsuranceIsFree(from.m_Owner, awardto.m_Owner);
            }

            // uncomment the line below if you want to prevent insurance awards for normal 1on1 duels
            //if(atarget.Challenger == from) return true;

            return false;
        }

        public static bool YoungProtection(PlayerMobile from, PlayerMobile target)
        {
            // dont allow young players to be challenged by experienced players
            // note, this will allow young players to challenge other young players
            if (from.Niubbo && !target.Niubbo)
                return true;
            return false;

        }

        public static bool AllowChallengeGump(Mobile from, Mobile target)
        {
            if (from == null || target == null) return false;

            // uncomment the code below if you want to restrict challenges to towns only
			//if (!from.Region.IsPartOf(typeof(Regions.TownRegion)) || !target.Region.IsPartOf(typeof(Regions.TownRegion)))
			//{
			//	from.SendMessage("You must be in a town to issue a challenge"); 
			//	return false;
			//}

            if (from.Region.IsPartOf(typeof(Regions.Jail)) || target.Region.IsPartOf(typeof(Regions.Jail)))
            {
                from.SendLocalizedMessage(1042632); // You'll need a better jailbreak plan then that!
                return false;
            }

            return true;
        }

        private class ChallengeTarget : Target
        {
            private PlayerMobile m_challenger;

            public ChallengeTarget(PlayerMobile m) : base(30, false, TargetFlags.None)
            {
                m_challenger = m;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == m_challenger && targeted is PlayerMobile pmt)
                {
                    // test them for young status
                    if (YoungProtection(m_challenger, pmt))
                    {
                        m_challenger.SendLocalizedMessage(505720, pmt.GetNameFor(m_challenger));// "{0} is too inexperience to be challenged"
                        return;
                    }

                    // check the owner for existing challenges
                    if (XmlAttach.FindAttachment(m_challenger, typeof(XmlPoints)) is XmlPoints a && !a.Deleted)
                    {
                        // issuing a challenge when one is already in place will initiate cancellation of the current challenge
                        if (a.Challenger != null)
                        {

                            // this will initiate the challenge cancellation timer

                            if (a.m_CancelTimer != null && a.m_CancelTimer.Running)
                            {
                                // timer is running
                                m_challenger.SendLocalizedMessage(505700, (a.m_CancelEnd - DateTime.UtcNow).TotalMinutes.ToString("F0"));// "{ 0} mins remaining until current challenge is cancelled."
                            }
                            else
                            {
                                m_challenger.SendLocalizedMessage(505701, XmlPoints.CancelTimeout.TotalMinutes.ToString("F0"));// "Canceling current challenge.  Please wait {0} minutes"
                                if (a.Challenger != null)
                                    a.Challenger.SendLocalizedMessage(505702, $"{m_challenger.GetNameFor(a.Challenger)}\t{XmlPoints.CancelTimeout.TotalMinutes.ToString("F0")}");// "{0} is canceling the current challenge. {1} minutes remain"

                                // start up the cancel challenge timer
                                a.DoTimer(XmlPoints.CancelTimeout);

                                // update the points gumps on the challenger if they are open
                                if (m_challenger.HasGump(typeof(PointsGump)))
                                {
                                    a.OnIdentify(m_challenger);
                                }
                                // update the points gumps on the challenge target if they are open
                                if (a.Challenger.HasGump(typeof(PointsGump)))
                                {
                                    XmlPoints ca = (XmlPoints)XmlAttach.FindAttachment(a.Challenger, typeof(XmlPoints));
                                    if (ca != null && !ca.Deleted)
                                        ca.OnIdentify(a.Challenger);
                                }
                            }
                            return;
                        }

                        // check the target for existing challengers
                        if (XmlAttach.FindAttachment(pmt, typeof(XmlPoints)) is XmlPoints xa && !xa.Deleted)
                        {
                            if (xa.Challenger != null)
                            {
                                m_challenger.SendLocalizedMessage(505703, pmt.GetNameFor(m_challenger));// "{0} is already being challenged."
                                return;
                            }
                            if (m_challenger == targeted)
                            {
                                m_challenger.SendLocalizedMessage(505704);// "You cannot challenge yourself."
                            }
                            else
                            {
                                // send the confirmation gump to the challenged player
                                m_challenger.SendGump(new IssueChallengeGump(a, xa));
                            }
                        }
                    }
                    else
                    {
                        m_challenger.SendLocalizedMessage(505705); // "No XmlPoints support."
                    }
                }
            }
        }

        public void DoTimer(TimeSpan delay)
        {
            if (m_CancelTimer != null)
                m_CancelTimer.Stop();

            m_CancelTimer = new CancelTimer(this, delay);
            m_CancelEnd = DateTime.UtcNow + delay;
            m_CancelTimer.Start();
        }


        public class CancelTimer : Timer
        {
            private XmlPoints m_attachment;

            public CancelTimer(XmlPoints a, TimeSpan delay) : base(delay)
            {
                Priority = TimerPriority.OneSecond;
                m_attachment = a;
            }

            protected override void OnTick()
            {
                if (m_attachment == null || m_attachment.Deleted || m_attachment.m_Owner == null) return;

                Mobile pm = m_attachment.m_Owner;

                if (m_attachment.Challenger != null)
                {
                    pm.SendLocalizedMessage(505721, m_attachment.Challenger.GetNameFor(pm));// "Challenge with {0} has been cancelled"

                    if (pm.HasGump(typeof(PointsGump)))
                    {
                        m_attachment.OnIdentify(pm);
                    }
                }

                // clear the challenger on the challengers attachment
                XmlPoints xa = XmlAttach.FindAttachment(m_attachment.Challenger, typeof(XmlPoints)) as XmlPoints;

                if (xa != null && !xa.Deleted)
                {
                    // check the challenger field to see if it matches the current
                    if (xa.Challenger == pm)
                    {

                        if (m_attachment.Challenger != null && pm != null)
                        {
                            m_attachment.Challenger.SendLocalizedMessage(505721, pm.GetNameFor(m_attachment.Challenger)); // "Challenge with {0} has been cancelled"
                        }
                        // then clear it
                        xa.Challenger = null;
                    }
                }
                // clear challenger on this attachment
                m_attachment.Challenger = null;

                // refresh any open gumps
                if (pm != null && xa != null && xa.m_Owner != null)
                {
                    if (pm.HasGump(typeof(PointsGump)))
                    {
                        m_attachment.OnIdentify(pm);
                    }

                    // and update the gumps if they are open
                    if (xa.m_Owner.HasGump(typeof(PointsGump)))
                    {
                        xa.OnIdentify(xa.m_Owner);
                    }
                }
            }
        }

        //[Usage( "Challenge" )]
		//[Description( "Challenge another player to a duel for points" )]
		//public static void Challenge_OnCommand( CommandEventArgs e )
		//{
        //    // target the player you wish to challenge
        //    if (e.Mobile is PlayerMobile pm)
		//	    e.Mobile.Target = new ChallengeTarget(pm);
		//}
        //
		//[Usage( "LMSChallenge" )]
		//[Description( "Creates a Last Man Standing challenge game" )]
		//public static void LMSChallenge_OnCommand( CommandEventArgs e )
		//{
		//	BaseChallengeGame.DoSetupChallenge(e.Mobile, 100302, typeof(LastManStandingGauntlet));
		//}
        //
		//[Usage( "TeamLMSChallenge" )]
		//[Description( "Creates a Team Last Man Standing challenge game" )]
		//public static void TeamLMSChallenge_OnCommand( CommandEventArgs e )
		//{
		//	BaseChallengeGame.DoSetupChallenge(e.Mobile, 100413, typeof(TeamLMSGauntlet));
		//}
        //
		//[Usage( "Deathmatch" )]
		//[Description( "Creates a Deathmatch challenge game" )]
		//public static void Deathmatch_OnCommand( CommandEventArgs e )
		//{
		//	BaseChallengeGame.DoSetupChallenge(e.Mobile, 100400, typeof(DeathmatchGauntlet));
		//}
        //
		//[Usage( "TeamDeathmatch" )]
		//[Description( "Creates a Team Deathmatch challenge game" )]
		//public static void TeamDeathmatch_OnCommand( CommandEventArgs e )
		//{
		//	BaseChallengeGame.DoSetupChallenge(e.Mobile, 100415, typeof(TeamDeathmatchGauntlet));
		//}
        //
		//[Usage( "DeathBall" )]
		//[Description( "Creates a DeathBall challenge game" )]
		//public static void DeathBall_OnCommand( CommandEventArgs e )
		//{
		//	BaseChallengeGame.DoSetupChallenge(e.Mobile, 100411, typeof(DeathBallGauntlet));
		//}
        //
		//[Usage( "TeamDeathball" )]
		//[Description( "Creates a Team Deathball challenge game" )]
		//public static void TeamDeathBall_OnCommand( CommandEventArgs e )
		//{
		//	BaseChallengeGame.DoSetupChallenge(e.Mobile, 100416, typeof(TeamDeathballGauntlet));
		//}
        //
		//[Usage( "KingOfTheHill" )]
		//[Description( "Creates a King of the Hill challenge game" )]
		//public static void KingOfTheHill_OnCommand( CommandEventArgs e )
		//{
		//	BaseChallengeGame.DoSetupChallenge(e.Mobile, 100410, typeof(KingOfTheHillGauntlet));
		//}
        //
		//[Usage( "TeamKotH" )]
		//[Description( "Creates a Team King of the Hill challenge game" )]
		//public static void TeamKotH_OnCommand( CommandEventArgs e )
		//{
		//	BaseChallengeGame.DoSetupChallenge(e.Mobile, 100417, typeof(TeamKotHGauntlet));
		//}
        //
		//[Usage( "CTFChallenge" )]
		//[Description( "Creates a CTF challenge game" )]
		//public static void CTFChallenge_OnCommand( CommandEventArgs e )
		//{
		//	BaseChallengeGame.DoSetupChallenge(e.Mobile, 100418, typeof(CTFGauntlet));
		//}

        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlPoints(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlPoints()
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // check for points decay
            if (Kills > 0 && POINTS_DECAY > 0 && Points > STARTING_POINTS && (DateTime.UtcNow - m_LastDecay) > m_PointsDecayTime &&
                (DateTime.UtcNow - LastKill) > m_PointsDecayTime && (DateTime.UtcNow - LastDeath) > m_PointsDecayTime)
            {
                Points -= POINTS_DECAY;
                if (Points < STARTING_POINTS)
                    Points = STARTING_POINTS;
                m_LastDecay = DateTime.UtcNow;
            }

            writer.Write(1);

            //writer.Write(ReceiveBroadcasts);
            //writer.Write(Broadcast);

            writer.Write(m_StartingLoc);
            writer.Write(m_StartingMap);

            writer.Write(ChallengeGame);
            writer.Write(ChallengeSetup);
            writer.Write(m_CancelEnd - DateTime.UtcNow);
            writer.Write(Rank);
            writer.Write(DeltaRank);
            writer.Write(WhenRanked);
            writer.Write(m_LastDecay);
            writer.Write(Credits);
            writer.Write(Points);
            writer.Write(Kills);
            writer.Write(Deaths);
            writer.Write(Challenger);
            writer.Write(LastKill);
            writer.Write(LastDeath);
            // write out the kill list
            writer.Write(KillList.Count);
            foreach (KillEntry k in KillList)
            {
                writer.Write(k.Killed);
                writer.Write(k.WhenKilled);
                writer.Write(k.NextKill);
            }
            writer.WriteMobile(m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            //ReceiveBroadcasts = reader.ReadBool();
            //Broadcast = reader.ReadBool();

            m_StartingLoc = reader.ReadPoint3D();
            m_StartingMap = reader.ReadMap();
            ChallengeGame = reader.ReadItem<BaseChallengeGame>();
            ChallengeSetup = reader.ReadItem<BaseChallengeGame>();
            TimeSpan remaining = reader.ReadTimeSpan();
            if (remaining > TimeSpan.Zero)
            {
                DoTimer(remaining);
            }
            Rank = reader.ReadInt();
            DeltaRank = reader.ReadInt();
            WhenRanked = reader.ReadDateTime();
            m_LastDecay = reader.ReadDateTime();
            Credits = reader.ReadInt();
            Points = reader.ReadInt();
            Kills = reader.ReadInt();
            Deaths = reader.ReadInt();
            Challenger = reader.ReadMobile<PlayerMobile>();
            LastKill = reader.ReadDateTime();
            LastDeath = reader.ReadDateTime();
            // read in the kill list
            int count = reader.ReadInt();
            KillList = new List<KillEntry>();
            for (int i = 0; i < count; ++i)
            {
                PlayerMobile m = reader.ReadMobile<PlayerMobile>();
                DateTime when = reader.ReadDateTime(), next;
                if (version < 1)
                    next = when + TimeSpan.FromSeconds(KILL_DELAY);
                else
                    next = reader.ReadDateTime();

                if (m != null && !m.Deleted)
                {
                    KillList.Add(new KillEntry(m, when, next));
                }
            }

            // get the owner of this in order to rebuild the rankings
            m_Owner = reader.ReadMobile<PlayerMobile>();
            if (m_Owner != null && m_Owner.AccessLevel == AccessLevel.Player)
                UnRankedEntries[m_Owner] = new RankEntry(m_Owner, this);
        }

        private static void PostLoadHandler()
        {
            RefreshRankList();
        }

        // updates the attachment kill list and removes expired entries
        private void RefreshKillList()
        {
            for (int i = KillList.Count - 1; i >= 0; --i)
            {
                if (KillList[i].NextKill <= DateTime.UtcNow)
                    KillList.RemoveAt(i);
            }
        }

        public void ReportPointLoss_Callback((int, string, PlayerMobile) args)
        {
            int points = args.Item1;
            string name = args.Item2;
            PlayerMobile m = args.Item3;

            if (m != null)
            {
                m.SendLocalizedMessage(505725, $"{points}\t{name}");// "You lost {0} point(s) for being killed by {1}"
            }
        }

        public static void AutoRes_Callback((PlayerMobile, bool) args)
        {
            PlayerMobile m = args.Item1;
            bool refresh = args.Item2;

            if (m != null)
            {
                // auto tele ghosts to the corpse
                m.PlaySound(0x214);
                m.FixedEffect(0x376A, 10, 16);
                m.Resurrect();
                if (m.Corpse != null)
                {
                    m.MoveToWorld(m.Corpse.Location, m.Corpse.Map);
                    m.Corpse.OnDoubleClick(m);
                    m.Corpse.LootType = LootType.Regular;
                }

                if (refresh)
                {
                    m.Hits = m.HitsMax;
                    m.Mana = int.MaxValue;
                    m.Stam = int.MaxValue;
                }
            }
        }

        public static void Return_Callback((PlayerMobile, PlayerMobile, Point3D, Map) args)
        {
            PlayerMobile killer = args.Item1;
            PlayerMobile killed = args.Item2;
            Point3D loc = args.Item3;
            Map map = args.Item4;

            if (killer != null && killed != null && map != null && map != Map.Internal)
            {
                // auto tele players and corpses
                // if there were nearby pets/mounts then tele those as well

                List<BaseCreature> petlist = new List<BaseCreature>();
                foreach (Mobile m in killer.GetMobilesInRange(16))
                {
                    if (m is BaseCreature bc && bc.ControlMaster == killer)
                    {
                        petlist.Add(bc);
                    }
                }
                foreach (Mobile m in killed.GetMobilesInRange(16))
                {
                    if (m is BaseCreature bc && bc.ControlMaster == killed)
                    {
                        petlist.Add(bc);
                    }
                }

                // port the pets
                foreach (BaseCreature m in petlist)
                {
                    m.MoveToWorld(loc, map);
                }

                // port the killer and corpse
                killer.PlaySound(0x214);
                killer.FixedEffect(0x376A, 10, 16);
                killer.MoveToWorld(loc, map);
                if (killer.Corpse != null)
                {
                    killer.Corpse.MoveToWorld(loc, map);
                }

                // port the killed and corpse
                killed.PlaySound(0x214);
                killed.FixedEffect(0x376A, 10, 16);
                killed.MoveToWorld(loc, map);
                if (killed.Corpse != null)
                {
                    killed.Corpse.MoveToWorld(loc, map);
                }
            }
        }

        public static Dictionary<Account, Dictionary<Account, DateTime>> BeneficialActs = new Dictionary<Account, Dictionary<Account, DateTime>>();
        public bool CanAffectPoints(XmlPoints killer, XmlPoints killed, bool assumechallenge, bool internalcheck = false)
        {
            if(!internalcheck)
            {
                if (killer != null && killed != null)
                {
                    if (killer.OwnerAccount != null && killed.OwnerAccount != null && BeneficialActs.TryGetValue(killer.OwnerAccount, out var main) && main.TryGetValue(killed.OwnerAccount, out var dt))
                    {
                        if (dt < DateTime.UtcNow)
                        {
                            main.Remove(killed.m_Owner.Account);
                            if (main.Count < 1)
                            {
                                BeneficialActs.Remove(killer.m_Owner.Account);
                            }
                        }
                        else
                            return false;
                    }
                }
                else
                    return false;
            }
            // uncomment this for newbie protection
            if (((killer.m_Owner.Niubbo || killed.m_Owner.Niubbo) && Challenger != killer.m_Owner && Challenger != killed.m_Owner) ||
                (killer.m_Owner.Account != null && killed.m_Owner.Account != null && killer.m_Owner.Account.UUID.Overlaps(killed.m_Owner.Account.UUID)))
            {
                return false;
            }

            // check for within guild/town kills and ignore them if this has been disabled
            if (!AllowWithinGuildTownPoints && SameGuildPartyOrTown(killed.m_Owner, killer.m_Owner)) return false;

            // check for within team kills and ignore them
            if (AreTeamMembers(killer, killed)) return false;

            // are the players challengers?
            bool inchallenge = false;
            if ((m_Owner == killer.m_Owner && Challenger == killed.m_Owner) || (m_Owner == killed.m_Owner && Challenger == killer.m_Owner))
            {
                inchallenge = true;
            }

            bool norestriction = UnrestrictedChallenges;

            // check for team challenges
            if (ChallengeGame != null && !ChallengeGame.Deleted)
            {
                // check to see if points have been disabled in this game
                if (!ChallengeGame.AllowPoints) return false;

                inchallenge = true;

                // check for kill delay limitations on points awards
                norestriction = !ChallengeGame.UseKillDelay;
            }

            // if UnlimitedChallenges has been set then allow points
            // otherwise, challenges have to obey the same restrictions on minimum time between kills as normal pvp
            if (norestriction && (inchallenge || assumechallenge)) return true;

            // only allow guild/town kills to yield points if in a challenge
            if (!(assumechallenge || inchallenge) && SameGuildPartyOrTown(killed.m_Owner, killer.m_Owner)) return false;

            // uncomment the line below to limit points to challenges. regular pvp will not give points
            //if(!inchallenge && !assumechallenge) return false;

            // check to see whether killing the target would yield points
            // get a point for killing if they havent been killed recent

            // get the points attachment on the killer if this isnt the killer
            XmlPoints a = this;
            if (m_Owner != killer.m_Owner)
            {
                a = killer;
            }
            if (a != null)
            {
                a.RefreshKillList();

                // check the kill list
                foreach (KillEntry k in a.KillList)
                {
                    if (k.NextKill > DateTime.UtcNow)
                    {
                        // found a match on the list so dont give any points
                        if (k.Killed == killed.m_Owner)
                        {
                            return false;
                        }
                    }
                }
            }

            // check to see whether the killed target could yield points
            if (m_Owner == killed.m_Owner)
            {
                // is it still within the minimum delay for being killed (or if it died recently and didn't recover minimum hp to be of any match)?
                if (DateTime.UtcNow < LastDeath + m_DeathDelay || killed.m_Owner.DiedRecently) return false;
            }

            return true;
        }

        public static void ClearAggression(Mobile source, Mobile target)
        {
            // and remove the challenger from the aggressor list so that the res noto is not affected
            List<AggressorInfo> klist = target.Aggressors;
            for (int i = klist.Count - 1; i >= 0; --i)
            {
                var info = target.Aggressors[i];
                if (info.Attacker == source || info.Defender == source)
                {
                    klist.RemoveAt(i);
                    break;
                }
            }
            klist = target.Aggressed;
            for (int i = klist.Count - 1; i >= 0; --i)
            {
                var info = target.Aggressors[i];
                if (info.Attacker == source || info.Defender == source)
                {
                    klist.RemoveAt(i);
                    break;
                }
            }
        }

        public override bool HandlesOnKilled { get { return true; } }

        private static Dictionary<XmlPoints, Stack<XmlPoints>> s_KilledKillers = new Dictionary<XmlPoints, Stack<XmlPoints>>();
        // handles point loss when the player is killed
        public override void OnKilled(Mobile killed, Mobile killer, bool last)
        {
            if (killer != killed && XmlAttach.FindAttachment(killer, typeof(XmlPoints)) is XmlPoints xpkr && !xpkr.Deleted)
            {
                if (!s_KilledKillers.TryGetValue(this, out Stack<XmlPoints> stx))
                    s_KilledKillers[this] = stx = new Stack<XmlPoints>();
                PlayerMobile pmkiller = xpkr.m_Owner;
                // if this was a challenge duel then clear agression
                if (m_Owner == Challenger || pmkiller == Challenger || AreInSameGame(xpkr, this))
                {
                    // and remove the challenger from the aggressor list so that the res noto is not affected
                    ClearAggression(pmkiller, m_Owner);
                    ClearAggression(m_Owner, pmkiller);
                    // and remove the challenger from the corpse aggressor list so that the corpse noto is not affected
                    if (m_Owner.Corpse is Corpse c)
                    {
                        List<Mobile> klist = c.Aggressors;
                        if (klist != null)
                        {
                            for (int j = 0; j < klist.Count; j++)
                            {
                                if (klist[j] == pmkiller)
                                {
                                    klist.Remove(pmkiller);
                                    break;
                                }
                            }
                        }
                    }
                }

                // check to see whether points can be taken
                if (CanAffectPoints(xpkr, this, false))
                {
                    DamageEntry de = m_Owner.FindDamageEntryFor(xpkr.m_Owner);
                    if (de != null && de.DamageGiven > 0)
                    {
                        stx.Push(xpkr);
                    }
                }

                // handle challenge team kills
                if (ChallengeGame != null && !ChallengeGame.Deleted && ChallengeGame.GetParticipant(pmkiller) != null && ChallengeGame.GetParticipant(m_Owner) != null)
                {
                    ChallengeGame.OnKillPlayer(pmkiller, m_Owner);
                    ChallengeGame.OnPlayerKilled(pmkiller, m_Owner);
                }

                if (m_Owner == Challenger || pmkiller == Challenger)
                {
                    Challenger = null;
                    xpkr.Challenger = null;

                    if (AutoResAfterDuel)
                    {
                        // immediately bless the corpse to prevent looting
                        if (m_Owner.Corpse != null)
                            m_Owner.Corpse.LootType = LootType.Blessed;

                        // prepare the autores callback
                        Timer.DelayCall(TimeSpan.FromSeconds(5), AutoRes_Callback, (m_Owner, false));
                    }

                    if (TeleportOnDuel)
                    {
                        // teleport back to original location
                        Timer.DelayCall(TimeSpan.FromSeconds(7), Return_Callback, (pmkiller, m_Owner, m_StartingLoc, m_StartingMap));
                    }
                }

                if (last)
                {
                    if (stx.Count > 0)
                        FinalizeKilled(stx);
                    //remove the stack after doing all the operations, so we free the dictionary for the next point assignment
                    s_KilledKillers.Remove(this);
                }
            }
        }

        private void FinalizeKilled(Stack<XmlPoints> stx)
        {
            StringBuilder vsb = null;
            //StringBuilder sb = null;
            // begin the section to award points
            if (m_LogKills)
                vsb = new StringBuilder($"{Core.MistedDateTime}: ");

            // give the killer his points, either a fixed amount or scaled by the difference with the points of the killed
            // if the killed has more points than the killed then gain more
            int killedpoints = Points, killers = Math.Min(3, stx.Count), lose = 0;
            int killerpoints, win = 0, cred = 0;
            PlayerMobile pmkiller = stx.Peek().m_Owner;
            double losescale = m_LoseScale + (0.025 * (killers - 1));
            //bool update = true;

            for (int i = 0; i < killers; ++i)
            {
                var xpkr = stx.Pop();
                killerpoints = xpkr.Points;
                lose += Math.Max(1, (int)((killedpoints - killerpoints) * losescale));
                win += Math.Max(1, (int)((killedpoints - killerpoints) * m_WinScale));
                cred += Math.Max(1, (int)((killedpoints - killerpoints) * m_CreditScale));
                //avoid unnecessary division operand and relative bitshifting for assignation
                if (i == 0)
                {
                    xpkr.Points += win;
                    xpkr.Credits += cred;
                    //comunicate to the first one the result
                    xpkr.m_Owner.SendLocalizedMessage(505723, $"{win}\t{m_Owner.GetNameFor(xpkr.m_Owner)}");// "You receive {0} points for killing {1}"
                    xpkr.KillList.Add(new KillEntry(m_Owner, DateTime.UtcNow, TimeSpan.FromSeconds(Utility.RandomMinMax(KILL_DELAY >> 1, KILL_DELAY))));
                    if (m_GainHonorFromDuel)
                    {
                        bool gainedPath = false;
                        if (VirtueHelper.Award(pmkiller, VirtueName.Honor, win, ref gainedPath))
                        {
                            if (gainedPath)
                            {
                                pmkiller.SendLocalizedMessage(1063226); // You have gained a path in Honor!
                            }
                            else
                            {
                                pmkiller.SendLocalizedMessage(1063225); // You have gained in Honor.
                            }
                        }
                    }
                    if (m_LogKills)
                        vsb.Append($"{pmkiller} [{pmkiller.Account}] scores a kill (kp/cp +{win}/+{cred})");

                    // if broadcast is enabled then announce it
                    //if (Broadcast && m_SystemBroadcast)
                    //{
                    //    BroadcastCombatResult(pmkiller, m_Owner);  // "{0} has defeated {1} in combat."
                    //}
                }
                else
                {
                    //this will create a medium point assignation, plus the second (first assist) will get the medium points divided by 2, the third (or second assist) will get it divided by 4
                    int val = Math.Max(1, (win / (i + 1)) >> i);
                    //if (val > 0)
                    //{
                    int cval;
                    xpkr.Points += val;
                    xpkr.m_Owner.SendLocalizedMessage(505747, $"{val}\t{m_Owner.GetNameFor(xpkr.m_Owner)}");// You receive ~1_VAL~ points for the assist in killing ~2_VAL~
                    cval = Math.Max(1, (cred / (i + 1)) >> i);
                    //if (cval > 0)
                    xpkr.Credits += val;
                    if (m_LogKills)
                        vsb.Append($" {i}° assist {xpkr.m_Owner} [{xpkr.m_Owner.Account}] (kp/cp +{val}/+{cval})");

                    //only delay the next kill if we really get a revenue for the kill, at least in terms of POINTS, credits may vary
                    xpkr.KillList.Add(new KillEntry(m_Owner, DateTime.UtcNow, TimeSpan.FromSeconds(Utility.RandomMinMax(KILL_DELAY >> 1, KILL_DELAY) >> i)));
                    //update = true;
                    //}
                    //else
                    //{
                    //    update = false;
                    //    xpkr.m_Owner.SendLocalizedMessage(505748, m_Owner.GetNameFor(xpkr.m_Owner));//You gave an assist in killing ~1_val~, but his points are not enough for you!
                    //}
                }
                //if (update)
                {
                    //raise count only if we gain at least points from the kill
                    xpkr.Kills++;
                    xpkr.LastKill = DateTime.UtcNow;
                    UpdateRanking(xpkr.m_Owner, xpkr);
                    // update the points gump if it is open
                    if (xpkr.m_Owner.HasGump(typeof(PointsGump)))
                    {
                        // redisplay it with the new info
                        xpkr.OnIdentify(xpkr.m_Owner);
                    }
                    // update the top players gump if it is open
                    if (xpkr.m_Owner.FindGump(typeof(TopPlayersGump)) is TopPlayersGump gump)
                    {
                        gump = new TopPlayersGump(xpkr.m_Owner, gump.TownFilter, gump.GuildFilter, gump.NameFilter);
                        xpkr.m_Owner.CloseGump(typeof(TopPlayersGump));
                        xpkr.m_Owner.SendGump(gump);
                    }
                }
            }
            // take points from the killed, either a fixed amount or scaled by the difference with the points of the killer
            // if the killer has fewer points than the killed then lose more
            int variance = Points;
            Points -= lose / killers;
            // comment out this code if you dont want to have a zero floor and want to allow negative points
            if (Points < 0)
                Points = 0;
            variance -= Points;
            if (m_LogKills)
            {
                vsb.Append($" - KILLED {m_Owner} [{m_Owner.Account}] (kp -{variance})");
                try
                {
                    if (!Directory.Exists("Logs/Vari"))
                    {
                        Directory.CreateDirectory("Logs/Vari");
                    }

                    using (StreamWriter op = new StreamWriter("Logs/Vari/XmlPointKills.log", true))
                    {
                        op.WriteLine(vsb.ToString());
                    }
                }
                catch { }
            }
            Deaths++;
            LastDeath = DateTime.UtcNow;

            if (variance > 0)
            {
                // prepare the message to report the point loss.  Need the delay otherwise it wont show up due to the death sequence
                Timer.DelayCall(TimeSpan.FromSeconds(3), ReportPointLoss_Callback, (variance, pmkiller.GetNameFor(m_Owner), m_Owner));

                // update the overall ranking list
            }
            //vsb.Dispose();
            UpdateRanking(m_Owner, this);
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // only allow attachment to players
            m_Owner = AttachedTo as PlayerMobile;
            if (m_Owner == null)
                Delete();
        }

        public override void OnDelete()
        {
            base.OnDelete();
            m_Owner = null;
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            // uncomment this if you dont want players being able to check points/rank on other players
            //if((from != null) && (AttachedTo != from) && (from.AccessLevel == AccessLevel.Player)) return null;

            if (m_Owner != null)
            {
                sbyte offset;
                if(from != null && from.NetState != null)
                {
                    offset = from.NetState.TimeOffset;
                }
                else
                {
                    offset = 0;
                }
                StringBuilder msg = new StringBuilder();

                RefreshKillList();

                if (KillList.Count > 0)
                {
                    foreach (KillEntry k in KillList)
                    {
                        if (k.Killed != null && !k.Killed.Deleted)
                        {
                            msg.Append("<br>");
                            msg.AppendFormat($"{k.Killed.GetNameFor(from)} killed at {k.WhenKilled.AddHours(offset)}");// "{0} killed at {1}"
                        }
                    }
                }

                // display the points info gump
                if (from != null)
                {
                    from.CloseGump(typeof(PointsGump));
                    from.SendGump(new PointsGump(this, from, m_Owner, msg.ToString()));
                }
            }

            return null;
        }

		//********************************************************************
		//** Gumps section
		//********************************************************************

        public class PointsGump : Gump
        {
            XmlPoints m_attachment;
            Mobile m_target;
            string m_text;

            public PointsGump(XmlPoints a, Mobile from, Mobile target, string text) : base(0, 0)
            {
                if (from == null || target == null || a == null) return;

                int cliloc = 505727;
                if (target == from || from.AccessLevel > AccessLevel.Counselor)
                    cliloc++;

                m_attachment = a;
                m_target = target;
                if (string.IsNullOrEmpty(text))
                    text = " ";
                m_text = text;

                // prepare the page
                AddPage(0);

                int val = GetRanking(target);

                StringBuilder msg = new StringBuilder();
                msg.Append(target.GetRawNameFor(from));//Points Standing for ~1_val~
                msg.Append('\t');
                msg.Append(a.Points - STARTING_POINTS);// "Current Points = {0}"
                msg.Append('\t');
                if (val > 0)
                {
                    msg.Append(val);// "Rank = {0}"
                }
                else
                {
                    msg.Append("#505729");// "No ranking."
                }
                msg.Append('\t');
                // report the number of Credits available if the player is checking.  Dont display this if others are checking (unless they are staff).
                if (cliloc > 505727)
                {
                    msg.Append(a.Credits);// "Available Credits = {0}"
                    msg.Append('\t');
                }
                msg.Append(a.Kills);// "Total Kills = {0}"
                msg.Append('\t');
                msg.Append(a.Deaths);// "Total Deaths = {1}"
                msg.Append('\t');
                msg.Append(m_text);

                if (from == target)
                {
                    AddBackground(0, 0, 440, 295, 5054);
                    AddAlphaRegion(0, 0, 440, 295);
                }
                else
                {
                    AddBackground(0, 0, 440, 190, 5054);
                    AddAlphaRegion(0, 0, 440, 190);
                }

                // 1 on 1 duel status
                if (a.Challenger != null)
                {
                    int challengehue = 505732;

                    if (a.m_CancelTimer != null && a.m_CancelTimer.Running)
                        challengehue = 505732;
                    // also check the challenger timer to see if he is cancelling
                    if (XmlAttach.FindAttachment(a.Challenger, typeof(XmlPoints)) is XmlPoints ca && !ca.Deleted)
                    {
                        if ((ca.m_CancelTimer != null && ca.m_CancelTimer.Running) || (ca.ChallengeGame != null && ca.ChallengeGame.ChallengeBeingCancelled))
                            challengehue = 505732;
                    }

                    AddHtmlLocalized(20, 143, 400, 40, challengehue, a.Challenger.GetRawNameFor(from));// "Currently challenging {0}"
                }
                else
                    // challenge game status
                    if (a.ChallengeGame != null && !a.ChallengeGame.Deleted)
                {
                    AddLabel(50, 143, 68, String.Format("{0}", a.ChallengeGame.ChallengeName));
                    // add the info button that will open the game gump
                    AddButton(23, 143, 0x5689, 0x568A, 310, GumpButtonType.Reply, 0);

                }

                AddHtmlLocalized(20, 20, 400, 120, cliloc, msg.ToString(), true, true);

                //int x1 = 20;
				//int x2 = 150;
                int x3 = 290;

                if (from == target)
                {
                    // add the see kills checkbox
                    //AddLabel(x1 + 30, 165, 55, "See kills");
					//AddButton( x1, 165, (a.ReceiveBroadcasts ? 0xD3 :0xD2), (a.ReceiveBroadcasts ? 0xD2 :0xD3), 100, GumpButtonType.Reply, 0);

                    // add the broadcast kills checkbox
                    //AddLabel(x2 + 30, 165, 55, "Broadcast kills");
					//AddButton( x2, 165, (a.Broadcast ? 0xD3 :0xD2), (a.Broadcast ? 0xD2 :0xD3), 200, GumpButtonType.Reply, 0);

                    // add the topplayers button
                    AddLabel(x3 + 30, 165, 55, "Top players");
                    AddButton(x3, 165, 0xFAB, 0xFAD, 300, GumpButtonType.Reply, 0);

                    //// add the challenge button
                    //AddLabel(x1 + 30, 190, 55, "Challenge");
					//AddButton( x1, 190, 0xFAB, 0xFAD, 400, GumpButtonType.Reply, 0);
                    //
                    //// add the last man standing challenge button
                    //AddLabel(x2 + 30, 190, 55, "LMS");
					//AddButton( x2, 190, 0xFAB, 0xFAD, 401, GumpButtonType.Reply, 0);
                    //
                    //// add the deathmatch challenge button
                    //AddLabel(x3 + 30, 190, 55, "Deathmatch");
					//AddButton( x3, 190, 0xFAB, 0xFAD, 403, GumpButtonType.Reply, 0);
                    //
                    //// add the kingofthehill challenge button
                    //AddLabel(x1 + 30, 215, 55, "KotH");
					//AddButton( x1, 215, 0xFAB, 0xFAD, 404, GumpButtonType.Reply, 0);
                    //
                    //// add the deathball challenge button
                    //AddLabel(x2 + 30, 215, 55, "DeathBall");
					//AddButton( x2, 215, 0xFAB, 0xFAD, 405, GumpButtonType.Reply, 0);
                    //
                    //// add the teamlms challenge button
                    //AddLabel(x3 + 30, 215, 55, "Team LMS");
					//AddButton( x3, 215, 0xFAB, 0xFAD, 406, GumpButtonType.Reply, 0);
                    //
                    //// add the team deathmatch challenge button
                    //AddLabel(x1 + 30, 240, 55, "Team DMatch");
					//AddButton( x1, 240, 0xFAB, 0xFAD, 407, GumpButtonType.Reply, 0);
                    //
                    //// add the team deathball challenge button
                    //AddLabel(x2 + 30, 240, 55, "Team DBall");
					//AddButton( x2, 240, 0xFAB, 0xFAD, 408, GumpButtonType.Reply, 0);
                    //
                    //// add the team KotH challenge button
                    //AddLabel(x3 + 30, 240, 55, "Team KotH");
					//AddButton( x3, 240, 0xFAB, 0xFAD, 409, GumpButtonType.Reply, 0);
                    //
                    //// add the CTF challenge button
                    //AddLabel(x1 + 30, 265, 55, "CTF");
					//AddButton( x1, 265, 0xFAB, 0xFAD, 410, GumpButtonType.Reply, 0);
                }
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (m_attachment == null || state == null || !(state.Mobile is PlayerMobile pm) || info == null) return;

                switch (info.ButtonID)
                {
                    //case 100:
					//	// toggle see kills
					//	m_attachment.ReceiveBroadcasts = !m_attachment.ReceiveBroadcasts;
                    //
					//	pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
					//	break;
                    //case 200:
					//	// toggle broadcast my kills
					//	m_attachment.Broadcast = !m_attachment.Broadcast;
                    //
					//	pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
					//	break;
                    case 300:
                        // top players
                        if (pm.FindGump(typeof(TopPlayersGump)) is TopPlayersGump g)
                        {
                            g = new TopPlayersGump(pm, g.TownFilter, g.GuildFilter, g.NameFilter);
                            pm.CloseGump(typeof(TopPlayersGump));
                        }
                        else
                            g = new TopPlayersGump(pm);
                        pm.SendGump(g);

                        pm.SendGump(new PointsGump(m_attachment, pm, m_target, m_text));
                        break;
                    case 310:
                        // Challenge game info
                        if (m_attachment.ChallengeGame != null && !m_attachment.ChallengeGame.Deleted)
                            m_attachment.ChallengeGame.OnDoubleClick(pm);

                        pm.SendGump(new PointsGump(m_attachment, pm, m_target, m_text));
                        break;
                        //case 400:
                        //    // 1 on 1 challenge duel
                        //    pm.Target = new ChallengeTarget(pm);
                        //
                        //    pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
                        //    break;
                        //case 401:
                        //    // last man standing
                        //    BaseChallengeGame.DoSetupChallenge(pm, 100302, typeof(LastManStandingGauntlet));
                        //
                        //    pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
                        //    break;
                        //case 403:
                        //    // deathmatch challenge
                        //    BaseChallengeGame.DoSetupChallenge(pm, 100400, typeof(DeathmatchGauntlet));
                        //
                        //    pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
                        //    break;
                        //case 404:
                        //    // kingofthehill challenge
                        //    BaseChallengeGame.DoSetupChallenge(pm, 100410, typeof(KingOfTheHillGauntlet));
                        //
                        //    pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
                        //    break;
                        //case 405:
                        //    // deathball challenge
                        //    BaseChallengeGame.DoSetupChallenge(pm, 100411, typeof(DeathBallGauntlet));
                        //
                        //    pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
                        //    break;
                        //case 406:
                        //    // team lms challenge
                        //    BaseChallengeGame.DoSetupChallenge(pm, 100413, typeof(TeamLMSGauntlet));
                        //
                        //    pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
                        //    break;
                        //case 407:
                        //    // team deathmatch challenge
                        //    BaseChallengeGame.DoSetupChallenge(pm, 100415, typeof(TeamDeathmatchGauntlet));
                        //
                        //    pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
                        //    break;
                        //case 408:
                        //    // team deathball challenge
                        //    BaseChallengeGame.DoSetupChallenge(pm, 100416, typeof(TeamDeathballGauntlet));
                        //
                        //    pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
                        //    break;
                        //case 409:
                        //    // team KotH challenge
                        //    BaseChallengeGame.DoSetupChallenge(pm, 100417, typeof(TeamKotHGauntlet));
                        //
                        //    pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
                        //    break;
                        //case 410:
                        //    // CTF challenge
                        //    BaseChallengeGame.DoSetupChallenge(pm, 100418, typeof(CTFGauntlet));
                        //
                        //    pm.SendGump( new PointsGump(m_attachment, pm, m_target, m_text));
                        //    break;
                        //    
                }
            }
        }

        public class TopPlayersGump : Gump
        {
            public string TownFilter { get; private set; }
            public string GuildFilter { get; private set; }
            public string NameFilter { get; private set; }

            public TopPlayersGump(Mobile from, string townfilt = null, string guildfilt = null, string namefilt = null) : base(0, 0)
            {
                TownFilter = townfilt;
                GuildFilter = guildfilt;
                NameFilter = namefilt;
                int numberToDisplay = 20;
                int height = numberToDisplay * 20 + 65;

                // prepare the page
                AddPage(0);

                int width = 800;

                AddBackground(0, 0, width, height, 5054);
                AddAlphaRegion(0, 0, width, height);
                AddImageTiled(20, 20, width - 40, height - 45, 0xBBC);
                AddLabel(20, 2, 55, "Migliori giocatori");

                // faction filter
                AddLabel(40, height - 20, 55, "Filtro Fazione");
                AddImageTiled(130, height - 20, 100, 19, 0xBBC);
                AddTextEntry(130, height - 20, 100, 19, 0, 200, TownFilter, 12);

                AddButton(20, height - 20, 0x15E1, 0x15E5, 200, GumpButtonType.Reply, 0);

                // name filter
                AddLabel(260, height - 20, 55, "Filtro Nome");  //
                AddImageTiled(340, height - 20, 160, 19, 0xBBC);
                AddTextEntry(340, height - 20, 160, 19, 0, 100, NameFilter, 22);

                AddButton(240, height - 20, 0x15E1, 0x15E5, 100, GumpButtonType.Reply, 0);

                // guild filter
                AddLabel(540, height - 20, 55, "Filtro Gilda");  //
                AddImageTiled(620, height - 20, 150, 19, 0xBBC);
                AddTextEntry(620, height - 20, 150, 19, 0, 300, GuildFilter, 20);

                AddButton(520, height - 20, 0x15E1, 0x15E5, 300, GumpButtonType.Reply, 0);

                RefreshRankList();

                int xloc = 23;
                AddLabel(xloc, 20, 0, "Nome");
                xloc += 177;
                AddLabel(xloc, 20, 0, "Fazione");
                xloc += 85;
                AddLabel(xloc, 20, 0, "Gilda");
                xloc += 75;
                AddLabel(xloc, 20, 0, "Punti");
                xloc += 60;
                AddLabel(xloc, 20, 0, "Kill");
                xloc += 60;
                AddLabel(xloc, 20, 0, "Morto");
                xloc += 60;
                AddLabel(xloc, 20, 0, "Rank");
                xloc += 45;
                AddLabel(xloc, 20, 0, "Cambi");
                xloc += 45;
                AddLabel(xloc, 20, 0, "Tempo in Rank");

                // go through the sorted list and display the top ranked players

                int y = 40;
                int count = 0;
                for (int i = 0; i < RankList.Count; ++i)
                {
                    if (count >= numberToDisplay)
                    {
                        break;
                    }

                    RankEntry r = RankList[i];

                    if (r == null)
                    {
                        continue;
                    }

                    XmlPoints a = r.PointsAttachment;

                    if (a == null)
                    {
                        continue;
                    }

                    if (r.Killer != null && !r.Killer.Deleted && r.Rank > 0 && a != null && !a.Deleted)
                    {
                        string townname = null;

                        if (r.Killer.Town != null)
                        {
                            townname = r.Killer.Town.Name;
                        }

                        string guildname = null;

                        if (r.Killer.Guild != null && !r.Killer.Guild.Disbanded && !string.IsNullOrWhiteSpace(r.Killer.Guild.ShortAbbreviation))
                        {
                            guildname = r.Killer.Guild.ShortAbbreviation;
                        }
                        // check for any ranking change and update rank date
                        if (r.Rank != a.Rank)
                        {
                            a.WhenRanked = DateTime.UtcNow;
                            if (a.Rank > 0)
                            {
                                a.DeltaRank = a.Rank - r.Rank;
                            }

                            a.Rank = r.Rank;
                        }

                        // check for town filter
                        if (!string.IsNullOrEmpty(TownFilter))
                        {
                            if (string.IsNullOrEmpty(townname))
                            {
                                continue;
                            }
                            // parse the comma separated list
                            string[] args = TownFilter.Split(',');
                            if (args != null)
                            {
                                bool found = false;
                                string invariant = townname.ToLowerInvariant();
                                foreach (string arg in args)
                                {
                                    if (arg != null && invariant.Contains(arg.ToLowerInvariant().Trim()))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    continue;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(GuildFilter))
                        {
                            if (string.IsNullOrEmpty(guildname))
                            {
                                continue;
                            }
                            // parse the comma separated list
                            string[] args = GuildFilter.Split(',');
                            if (args != null)
                            {
                                bool found = false;
                                string invariant = guildname.ToLowerInvariant();
                                foreach (string arg in args)
                                {
                                    if (arg != null && invariant.Contains(arg.ToLowerInvariant().Trim()))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    continue;
                                }
                            }
                        }

                        // check for name filter
                        if (!string.IsNullOrEmpty(NameFilter))
                        {
                            if (string.IsNullOrEmpty(r.Killer.GetRawNameFor(from)))
                            {
                                continue;
                            }
                            // parse the comma separated list
                            string[] args = NameFilter.Split(',');
                            if (args != null)
                            {
                                string invariant = r.Killer.GetRawNameFor(from).ToLowerInvariant();
                                bool found = false;
                                foreach (string arg in args)
                                {
                                    if (arg != null && invariant.Contains(arg.ToLowerInvariant().Trim()))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    continue;
                                }
                            }
                        }

                        ++count;

                        TimeSpan timeranked = DateTime.UtcNow - a.WhenRanked;

                        int days = (int)timeranked.TotalDays;
                        int hours = (int)(timeranked.TotalHours - days * 24);
                        int mins = (int)(timeranked.TotalMinutes - ((int)timeranked.TotalHours) * 60);

                        xloc = 23;
                        AddLabel(xloc, y, 0, r.Killer.GetRawNameFor(from));
                        xloc += 177;
                        AddLabel(xloc, y, 0, townname);
                        xloc += 85;
                        AddLabel(xloc, y, 0, guildname);
                        xloc += 75;
                        AddLabel(xloc, y, 0, a.Points.ToString());
                        xloc += 60;
                        AddLabel(xloc, y, 0, a.Kills.ToString());
                        xloc += 60;
                        AddLabel(xloc, y, 0, a.Deaths.ToString());
                        xloc += 60;
                        AddLabel(xloc, y, 0, a.Rank.ToString());

                        string label = null;

                        if (days > 0)
                        {
                            label += string.Format("{0} giorni ", days);
                        }

                        if (hours > 0)
                        {
                            label += string.Format("{0} ore ", hours);
                        }

                        if (mins > 0)
                        {
                            label += string.Format("{0} minuti", mins);
                        }

                        if (label == null)
                        {
                            label = "appena cambiato";
                        }

                        string deltalabel = a.DeltaRank.ToString();
                        int deltahue = 0;
                        if (a.DeltaRank > 0)
                        {
                            deltalabel = string.Format("+{0}", a.DeltaRank);
                            deltahue = 68;
                        }
                        else
                            if (a.DeltaRank < 0)
                        {
                            deltahue = 33;
                        }
                        xloc += 50;
                        AddLabel(xloc, y, deltahue, deltalabel);
                        xloc += 40;
                        AddLabel(xloc, y, 0, label);

                        y += 20;
                    }
                }
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (state == null || state.Mobile == null || info == null)
                {
                    return;
                }
                // Get the current name
                TextRelay entry = info.GetTextEntry(200);
                if (entry != null)
                {
                    TownFilter = entry.Text;
                }

                entry = info.GetTextEntry(100);
                if (entry != null)
                {
                    NameFilter = entry.Text;
                }

                entry = info.GetTextEntry(300);
                if (entry != null)
                {
                    GuildFilter = entry.Text;
                }

                switch (info.ButtonID)
                {
                    case 100:
                    case 200:
                    case 300:
                    {
                        // redisplay the gump
                        state.Mobile.SendGump(new TopPlayersGump(state.Mobile, TownFilter, GuildFilter, NameFilter));
                        break;
                    }
                }
            }
        }

        private class IssueChallengeGump : Gump
        {
            private XmlPoints m_From;
            private XmlPoints m_Target;

            public IssueChallengeGump(XmlPoints afrom, XmlPoints atarg) : base(0, 0)
            {
                if (afrom != null && atarg != null)
                {
                    if (!AllowChallengeGump(afrom.m_Owner, atarg.m_Owner))
                    {
                        afrom.m_Owner.SendLocalizedMessage(505730); // "You cannot issue a challenge here."
                        return;
                    }

                    m_From = afrom;
                    m_Target = atarg;

                    afrom.m_Owner.CloseGump(typeof(IssueChallengeGump));

                    // figure out how many duel locations
                    int locsize = XmlPoints.DuelLocations.Length;

                    if (!TeleportOnDuel) locsize = 0;

                    int height = 170 + locsize * 30;

                    Closable = false;
                    Dragable = true;
                    AddPage(0);
                    AddBackground(10, 200, 200, height, 5054);

                    AddLabel(20, 205, 68, "You are challenging");
                    AddLabel(20, 225, 68, $"{atarg.m_Owner.GetNameFor(afrom.m_Owner)}. Continue?");


                    // display the available duel locations

                    int y = 250;
                    int texthue = 0;

                    AddLabel(55, y, texthue, "Cancel");
                    AddRadio(20, y, 9721, 9724, true, 0);
                    y += 30;

                    AddLabel(55, y, texthue, "Duel here");
                    AddRadio(20, y, 9721, 9724, false, 1);
                    y += 30;

                    // block teleporting if in a recall-restricted region
                    if (TeleportOnDuel && SpellHelper.CheckTravel(afrom.m_Owner.Map, afrom.m_Owner.Location, TravelCheckType.RecallFrom) && SpellHelper.CheckTravel(atarg.m_Owner.Map, atarg.m_Owner.Location, TravelCheckType.RecallFrom))
                    {
                        for (int i = 0; i < XmlPoints.DuelLocations.Length; ++i)
                        {
                            // check availability

                            if (!DuelLocationAvailable(XmlPoints.DuelLocations[i]))
                            {
                                texthue = 33;
                            }
                            else
                            {
                                texthue = 0;
                            }
                            AddLabel(55, y, texthue, XmlPoints.DuelLocations[i].Name);
                            AddRadio(20, y, 9721, 9724, false, i + 2);
                            y += 30;

                        }
                    }

                    // check to see if points can be gained from this
                    if (afrom == null || afrom.Deleted || atarg == null || atarg.Deleted || !afrom.CanAffectPoints(afrom, atarg, true))
                    {
                        AddLabel(20, y, 33, "You will NOT gain points!");
                    }
                    y += 30;
                    
                    //y += 25;
                    //AddRadio( 35, y, 9721, 9724, false, 1 ); // accept/yes radio
                    //AddRadio( 135, y, 9721, 9724, true, 2 ); // decline/no radio
                    //
                    //AddHtmlLocalized(72, y, 200, 30, 1049016, 0x7fff , false , false ); // Yes
                    //AddHtmlLocalized(172, y, 200, 30, 1049017, 0x7fff , false , false ); // No

                    AddButton(80, y, 2130, 2129, 3, GumpButtonType.Reply, 0); // Okay button
                }
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {

                if (info == null || state == null || state.Mobile == null) return;

                int radiostate = -1;
                if (info.Switches.Length > 0)
                {
                    radiostate = info.Switches[0];
                }



                switch (info.ButtonID)
                {
                    default:
                    {
                        if (radiostate > 1)
                        {
                            // issue the challenge
                            m_Target.m_Owner.SendGump(new ConfirmChallengeGump(m_From, m_Target, XmlPoints.DuelLocations[radiostate - 2]));
                            m_From.m_Owner.SendMessage($"You have issued a challenge to {m_Target.m_Owner.GetNameFor(m_From.m_Owner)}.");
                        }
                        else
                            if (radiostate == 1)
                        {
                            // issue the challenge
                            m_Target.m_Owner.SendGump(new ConfirmChallengeGump(m_From, m_Target, null));
                            m_From.m_Owner.SendMessage($"You have issued a challenge to {m_Target.m_Owner.GetNameFor(m_From.m_Owner)}.");

                        }
                        else
                            if (radiostate == 0)
                        {
                            if (m_From != null)
                                m_From.m_Owner.SendMessage($"You decided against challenging {m_Target.m_Owner.GetNameFor(m_From.m_Owner)}.");
                        }
                        break;
                    }
                }
            }
        }


        private class ConfirmChallengeGump : Gump
        {
            private XmlPoints m_AFrom;
            private XmlPoints m_ATarget;
            private DuelLocationEntry m_DuelLocation;

            public ConfirmChallengeGump(XmlPoints afrom, XmlPoints atarg, DuelLocationEntry duelloc) : base(0, 0)
            {
                if (atarg == null || afrom == null) return;

                // uncomment the line below to log all challenges into the command log
                //CommandLogging.WriteLine( from, "{0} {1} challenged {2}", from.AccessLevel, CommandLogging.Format( from ), CommandLogging.Format( target ));
                m_AFrom = afrom;
                m_ATarget = atarg;
                PlayerMobile from = afrom.m_Owner, target = atarg.m_Owner;
                m_DuelLocation = duelloc;

                Closable = false;
                Dragable = true;
                AddPage(0);
                AddBackground(10, 200, 200, 150, 5054);

                AddLabel(20, 205, 68, "You have been challenged by");
                AddLabel(20, 225, 68, $"{from.GetNameFor(target)}. Accept?");

                int y = 250;
                if (m_DuelLocation != null)
                {
                    AddLabel(20, y, 0, $"Location: {m_DuelLocation.Name}");
                }
                else
                {
                    AddLabel(20, y, 0, "Location: Duel Here");
                }
                y += 20;

                if (afrom == null || afrom.Deleted || atarg == null || atarg.Deleted || !atarg.CanAffectPoints(atarg, afrom, true))
                {
                    AddLabel(20, y, 33, "You will NOT gain points!");
                }

                AddRadio(35, 290, 9721, 9724, false, 1); // accept/yes radio
                AddRadio(135, 290, 9721, 9724, true, 2); // decline/no radio
                AddHtmlLocalized(72, 290, 200, 30, 1049016, 0x7fff, false, false); // Yes
                AddHtmlLocalized(172, 290, 200, 30, 1049017, 0x7fff, false, false); // No

                AddButton(80, 320, 2130, 2129, 3, GumpButtonType.Reply, 0); // Okay button
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {

                if (info == null || state == null || state.Mobile == null || m_AFrom == null || m_ATarget == null) return;
                PlayerMobile from = m_AFrom.m_Owner, target = m_ATarget.m_Owner;

                int radiostate = -1;
                if (info.Switches.Length > 0)
                {
                    radiostate = info.Switches[0];
                }


                switch (info.ButtonID)
                {
                    default:
                    {
                        if (radiostate == 1)
                        {
                            // challenge accept
                            // check to make sure the duel location is available
                            if (m_DuelLocation != null && !DuelLocationAvailable(m_DuelLocation))
                            {
                                target.SendLocalizedMessage(505735, target.GetNameFor(from));
                                return;
                            }

                            // make sure neither participant is in combat
                            if (CheckCombat(from))
                            {
                                from.SendLocalizedMessage(505721, target.GetNameFor(from));// "Challenge with {0} has been cancelled"
                                from.SendLocalizedMessage(505722, target.GetNameFor(from));// "{0} is in combat."
                                target.SendLocalizedMessage(505721, from.GetNameFor(target));// "Challenge with {0} has been cancelled"
                                target.SendLocalizedMessage(505722, from.GetNameFor(target));// "{0} is in combat."
                                return;
                            }

                            // make sure neither participant is in combat
                            if (CheckCombat(target))
                            {
                                from.SendLocalizedMessage(505721, target.GetNameFor(from));// "Challenge with {0} has been cancelled"
                                from.SendLocalizedMessage(505722, target.GetNameFor(from));// "{0} is in combat."
                                target.SendLocalizedMessage(505721, from.GetNameFor(target));// "Challenge with {0} has been cancelled"
                                target.SendLocalizedMessage(505722, from.GetNameFor(target));// "{0} is in combat."
                                return;
                            }

                            // first confirm that they dont already have a challenge going
                            if (m_AFrom.Challenger != null || m_AFrom.ChallengeGame != null)
                            {
                                target.SendLocalizedMessage(505736, from.GetNameFor(target));//"{0} has already been challenged."
                                from.SendLocalizedMessage(505737);//"You are already being challenged."
                                return;
                            }

                            // first confirm that they dont already have a challenge going
                            if (m_ATarget.Challenger != null || m_ATarget.ChallengeGame != null)
                            {
                                target.SendLocalizedMessage(505737);// "You are already being challenged."
                                from.SendLocalizedMessage(505736, target.GetNameFor(from));// "{0} has already been challenged."
                                return;
                            }

                            // if they accept then assign the challenger fields on their points attachments
                            if (m_AFrom != null && !m_AFrom.Deleted)
                            {
                                m_AFrom.Challenger = target;
                            }

                            // assign the challenger field on the target points attachment
                            m_ATarget.Challenger = from;

                            // notify the challenger and set up noto
                            from.SendLocalizedMessage(505738, target.GetNameFor(from));// "{0} accepted your challenge!"
                            from.Send(new MobileMoving(target, Notoriety.Compute(from, target), from));

                            // update the points gump if it is open
                            if (from.HasGump(typeof(PointsGump)))
                            {
                                // redisplay it with the new info
                                if (m_AFrom != null && !m_AFrom.Deleted)
                                    m_AFrom.OnIdentify(from);
                            }

                            // notify the challenged and set up noto
                            target.SendLocalizedMessage(505739, from.GetNameFor(target));// "You have accepted the challenge from {0}!"
                            target.Send(new MobileMoving(from, Notoriety.Compute(target, from), target));

                            // update the points gump if it is open
                            if (target.HasGump(typeof(PointsGump)))
                            {
                                // redisplay it with the new info
                                if (m_ATarget != null && !m_ATarget.Deleted)
                                    m_ATarget.OnIdentify(target);
                            }

                            // cancel any precast spells
                            target.Spell = null;
                            target.Target = null;
                            from.Spell = null;
                            from.Target = null;

                            // let the challenger pick the dueling site
                            if (TeleportOnDuel && m_DuelLocation != null)
                            {

                                Point3D duelloc = m_DuelLocation.DuelLocation;
                                Map duelmap = m_DuelLocation.DuelMap;
                                m_AFrom.m_StartingLoc = from.Location;
                                m_AFrom.m_StartingMap = from.Map;
                                m_ATarget.m_StartingLoc = target.Location;
                                m_ATarget.m_StartingMap = target.Map;
                                target.MoveToWorld(duelloc, duelmap);

                                // move over by 1
                                duelloc.X += 1;
                                from.MoveToWorld(duelloc, duelmap);
                            }
                            else
                            {
                                m_AFrom.m_StartingMap = null;
                                m_ATarget.m_StartingMap = null;
                            }

                        }
                        else
                        {
                            from.SendLocalizedMessage(505740, target.GetNameFor(from));// "Your challenge to {0} was declined."
                            target.SendLocalizedMessage(505741, from.GetNameFor(target));// "You declined the challenge by {0}."
                        }
                        break;
                    }
                }
            }
        }
    }
}