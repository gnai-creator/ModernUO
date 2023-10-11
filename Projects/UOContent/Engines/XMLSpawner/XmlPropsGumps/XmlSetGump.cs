using Server.Commands;
using Server.HuePickers;
using Server.Network;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace Server.Gumps
{
    public class XmlSetGump : Gump
    {
        private PropertyInfo m_Property;
        private Mobile m_Mobile;
        private object m_Object;
        private Stack<PropertiesGump.StackEntry> m_Stack;
        private int m_Page;
        private List<PropertyObject> m_List;

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

        private static int EntryWidth = 212;

        private static int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
        private static int TotalHeight = OffsetSize + (2 * (EntryHeight + OffsetSize));

        private static int BackWidth = BorderSize + TotalWidth + BorderSize;
        private static int BackHeight = BorderSize + TotalHeight + BorderSize;

        public XmlSetGump(PropertyInfo prop, Mobile mobile, object o, Stack<PropertiesGump.StackEntry> stack, int page, List<PropertyObject> list) : base(GumpOffsetX, GumpOffsetY)
        {
            m_Property = prop;
            m_Mobile = mobile;
            m_Object = o;
            m_Stack = stack;
            m_Page = page;
            m_List = list;

            bool canNull = !prop.PropertyType.IsValueType;
            bool canDye = prop.IsDefined(typeof(HueAttribute), false);
            bool isBody = prop.IsDefined(typeof(BodyAttribute), false);

            int xextend = 0;
            if (prop.PropertyType == typeof(string))
            {
                xextend = 300;
            }

            object val = prop.GetValue(m_Object, null);
            string initialText;

            if (val == null)
            {
                initialText = "";
            }
            else if (val is TextDefinition)
            {
                initialText = ((TextDefinition)val).GetValue();
            }
            else
            {
                initialText = val.ToString();
            }

            AddPage(0);

            AddBackground(0, 0, BackWidth + xextend, BackHeight + (canNull ? (EntryHeight + OffsetSize) : 0) + (canDye ? (EntryHeight + OffsetSize) : 0) + (isBody ? (EntryHeight + OffsetSize) : 0), BackGumpID);
            AddImageTiled(BorderSize, BorderSize, TotalWidth + xextend - (OldStyle ? SetWidth + OffsetSize : 0), TotalHeight + (canNull ? (EntryHeight + OffsetSize) : 0) + (canDye ? (EntryHeight + OffsetSize) : 0) + (isBody ? (EntryHeight + OffsetSize) : 0), OffsetGumpID);

            int x = BorderSize + OffsetSize;
            int y = BorderSize + OffsetSize;

            AddImageTiled(x, y, EntryWidth + xextend, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth + xextend - TextOffsetX, EntryHeight, TextHue, prop.Name);
            x += EntryWidth + xextend + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            x = BorderSize + OffsetSize;
            y += EntryHeight + OffsetSize;

            AddImageTiled(x, y, EntryWidth + xextend, EntryHeight, EntryGumpID);
            AddTextEntry(x + TextOffsetX, y, EntryWidth + xextend - TextOffsetX, EntryHeight, TextHue, 0, initialText);
            x += EntryWidth + xextend + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 1, GumpButtonType.Reply, 0);

            if (canNull)
            {
                x = BorderSize + OffsetSize;
                y += EntryHeight + OffsetSize;

                AddImageTiled(x, y, EntryWidth + xextend, EntryHeight, EntryGumpID);
                AddLabelCropped(x + TextOffsetX, y, EntryWidth + xextend - TextOffsetX, EntryHeight, TextHue, "Null");
                x += EntryWidth + xextend + OffsetSize;

                if (SetGumpID != 0)
                {
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                }

                AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 2, GumpButtonType.Reply, 0);
            }

            if (canDye)
            {
                x = BorderSize + OffsetSize;
                y += EntryHeight + OffsetSize;

                AddImageTiled(x, y, EntryWidth + xextend, EntryHeight, EntryGumpID);
                AddLabelCropped(x + TextOffsetX, y, EntryWidth + xextend - TextOffsetX, EntryHeight, TextHue, "Hue Picker");
                x += EntryWidth + xextend + OffsetSize;

                if (SetGumpID != 0)
                {
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                }

                AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 3, GumpButtonType.Reply, 0);
            }

            if (isBody)
            {
                x = BorderSize + OffsetSize;
                y += EntryHeight + OffsetSize;

                AddImageTiled(x, y, EntryWidth + xextend, EntryHeight, EntryGumpID);
                AddLabelCropped(x + TextOffsetX, y, EntryWidth + xextend - TextOffsetX, EntryHeight, TextHue, "Body Picker");
                x += EntryWidth + xextend + OffsetSize;

                if (SetGumpID != 0)
                {
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                }

                AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 4, GumpButtonType.Reply, 0);
            }
        }

        private class InternalPicker : HuePicker
        {
            private PropertyInfo m_Property;
            private Mobile m_Mobile;
            private object m_Object;
            private Stack<PropertiesGump.StackEntry> m_Stack;
            private int m_Page;
            private List<PropertyObject> m_List;

            public InternalPicker(PropertyInfo prop, Mobile mobile, object o, Stack<PropertiesGump.StackEntry> stack, int page, List<PropertyObject> list) : base(((IHued)o).HuedItemID)
            {
                m_Property = prop;
                m_Mobile = mobile;
                m_Object = o;
                m_Stack = stack;
                m_Page = page;
                m_List = list;
            }

            public override void OnResponse(int hue)
            {
                try
                {
                    CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, string.Format("{0} -> {1}", m_Property.GetValue(m_Object), hue));
                    m_Property.SetValue(m_Object, hue, null);
                }
                catch
                {
                    m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                }

                m_Mobile.SendGump(new XmlPropertiesGump(m_Mobile, m_Object, m_Stack, m_List, m_Page));
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            object toSet;
            bool shouldSet, shouldSend = true;

            switch (info.ButtonID)
            {
                case 1:
                {
                    TextRelay text = info.GetTextEntry(0);

                    if (text != null)
                    {
                        try
                        {
                            toSet = PropertiesGump.GetObjectFromString(m_Property.PropertyType, text.Text);
                            shouldSet = true;
                        }
                        catch
                        {
                            toSet = null;
                            shouldSet = false;
                            m_Mobile.SendMessage("Bad format");
                        }
                    }
                    else
                    {
                        toSet = null;
                        shouldSet = false;
                    }

                    break;
                }
                case 2: // Null
                {
                    toSet = null;
                    shouldSet = true;

                    break;
                }
                case 3: // Hue Picker
                {
                    toSet = null;
                    shouldSet = false;
                    shouldSend = false;

                    m_Mobile.SendHuePicker(new InternalPicker(m_Property, m_Mobile, m_Object, m_Stack, m_Page, m_List));

                    break;
                }
                case 4: // Body Picker
                {
                    toSet = null;
                    shouldSet = false;
                    shouldSend = false;

                    m_Mobile.SendGump(new SetBodyGump(m_Property, m_Mobile, m_Object, m_Stack, m_Page, m_List));

                    break;
                }
                default:
                {
                    toSet = null;
                    shouldSet = false;

                    break;
                }
            }

            if (shouldSet)
            {
                try
                {
                    CommandLogging.LogChangeProperty(m_Mobile, (m_Object is BaseAttributes ? ((BaseAttributes)m_Object).Owner : m_Object), m_Property.Name, string.Format("{0} -> {1}", m_Property.GetValue(m_Object) ?? "(-null-)", toSet ?? "(-null-)"));//toSet==null? "(null)" : toSet.ToString() );
                    m_Property.SetValue(m_Object, toSet, null);
                }
                catch
                {
                    m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                }
            }

            if (shouldSend)
            {
                m_Mobile.SendGump(new XmlPropertiesGump(m_Mobile, m_Object, m_Stack, m_List, m_Page));
            }
        }
    }
}
