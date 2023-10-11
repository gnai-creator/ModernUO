/*using System;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using System.Collections.Generic;
using Server.Targeting;
using Server.Regions;
using Server.Engines.XmlSpawner2;

namespace Server.Items
{
    public class TeamDeathmatchGauntlet : BaseChallengeGame
    {
		public class ChallengeEntry : BaseChallengeEntry
		{

            public ChallengeEntry(PlayerMobile m, int team) : base(m)
            {
                Team = team;
            }

            public ChallengeEntry(PlayerMobile m) : base (m)
            {
            }
            
            public ChallengeEntry() : base ()
            {
            }
		}

		private static TimeSpan MaximumOutOfBoundsDuration = TimeSpan.FromSeconds(15);    // maximum time allowed out of bounds before disqualification

        private static TimeSpan MaximumOfflineDuration = TimeSpan.FromSeconds(60);    // maximum time allowed offline before disqualification

        private static TimeSpan MaximumHiddenDuration = TimeSpan.FromSeconds(10);    // maximum time allowed hidden before disqualification

        private static TimeSpan RespawnTime = TimeSpan.FromSeconds(6);    // delay until autores if autores is enabled

        public static bool OnlyInChallengeGameRegion = false;           // if this is true, then the game can only be set up in a challenge game region

        // how long before the gauntlet decays if a gauntlet is dropped but never started
        public override TimeSpan DecayTime { get{ return TimeSpan.FromMinutes( 15 ); } }  // this will apply to the setup

        public override List<PlayerMobile> Organizers { get; } = new List<PlayerMobile>();

        public override bool AllowPoints { get{ return false; } }   // determines whether kills during the game will award points.  If this is false, UseKillDelay is ignored

        public override bool UseKillDelay { get{ return true; } }   // determines whether the normal delay between kills of the same player for points is enforced

        public bool AutoRes { get { return true; } }            // determines whether players auto res after being killed

        public bool AllowOnlyInChallengeRegions { get { return false; } }

        [CommandProperty( AccessLevel.GameMaster )]
        public TimeSpan MatchLength { get; set; } = TimeSpan.FromMinutes(10);// default match length is 10 mins

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime MatchStart { get; set; }

        [CommandProperty( AccessLevel.GameMaster )]
        public override PlayerMobile Challenger { get; set; }

        public override bool GameLocked { get; set; }

        public override bool GameInProgress { get; set; }

        [CommandProperty( AccessLevel.GameMaster )]
        public override bool GameCompleted { get{ return !GameInProgress && GameLocked; } }

        [CommandProperty( AccessLevel.GameMaster )]
        public override int ArenaSize { get; set; } = 0;// maximum distance from the challenge gauntlet allowed before disqualification.  Zero is unlimited range

        [CommandProperty(AccessLevel.GameMaster)]
        public int TargetScore { get; set; } = 10;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Winner { get; set; } = 0;

        public override List<BaseChallengeEntry> Participants { get; set; } = new List<BaseChallengeEntry>();

        public override int TotalPurse { get; set; }

        public override int EntryFee { get; set; }

        public override bool InsuranceIsFree(Mobile from, Mobile awardto)
        {
            return true;
        }

        public override void OnTick()
		{
            CheckForDisqualification();
		}

		public void CheckForDisqualification()
		{
		
            if(Participants == null || !GameInProgress) return;
            
             bool statuschange = false;

            foreach(ChallengeEntry entry in Participants)
            {
                if(entry.Participant == null || entry.Status != ChallengeStatus.Active) continue;

                bool hadcaution = (entry.Caution != ChallengeStatus.None);

                // and a map check
                if(entry.Participant.Map != Map)
                {
                    // check to see if they are offline
                    if(entry.Participant.Map == Map.Internal)
                    {
                        // then give them a little time to return before disqualification
                        if(entry.Caution == ChallengeStatus.Offline)
                        {
                            // were previously out of bounds so check for disqualification
                            // check to see how long they have been out of bounds
                            if(DateTime.UtcNow - entry.LastCaution > MaximumOfflineDuration)
                            {
                                // penalize them
                                SubtractScore(entry);
                                entry.LastCaution  = DateTime.UtcNow;
                            }
                        } else
                        {
                            entry.LastCaution  = DateTime.UtcNow;
                            statuschange = true;
                        }
    
                        entry.Caution = ChallengeStatus.Offline;

                    } else
                    {
                        // changing to any other map results in instant
                        // teleport back to the gauntlet
                        // and point loss
                        RespawnWithPenalty(entry);
                        entry.Caution = ChallengeStatus.None;
                    }
                    

                } else
                // make a range check
                if(ArenaSize > 0 && !Utility.InRange(entry.Participant.Location, Location, ArenaSize)
                || (IsInChallengeGameRegion && !(Region.Find(entry.Participant.Location, entry.Participant.Map) is ChallengeGameRegion)))
                {
                    if(entry.Caution == ChallengeStatus.OutOfBounds)
                    {
                        // were previously out of bounds so check for disqualification
                        // check to see how long they have been out of bounds
                        if(DateTime.UtcNow - entry.LastCaution > MaximumOutOfBoundsDuration)
                        {
                            // teleport them back to the gauntlet
                            RespawnWithPenalty(entry);
                            GameBroadcast(100401, entry.Participant.Name);  // "{0} was penalized."
                            entry.Caution = ChallengeStatus.None;
                            statuschange = true;
                        }
                    } else
                    {
                        entry.LastCaution  = DateTime.UtcNow;
                        // inform the player
                        XmlPoints.SendText(entry.Participant, 100309, MaximumOutOfBoundsDuration.TotalSeconds);  // "You are out of bounds!  You have {0} seconds to return"
                        statuschange = true;
                    }

                    entry.Caution = ChallengeStatus.OutOfBounds;
                    

                } else
                // make a hiding check
                if(entry.Participant.Hidden)
                {
                    if(entry.Caution == ChallengeStatus.Hidden)
                    {
                        // were previously hidden so check for disqualification
                        // check to see how long they have hidden
                        if(DateTime.UtcNow - entry.LastCaution > MaximumHiddenDuration)
                        {
                            // penalize them
                            SubtractScore(entry);
                            entry.Participant.Hidden = false;
                            GameBroadcast(100401, entry.Participant.Name);  // "{0} was penalized."
                            entry.Caution = ChallengeStatus.None;
                            statuschange = true;
                        }
                    } else
                    {
                        entry.LastCaution  = DateTime.UtcNow;
                        // inform the player
                        XmlPoints.SendText(entry.Participant, 100310, MaximumHiddenDuration.TotalSeconds); // "You have {0} seconds become unhidden"
                        statuschange = true;
                    }

                    entry.Caution = ChallengeStatus.Hidden;


                } else
                {
                    entry.Caution = ChallengeStatus.None;
                }

                if(hadcaution && entry.Caution == ChallengeStatus.None)
                    statuschange = true;

            }
            
            if(statuschange)
            {
                // update gumps with the new status
                TeamDeathmatchGump.RefreshAllGumps(this, false);
            }

            // it is possible that the game could end like this so check
            CheckForGameEnd();



		}

        public override void OnDelete()
        {
            ClearNameHue();

            base.OnDelete();

        }

		public override void EndGame()
		{
            ClearNameHue();

            base.EndGame();

		}

        public override void StartGame()
        {
            base.StartGame();

            MatchStart = DateTime.UtcNow;

            SetNameHue();
        }

		public override void CheckForGameEnd()
		{

            if(Participants == null || !GameInProgress) return;

            List<TeamInfo> winner = new List<TeamInfo>();

            List<TeamInfo> teams = GetTeams();

            int leftstanding = 0;

            int maxscore = -99999;

            // has any team reached the target score
            TeamInfo lastt = null;

            foreach(TeamInfo t in teams)
            {
                if(!HasValidMembers(t)) continue;

                if(TargetScore > 0 && t.Score >= TargetScore)
                {
                        winner.Add(t);
                        t.Winner = true;
                }

                if(t.Score >= maxscore)
                {
                    maxscore = t.Score;
                }
                leftstanding++;
                lastt = t;
            }

            // check to make sure the team hasnt been disqualified

            // if only one is left then they are the winner
            if(leftstanding == 1 && winner.Count == 0)
            {
                winner.Add(lastt);
                lastt.Winner = true;
            }

            if(winner.Count == 0 && MatchLength > TimeSpan.Zero && (DateTime.UtcNow >= MatchStart + MatchLength))
            {
                // find the highest score
                // has anyone reached the target score

                foreach(TeamInfo t in teams)
                {
                
                    if(!HasValidMembers(t)) continue;

                    if(t.Score >= maxscore)
                    {
                        winner.Add(t);
                        t.Winner = true;
                    }
                }
            }

            // and then check to see if this is the Deathmatch
            if(winner.Count > 0)
            {

                // declare the winner(s) and end the game
                foreach(TeamInfo t in winner)
                {
                    // flag all members as winners
                    foreach(BaseChallengeEntry entry in t.Members)
                        entry.Winner = true;
                    GameBroadcast(505718, t.ID.ToString());  // "Team {0} is the winner!"
                    AwardTeamWinnings(t.ID, TotalPurse/winner.Count);
                    
                    if(winner.Count == 1) Winner = t.ID;
                }

                RefreshAllNoto();

                EndGame();
                TeamDeathmatchGump.RefreshAllGumps(this, true);
            }

		}
		
		public void SubtractScore(ChallengeEntry entry)
		{
            if(entry == null) return;
            
            entry.Score--;

            // refresh the gumps
            TeamDeathmatchGump.RefreshAllGumps(this, false);
		}
		
		public void AddScore(ChallengeEntry entry)
		{
            if(entry == null) return;

            entry.Score++;
            
            // refresh the gumps
            TeamDeathmatchGump.RefreshAllGumps(this, false);
		}

		public void RespawnWithPenalty(ChallengeEntry entry)
		{
            if(entry == null) return;
            
            SubtractScore(entry);
            
            // move the participant to the gauntlet
            if(entry.Participant != null)
            {
                entry.Participant.MoveToWorld(this.Location, this.Map);
                entry.Participant.PlaySound( 0x214 );
				entry.Participant.FixedEffect( 0x376A, 10, 16 );
				GameBroadcast(100401, entry.Participant.Name);  // "{0} was penalized."
            }
		}

        public override void OnPlayerKilled(PlayerMobile killer, PlayerMobile killed)
        {
            if(killed == null) return;

            if(AutoRes)
            {
                // prepare the autores callback
                    Timer.DelayCall( RespawnTime, new TimerStateCallback( XmlPoints.AutoRes_Callback ),
                    new object[]{ killed, true } );
            }

            // find the player in the participants list and announce it
            if(Participants != null)
            {

                foreach(ChallengeEntry entry in Participants)
                {
                    if(entry.Status == ChallengeStatus.Active && entry.Participant == killed)
                    {
                        GameBroadcast(505731, killed.Name); // "{0} has been killed"
                        SubtractScore(entry);
                    }
                }
            }

            // see if the game is over
            CheckForGameEnd();
        }
        
        public override void OnKillPlayer(PlayerMobile killer, PlayerMobile killed)
        {
            if(killer == null) return;

            // find the player in the participants list and announce it
            if(Participants != null)
            {

                foreach(ChallengeEntry entry in Participants)
                {
                    if(entry.Status == ChallengeStatus.Active && entry.Participant == killer)
                    {
                        AddScore(entry);
                    }
                }
            }
        }

        public override bool AreTeamMembers(Mobile from, Mobile target)
        {
            if(from == null || target == null) return false;

            int frommember = 0;
            int targetmember = 0;

            // go through each teams members list and determine whether the players are on any team list
            if(Participants != null)
            {
                foreach(BaseChallengeEntry entry in Participants)
                {
                    if(!(entry.Status == ChallengeStatus.Active)) continue;

                    Mobile m = entry.Participant;

                    if(m == from)
                    {
                        frommember = entry.Team;
                    }
                    if(m == target)
                    {
                        targetmember = entry.Team;
                    }
                }
            }

            return (frommember == targetmember && frommember != 0 && targetmember != 0);

        }

        public override bool AreChallengers(Mobile from, Mobile target)
        {
            if(from == null || target == null) return false;

            int frommember = 0;
            int targetmember = 0;

            // go through each teams members list and determine whether the players are on any team list
            if(Participants != null)
            {
                foreach(ChallengeEntry entry in Participants)
                {
                    if(!(entry.Status == ChallengeStatus.Active)) continue;

                    Mobile m = entry.Participant;

                    if(m == from)
                    {
                        frommember = entry.Team;
                    }
                    if(m == target)
                    {
                        targetmember = entry.Team;
                    }
                }
            }

            return (frommember != targetmember && frommember != 0 && targetmember != 0);

        }

        public TeamDeathmatchGauntlet(PlayerMobile challenger) : base( 0x1414 )
        {
            Challenger = challenger;

            Organizers.Add(challenger);

            // check for points attachments
            XmlAttachment afrom = XmlAttach.FindAttachment(challenger, typeof(XmlPoints));

            Movable = false;

            Hue = 33;

            if(challenger == null || afrom == null || afrom.Deleted)
            {
                Delete();
            }
            else
            {
                Name = $"Team Deathmatch - Challenge by {challenger.Name}";
            }
        }


        public TeamDeathmatchGauntlet( Serial serial ) : base( serial )
        {
        }

        public override void Serialize( GenericWriter writer )
        {
            base.Serialize( writer );

            writer.Write( (int) 0 ); // version

            writer.Write(Challenger);
            writer.Write(GameLocked);
            writer.Write(GameInProgress);
            writer.Write(TotalPurse);
            writer.Write(EntryFee);
            writer.Write(ArenaSize);
            writer.Write(TargetScore);
            writer.Write(MatchLength);
            
            if(GameTimer != null && GameTimer.Running)
            {
                writer.Write(DateTime.UtcNow - MatchStart);
            } else
            {
                writer.Write(TimeSpan.Zero);
            }

            if(Participants != null)
            {
                writer.Write(Participants.Count);

                foreach(ChallengeEntry entry in Participants)
                {
                    writer.Write(entry.Participant);
                    writer.Write((int)entry.Status);
                    writer.Write(entry.Accepted);
                    writer.Write(entry.PageBeingViewed);
                    writer.Write(entry.Score);
                    writer.Write(entry.Winner);
                    writer.Write(entry.Team);
                }
            } else
            {
                writer.Write((int)0);
            }

        }

        public override void Deserialize( GenericReader reader )
        {
            base.Deserialize( reader );

            int version = reader.ReadInt();

            switch(version)
            {
                case 0:
                {
                    Challenger = reader.ReadMobile<PlayerMobile>();

                    Organizers.Add(Challenger);

                    GameLocked = reader.ReadBool();
                    GameInProgress = reader.ReadBool();
                    TotalPurse = reader.ReadInt();
                    EntryFee = reader.ReadInt();
                    ArenaSize = reader.ReadInt();
                    TargetScore = reader.ReadInt();
                    MatchLength = reader.ReadTimeSpan();

                    TimeSpan elapsed = reader.ReadTimeSpan();

                    if (elapsed > TimeSpan.Zero)
                    {
                        MatchStart = DateTime.UtcNow - elapsed;
                    }

                    int count = reader.ReadInt();
                    for (int i = 0; i < count; ++i)
                    {
                        ChallengeEntry entry = new ChallengeEntry
                        {
                            Participant = reader.ReadMobile<PlayerMobile>(),
                            Status = (ChallengeStatus)reader.ReadInt(),
                            Accepted = reader.ReadBool(),
                            PageBeingViewed = reader.ReadInt(),
                            Score = reader.ReadInt(),
                            Winner = reader.ReadBool(),
                            Team = reader.ReadInt()
                        };

                        Participants.Add(entry);
                    }
                    break;
                }
            }
            
             if(GameCompleted)
                Timer.DelayCall( PostGameDecayTime, new TimerCallback( Delete ) );
            
            // start the challenge timer
            StartChallengeTimer();
            
            SetNameHue();
        }

        public override void OnDoubleClick( Mobile from )
        {

            from.SendGump( new TeamDeathmatchGump( this, from ) );

        }
    }
}
*/