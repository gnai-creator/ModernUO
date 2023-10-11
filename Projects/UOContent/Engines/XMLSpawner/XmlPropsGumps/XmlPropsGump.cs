using Server.Commands.Generic;
using Server.Engines.XmlSpawner2;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Reflection;
using CPA = Server.CommandPropertyAttribute;
/*
** modified properties gumps taken from RC0 properties gump scripts to support the special XmlSpawner properties gump
*/

namespace Server.Gumps
{
    public class XmlPropertiesGump : Gump
    {
        private List<PropertyObject> m_List;
        private int m_Page;
        private Mobile m_Mobile;
        private object m_Object;
        private Stack<PropertiesGump.StackEntry> m_Stack;

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
        public static int SetOffsetX = PropsConfig.SetOffsetX, SetOffsetY = PropsConfig.SetOffsetY;
        public static int SetButtonID1 = PropsConfig.SetButtonID1;
        public static int SetButtonID2 = PropsConfig.SetButtonID2;

        public static int PrevWidth = PropsConfig.PrevWidth;
        public static int PrevOffsetX = PropsConfig.PrevOffsetX, PrevOffsetY = PropsConfig.PrevOffsetY;
        public static int PrevButtonID1 = PropsConfig.PrevButtonID1;
        public static int PrevButtonID2 = PropsConfig.PrevButtonID2;

        public static int NextWidth = PropsConfig.NextWidth;
        public static int NextOffsetX = PropsConfig.NextOffsetX, NextOffsetY = PropsConfig.NextOffsetY;
        public static int NextButtonID1 = PropsConfig.NextButtonID1;
        public static int NextButtonID2 = PropsConfig.NextButtonID2;

        public static int OffsetSize = PropsConfig.OffsetSize;

        public static int EntryHeight = PropsConfig.EntryHeight;
        public static int BorderSize = PropsConfig.BorderSize;

        private static bool PrevLabel = OldStyle, NextLabel = OldStyle;

        private static int PrevLabelOffsetX = PrevWidth + 1;
        //private static int PrevLabelOffsetY = 0;

        //private static int NextLabelOffsetX = -29;
        //private static int NextLabelOffsetY = 0;

        private static int NameWidth = 103;
        private static int ValueWidth = 82;

        private static int EntryCount = 69;
        private static int ColumnEntryCount = 23;
        private static int TotalColumns = 3;

        private static int TypeWidth = NameWidth + OffsetSize + ValueWidth;

        private static int TotalWidth = OffsetSize + NameWidth + OffsetSize + ValueWidth + OffsetSize + SetWidth + OffsetSize;
        private static int TotalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (EntryCount + 1));

        private static int BackWidth = BorderSize + TotalWidth + BorderSize;
        private static int BackHeight = BorderSize + TotalHeight + BorderSize;

        public XmlPropertiesGump(Mobile mobile, object o) : base(GumpOffsetX, GumpOffsetY)
        {
            m_Mobile = mobile;
            m_Object = o;
            m_List = PropertiesGump.BuildList(m_Mobile, m_Object.GetType());

            Initialize(0);
        }

        public XmlPropertiesGump(Mobile mobile, object o, Stack<PropertiesGump.StackEntry> stack, object parent) : base(GumpOffsetX, GumpOffsetY)
        {
            m_Mobile = mobile;
            m_Object = o;
            m_Stack = stack;
            m_List = PropertiesGump.BuildList(m_Mobile, m_Object.GetType());

            if (parent != null)
            {
                if (m_Stack == null)
                {
                    m_Stack = new Stack<PropertiesGump.StackEntry>();
                }

                m_Stack.Push(new PropertiesGump.StackEntry(parent, null));
            }

            Initialize(0);
        }

        public XmlPropertiesGump(Mobile mobile, object o, Stack<PropertiesGump.StackEntry> stack, List<PropertyObject> list, int page) : base(GumpOffsetX, GumpOffsetY)
        {
            m_Mobile = mobile;
            m_Object = o;
            m_List = list;
            m_Stack = stack;

            Initialize(page);
        }

        private void Initialize(int page)
        {
            m_Page = page;

            int count = m_List.Count - (page * EntryCount);

            if (count < 0)
            {
                count = 0;
            }
            else if (count > EntryCount)
            {
                count = EntryCount + 1;
            }

            int lastIndex = (page * EntryCount) + count - 1;

            if (lastIndex >= 0 && lastIndex < m_List.Count && m_List[lastIndex].Type == null)
            {
                --count;
            }

            int totalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (ColumnEntryCount + 1));

            AddPage(0);

            AddBackground(0, 0, TotalWidth * TotalColumns + BorderSize * 2, BorderSize + totalHeight + BorderSize, BackGumpID);
            AddImageTiled(BorderSize, BorderSize + EntryHeight, (TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0)) * TotalColumns, totalHeight - EntryHeight, OffsetGumpID);

            int x = BorderSize + OffsetSize;
            int y = BorderSize /*+ OffsetSize*/;
            //TotalWidth - PrevWidth - NextWidth - (OffsetSize * 4) - (OldStyle ? SetWidth + OffsetSize : 0);

            if (m_Object is Item)
            {
                AddLabelCropped(x + TextOffsetX, y, TypeWidth - TextOffsetX, EntryHeight, TextHue, ((Item)m_Object).Name);
            }

            int propcount = 0;
            for (int i = 0, index = page * EntryCount; i < count && index < m_List.Count; ++i, ++index)
            {
                // do the multi column display
                int column = propcount / ColumnEntryCount;
                if (propcount % ColumnEntryCount == 0)
                {
                    y = BorderSize;
                }

                x = BorderSize + OffsetSize + column * (ValueWidth + NameWidth + OffsetSize * 2 + SetOffsetX + SetWidth);
                y += EntryHeight + OffsetSize;



                PropertyObject o = m_List[index];

                if (o.Type == null)
                {
                    AddImageTiled(x - OffsetSize, y, TotalWidth, EntryHeight, BackGumpID + 4);
                    propcount++;
                }
                else if(o.Info != null)
                {
                    propcount++;

                    PropertyInfo prop = o.Info;

                    // look for the default value of the equivalent property in the XmlSpawnerDefaults.DefaultEntry class

                    int huemodifier = TextHue;
                    XmlSpawnerDefaults de = XmlSpawnerDefaults.Instance;
                    FieldInfo finfo = XmlSpawnerDefaults.TypeInstance.GetField(prop.Name);
                    // is there an equivalent default field?
                    if (finfo != null)
                    {
                        // see if the value is different from the default
                        if (PropertiesGump.ValueToString(finfo.GetValue(de)) != PropertiesGump.ValueToString(m_Object, prop))
                        {
                            huemodifier = 68;
                        }
                    }

                    AddImageTiled(x, y, NameWidth, EntryHeight, EntryGumpID);
                    AddLabelCropped(x + TextOffsetX, y, NameWidth - TextOffsetX, EntryHeight, huemodifier, prop.Name);
                    x += NameWidth + OffsetSize;
                    AddImageTiled(x, y, ValueWidth, EntryHeight, EntryGumpID);
                    AddLabelCropped(x + TextOffsetX, y, ValueWidth - TextOffsetX, EntryHeight, huemodifier, PropertiesGump.ValueToString(m_Object, prop));
                    x += ValueWidth + OffsetSize;

                    if (SetGumpID != 0)
                    {
                        AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                    }

                    CPA cpa = PropertiesGump.GetCPA(prop);

                    if (prop.CanWrite && cpa != null && m_Mobile.AccessLevel >= cpa.WriteLevel)
                    {
                        AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, i + 3, GumpButtonType.Reply, 0);
                    }
                }
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            if (!BaseCommand.IsAccessible(from, m_Object))
            {
                from.SendMessage("You may no longer access their properties.");
                return;
            }

            switch (info.ButtonID)
            {
                case 0: // Closed
                {
                    if (m_Stack != null && m_Stack.Count > 0)
                    {
                        PropertiesGump.StackEntry obj = m_Stack.Pop();

                        from.SendGump(new XmlPropertiesGump(from, obj.m_Object, m_Stack, null));
                    }

                    break;
                }
                case 1: // Previous
                {
                    if (m_Page > 0)
                    {
                        from.SendGump(new XmlPropertiesGump(from, m_Object, m_Stack, m_List, m_Page - 1));
                    }

                    break;
                }
                case 2: // Next
                {
                    if ((m_Page + 1) * EntryCount < m_List.Count)
                    {
                        from.SendGump(new XmlPropertiesGump(from, m_Object, m_Stack, m_List, m_Page + 1));
                    }

                    break;
                }
                default:
                {
                    int index = (m_Page * EntryCount) + (info.ButtonID - 3);

                    if (index >= 0 && index < m_List.Count)
                    {
                        PropertyInfo prop = m_List[index].Info;

                        if (prop == null)
                        {
                            return;
                        }

                        CPA attr = PropertiesGump.GetCPA(prop);

                        if (!prop.CanWrite || attr == null || from.AccessLevel < attr.WriteLevel)
                        {
                            return;
                        }

                        Type type = prop.PropertyType;

                        if (PropertiesGump.IsType(type, PropertiesGump.typeofMobile) || PropertiesGump.IsType(type, PropertiesGump.typeofItem))
                        {
                            from.SendGump(new XmlSetObjectGump(prop, from, m_Object, m_Stack, type, m_Page, m_List));
                        }
                        else if (PropertiesGump.IsType(type, PropertiesGump.typeofType))
                        {
                            from.Target = new XmlSetObjectTarget(prop, from, m_Object, m_Stack, type, m_Page, m_List);
                        }
                        else if (PropertiesGump.IsType(type, PropertiesGump.typeofPoint3D))
                        {
                            from.SendGump(new XmlSetPoint3DGump(prop, from, m_Object, m_Stack, m_Page, m_List));
                        }
                        else if (PropertiesGump.IsType(type, PropertiesGump.typeofPoint2D))
                        {
                            from.SendGump(new XmlSetPoint2DGump(prop, from, m_Object, m_Stack, m_Page, m_List));
                        }
                        else if (PropertiesGump.IsType(type, PropertiesGump.typeofTimeSpan))
                        {
                            from.SendGump(new XmlSetTimeSpanGump(prop, from, m_Object, m_Stack, m_Page, m_List));
                        }
                        else if (PropertiesGump.IsCustomEnum(type))
                        {
                            from.SendGump(new XmlSetCustomEnumGump(prop, from, m_Object, m_Stack, m_Page, m_List, PropertiesGump.GetCustomEnumNames(type)));
                        }
                        else if (PropertiesGump.IsType(type, PropertiesGump.typeofEnum))
                        {
                            from.SendGump(new XmlSetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List, Enum.GetNames(type), PropertiesGump.GetObjects(Enum.GetValues(type))));
                        }
                        else if (PropertiesGump.IsType(type, PropertiesGump.typeofBool))
                        {
                            from.SendGump(new XmlSetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List, PropertiesGump.BoolNames, PropertiesGump.BoolValues));
                        }
                        else if (PropertiesGump.IsType(type, PropertiesGump.typeofString) || PropertiesGump.IsType(type, PropertiesGump.typeofReal) || PropertiesGump.IsType(type, PropertiesGump.typeofNumeric) || PropertiesGump.IsType(type, PropertiesGump.typeofText))
                        {
                            from.SendGump(new XmlSetGump(prop, from, m_Object, m_Stack, m_Page, m_List));
                        }
                        else if (PropertiesGump.IsType(type, PropertiesGump.typeofPoison))
                        {
                            from.SendGump(new XmlSetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List, PropertiesGump.PoisonNames, PropertiesGump.PoisonValues));
                        }
                        else if (PropertiesGump.IsType(type, PropertiesGump.typeofMap))
                        {
                            from.SendGump(new XmlSetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List, Map.GetMapNames(), Map.GetMapValues()));
                        }
                        else if (PropertiesGump.IsType(type, PropertiesGump.typeofSkills) && m_Object is Mobile)
                        {
                            from.SendGump(new XmlPropertiesGump(from, m_Object, m_Stack, m_List, m_Page));
                            from.SendGump(new SkillsGump(from, (Mobile)m_Object));
                        }
                        else if (PropertiesGump.HasAttribute(type, PropertiesGump.typeofPropertyObject, true))
                        {
                            from.SendGump(new XmlPropertiesGump(from, prop.GetValue(m_Object, null), m_Stack, m_Object));
                        }
                    }

                    break;
                }
            }
        }
    }
}
