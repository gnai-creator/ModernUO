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
    public class CTFBase : Item
    {
        private CTFGauntlet m_gauntlet;

        public int Team { get; set; }

        public CTFFlag Flag { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ProximityRange { get; set; } = 1;

        public bool HasFlag { get; set; }

        public CTFBase(CTFGauntlet gauntlet, int team) : base( 0x1183 )
		{
            Movable = false;
            Hue = BaseChallengeGame.TeamColor(team);
            Team = team;
            Name = String.Format("Team {0} Base", team);
            m_gauntlet = gauntlet;

            // add the flag

            Flag = new CTFFlag(this, team);
            Flag.HomeBase = this;
            HasFlag = true;
		}

		public CTFBase( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write(Team);
			writer.Write(ProximityRange);
			writer.Write(Flag);
			writer.Write(m_gauntlet);
			writer.Write(HasFlag);
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			Team = reader.ReadInt();
			ProximityRange = reader.ReadInt();
			Flag = reader.ReadItem() as CTFFlag;
			m_gauntlet = reader.ReadItem() as CTFGauntlet;
			HasFlag = reader.ReadBool();
		}

		public override void OnDelete()
		{
            // delete any flag associated with the base
            if(Flag != null)
                Flag.Delete();

            base.OnDelete();
		}

		public override void OnLocationChange( Point3D oldLocation )
        {
            // set the flag location
            PlaceFlagAtBase();
        }
        
        public void PlaceFlagAtBase()
        {
            if(Flag != null)
            {
                Flag.MoveToWorld(new Point3D(Location.X + 1, Location.Y, Location.Z + 4), Map);
            }
        }
        
        public override void OnMapChange( )
        {
            // set the flag location
            PlaceFlagAtBase();
        }
        
        public void ReturnFlag()
        {
            ReturnFlag(true);
        }

        public void ReturnFlag(bool verbose)
        {
            if(Flag == null) return;

            PlaceFlagAtBase();
            HasFlag = true;
            if(m_gauntlet != null && verbose)
            {
                m_gauntlet.GameBroadcast(505716, Team.ToString()); // "Team {0} flag has been returned to base"
            }

        }


		public override bool HandlesOnMovement { get{ return m_gauntlet != null; } }

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
            if(m == null || m_gauntlet == null) return;
            
            if(m == null || m.AccessLevel > AccessLevel.Player) return;

            // look for players within range of the base
            // check to see if player is within range of the spawner
  			if ((this.Parent == null) && Utility.InRange( m.Location, this.Location, ProximityRange ) && m_gauntlet.GetParticipant(m) is CTFGauntlet.ChallengeEntry entry)
            {
                bool carryingflag = false;
                // is the player carrying a flag?
                foreach(CTFBase b in m_gauntlet.HomeBases)
                {
                    if(b != null && !b.Deleted && b.Flag != null && b.Flag.RootParent == m)
                    {
                        carryingflag = true;
                        break;
                    }
                }

                // if the player is on an opposing team and the flag is at the base and the player doesnt already
                // have a flag then give them the flag
                if(entry.Team != Team && HasFlag && !carryingflag)
                {
                    m.AddToBackpack(Flag);
                    HasFlag = false;
                    m_gauntlet.GameBroadcast(100420, entry.Team, Team); // "Team {0} has the Team {1} flag"
                    m_gauntlet.GameBroadcastSound(513);
                } else
                if(entry.Team == Team)
                {

                    // if the player has an opposing teams flag then give them a point and return the flag
                    foreach(CTFBase b in m_gauntlet.HomeBases)
                    {
                        if(b != null && !b.Deleted && b.Flag != null && b.Flag.RootParent == m && b.Team != entry.Team)
                        {
                            m_gauntlet.GameBroadcast(505717, entry.Team.ToString());  // "Team {0} has scored"
                            m_gauntlet.AddScore(entry);

                            Effects.SendTargetParticles( entry.Participant, 0x375A, 35, 20, BaseChallengeGame.TeamColor(entry.Team), 0x00, 9502,
                                (EffectLayer)255, 0x100 );
                            // play the score sound
                            m_gauntlet.ScoreSound(entry.Team);

                            b.ReturnFlag(false);
                            break;
                        }
                    }
                }
            }
		}
    }

    public class CTFFlag : Item
    {
        public CTFBase  HomeBase;

		public CTFFlag(CTFBase homebase, int team) : base( 0x161D )
		{
            Hue = BaseChallengeGame.TeamColor(team);;
            Name = $"Team {team} Flag";
            HomeBase = homebase;
		}

		public CTFFlag( Serial serial ) : base( serial )
		{
		}

		public override bool OnDroppedInto( Mobile from, Container target, Point3D p )
        {
            // allow movement within a players backpack
            if(from != null && from.Backpack == target)
            {
                return base.OnDroppedInto(from, target, p);
            }

            return false;
        }
        
        public override bool OnDroppedOnto( Mobile from, Item target )
        {
            return false;
        }
        
        public override bool OnDroppedToMobile( Mobile from, Mobile target )
        {
            return false;
        }

		public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            // only allow staff to pick it up when at a base
            if((from != null && from.AccessLevel > AccessLevel.Player) || RootParent != null)
            {
				return base.CheckLift(from, item, ref reject);
            }
            return false;
        }

        public override bool OnDroppedToWorld(Mobile from,Point3D point)
        {
            return false;
        }


		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( HomeBase );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			HomeBase = reader.ReadItem() as CTFBase;
		}
    }


    public class CTFGauntlet : BaseChallengeGame
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

        public List<CTFBase> HomeBases { get; set; } = new List<CTFBase>();

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

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime MatchEnd { get; private set; }

        [CommandProperty( AccessLevel.GameMaster )]
        public override PlayerMobile Challenger { get; set; }

        public override bool GameLocked { get; set; }

        public override bool GameInProgress { get; set; }

        [CommandProperty( AccessLevel.GameMaster )]
        public override bool GameCompleted { get{ return !GameInProgress && GameLocked; } }

        [CommandProperty(AccessLevel.GameMaster)]
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

        public void ScoreSound(int team)
		{
            foreach(BaseChallengeEntry entry in Participants)
            {
                if(entry.Participant == null || entry.Status != ChallengeStatus.Active) continue;

                if(entry.Team == team)
                {
                    // play the team scored sound
                    entry.Participant.PlaySound(503);
                } else
                {
                    // play the opponent scored sound
                    entry.Participant.PlaySound(855);
                }
            }
		}


        public override void OnTick()
		{
            CheckForDisqualification();

            // check for anyone carrying flags
            if(HomeBases != null)
            {
                List<CTFBase> dlist = null;

                foreach(CTFBase b in HomeBases)
                {
                    if(b == null || b.Deleted)
                    {
                        if(dlist == null)
                            dlist = new List<CTFBase>();
                        dlist.Add(b);
                        continue;
                    }

					if (!b.Deleted && b.Flag != null && !b.Flag.Deleted)
					{
						if (b.Flag.RootParent is Mobile m)
						{
                            // make sure a participant has it
                            BaseChallengeEntry entry = GetParticipant(m);

							if (entry != null)
							{
								// display the flag
								//m.PublicOverheadMessage( MessageType.Regular, BaseChallengeGame.TeamColor(b.Team), false, b.Team.ToString());

								Effects.SendTargetParticles(m, 0x375A, 35, 10, BaseChallengeGame.TeamColor(b.Team), 0x00, 9502,
								(EffectLayer)255, 0x100);

							}
							else
							{
								b.ReturnFlag();
							}
						}
                        else if(!b.HasFlag)// if the flag somehow ends up on the ground, send it back to the base
                        {
							b.ReturnFlag();
						}
					}
	
                }

                if(dlist != null)
                {
                    foreach(CTFBase b in dlist)
                        HomeBases.Remove(b);
                }
            }
		}

		public void ReturnAnyFlags(Mobile m)
		{
            // check for anyone carrying flags
            if(HomeBases != null)
            {
                foreach(CTFBase b in HomeBases)
                {
                    if(!b.Deleted && b.Flag != null && !b.Flag.Deleted)
                    {
                        if(b.Flag.RootParent is Mobile mob)
                        {
                            if(m == mob)
                            {
                                b.ReturnFlag();
                            }
                        }
                        else if(b.Flag.RootParent is Corpse c)
                        {
                            if(m == c.Owner)
                            {
                                b.ReturnFlag();
                            }
                        }
                    }
                }
            }
		}

		public void CheckForDisqualification()
		{
		
            if(Participants == null || !GameInProgress) return;

             bool statuschange = false;

            foreach(BaseChallengeEntry entry in Participants)
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
                                // return any flag they might be carrying
                                ReturnAnyFlags(entry.Participant);
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
                        // changing to any other map results in
                        // return of any flag they might be carrying
                        ReturnAnyFlags(entry.Participant);
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
                            // return any flag they might be carrying
                            ReturnAnyFlags(entry.Participant);
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
                            // return any flag they might be carrying
                            ReturnAnyFlags(entry.Participant);
                            entry.Participant.Hidden = false;
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
                CTFGump.RefreshAllGumps(this, false);
            }

            // it is possible that the game could end like this so check
            CheckForGameEnd();
		}
		
		public CTFBase FindBase(int team)
        {
            // go through the current bases and see if there is one for this team
            if(HomeBases != null)
            {
                foreach(CTFBase b in HomeBases)
                {
                    if(b.Team == team)
                    {
                        // found one
                        return b;
                    }
                }
            }
            
            return null;
        }

		public void DeleteBases()
        {
            if(HomeBases != null)
            {
                foreach(CTFBase b in HomeBases)
                {
                    b.Delete();
                }
                
                HomeBases = new List<CTFBase>();
            }
        }

        public override void OnDelete()
        {
            ClearNameHue();

            // remove all bases
            DeleteBases();

            base.OnDelete();

        }

		public override void EndGame()
		{
            ClearNameHue();
            
            DeleteBases();
            
            MatchEnd = Core.MistedDateTime;

            base.EndGame();
		}

        public override void StartGame()
        {
            base.StartGame();

            MatchStart = Core.MistedDateTime;

            SetNameHue();
			
			// teleport to base
			TeleportPlayersToBase();
        }

		public void TeleportPlayersToBase()
		{
			// teleport players to the base
			if (Participants != null)
			{
				foreach (ChallengeEntry entry in Participants)
				{
					CTFBase teambase = FindBase(entry.Team);

					if (entry.Participant != null && teambase != null)
					{
						entry.Participant.MoveToWorld(teambase.Location, teambase.Map);
					}
				}
			}
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

            if(winner.Count == 0 && MatchLength > TimeSpan.Zero && (Core.MistedDateTime >= MatchStart + MatchLength))
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

            // and then check to see if this is the CTF
            if(winner.Count > 0)
            {

                // declare the winner(s) and end the game
                foreach(TeamInfo t in winner)
                {
                    // flag all members as winners
                    foreach(BaseChallengeEntry entry in t.Members)
                        entry.Winner = true;

                    GameBroadcast(505718, t.ID.ToString());  // "Team {0} is the winner!"

                    GameBroadcastSound(744);
                    AwardTeamWinnings(t.ID, TotalPurse/winner.Count);

                    if(winner.Count == 1) Winner = t.ID;
                }

                RefreshAllNoto();

                EndGame();
                CTFGump.RefreshAllGumps(this, true);
            }

		}
		
		public void SubtractScore(ChallengeEntry entry)
		{
            if(entry == null) return;

            entry.Score--;

            // refresh the gumps
            CTFGump.RefreshAllGumps(this, false);
		}

		public void AddScore(ChallengeEntry entry)
		{
            if(entry == null) return;

            entry.Score++;
            
            // refresh the gumps
            CTFGump.RefreshAllGumps(this, false);
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

            // return any flag they were carrying
            ReturnAnyFlags(killed);
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

            return (frommember != targetmember && frommember != 0 && targetmember != 0);

        }

        public CTFGauntlet(PlayerMobile challenger) : base( 0x1414 )
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
                Name = $"Capture the Flag - Challenge by {challenger.Name}";
            }
        }


        public CTFGauntlet( Serial serial ) : base( serial )
        {
        }

        public override void Serialize( GenericWriter writer )
        {
            base.Serialize( writer );

            writer.Write( (int) 0 ); // version
            
            // save the home base list
            if(HomeBases != null)
            {
                writer.Write(HomeBases.Count);
                foreach(CTFBase b in HomeBases)
                {
                    writer.Write(b);
                }
            } else
            {
                writer.Write((int)0);
            }

            writer.WriteMobile<PlayerMobile>(Challenger);
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
            
            writer.Write(MatchEnd);

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
                    int count = reader.ReadInt();
                    for (int i = 0; i < count; ++i)
                    {
                        CTFBase b = reader.ReadItem() as CTFBase;
                        HomeBases.Add(b);
                    }
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

                    MatchEnd = reader.ReadDateTime();

                    count = reader.ReadInt();
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
            from.SendGump( new CTFGump( this, from ) );
        }
    }
}
*/