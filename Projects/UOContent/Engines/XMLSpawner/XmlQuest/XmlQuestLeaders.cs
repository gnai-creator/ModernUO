using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Engines.XmlSpawner2
{

    public class XmlQuestLeaders
    {
        private static Dictionary<Mobile, QuestRankEntry> UnrankedQuestList = new Dictionary<Mobile, QuestRankEntry>();
        private static List<QuestRankEntry> QuestRankList = new List<QuestRankEntry>();
        private static bool needsupdate = true;

        public class QuestRankEntry : IComparable<QuestRankEntry>
        {
            public Mobile Quester;
            public int Rank;
            public XmlQuestPoints QuestPointsAttachment;

            public QuestRankEntry(Mobile m, XmlQuestPoints attachment)
            {
                Quester = m;
                QuestPointsAttachment = attachment;
            }

            public int CompareTo(QuestRankEntry p)
            {
                if (p.QuestPointsAttachment == null || QuestPointsAttachment == null)
                {
                    return 0;
                }

                // break points ties with quests completed (more quests means higher rank)
                if (p.QuestPointsAttachment.Points - QuestPointsAttachment.Points == 0)
                {
                    // if kills are the same then compare previous rank
                    if (p.QuestPointsAttachment.QuestsCompleted - QuestPointsAttachment.QuestsCompleted == 0)
                    {
                        return p.QuestPointsAttachment.Rank - QuestPointsAttachment.Rank;
                    }

                    return p.QuestPointsAttachment.QuestsCompleted - QuestPointsAttachment.QuestsCompleted;
                }

                return p.QuestPointsAttachment.Points - QuestPointsAttachment.Points;
            }
        }

        private static void RefreshQuestRankList()
        {
            if (needsupdate)
            {
                QuestRankList = new List<QuestRankEntry>(UnrankedQuestList.Values);
                QuestRankList.Sort();

                int rank = 0;
                //int prevpoints = 0;
                for (int i = 0; i < QuestRankList.Count; ++i)
                {
                    QuestRankEntry p = QuestRankList[i];

                    // bump the rank for every successive player in the list.  Players with the same points total will be
                    // ordered by quests completed
                    rank++;
                    p.Rank = rank;
                }
                needsupdate = false;
            }
        }

        public static int GetQuestRanking(Mobile m)
        {
            if (m == null)
            {
                return 0;
            }

            RefreshQuestRankList();
            // go through the sorted list and calculate rank
            if (UnrankedQuestList.TryGetValue(m, out QuestRankEntry p))
                return p.Rank;

            // rank 0 means unranked
            return 0;
        }

        public static void ReLoadRanking(Mobile quester, XmlQuestPoints attachment)
        {
            if (quester != null && attachment != null)
            {
                if (quester.AccessLevel == AccessLevel.Player && attachment.QuestsCompleted > 0)
                    UnrankedQuestList[quester] = new QuestRankEntry(quester, attachment);
            }
            else
                RefreshQuestRankList();
        }

        public static void UpdateQuestRanking(Mobile m, XmlQuestPoints attachment)
        {
            if (m == null)
                return;
            // flag the rank list for updating on the next attempt to retrieve a rank
            needsupdate = true;

            if (m.AccessLevel > AccessLevel.Player)
                UnrankedQuestList.Remove(m);
            else if (!UnrankedQuestList.TryGetValue(m, out QuestRankEntry p) || p.QuestPointsAttachment != attachment)
                UnrankedQuestList[m] = new QuestRankEntry(m, attachment);
        }

        public class TopQuestPlayersGump : Gump
        {
            public string TownFilter { get; private set; }
            public string GuildFilter { get; private set; }
            public string NameFilter { get; private set; }

            public TopQuestPlayersGump(Mobile from, string townfilt = null, string guildfilt = null, string namefilt = null) : base(0, 0)
            {
                TownFilter = townfilt;
                GuildFilter = guildfilt;
                NameFilter = namefilt;

                int numberToDisplay = 20;
                int height = numberToDisplay * 20 + 65;

                // prepare the page
                AddPage(0);

                int width = 790;

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

                RefreshQuestRankList();

                int xloc = 23;
                AddLabel(xloc, 20, 0, "Nome");
                xloc += 177;
                AddLabel(xloc, 20, 0, "Fazione");
                xloc += 85;
                AddLabel(xloc, 20, 0, "Gilda");
                xloc += 85;
                AddLabel(xloc, 20, 0, "Punti");
                xloc += 70;
                AddLabel(xloc, 20, 0, "Quest");
                //AddLabel( xloc, 20, 0, "" );
                xloc += 60;
                AddLabel(xloc, 20, 0, "Rank");
                xloc += 45;
                AddLabel(xloc, 20, 0, "Cambi");
                xloc += 45;
                AddLabel(xloc, 20, 0, "Tempo in Rank");

                // go through the sorted list and display the top ranked players

                int y = 40;
                int count = 0;
                for (int i = 0; i < QuestRankList.Count; ++i)
                {
                    if (count >= numberToDisplay)
                    {
                        break;
                    }

                    QuestRankEntry r = QuestRankList[i];

                    if (r == null)
                    {
                        continue;
                    }

                    XmlQuestPoints a = r.QuestPointsAttachment;

                    if (a == null)
                    {
                        continue;
                    }

                    if (r.Quester != null && !r.Quester.Deleted && r.Rank > 0 && a != null && !a.Deleted)
                    {
                        string townname = null;

                        if (r.Quester.Town != null)
                        {
                            townname = r.Quester.Town.Name;
                        }

                        string guildname = null;

                        if (r.Quester is PlayerMobile pm && pm.Guild != null && !pm.Guild.Disbanded && !string.IsNullOrWhiteSpace(pm.Guild.ShortAbbreviation))
                        {
                            guildname = pm.Guild.ShortAbbreviation;
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

                        // check for guild filter
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
                            if (string.IsNullOrEmpty(r.Quester.GetRawNameFor(from)))
                            {
                                continue;
                            }
                            // parse the comma separated list
                            string[] args = NameFilter.Split(',');
                            if (args != null)
                            {
                                string invariant = r.Quester.GetRawNameFor(from).ToLowerInvariant();
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

                        string quests = "???";
                        try
                        {
                            quests = a.QuestsCompleted.ToString();
                        }
                        catch { }

                        xloc = 23;
                        AddLabel(xloc, y, 0, r.Quester.GetRawNameFor(from));
                        xloc += 177;
                        AddLabel(xloc, y, 0, townname);
                        xloc += 85;
                        AddLabel(xloc, y, 0, guildname);
                        xloc += 85;
                        AddLabel(xloc, y, 0, a.Points.ToString());
                        xloc += 70;
                        AddLabel(xloc, y, 0, quests);
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
                        state.Mobile.SendGump(new TopQuestPlayersGump(state.Mobile, TownFilter, GuildFilter, NameFilter));
                        break;
                    }
                }
            }
        }
    }
}
