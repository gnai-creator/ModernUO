using Server.Items;
using Server.Mobiles;
using System;
/*
using System.Collections.Generic;
using Server.Commands;
using System.IO;
*/
namespace Server.Engines.XmlSpawner2
{
    public class XmlBond : XmlAttachment
    {
        /*public static void Initialize()
		{
			CommandSystem.Register("RecoveBond", AccessLevel.Developer, new CommandEventHandler(Recover_OnCommand));
		}

		private static void Recover_OnCommand( CommandEventArgs e )
		{
			try
			{
				List<string> strings = new List<string>();
				FileInfo file = new FileInfo("recuperibond.txt");
				int recovered=0;
				int total=0;
				using(StreamReader read = new StreamReader(file.FullName))
				{
					string readed=null;
					while((readed=read.ReadLine())!=null)
					{
						//sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", master.Serial.Value, bc.Serial.Value, Expiration.Ticks, bc.GetType, bc.Hue);
						string[] user_serial=readed.Split('\t');
						if(user_serial.Length>1)
						{
							Mobile owner = World.FindMobile(int.Parse(user_serial[0]));
							BaseCreature bc = World.FindMobile(int.Parse(user_serial[1])) as BaseCreature;
							long ticks = long.Parse(user_serial[2]);
							if(bc!=null && bc.ControlMaster==owner)
							{
								XmlBond bond = XmlAttach.FindAttachment(bc, typeof(XmlBond)) as XmlBond;
								if(bond==null || bond.Deleted)
								{
									bond = new XmlBond(new TimeSpan(ticks));
									XmlAttach.AttachTo(bc, bond);
								}
								else
								{
									bond.Remaining = bond.Remaining+new TimeSpan(ticks);
									bond.Expiration = bond.Remaining;
								}
								recovered++;
							}
							else
							{
								//non recovered
								strings.Add(readed);
							}
							total++;
						}
					}
				}
				Console.WriteLine("Done recover - totali {0} - recuperati {1}", total, recovered);
				using ( StreamWriter sw = new StreamWriter( "bond_unrecovered.txt", false ) )
				{
					foreach(string s in strings)
						sw.WriteLine(s);
				}
				e.Mobile.SendMessage("Recuperati {0} animali bondati - totali da recuperare {1}", recovered, total);
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
			}
		}*/

        private InternalTimer m_Timer;
        public void StopTimer()
        {
            if (m_Timer != null && m_Timer.Running)
            {
                m_Remaining = m_Timer.Next - DateTime.UtcNow;
                m_Timer.Stop();
            }
        }
        public void StartTimer()
        {
            if (m_Timer == null || !m_Timer.Running)
            {
                m_Timer = new XmlBond.InternalTimer(this);
                m_Timer.Start();
            }
        }

        private TimeSpan m_Remaining;
        [CommandProperty(AccessLevel.Developer)]
        public TimeSpan Remaining
        {
            get
            {
                if (m_Timer != null && m_Timer.Running)
                {
                    return m_Timer.Next - DateTime.UtcNow;
                }

                return m_Remaining;
            }
            set
            {
                m_Remaining = value;
                if (m_Timer != null && m_Timer.Running)
                {
                    m_Timer.Stop();
                    m_Timer = new XmlBond.InternalTimer(this);
                    m_Timer.Start();
                }
            }
        }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlBond(ASerial serial) : base(serial)
        {
        }

        public XmlBond(TimeSpan tempo)
        {
            Remaining = tempo;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(6);
            writer.Write(Remaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            if (version > 5)
            {
                m_Remaining = reader.ReadTimeSpan();
            }
            else
            {
                AddToPostLoadDelete(this);
                if (version > 0)
                {
                    m_Remaining = reader.ReadTimeSpan();
                }

                if (m_Remaining < TimeSpan.FromHours(200))
                {
                    m_Remaining = TimeSpan.FromHours(200);
                }
            }
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // apply the mod
            if (AttachedTo is BaseCreature bc && Remaining != TimeSpan.Zero)
            {
                bc.IsBonded = true;
                if (bc.ControlMaster != null && bc.ControlMaster.NetState != null && bc.ControlMaster.NetState.Running)
                {
                    StartTimer();
                }
            }
            else
            {
                Delete();
            }
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            if (from == null)
            {
                return null;
            }

            if (AttachedTo is BaseCreature)
            {
                return new LogEntry(1005170, string.Format("{0}\t{1}", (int)Remaining.TotalHours, Remaining.Minutes));
            }
            return null;
        }

        public override void OnDelete()
        {
            if (AttachedTo is BaseCreature bc)
            {
                if (Remaining <= TimeSpan.Zero)
                {
                    if (!bc.Deleted)
                    {
                        if (bc is BaseMount mount)
                        {
                            mount.Rider = null;
                        }

                        if (bc.IsDeadBondedPet)
                        {
                            bc.ResurrectPet();
                        }

                        Mobile master = bc.ControlMaster;
                        bc.IsBonded = false;
                        if (master != null && !master.Deleted)
                        {
                            master.BankBox.AddItem(new ShrinkStatuette(bc));
                        }
                    }
                }
                else
                {
                    if (!bc.Deleted)
                    {
                        if (bc.IsDeadBondedPet)
                        {
                            bc.ResurrectPet();
                        }

                        Mobile master = bc.ControlMaster;
                        bc.IsBonded = false;
                        bc.ControlMaster = master;
                    }
                }
            }
        }

        public override bool OnExpiration()
        {
            return false;
            /*if(Remaining>TimeSpan.Zero)
			{
				Expiration=Remaining;
				Remaining=TimeSpan.Zero;
				return false;
			}*/
            //return true;
        }

        /*public override void OnMovement(MovementEventArgs e )
		{
			base.OnMovement(e);

			if(e.Mobile == null || e.Mobile.AccessLevel > AccessLevel.Player) return;
			Mobile m = e.Mobile;
			Region reg = m.Region;
			BaseCreature bc = (BaseCreature)AttachedTo;

			if(AttachedTo is BaseCreature && m_Timer.Running && reg.AllowHarvestRes)
			{
				XmlBond b = XmlAttach.FindAttachment(bc, typeof(XmlBond)) as XmlBond;
				b.StopTimer();
			} 
			else
				return;
		}*/

        private class InternalTimer : Timer
        {
            private XmlBond m_Attachment;

            public InternalTimer(XmlBond attachment) : base(attachment.m_Remaining)
            {
                Priority = TimerPriority.OneSecond;
                m_Attachment = attachment;
            }

            protected override void OnTick()
            {
                if (m_Attachment != null)//se per un motivo qualsiasi si cancellasse l'attachment, anche a mano, questo timer che esaurisce le ore farà crashare il server...
                {
                    m_Attachment.m_Remaining = TimeSpan.Zero;
                    m_Attachment.Delete();
                }
            }
        }
    }
}