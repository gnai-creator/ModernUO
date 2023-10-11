using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Engines.XmlSpawner2
{
    public enum ChallengeStatus
    {
        None,
        Active,
        Dead,
        OutOfBounds,
        Offline,
        Forfeit,
        Hidden,
        Disqualified
    }

    public class TeamInfo
    {
        public int ID;
        public int NActive;
        public int Score;
        public bool Winner;
        public List<BaseChallengeEntry> Members = new List<BaseChallengeEntry>();

        public TeamInfo(int teamid)
        {
            ID = teamid;
        }
    }

    public abstract class BaseChallengeEntry
    {
        private PlayerMobile m_Participant;
        private ChallengeStatus m_Status;
        private ChallengeStatus m_Caution;
        private bool m_Accepted;
        private DateTime m_LastCaution;
        private int m_PageBeingViewed;
        private int m_Score;
        private int m_Team;
        private bool m_Winner;

        public virtual PlayerMobile Participant { get { return m_Participant; } set { m_Participant = value; } }
        public virtual ChallengeStatus Status { get { return m_Status; } set { m_Status = value; } }
        public virtual ChallengeStatus Caution { get { return m_Caution; } set { m_Caution = value; } }
        public virtual bool Accepted { get { return m_Accepted; } set { m_Accepted = value; } }
        public virtual DateTime LastCaution { get { return m_LastCaution; } set { m_LastCaution = value; } }
        public virtual int PageBeingViewed { get { return m_PageBeingViewed; } set { m_PageBeingViewed = value; } }
        public virtual int Score { get { return m_Score; } set { m_Score = value; } }
        public virtual int Team { get { return m_Team; } set { m_Team = value; } }
        public virtual bool Winner { get { return m_Winner; } set { m_Winner = value; } }

        public BaseChallengeEntry(PlayerMobile m)
        {
            Participant = m;
            Status = ChallengeStatus.Active;
            Accepted = false;

        }

        public BaseChallengeEntry()
        {
        }
    }


    public abstract class BaseChallengeGame : Item
    {
        public ChallengeTimer GameTimer { get; private set; }

        public bool IsInChallengeGameRegion { get; set; }

        // how long before the gauntlet is removed after a game is completed
        public virtual TimeSpan PostGameDecayTime { get { return TimeSpan.FromMinutes(5.0); } }

        // how long before the gauntlet decays if a gauntlet is dropped but never started
        public override TimeSpan DecayTime { get { return TimeSpan.FromMinutes(15); } }  // this will apply to the setup

        public override bool Decays { get { return !GameLocked; } }

        public abstract int EntryFee { get; set; }

        public abstract int TotalPurse { get; set; }

        public abstract int ArenaSize { get; set; }

        public abstract PlayerMobile Challenger { get; set; }

        public virtual bool AreInGame(Mobile from)
        {
            if (from == null) return false;

            // go through each teams members list and determine whether the players are on any team list
            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (!(entry.Status == ChallengeStatus.Active)) continue;

                    if (entry.Participant == from)
                    {
                        return true;
                    }
                }
            }

            return false;

        }

        public abstract void CheckForGameEnd();

        public abstract bool AreTeamMembers(Mobile from, Mobile target);


        public abstract bool AreChallengers(Mobile from, Mobile target);


        public abstract void OnPlayerKilled(PlayerMobile killer, PlayerMobile killed);

        public virtual void OnKillPlayer(PlayerMobile killer, PlayerMobile killed)
        {
        }

        public virtual void SetupChallenge(Mobile from)
        {
        }

        public abstract void OnTick();

        public void ResetAcceptance()
        {
            // go through the participant list and clear all acceptance flags
            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    entry.Accepted = false;
                }
            }
        }

        public void ClearArena()
        {

            if (ArenaSize <= 0) return;

            List<PlayerMobile> mlist = new List<PlayerMobile>();

            // who is currently within the arena
            foreach (NetState ns in this.GetClientsInRange(ArenaSize))
            {
                if (ns == null || !(ns.Mobile is PlayerMobile pm)) continue;

                BaseChallengeEntry entry = GetParticipant(pm);

                // if this is not a current participant then move them
                if (entry == null)
                {
                    // prepare to move them off
                    mlist.Add(pm);
                }
            }

            // move non-participants
            foreach (PlayerMobile p in mlist)
            {
                for (int i = 0; i < 10; ++i)
                {
                    int x = Location.X + (ArenaSize + i) * (Utility.RandomBool() ? 1 : -1);
                    int y = Location.Y + (ArenaSize + i) * (Utility.RandomBool() ? 1 : -1);
                    int z = Map.GetAverageZ(x, y);
                    Point3D newloc = new Point3D(x, y, z);

                    if (XmlSpawner.IsValidMapLocation(newloc, p.Map))
                    {
                        p.MoveToWorld(newloc, p.Map);
                    }
                }
            }
        }


        public virtual bool HasEntryFee(Mobile m)
        {
            Container bank = m.BankBox;
            int total = 0;

            if (bank != null)
            {
                Item[] goldlist = bank.FindItemsByType(typeof(Gold), true);

                if (goldlist != null)
                {
                    foreach (Gold g in goldlist)
                        total += g.Amount;
                }
            }
            return (total >= EntryFee);
        }


        public virtual bool CollectEntryFee(Mobile m, int amount)
        {
            if (m == null) return false;

            if (amount <= 0) return true;

            // take the money
            if (m.BankBox != null)
            {
                if (!m.BankBox.ConsumeTotal(typeof(Gold), amount, true))
                {
                    m.SendLocalizedMessage(505368, amount.ToString());//Non hai monete a sufficienza, ti servono almeno ~1_val~ monete in banca.
                    return false;
                }
            }
            else
            {
                return false;
            }

            m.SendLocalizedMessage(505742, amount.ToString());//{0} gold has been withdrawn from your bank."

            // and add it to the purse
            TotalPurse += amount;

            return true;
        }

        public virtual bool CheckQualify(PlayerMobile m)
        {
            if (m == null) return false;


            if (!(XmlAttach.FindAttachment(m, typeof(XmlPoints)) is XmlPoints a))
            {
                XmlAttach.AttachTo(m, a = new XmlPoints());
            }

            // make sure they qualify
            if (a.HasChallenge)
            {
                Challenger.SendLocalizedMessage(505743, m.GetNameFor(Challenger));// "{0} is already in a Challenge."
                return false;
            }

            // and they have the Entry fee in the bank
            if (!HasEntryFee(m))
            {
                Challenger.SendLocalizedMessage(505744, m.GetNameFor(Challenger));// "{0} cannot afford the Entry fee."
                return false;
            }

            return true;
        }

        public virtual void Forfeit(Mobile m)
        {
            if (m == null) return;

            ClearNameHue(m);

            // inform him that he has been kicked
            m.SendLocalizedMessage(505745, ChallengeName);// "You dropped out of {0}"
            GameBroadcast(505714, m.Name);  // "{0} has dropped out."

            RefreshSymmetricNoto(m);

            // this could end the game so check
            CheckForGameEnd();

            // and clear his challenge game
            XmlPoints a = (XmlPoints)XmlAttach.FindAttachment(m, typeof(XmlPoints));
            if (a != null)
            {
                a.ChallengeGame = null;
            }
        }

        public virtual void AwardWinnings(Mobile m, int amount)
        {
            if (m == null) return;

            if (m.Backpack != null && amount > 0)
            {
                // give them a check for the winnings
                BankCheck check = new BankCheck(amount);
                check.Name = $"Prize from {ChallengeName}";
                m.AddToBackpack(check);
                m.SendLocalizedMessage(505746, amount.ToString());// "You have received a bank check for {0}"
            }
        }

        public void AwardTeamWinnings(int team, int amount)
        {
            if (team == 0) return;

            int count = 0;
            // go through all of the team members
            foreach (BaseChallengeEntry entry in Participants)
            {
                if (entry.Team == team)
                {
                    ++count;
                }
            }

            if (count == 0) return;

            int split = amount / count;

            // and split the purse
            foreach (BaseChallengeEntry entry in Participants)
            {
                if (entry.Team == team)
                {
                    Mobile m = entry.Participant;
                    if (m.Backpack != null && amount > 0)
                    {
                        // give them a check for the winnings
                        BankCheck check = new BankCheck(split)
                        {
                            Name = $"Prize from {ChallengeName}"
                        };
                        m.AddToBackpack(check);
                        m.SendLocalizedMessage(505746, split.ToString());// "You have received a bank check for {0}"
                    }
                }
            }
        }

        //public static void DoSetupChallenge(Mobile from, int nameindex, Type gametype)
		//{
        //    if(from != null && gametype != null)
        //    {
        //        bool onlyinchallenge = false;
        //
        //        FieldInfo finfo = null;
        //        finfo = gametype.GetField( "OnlyInChallengeGameRegion" );
        //        if(finfo != null && finfo.IsStatic && finfo.FieldType == typeof(bool))
        //        {
        //            try{
        //                onlyinchallenge = (bool)finfo.GetValue(null);
        //            } catch{}
        //        }
        //
        //        // is this in a challenge game region?
        //        Region r = Region.Find(from.Location, from.Map);
        //        if(r is ChallengeGameRegion)
        //        {
        //            ChallengeGameRegion cgr = r as ChallengeGameRegion;
        //
        //            if(cgr.ChallengeGame != null && !cgr.ChallengeGame.Deleted && !cgr.ChallengeGame.GameCompleted && !cgr.ChallengeGame.IsOrganizer(from))
        //            {
        //                from.SendMessage(String.Format(XmlPoints.GetText(from, 100303), XmlPoints.GetText(from, nameindex)));  //"Unable to set up a {0} Challenge: Another Challenge Game is already in progress in this Challenge Game region.", "Last Man Standing"
        //                return;
        //            }
        //        } else
        //        if(onlyinchallenge)
        //        {
        //            from.SendMessage(String.Format(XmlPoints.GetText(from, 100304), XmlPoints.GetText(from, nameindex))); // "Unable to set up a {0} Challenge: Must be in a Challenge Game region.", "Last Man Standing"
        //            return;
        //        }
        //
        //        // create the game gauntlet
        //        object newgame = null;
        //        object[] gameargs = new object[1];
        //        gameargs[0] = from;
        //
        //        try{
        //        newgame = Activator.CreateInstance( gametype, gameargs );
        //        } catch{}
        //
        //        BaseChallengeGame g = newgame as BaseChallengeGame;
        //
        //        if(g == null || g.Deleted)
        //        {
        //            from.SendMessage(String.Format(XmlPoints.GetText(from, 100305), XmlPoints.GetText(from, nameindex)));  // "Unable to set up a {0} Challenge.", "Last Man Standing"
        //            return;
        //        }
        //
        //
        //        g.MoveToWorld(from.Location, from.Map);
        //        from.SendMessage(String.Format(XmlPoints.GetText(from, 100306), XmlPoints.GetText(from, nameindex))); // "Setting up a {0} Challenge.", "Last Man Standing"
        //
        //        // call any game-specific setups
        //        g.SetupChallenge(from);
        //
        //        if(r is ChallengeGameRegion)
        //        {
        //            ChallengeGameRegion cgr = r as ChallengeGameRegion;
        //
        //            cgr.ChallengeGame = g;
        //            
        //            g.IsInChallengeGameRegion = true;
        //
        //            // announce challenge game region games
        //            XmlPoints.BroadcastMessage( AccessLevel.Player, 0x482, String.Format(XmlPoints.SystemText(100307), XmlPoints.SystemText(nameindex), r.Name, from.Name) );  // "{0} Challenge being prepared in '{1}' by {2}", "Last Man Standing"
        //
        //        }
        //
        //        // if there was a previous challenge being setup then delete it unless it is still in progress
        //        XmlPoints afrom = (XmlPoints)XmlAttach.FindAttachment(from, typeof(XmlPoints));
        //
        //        if(afrom != null)
        //        {
        //            if(afrom.ChallengeSetup != null && !(afrom.ChallengeSetup.GameInProgress || afrom.ChallengeSetup.GameCompleted))
        //            {
        //                afrom.ChallengeSetup.Delete();
        //            }
        //            afrom.ChallengeSetup = g;
        //        }
        //
        //    }
		//}

        public virtual void StartGame()
        {
            GameLocked = true;

            GameInProgress = true;

            InvalidateProperties();

            // if there are any non-participants in the arena area then kick them
            //ClearArena();

            GameBroadcast(505712); // "Let the games begin!"

            // set up the noto on everyone
            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (entry.Participant != null)
                    {
                        RefreshNoto(entry.Participant);
                        XmlAttachment afrom = XmlAttach.FindAttachment(entry.Participant, typeof(XmlPoints));

                        // update the points gumps on the players if they are open
                        if (afrom != null && entry.Participant.HasGump(typeof(XmlPoints.PointsGump)))
                        {
                            afrom.OnIdentify(entry.Participant);
                        }
                    }
                }
            }


            // start the challenge timer
            StartChallengeTimer();
        }

        public virtual void ClearChallenge(Mobile from)
        {
            // check for points attachments
            if (XmlAttach.FindAttachment(from, typeof(XmlPoints)) is XmlPoints afrom)
            {
                afrom.ChallengeGame = null;
            }
        }


        public virtual void EndGame()
        {
            // go through the participant list and clear the challenge team
            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (entry.Participant == null || entry.Status == ChallengeStatus.Forfeit) continue;

                    ClearChallenge(entry.Participant);

                    // clear combatants
                    entry.Participant.Combatant = null;
                    entry.Participant.Warmode = false;
                }
            }

            RefreshAllNoto();

            GameInProgress = false;

            // stop the challenge timer
            if (GameTimer != null)
                GameTimer.Stop();

            InvalidateProperties();

            // start the gauntlet deletion timer
            Timer.DelayCall(PostGameDecayTime, new TimerCallback(Delete));

        }

        public void StartChallengeTimer()
        {
            if (GameTimer != null)
                GameTimer.Stop();

            GameTimer = new ChallengeTimer(this, TimeSpan.FromSeconds(1));

            GameTimer.Start();
        }

        public class ChallengeTimer : Timer
        {
            private BaseChallengeGame m_Gauntlet;

            public ChallengeTimer(BaseChallengeGame gauntlet, TimeSpan delay) : base(delay, delay)
            {
                Priority = TimerPriority.OneSecond;
                m_Gauntlet = gauntlet;
            }

            protected override void OnTick()
            {
                // check for disqualification

                if (m_Gauntlet != null && !m_Gauntlet.Deleted && m_Gauntlet.GameInProgress)
                {
                    m_Gauntlet.OnTick();
                }
                else
                {
                    Stop();
                }
            }
        }

        public virtual bool InsuranceIsFree(Mobile from, Mobile awardto)
        {
            return false;
        }

        public virtual bool IsOrganizer(Mobile from)
        {
            if (from == null || Organizers == null) return false;

            foreach (PlayerMobile m in Organizers)
            {
                if (from == m) return true;
            }

            return false;
        }

        public virtual bool ChallengeBeingCancelled { get { return false; } }

        public virtual string ChallengeName { get { return Name; } }

        public virtual List<PlayerMobile> Organizers { get { return null; } }

        public virtual bool UseKillDelay { get { return true; } }

        public virtual bool AllowPoints { get { return false; } }

        public abstract bool GameInProgress { get; set; }

        public abstract bool GameLocked { get; set; }

        public abstract bool GameCompleted { get; }

        public abstract List<BaseChallengeEntry> Participants { get; set; }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (GameInProgress)
            {
                list.Add(1060742); // Active
            }
            else
            if (GameCompleted)
            {
                list.Add(1046033); // Completed
            }
            else
            {
                list.Add(3000097); // Setup
            }
        }

        public override void OnDelete()
        {
            // if the game is in progress, then return all Entry fees
            if (GameInProgress)
            {
                GameBroadcast(505715, ChallengeName);  // "{0} cancelled"

                // go through the participants and return their fees and clear noto
                if (Participants != null)
                {
                    foreach (BaseChallengeEntry entry in Participants)
                    {
                        if (entry.Status == ChallengeStatus.Forfeit) continue;

                        Mobile from = entry.Participant;

                        // return the entry fee
                        if (from != null && from.BankBox != null && EntryFee > 0)
                        {
                            Item gold = new Gold(EntryFee);

                            if (!from.BankBox.TryDropItem(from, gold, false))
                            {
                                gold.Delete();
                                from.AddToBackpack(new BankCheck(EntryFee));
                                from.SendLocalizedMessage(505709, EntryFee.ToString());// "Entry fee of {0} gold has been returned to you."
                            }
                            else
                            {
                                from.SendLocalizedMessage(505711, EntryFee.ToString());// "Entry fee of {0} gold has been returned to your bank account."
                            }
                        }

                        entry.Status = ChallengeStatus.None;
                    }

                    // clear all noto
                    foreach (BaseChallengeEntry entry in Participants)
                    {
                        RefreshNoto(entry.Participant);
                    }
                }

                EndGame();
            }
            else if (!GameCompleted)
            {
                // this is when a game is cancelled during setup
                GameBroadcast(505715, ChallengeName); // "{0} cancelled"
            }

            base.OnDelete();
        }

        public BaseChallengeGame(int itemid) : base(itemid)
        {
        }

        public BaseChallengeGame() : base(0)
        {
        }


        public BaseChallengeGame(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(IsInChallengeGameRegion);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    IsInChallengeGameRegion = reader.ReadBool();
                    break;
            }


        }

        public int ActivePlayers()
        {
            if (Participants == null || !GameInProgress) return 0;

            int leftstanding = 0;

            foreach (BaseChallengeEntry entry in Participants)
            {
                if (entry.Status == ChallengeStatus.Active)
                {
                    leftstanding++;
                }
            }
            return leftstanding;
        }

        public bool HasValidMembers(TeamInfo t)
        {
            // make sure the team has valid members
            foreach (BaseChallengeEntry entry in t.Members)
            {
                // just need to find one active member
                if (entry.Status == ChallengeStatus.Active)
                {
                    return true;
                }
            }

            return false;
        }

        public static int TeamColor(int team)
        {
            if (team < 6)
                return 20 + team * 40;
            else
                return 10 + (team - 6) * 20;
        }

        public virtual TeamInfo NewTeam(int team)
        {
            return new TeamInfo(team);
        }

        public TeamInfo GetTeam(int team)
        {
            List<TeamInfo> Teams = GetTeams();
            if (Teams != null)
            {
                foreach (TeamInfo t in Teams)
                {
                    if (t.ID == team) return t;
                }
            }

            return null;
        }

        public List<TeamInfo> GetTeams()
        {
            if (Participants == null) return null;

            List<TeamInfo> Teams = new List<TeamInfo>();

            foreach (BaseChallengeEntry entry in Participants)
            {
                if (entry == null) continue;

                int tid = entry.Team;
                TeamInfo team = null;

                // find the team info for the team the participant is on
                foreach (TeamInfo t in Teams)
                {
                    if (t.ID == tid)
                    {
                        team = t;
                    }
                }

                // keep track of the teams
                if (team == null)
                {
                    team = NewTeam(tid);
                    Teams.Add(team);
                }

                team.Members.Add(entry);

                // keep track of the number of total and active players
                if (entry.Status == ChallengeStatus.Active)
                {
                    team.NActive++;
                    team.Score += entry.Score;
                }

            }

            return Teams;
        }

        public void SetNameHue()
        {
            // set the namehue for each participant based on their team
            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (entry.Participant != null && entry.Status == ChallengeStatus.Active && entry.Team != 0)
                        entry.Participant.NameHue = TeamColor(entry.Team);
                }
            }
        }

        public void ClearNameHue()
        {
            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (entry.Participant != null)
                        entry.Participant.NameHue = -1;
                }
            }
        }

        public void ClearNameHue(Mobile m)
        {
            if (m == null) return;

            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (entry.Participant == m)
                        m.NameHue = -1;
                }
            }
        }

        public void RefreshAllNoto()
        {

            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    RefreshNoto(entry.Participant);
                }
            }

        }

        public void RefreshNoto(Mobile from)
        {
            if (from == null) return;

            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (entry.Participant != from)
                    {
                        from.Send(new MobileMoving(entry.Participant, Notoriety.Compute(from, entry.Participant), from));
                    }
                }
            }

        }

        public void RefreshSymmetricNoto(Mobile from)
        {
            if (from == null) return;

            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (entry.Participant != from)
                    {
                        from.Send(new MobileMoving(entry.Participant, Notoriety.Compute(from, entry.Participant), from));
                        entry.Participant.Send(new MobileMoving(from, Notoriety.Compute(entry.Participant, from), entry.Participant));
                    }
                }
            }
        }

        public BaseChallengeEntry GetParticipant(Mobile m)
        {
            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (entry.Participant == m && entry.Status == ChallengeStatus.Active) return entry;
                }
            }

            return null;
        }

        public virtual void GameBroadcastSound(int sound)
        {
            foreach (BaseChallengeEntry entry in Participants)
            {
                if (entry.Participant == null || entry.Status == ChallengeStatus.Forfeit) continue;

                // play the sound
                entry.Participant.PlaySound(sound);
            }
        }

        public virtual void GameBroadcast(int clilocnum)
        {
            // go through the participant list and send all participants the message
            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (entry.Participant == null || entry.Status == ChallengeStatus.Forfeit) continue;

                    entry.Participant.SendLocalizedMessage(clilocnum, "", 40);
                }
            }
        }

        public virtual void GameBroadcast(int clilocnum, string args)
        {
            // go through the participant list and send all participants the message
            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (entry.Participant == null || entry.Status == ChallengeStatus.Forfeit) continue;
                    entry.Participant.SendLocalizedMessage(clilocnum, args, 40);
                }
            }
        }

        public virtual void GameBroadcast(string msg)
        {
            // go through the participant list and send all participants the message
            if (Participants != null)
            {
                foreach (BaseChallengeEntry entry in Participants)
                {
                    if (entry.Participant == null || entry.Status == ChallengeStatus.Forfeit) continue;

                    entry.Participant.SendMessage(40, msg);
                }
            }
        }
    }
}