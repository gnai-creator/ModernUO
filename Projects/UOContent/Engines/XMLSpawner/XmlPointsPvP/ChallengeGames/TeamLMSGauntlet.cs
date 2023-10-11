/*
using Server.Engines.XmlSpawner2;
using Server.Gumps;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class TeamLMSGauntlet : BaseChallengeGame
    {

		public class ChallengeEntry : BaseChallengeEntry
		{

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

        public override bool GameLocked { get; set; }

        public override bool GameInProgress { get; set; }

        [CommandProperty( AccessLevel.GameMaster )]
        public override PlayerMobile Challenger { get; set; }

        [CommandProperty( AccessLevel.GameMaster )]
        public override bool GameCompleted { get{ return !GameInProgress && GameLocked; } }

        [CommandProperty( AccessLevel.GameMaster )]
        public override int ArenaSize { get; set; } = 0;// maximum distance from the challenge gauntlet allowed before disqualification.  Zero is unlimited range

        [CommandProperty(AccessLevel.GameMaster)]
        public int Winner { get; set; } = 0;

        public override List<BaseChallengeEntry> Participants { get; set; } = new List<BaseChallengeEntry>();

        public override int TotalPurse { get; set; }

        public override int EntryFee { get; set; }

        public override bool InsuranceIsFree(Mobile from, Mobile awardto)
        {
            return true;
        }

        public override bool AreTeamMembers(Mobile from, Mobile target)
        {
            if(from == null || target == null) return false;

            int frommember = 0;
            int targetmember = 0;

            // go through each teams members list and determine whether the players are on any team list
            if(Participants != null)
            {
                foreach(IChallengeEntry entry in Participants)
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
                foreach(IChallengeEntry entry in Participants)
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

        
        public override void OnTick()
		{
            CheckForDisqualification();
		}

		public void CheckForDisqualification()
		{
		
            if(Participants == null || !GameInProgress) return;

            bool statuschange = false;

            foreach(IChallengeEntry entry in Participants)
            {
                if(entry.Participant == null || entry.Status == ChallengeStatus.Forfeit || entry.Status == ChallengeStatus.Disqualified) continue;

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
                                entry.Status = ChallengeStatus.Disqualified;
                                GameBroadcast(505719, entry.Participant.Name);  // "{0} has been disqualified"
                                RefreshSymmetricNoto(entry.Participant);
                                statuschange = true;
                            }
                        } else
                        {
                            entry.LastCaution  = DateTime.UtcNow;
                            statuschange = true;
                        }
    
                        entry.Caution = ChallengeStatus.Offline;
                    } else
                    {
                        // changing to any other map is instant disqualification
                        entry.Status = ChallengeStatus.Disqualified;
                        GameBroadcast(505719, entry.Participant.Name);  // "{0} has been disqualified"
                        RefreshSymmetricNoto(entry.Participant);
                        statuschange = true;
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
                            entry.Status = ChallengeStatus.Disqualified;
                            GameBroadcast(505719, entry.Participant.Name);  // "{0} has been disqualified"
                            RefreshSymmetricNoto(entry.Participant);
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
                            entry.Status = ChallengeStatus.Disqualified;
                            GameBroadcast(505719, entry.Participant.Name);  // "{0} has been disqualified"
                            RefreshSymmetricNoto(entry.Participant);
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
                TeamLMSGump.RefreshAllGumps(this, false);
            }

            // it is possible that the game could end like this so check
            CheckForGameEnd();



		}

		public override void CheckForGameEnd()
		{
            if(Participants == null || !GameInProgress) return;

            List<int> remaining = new List<int>();

            // determine how many teams remain
            foreach(BaseChallengeEntry entry in Participants)
            {
                if(entry.Status == ChallengeStatus.Active)
                {
                    if(!remaining.Contains(entry.Team))
                    {
                        remaining.Add(entry.Team);
                    }
                }
            }

            // and then check to see if this is the last team standing
            if(remaining.Count == 1)
            {
                // declare the winner and end the game
                Winner = remaining[0];
                GameBroadcast(505718, Winner.ToString());  // "Team {0} is the winner!"
                AwardTeamWinnings(Winner, TotalPurse);

                EndGame();
                TeamLMSGump.RefreshAllGumps(this, true);
            }
            if(remaining.Count < 1)
            {
                // declare a tie and keep the fees
                GameBroadcast(505713);  // "The match is a draw"

                EndGame();
                TeamLMSGump.RefreshAllGumps(this, true);
            }
		}

        public override void OnPlayerKilled(PlayerMobile killer, PlayerMobile killed)
        {
            if(killed == null) return;

            if(AutoRes)
            {
                // prepare the autores callback
                    Timer.DelayCall( RespawnTime, new TimerStateCallback( XmlPoints.AutoRes_Callback ),
                    new object[]{ killed, false } );
            }

            // find the player in the participants list and set their status to Dead
            if(Participants != null)
            {
                foreach(IChallengeEntry entry in Participants)
                {
                    if(entry.Participant == killed && entry.Status != ChallengeStatus.Forfeit)
                    {
                        entry.Status = ChallengeStatus.Dead;
                        // clear up their noto
                        RefreshSymmetricNoto(killed);

                        GameBroadcast(505731, killed.Name); // "{0} has been killed"
                    }
                }
            }
            
            TeamLMSGump.RefreshAllGumps(this, true);

            // see if the game is over
            CheckForGameEnd();
        }

        public TeamLMSGauntlet(PlayerMobile challenger) : base( 0x1414 )
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
                Name = $"Team LMS - Challenge by {challenger.Name}";
            }
        }


        public TeamLMSGauntlet( Serial serial ) : base( serial )
        {
        }

        public override void Serialize( GenericWriter writer )
        {
            base.Serialize( writer );

            writer.Write( (int) 0 ); // version

            writer.WriteMobile(Challenger);
            writer.Write(GameLocked);
            writer.Write(GameInProgress);
            writer.Write(TotalPurse);
            writer.Write(EntryFee);
            writer.Write(ArenaSize);
            writer.Write(Winner);

            if(Participants != null)
            {
                writer.Write(Participants.Count);

                foreach(ChallengeEntry entry in Participants)
                {
                    writer.WriteMobile(entry.Participant);
                    writer.Write((int)entry.Status);
                    writer.Write(entry.Accepted);
                    writer.Write(entry.PageBeingViewed);
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
                    Winner = reader.ReadInt();

                    int count = reader.ReadInt();
                    for (int i = 0; i < count; ++i)
                    {
                        ChallengeEntry entry = new ChallengeEntry();
                        entry.Participant = reader.ReadMobile<PlayerMobile>();
                        entry.Status = (ChallengeStatus)reader.ReadInt();
                        entry.Accepted = reader.ReadBool();
                        entry.PageBeingViewed = reader.ReadInt();
                        entry.Team = reader.ReadInt();

                        Participants.Add(entry);
                    }
                    break;
                }
            }
            
             if(GameCompleted)
                Timer.DelayCall( PostGameDecayTime, new TimerCallback( Delete ) );
            
            StartChallengeTimer();
            
            SetNameHue();
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

            SetNameHue();
        }


        public override void OnDoubleClick( Mobile from )
        {
            from.SendGump( new TeamLMSGump( this, from ) );
        }
    }
}
*/