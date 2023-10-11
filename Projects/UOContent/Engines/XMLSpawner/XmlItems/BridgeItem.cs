/* Questo è un item che permette la realizzazione, mediante item attivatore (leva, xmlspawner, npc, etc),
 * di un ponte tra due punti distanti tra loro. Quest'item non provvederà a disattivarsi da solo o a fare altro,
 * il resto deve essere demandato all'attivatore.
 */

using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class BridgeItem : Item
    {
        public override int LabelNumber => 504522;
        private bool m_Active = false;
        private bool m_Stale = false;//for safety reason

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => m_Active;
            set
            {
                if (m_Active != value && !m_Stale)//only proceed if we are not in the middle of timed modifications...
                {
                    m_Active = value;
                    ActivationMethod();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BridgeSound { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D BridgeStart { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D BridgeEnd { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BridgeItemIDmin { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BridgeItemIDmax { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BridgeHueMin { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BridgeHueMax { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BridgeTimingMsec { get; set; }

        private int m_OneLine = 0;

        private List<Item> m_CurrentBridge = new List<Item>();

        [Constructable]
        public BridgeItem() : base(0x1ED0)
        {
            BridgeItemIDmin = 1205;
            BridgeItemIDmax = 1208;
            BridgeHueMax = 0;
            BridgeHueMin = 0;
            BridgeSound = 242;
            Visible = false;
            Movable = false;
        }

        public BridgeItem(Serial serial) : base(serial)
        {
        }

        public void ActivationMethod()
        {
            if (m_Active)
            {
                if (BridgeItemIDmin <= 0)
                {
                    return;
                }

                if (BridgeStart == Point3D.Zero || BridgeEnd == Point3D.Zero)
                {
                    return;
                }

                Map map = Map;
                if (map == null)
                {
                    return;
                }

                if (BridgeItemIDmax < BridgeItemIDmin)
                {
                    BridgeItemIDmax = BridgeItemIDmin;
                }

                if (BridgeHueMax < BridgeHueMin)
                {
                    BridgeHueMax = BridgeHueMin;
                }

                bool reverseX = true, reverseY = true;
                int startX = BridgeEnd.X, startY = BridgeEnd.Y, endX = BridgeStart.X, endY = BridgeStart.Y, temp;
                if (BridgeStart.X < BridgeEnd.X)
                {
                    startX = BridgeStart.X;
                    endX = BridgeEnd.X;
                    reverseX = false;
                }
                if (BridgeStart.Y < BridgeEnd.Y)
                {
                    startY = BridgeStart.Y;
                    endY = BridgeEnd.Y;
                    reverseY = false;
                }

                int z = (BridgeStart.Z <= BridgeEnd.Z) ? BridgeEnd.Z : BridgeStart.Z;

                bool straight = true;
                if ((endX - startX) < (endY - startY))
                {
                    straight = false;
                }

                if (straight)
                {
                    if (reverseX)
                    {
                        temp = startX;
                        startX = endX;
                        endX = temp;
                        --endX;
                    }
                    else
                    {
                        endX++;
                    }
                }
                else
                {
                    if (reverseY)
                    {
                        temp = startY;
                        startY = endY;
                        endY = temp;
                        --endY;
                    }
                    else
                    {
                        endY++;
                    }
                }

                m_OneLine = (straight ? endY - startY : endX - startX) + 1;//for removing effects
                TemporizedBridge((straight ? BridgeStart.X : startX), (straight ? startY : BridgeStart.Y), z, endX, endY, map, straight, (straight ? reverseX : reverseY));
            }
            else
            {
                m_Stale = true;//safetyness
                TemporizeRemoval();
            }
        }

        private void TemporizeRemoval()
        {
            if (m_OneLine <= 0)//safety check for older items & wrong cases
            {
                m_OneLine = m_CurrentBridge.Count;
            }

            for (int i = m_CurrentBridge.Count - 1, c = m_OneLine; i >= 0 && c > 0; --i, --c)
            {
                Item item = m_CurrentBridge[i];
                if (c == 1 && BridgeSound > 0)//last item of a line makes sound
                {
                    Effects.PlaySound(item.Location, item.Map, BridgeSound);
                }

                m_CurrentBridge.Remove(item);
                item.Delete();
            }
            if (m_CurrentBridge.Count > 0)//se ne abbiamo ancora dilazioniamolo
            {
                Timer.DelayCall(TimeSpan.FromMilliseconds(BridgeTimingMsec), TemporizeRemoval);
            }
            else
            {
                m_Stale = false;
            }
        }

        private void TemporizedBridge(int x, int y, int z, int maxX, int maxY, Map map, bool straight, bool reverse)
        {
            if (straight)
            {
                for (int i = y; i <= maxY; ++i) //posizioniamo tutta la larghezza subito
                {
                    m_CurrentBridge.Add(new TrapPaver { ItemID = Utility.RandomMinMax(BridgeItemIDmin, BridgeItemIDmax), Hue = Utility.RandomMinMax(BridgeHueMin, BridgeHueMax), Location = new Point3D(x, i, z), Map = map });
                }
                if (BridgeSound > 0)
                {
                    Effects.PlaySound(new Point3D(x, y, z), map, BridgeSound);
                }
                int next = (reverse ? x - 1 : x + 1);
                if (next != maxX) //e dilazioniamo il resto ;)
                {
                    Timer.DelayCall(TimeSpan.FromMilliseconds(BridgeTimingMsec), delegate { if (!Deleted && m_Active) { TemporizedBridge(next, y, z, maxX, maxY, map, straight, reverse); } });
                }
            }
            else
            {
                for (int i = x; i <= maxX; ++i) //posizioniamo tutta la larghezza subito
                {
                    m_CurrentBridge.Add(new TrapPaver { ItemID = Utility.RandomMinMax(BridgeItemIDmin, BridgeItemIDmax), Hue = Utility.RandomMinMax(BridgeHueMin, BridgeHueMax), Location = new Point3D(i, y, z), Map = map });
                }
                if (BridgeSound > 0)
                {
                    Effects.PlaySound(new Point3D(x, y, z), map, BridgeSound);
                }
                int next = (reverse ? y - 1 : y + 1);
                if (next != maxY) //e dilazioniamo il resto ;)
                {
                    Timer.DelayCall(TimeSpan.FromMilliseconds(BridgeTimingMsec), delegate { if (!Deleted && m_Active) { TemporizedBridge(x, next, z, maxX, maxY, map, straight, reverse); } });
                }
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();
            if (m_CurrentBridge != null)
            {
                for (int i = m_CurrentBridge.Count - 1; i >= 0; --i)
                {
                    m_CurrentBridge[i].Delete();
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(5); //version

            //version 4
            writer.Write(m_OneLine);
            //version 3
            writer.Write(BridgeTimingMsec);
            //version 2
            writer.Write(BridgeSound);
            //version 1
            writer.Write(BridgeHueMax);
            writer.Write(BridgeHueMin);
            //version 0
            writer.Write(BridgeStart);
            writer.Write(BridgeEnd);
            writer.Write(m_Active);
            writer.Write(BridgeItemIDmin);
            writer.Write(BridgeItemIDmax);
            writer.WriteItemList(m_CurrentBridge);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 5:
                case 4:
                {
                    m_OneLine = reader.ReadInt();
                    goto case 3;
                }
                case 3:
                {
                    BridgeTimingMsec = reader.ReadInt();
                    goto case 2;
                }
                case 2:
                {
                    BridgeSound = reader.ReadInt();
                    goto case 1;
                }
                case 1:
                {
                    BridgeHueMax = reader.ReadInt();
                    BridgeHueMin = reader.ReadInt();
                    goto case 0;
                }
                case 0:
                {
                    BridgeStart = reader.ReadPoint3D();
                    BridgeEnd = reader.ReadPoint3D();
                    m_Active = reader.ReadBool();
                    BridgeItemIDmin = reader.ReadInt();
                    BridgeItemIDmax = reader.ReadInt();
                    m_CurrentBridge = reader.ReadStrongItemList();
                    break;
                }
            }
            if (version < 5)
            {
                Name = null;
                if (version < 3)
                {
                    BridgeTimingMsec = 500;
                }
            }

        }
    }

    public class TrapPaver : BaseFloor
    {
        [Constructable]
        public TrapPaver() : base(1313, 1)
        {
        }

        public TrapPaver(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }

        public override void OnDelete()
        {
            foreach (Mobile m in GetMobilesInRange(0))
            {
                if (m.Location == GetSurfaceTop())
                {
                    m.SendLocalizedMessage(504523);//"Il pavimento cede...");
                    Timer.DelayCall(delegate
                    {
                        if (m != null && !m.Deleted)
                        {
                            Movement.Movement.CheckMovement(m, m.Map, m.Location, m.Direction, out int newZ);
                            int oldZcalc = m.Z - 20;
                            m.Z = newZ;
                            if (newZ < oldZcalc)
                            {
                                Timer.DelayCall(TimeSpan.FromSeconds(2), delegate { if (m != null && !m.Deleted) { m.SendLocalizedMessage(504524); } });//"Sei caduto da un'altezza troppo elevata e sei morto.");
                                m.Kill();
                            }
                        }
                    });
                }
            }
        }
    }
}
