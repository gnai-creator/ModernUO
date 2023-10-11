using Server.Commands;
using Server.Gumps;
using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Engines.XmlSpawner2
{
    public class XmlQuestPoints : XmlAttachment
    {
        public static void Configure()
        {
            //check ranks after load, so we can get a count of invalid attachments (deleted players) and get rid of them instantaneously
            EventSink.WorldPostLoad += new WorldPostLoadEventHandler(PostLoadHandler);
        }

        private int m_Points;
        private int m_Completed;
        private int m_Credits;

        private List<XmlQuestPoints.QuestEntry> m_QuestList = new List<XmlQuestPoints.QuestEntry>();

        private DateTime m_WhenRanked;
        private int m_Rank;
        private int m_DeltaRank;

        public string townFilter;
        public string nameFilter;

        public List<XmlQuestPoints.QuestEntry> QuestList => m_QuestList;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Rank { get => m_Rank; set => m_Rank = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DeltaRank { get => m_DeltaRank; set => m_DeltaRank = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime WhenRanked { get => m_WhenRanked; set => m_WhenRanked = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Points { get => m_Points; set => m_Points = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Credits { get => m_Credits; set => m_Credits = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int QuestsCompleted { get => m_Completed; set => m_Completed = value; }

        public class QuestEntry
        {
            public Mobile Quester;
            public string Name;
            public DateTime WhenCompleted;
            public DateTime WhenStarted;
            public int Difficulty;
            public bool PartyEnabled;
            public int TimesCompleted = 1;

            public QuestEntry()
            {
            }

            public QuestEntry(Mobile m, IXmlQuest quest)
            {
                Quester = m;
                if (quest != null)
                {
                    WhenStarted = quest.TimeCreated;
                    WhenCompleted = DateTime.UtcNow;
                    Difficulty = quest.Difficulty;
                    Name = quest.Name;
                }
            }

            public virtual void Serialize(GenericWriter writer)
            {

                writer.Write(0); // version

                writer.Write(Quester);
                writer.Write(Name);
                writer.Write(WhenCompleted);
                writer.Write(WhenStarted);
                writer.Write(Difficulty);
                writer.Write(TimesCompleted);
                writer.Write(PartyEnabled);


            }

            public virtual void Deserialize(GenericReader reader)
            {

                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        Quester = reader.ReadMobile();
                        Name = reader.ReadString();
                        WhenCompleted = reader.ReadDateTime();
                        WhenStarted = reader.ReadDateTime();
                        Difficulty = reader.ReadInt();
                        TimesCompleted = reader.ReadInt();
                        PartyEnabled = reader.ReadBool();
                        break;
                }

            }

            public static void AddQuestEntry(Mobile m, IXmlQuest quest)
            {
                if (m == null || quest == null)
                {
                    return;
                }

                // get the XmlQuestPoints attachment from the mobile
                if (!(XmlAttach.FindAttachment(m, typeof(XmlQuestPoints)) is XmlQuestPoints p))
                {
                    return;
                }

                // look through the list of quests and see if it is one that has already been done
                if (p.m_QuestList == null)
                {
                    p.m_QuestList = new List<XmlQuestPoints.QuestEntry>();
                }

                bool found = false;
                foreach (QuestEntry e in p.m_QuestList)
                {
                    if (e.Name == quest.Name)
                    {
                        // found a match, so just change the number and dates
                        e.TimesCompleted++;
                        e.WhenStarted = quest.TimeCreated;
                        e.WhenCompleted = DateTime.UtcNow;
                        // and update the difficulty and party status
                        e.Difficulty = quest.Difficulty;
                        e.PartyEnabled = quest.PartyEnabled;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // add a new entry
                    p.m_QuestList.Add(new QuestEntry(m, quest));

                }
            }
        }


        public XmlQuestPoints(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlQuestPoints()
        {
        }

        public static new void Initialize()
        {
            CommandSystem.Register("QuestPointsReset", AccessLevel.Owner, new CommandEventHandler(QuestPointsReset_OnCommand));

            //CommandSystem.Register( "QuestLog", AccessLevel.Player, new CommandEventHandler( QuestLog_OnCommand ) );

            CommandSystem.Register("OldQuestListClear", AccessLevel.Developer, new CommandEventHandler(OldQuestListClear_OnCommand));
        }

        [Usage("OldQuestListClear")]
        [Description("Rettifica sistemi quando vengono cambiati nomi alle quest, per quest non più in corso e ripetibili.")]
        private static void OldQuestListClear_OnCommand(CommandEventArgs e)
        {
            List<Mobile> mlist = new List<Mobile>(World.Mobiles.Values);
            foreach (Mobile m in mlist)
            {
                if (m.Player)
                {
                    if (XmlAttach.FindAttachment(m, typeof(XmlQuestPoints)) is XmlQuestPoints q)
                    {
                        List<QuestEntry> qe = q.m_QuestList;
                        for (int i = qe.Count - 1; i >= 0; --i)
                        {
                            if (!(XmlAttach.FindAttachment(m, typeof(XmlQuestAttachment), qe[i].Name) is XmlQuestAttachment qa))
                            {
                                q.m_QuestList.Remove(qe[i]);
                            }
                        }
                    }
                }
            }
        }

        [Usage("QuestPointsReset")]
        [Description("Reset Quest points, stales all the points to the maximum value of 500 (preset), also resets and removes completed quests")]
        public static void QuestPointsReset_OnCommand(CommandEventArgs e)
        {
            List<Mobile> mlist = new List<Mobile>(World.Mobiles.Values);
            foreach (Mobile m in mlist)
            {
                if (m.Player)
                {
                    if (XmlAttach.FindAttachment(m, typeof(XmlQuestPoints)) is XmlQuestPoints q && !q.Deleted)
                    {
                        q.Points = Math.Min(500, q.Points);
                        List<QuestEntry> qe = q.m_QuestList;
                        for (int i = qe.Count - 1; i >= 0; --i)
                        {
                            if (!(XmlAttach.FindAttachment(m, typeof(XmlQuestAttachment), qe[i].Name) is XmlQuestAttachment qa))
                            {
                                q.m_QuestList.Remove(qe[i]);
                            }
                        }
                    }
                }
            }
        }



        [Usage("QuestLog")]
        [Description("Displays players quest history")]
        public static void QuestLog_OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
            {
                return;
            }

            e.Mobile.CloseGump(typeof(QuestLogGump));
            e.Mobile.SendGump(new QuestLogGump(e.Mobile));
        }

        public static void GiveQuestCredits(Mobile from, int credits)
        {
            if (from == null)
            {
                return;
            }

            // find the XmlQuestPoints attachment
            // if doesnt have one yet, then add it
            if (!(XmlAttach.FindAttachment(from, typeof(XmlQuestPoints)) is XmlQuestPoints p))
            {
                p = new XmlQuestPoints();
                XmlAttach.AttachTo(from, p);
            }
            // update the questpoints attachment information
            p.Credits += credits;
        }

        public static void GiveQuestPoints(Mobile from, int points, bool last, int multi)
        {
            if (from == null)
            {
                return;
            }

            // find the XmlQuestPoints attachment

            // if doesnt have one yet, then add it
            if (!(XmlAttach.FindAttachment(from, typeof(XmlQuestPoints)) is XmlQuestPoints p))
            {
                p = new XmlQuestPoints();
                XmlAttach.AttachTo(from, p);
            }

            // update the questpoints attachment information
            p.Points += points;
            p.Credits += points;

            if (last)
            {
                from.SendLocalizedMessage(505337, (points * multi).ToString());// "Hai ricevuto {0} punti quest!",points*multi);

                // update the overall ranking list
                XmlQuestLeaders.UpdateQuestRanking(from, p);
            }
        }

        public static void GiveQuestPoints(Mobile from, IXmlQuest quest)
        {
            if (from == null || quest == null)
            {
                return;
            }

            // find the XmlQuestPoints attachment

            // if doesnt have one yet, then add it
            if (!(XmlAttach.FindAttachment(from, typeof(XmlQuestPoints)) is XmlQuestPoints p))
            {
                p = new XmlQuestPoints();
                XmlAttach.AttachTo(from, p);
            }

            // if you wanted to scale the points given based on party size, karma, fame, etc.
            // this would be the place to do it
            int points = quest.Difficulty;

            // update the questpoints attachment information
            p.Points += points;
            p.Credits += points;
            p.QuestsCompleted++;

            from.SendLocalizedMessage(505337, points.ToString());// "Hai ricevuto {0} punti quest!",points);

            // add the completed quest to the quest list
            QuestEntry.AddQuestEntry(from, quest);

            // update the overall ranking list
            XmlQuestLeaders.UpdateQuestRanking(from, p);
        }

        public static int GetCredits(Mobile m)
        {
            int val = 0;

            if (XmlAttach.FindAttachment(m, typeof(XmlQuestPoints)) is XmlQuestPoints p)
            {
                val = p.Credits;
            }

            return val;
        }

        public static int GetPoints(Mobile m)
        {
            int val = 0;

            if (XmlAttach.FindAttachment(m, typeof(XmlQuestPoints)) is XmlQuestPoints p)
            {
                val = p.Points;
            }

            return val;
        }

        public static bool HasCredits(Mobile m, int credits, int minpoints)
        {
            if (m == null || m.Deleted)
            {
                return false;
            }

            if (XmlAttach.FindAttachment(m, typeof(XmlQuestPoints)) is XmlQuestPoints p)
            {
                if (p.Credits >= credits && p.Points >= minpoints)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TakeCredits(Mobile m, int credits)
        {
            if (m == null || m.Deleted)
            {
                return false;
            }

            if (XmlAttach.FindAttachment(m, typeof(XmlQuestPoints)) is XmlQuestPoints p)
            {
                if (p.Credits >= credits)
                {
                    p.Credits -= credits;
                    return true;
                }
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(m_Points);
            writer.Write(m_Credits);
            writer.Write(m_Completed);
            writer.Write(m_Rank);
            writer.Write(m_DeltaRank);
            writer.Write(m_WhenRanked);

            // save the quest history
            if (m_QuestList != null)
            {
                writer.Write(m_QuestList.Count);

                foreach (QuestEntry e in m_QuestList)
                {
                    e.Serialize(writer);
                }
            }
            else
            {
                writer.Write(0);
            }

            // need this in order to rebuild the rankings on deser
            if (AttachedTo is Mobile)
            {
                writer.Write(AttachedTo as Mobile);
            }
            else
            {
                writer.Write((Mobile)null);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            /*int version =*/
            reader.ReadInt();

            //switch(version)
            //{
            //	case 0:

            m_Points = reader.ReadInt();
            m_Credits = reader.ReadInt();
            m_Completed = reader.ReadInt();
            m_Rank = reader.ReadInt();
            m_DeltaRank = reader.ReadInt();
            m_WhenRanked = reader.ReadDateTime();

            int nquests = reader.ReadInt();

            if (nquests > 0)
            {
                m_QuestList = new List<XmlQuestPoints.QuestEntry>(nquests);
                for (int i = 0; i < nquests; ++i)
                {
                    QuestEntry e = new QuestEntry();
                    e.Deserialize(reader);

                    m_QuestList.Add(e);
                }
            }

            // get the owner of this in order to rebuild the rankings
            Mobile quester = reader.ReadMobile();

            // rebuild the ranking list
            // if they have never made a kill, then dont rank
            XmlQuestLeaders.ReLoadRanking(quester, this);
        }

        private static void PostLoadHandler()
        {
            XmlQuestLeaders.ReLoadRanking(null, null);
        }
    }
}
