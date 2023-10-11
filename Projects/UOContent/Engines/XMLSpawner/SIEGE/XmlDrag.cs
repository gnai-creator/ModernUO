using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlDrag : XmlAttachment
    {
        private Mobile m_DraggedBy = null;    // mobile doing the dragging
        private Point3D m_currentloc = Point3D.Zero;
        private Map m_currentmap = null;
        private int m_Distance = -2;

        private InternalTimer m_Timer;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile DraggedBy
        {
            get => m_DraggedBy;
            set
            {
                if (value != m_DraggedBy)
                {
                    if (m_DraggedBy != null)//we are changing dragger
                    {
                        SwitchSpeed(false);
                    }
                    m_DraggedBy = value;
                    BaseSiegeWeapon bsw = AttachedTo as BaseSiegeWeapon;
                    if (m_DraggedBy != null)//if new dragger is not null then switch speed
                    {
                        DoTimer();
                        SwitchSpeed(true);
                        if (bsw != null)
                        {
                            bsw.IsPackable = false;
                        }
                    }
                    else if (bsw != null)
                    {
                        bsw.IsPackable = true;
                    }
                }
            }
        }

        private void SwitchSpeed(bool attaching)
        {
            if (m_DraggedBy is BaseCreature bc)
            {
                float d = (attaching ? 2.0f : 0.5f);
                bc.PassiveSpeed *= d;
                bc.ActiveSpeed *= d;
            }
            else
            {
                m_DraggedBy.SwitchSpeedControl(RunningFlags.Dragging, attaching);
            }

            if (!attaching)
            {
                m_DraggedBy = null;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Distance { get => m_Distance; set => m_Distance = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D CurrentLoc { get => m_currentloc; set => m_currentloc = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map CurrentMap { get => m_currentmap; set => m_currentmap = value; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlDrag(ASerial serial)
            : base(serial)
        {
        }

        [Attachable]
        public XmlDrag()
        {
        }

        [Attachable]
        public XmlDrag(Mobile draggedby)
        {
            DraggedBy = draggedby;
        }

        [Attachable]
        public XmlDrag(string name, Mobile draggedby)
        {
            Name = name;
            DraggedBy = draggedby;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(m_DraggedBy);
            writer.Write(m_Distance);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            /*int version =*/
            reader.ReadInt();
            // version 0
            m_DraggedBy = reader.ReadMobile();
            m_Distance = reader.ReadInt();
        }

        public override void OnAttach()
        {
            base.OnAttach();

            if (AttachedTo != null)
            {
                DoTimer();
            }
            else
            {
                Delete();
            }
        }

        public override void OnReattach()
        {
            base.OnReattach();

            DoTimer();
        }

        public override void OnDelete()
        {
            base.OnDelete();

            if (m_DraggedBy != null)
            {
                SwitchSpeed(false);
            }

            if (m_Timer != null)
            {
                m_Timer.Stop();
            }
        }

        public void DoTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
            }

            m_Timer = new InternalTimer(this);
            m_Timer.Start();
        }

        // added the duration timer that begins on spawning
        private class InternalTimer : Timer
        {
            private XmlDrag m_attachment;

            public InternalTimer(XmlDrag attachment)
                : base(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500))
            {
                Priority = TimerPriority.FiftyMS;
                m_attachment = attachment;
            }

            protected override void OnTick()
            {
                if (m_attachment == null)
                {
                    return;
                }

                Mobile draggedby = m_attachment.DraggedBy;
                BaseCreature bc = draggedby as BaseCreature;

                if (!(m_attachment.AttachedTo is ISpawnable parent) || draggedby == null || !draggedby.Alive || draggedby == parent || (bc != null && (string.IsNullOrEmpty(m_attachment.Name) || m_attachment.Name.ToLower(Core.Culture) != "monster") && (!bc.Controlled || bc.ControlMaster == null)))
                {
                    if (draggedby != null)
                    {
                        m_attachment.SwitchSpeed(false);
                    }

                    m_attachment.DraggedBy = null;
                    Stop();
                    return;
                }

                // get the location of the mobile dragging

                Point3D newloc = draggedby.Location;
                Map newmap = draggedby.Map;

                if (newmap == null || newmap == Map.Internal)
                {
                    // if the mobile dragging has an invalid map, then disconnect
                    m_attachment.DraggedBy = null;
                    Stop();
                    return;
                }

                // update the location of the dragged object if the parent has moved
                if (newloc != m_attachment.CurrentLoc || newmap != m_attachment.CurrentMap)
                {
                    m_attachment.CurrentLoc = newloc;
                    m_attachment.CurrentMap = newmap;

                    int x = newloc.X;
                    int y = newloc.Y;
                    int lag = m_attachment.Distance;

                    int facing = 0;
                    // compute the new location for the dragged object
                    switch (draggedby.Direction & Direction.Mask)
                    {
                        case Direction.North: y -= lag; facing = 3; break;
                        case Direction.Right: x += lag; y -= lag; facing = -4; break;
                        case Direction.East: x += lag; facing = 0; break;
                        case Direction.Down: x += lag; y += lag; facing = -1; break;
                        case Direction.South: y += lag; facing = 1; break;
                        case Direction.Left: x -= lag; y += lag; facing = -2; break;
                        case Direction.West: x -= lag; facing = 2; break;
                        case Direction.Up: x -= lag; y -= lag; facing = -3; break;
                    }
                    if (parent is BaseSiegeWeapon bsw)
                    {
                        if (facing < 0)
                        {
                            facing = Math.Abs(facing);
                            bsw.Facing = Utility.RandomMinMax(facing - 1, facing);
                        }
                        else
                        {
                            bsw.Facing = facing;
                        }
                    }

                    parent.MoveToWorld(new Point3D(x, y, newloc.Z), newmap);
                }
            }
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            if (from == null || from.AccessLevel == AccessLevel.Player)
            {
                return null;
            }

            if (Expiration > TimeSpan.Zero)
            {
                return new LogEntry(1005120, string.Format("{0}\t{1}\t{2}", Name, DraggedBy, Expiration.TotalMinutes));//String.Format("{2}: Trainato da {0} termina in {1} minuti", DraggedBy, Expiration.TotalMinutes, Name);
            }
            else
            {
                return new LogEntry(1005121, string.Format("{0}\t{1}", Name, DraggedBy));
            }
        }
    }
}
