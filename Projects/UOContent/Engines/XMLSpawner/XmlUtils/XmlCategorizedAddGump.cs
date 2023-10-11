using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

/*
** Modified from RunUO 1.0.0 CategorizedAddGump.cs
** by ArteGordon
** 2/5/05
*/
namespace Server.Gumps
{

    public abstract class XmlAddCAGNode
    {
        public abstract string Caption { get; }
        public abstract void OnClick(Mobile from, int page, int index, Gump gump);
    }

    public class XmlAddCAGObject : XmlAddCAGNode
    {
        private Type m_Type;
        private int m_ItemID;
        private int m_Hue;
        private XmlAddCAGCategory m_Parent;

        public Type Type => m_Type;
        public int ItemID => m_ItemID;
        public int Hue => m_Hue;
        public XmlAddCAGCategory Parent => m_Parent;

        public override string Caption => (m_Type == null ? "bad type" : m_Type.Name);

        public override void OnClick(Mobile from, int page, int index, Gump gump)
        {
            if (m_Type == null)
            {
                from.SendMessage("That is an invalid type name.");
            }
            else
            {
                if (gump is XmlSpawnerGump)
                {
                    XmlSpawner m_Spawner = ((XmlSpawnerGump)gump).m_Spawner;

                    if (m_Spawner != null)
                    {
                        XmlSpawnerGump xg = m_Spawner.SpawnerGump;

                        if (xg != null)
                        {

                            xg.Rentry = new XmlSpawnerGump.ReplacementEntry
                            {
                                Typename = m_Type.Name,
                                Index = index,
                                Color = 0x1436
                            };

                            Timer.DelayCall(TimeSpan.Zero, XmlSpawnerGump.Refresh_Callback, from);
                            //from.CloseGump(typeof(XmlSpawnerGump));
                            //from.SendGump( new XmlSpawnerGump(xg.m_Spawner, xg.X, xg.Y, xg.m_ShowGump, xg.xoffset, xg.page, xg.Rentry) );
                        }
                    }
                }
            }
        }

        public XmlAddCAGObject(XmlAddCAGCategory parent, XmlTextReader xml)
        {
            m_Parent = parent;

            if (xml.MoveToAttribute("type"))
            {
                m_Type = ScriptCompiler.FindTypeByFullName(xml.Value, false);
            }

            if (xml.MoveToAttribute("gfx"))
            {
                m_ItemID = XmlConvert.ToInt32(xml.Value);
            }

            if (xml.MoveToAttribute("hue"))
            {
                m_Hue = XmlConvert.ToInt32(xml.Value);
            }
        }
    }

    public class XmlAddCAGCategory : XmlAddCAGNode
    {
        private string m_Title;
        private XmlAddCAGNode[] m_Nodes;
        private XmlAddCAGCategory m_Parent;

        public string Title => m_Title;
        public XmlAddCAGNode[] Nodes => m_Nodes;
        public XmlAddCAGCategory Parent => m_Parent;

        public override string Caption => m_Title;

        public override void OnClick(Mobile from, int page, int index, Gump gump)
        {
            from.SendGump(new XmlCategorizedAddGump(from, this, 0, index, gump));
        }

        private XmlAddCAGCategory()
        {
            m_Title = "no data";
            m_Nodes = new XmlAddCAGNode[0];
        }

        public XmlAddCAGCategory(XmlAddCAGCategory parent, XmlTextReader xml)
        {
            m_Parent = parent;

            if (xml.MoveToAttribute("title"))
            {
                if (xml.Value == "Add Menu")
                {
                    m_Title = "XmlAdd Menu";
                }
                else
                {
                    m_Title = xml.Value;
                }
            }
            else
            {
                m_Title = "empty";
            }

            if (m_Title == "Docked")
            {
                m_Title = "Docked 2";
            }

            if (xml.IsEmptyElement)
            {
                m_Nodes = new XmlAddCAGNode[0];
            }
            else
            {
                List<XmlAddCAGNode> nodes = new List<XmlAddCAGNode>();

                try
                {
                    while (xml.Read() && xml.NodeType != XmlNodeType.EndElement)
                    {

                        if (xml.NodeType == XmlNodeType.Element && xml.Name == "object")
                        {
                            nodes.Add(new XmlAddCAGObject(this, xml));
                        }
                        else if (xml.NodeType == XmlNodeType.Element && xml.Name == "category")
                        {
                            if (!xml.IsEmptyElement)
                            {

                                nodes.Add(new XmlAddCAGCategory(this, xml));
                            }
                        }
                        else
                        {
                            xml.Skip();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("XmlCategorizedAddGump: Corrupted Data/objects.xml file detected. Not all XmlCAG objects loaded. {0}", ex);
                }

                m_Nodes = nodes.ToArray();
            }
        }

        private static XmlAddCAGCategory m_Root;

        public static XmlAddCAGCategory Root
        {
            get
            {
                if (m_Root == null)
                {
                    m_Root = Load("Data/objects.xml");
                }

                return m_Root;
            }
        }

        public static XmlAddCAGCategory Load(string path)
        {
            if (File.Exists(path))
            {
                XmlTextReader xml = new XmlTextReader(path)
                {
                    WhitespaceHandling = WhitespaceHandling.None
                };

                while (xml.Read())
                {
                    if (xml.Name == "category" && xml.NodeType == XmlNodeType.Element)
                    {
                        XmlAddCAGCategory cat = new XmlAddCAGCategory(null, xml);

                        xml.Close();

                        return cat;
                    }
                }
            }

            return new XmlAddCAGCategory();
        }
    }



    public class XmlCategorizedAddGump : Gump
    {
        public static bool OldStyle = PropsConfig.OldStyle;

        public static int GumpOffsetX = PropsConfig.GumpOffsetX;
        public static int GumpOffsetY = PropsConfig.GumpOffsetY;

        public static int TextHue = PropsConfig.TextHue;
        public static int TextOffsetX = PropsConfig.TextOffsetX;

        public static int OffsetGumpID = PropsConfig.OffsetGumpID;
        public static int HeaderGumpID = PropsConfig.HeaderGumpID;
        public static int EntryGumpID = PropsConfig.EntryGumpID;
        public static int BackGumpID = PropsConfig.BackGumpID;
        public static int SetGumpID = PropsConfig.SetGumpID;

        public static int SetWidth = PropsConfig.SetWidth;
        public static int SetOffsetX = PropsConfig.SetOffsetX, SetOffsetY = PropsConfig.SetOffsetY /*+ (((EntryHeight - 20) / 2) / 2)*/;
        public static int SetButtonID1 = PropsConfig.SetButtonID1;
        public static int SetButtonID2 = PropsConfig.SetButtonID2;

        public static int PrevWidth = PropsConfig.PrevWidth;
        public static int PrevOffsetX = PropsConfig.PrevOffsetX, PrevOffsetY = PropsConfig.PrevOffsetY /*+ (((EntryHeight - 20) / 2) / 2)*/;
        public static int PrevButtonID1 = PropsConfig.PrevButtonID1;
        public static int PrevButtonID2 = PropsConfig.PrevButtonID2;

        public static int NextWidth = PropsConfig.NextWidth;
        public static int NextOffsetX = PropsConfig.NextOffsetX, NextOffsetY = PropsConfig.NextOffsetY /*+ (((EntryHeight - 20) / 2) / 2)*/;
        public static int NextButtonID1 = PropsConfig.NextButtonID1;
        public static int NextButtonID2 = PropsConfig.NextButtonID2;

        public static int OffsetSize = PropsConfig.OffsetSize;

        public static int EntryHeight = 24;//PropsConfig.EntryHeight;
        public static int BorderSize = PropsConfig.BorderSize;

        private static bool PrevLabel = false, NextLabel = false;

        private static int PrevLabelOffsetX = PrevWidth + 1;
        private static int PrevLabelOffsetY = 0;

        private static int NextLabelOffsetX = -29;
        private static int NextLabelOffsetY = 0;

        private static int EntryWidth = 180;
        private static int EntryCount = 15;

        private static int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
        private static int TotalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (EntryCount + 1));

        private static int BackWidth = BorderSize + TotalWidth + BorderSize;
        private static int BackHeight = BorderSize + TotalHeight + BorderSize;
        private Mobile m_Owner;
        private XmlAddCAGCategory m_Category;
        private int m_Page;

        private int m_Index = -1;
        private Gump m_Gump;
        private XmlSpawner m_Spawner;

        public XmlCategorizedAddGump(Mobile owner, int index, Gump gump) : this(owner, XmlAddCAGCategory.Root, 0, index, gump)
        {
        }

        public XmlCategorizedAddGump(Mobile owner, XmlAddCAGCategory category, int page, int index, Gump gump) : base(GumpOffsetX, GumpOffsetY)
        {
            if (category == null)
            {
                category = XmlAddCAGCategory.Root;
                page = 0;
            }

            owner.CloseGump(typeof(WhoGump));

            m_Owner = owner;
            m_Category = category;

            m_Index = index;
            m_Gump = gump;
            if (gump is XmlSpawnerGump)
            {
                m_Spawner = ((XmlSpawnerGump)gump).m_Spawner;


            }

            Initialize(page);
        }

        public void Initialize(int page)
        {
            m_Page = page;

            XmlAddCAGNode[] nodes = m_Category.Nodes;

            int count = nodes.Length - (page * EntryCount);

            if (count < 0)
            {
                count = 0;
            }
            else if (count > EntryCount)
            {
                count = EntryCount;
            }

            int totalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (count + 1));

            AddPage(0);

            AddBackground(0, 0, BackWidth, BorderSize + totalHeight + BorderSize, BackGumpID);
            AddImageTiled(BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), totalHeight, OffsetGumpID);

            int x = BorderSize + OffsetSize;
            int y = BorderSize + OffsetSize;

            if (OldStyle)
            {
                AddImageTiled(x, y, TotalWidth - (OffsetSize * 3) - SetWidth, EntryHeight, HeaderGumpID);
            }
            else
            {
                AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);
            }

            if (m_Category.Parent != null)
            {
                AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 1, GumpButtonType.Reply, 0);

                if (PrevLabel)
                {
                    AddHtmlLocalized(x + PrevLabelOffsetX, y + PrevLabelOffsetY, 60, 20, 1043354, 0, false, false);
                    //AddLabel(x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous");
                }
            }

            x += PrevWidth + OffsetSize;

            int emptyWidth = TotalWidth - (PrevWidth * 2) - NextWidth - (OffsetSize * 5) - (OldStyle ? SetWidth + OffsetSize : 0);

            if (!OldStyle)
            {
                AddImageTiled(x - (OldStyle ? OffsetSize : 0), y, emptyWidth + (OldStyle ? OffsetSize * 2 : 0), EntryHeight, EntryGumpID);
            }

            AddHtml(x + TextOffsetX, y + ((EntryHeight - 20) >> 1), emptyWidth - TextOffsetX, EntryHeight, string.Format("<center>{0}</center>", m_Category.Caption), false, false);

            x += emptyWidth + OffsetSize;

            if (OldStyle)
            {
                AddImageTiled(x, y, TotalWidth - (OffsetSize * 3) - SetWidth, EntryHeight, HeaderGumpID);
            }
            else
            {
                AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);
            }

            if (page > 0)
            {
                AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 2, GumpButtonType.Reply, 0);

                if (PrevLabel)
                {
                    AddHtmlLocalized(x + PrevLabelOffsetX, y + PrevLabelOffsetY, 60, 20, 1043354, 0, false, false);
                    //AddLabel(x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous");
                }
            }

            x += PrevWidth + OffsetSize;

            if (!OldStyle)
            {
                AddImageTiled(x, y, NextWidth, EntryHeight, HeaderGumpID);
            }

            if ((page + 1) * EntryCount < nodes.Length)
            {
                AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, 3, GumpButtonType.Reply, 1);

                if (NextLabel)
                {
                    AddHtmlLocalized(x + NextLabelOffsetX, y + NextLabelOffsetY, 60, 20, 1043353, 0, false, false);
                    //AddLabel(x + NextLabelOffsetX, y + NextLabelOffsetY, TextHue, "Next");
                }
            }

            for (int i = 0, index = page * EntryCount; i < EntryCount && index < nodes.Length; ++i, ++index)
            {
                x = BorderSize + OffsetSize;
                y += EntryHeight + OffsetSize;

                XmlAddCAGNode node = nodes[index];

                AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
                AddLabelCropped(x + TextOffsetX, y + ((EntryHeight - 20) >> 1), EntryWidth - TextOffsetX, EntryHeight, TextHue, node.Caption);

                x += EntryWidth + OffsetSize;

                if (SetGumpID != 0)
                {
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                }

                AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, i + 4, GumpButtonType.Reply, 0);

                if (node is XmlAddCAGObject)
                {
                    XmlAddCAGObject obj = (XmlAddCAGObject)node;
                    int itemID = obj.ItemID;

                    Rectangle2D bounds = ItemBounds.Table[itemID];

                    if (itemID != 1 && bounds.Height < (EntryHeight * 2))
                    {
                        if (bounds.Height < EntryHeight)
                        {
                            AddItem(x - OffsetSize - 22 - ((i % 2) * 44) - (bounds.Width >> 1) - bounds.X, y + (EntryHeight >> 1) - (bounds.Height >> 1) - bounds.Y, itemID);
                        }
                        else
                        {
                            AddItem(x - OffsetSize - 22 - ((i % 2) * 44) - (bounds.Width >> 1) - bounds.X, y + EntryHeight - 1 - bounds.Height - bounds.Y, itemID);
                        }
                    }
                }
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = m_Owner;

            switch (info.ButtonID)
            {
                case 0: // Closed
                {
                    return;
                }
                case 1: // Up
                {
                    if (m_Category.Parent != null)
                    {
                        int index = Array.IndexOf(m_Category.Parent.Nodes, m_Category) / EntryCount;

                        if (index < 0)
                        {
                            index = 0;
                        }

                        from.SendGump(new XmlCategorizedAddGump(from, m_Category.Parent, index, m_Index, m_Gump));
                    }

                    break;
                }
                case 2: // Previous
                {
                    if (m_Page > 0)
                    {
                        from.SendGump(new XmlCategorizedAddGump(from, m_Category, m_Page - 1, m_Index, m_Gump));
                    }

                    break;
                }
                case 3: // Next
                {
                    if ((m_Page + 1) * EntryCount < m_Category.Nodes.Length)
                    {
                        from.SendGump(new XmlCategorizedAddGump(from, m_Category, m_Page + 1, m_Index, m_Gump));
                    }

                    break;
                }
                default:
                {
                    int index = (m_Page * EntryCount) + (info.ButtonID - 4);

                    if (index >= 0 && index < m_Category.Nodes.Length)
                    {
                        m_Category.Nodes[index].OnClick(from, m_Page, m_Index, m_Gump);
                    }

                    break;
                }
            }
        }
    }
}
