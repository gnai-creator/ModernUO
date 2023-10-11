using Server.Engines.XmlSpawner2;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Gumps
{
    public class PointsRewardGump : Gump
    {
        private const int s_LargestString = 450;
        private const int maxItemsPerPage = 9;
        private const int y_inc = 35;
        private const int x_creditoffset = s_LargestString + 110;

        private List<XmlQuestPointsRewards> Rewards;
        private int ViewPage;

        public PointsRewardGump(Mobile from, int page) : base(20, 30)
        {
            from.CloseGump(typeof(PointsRewardGump));

            // determine the gump size based on the number of rewards
            Rewards = XmlQuestPointsRewards.PointsRewardList;

            ViewPage = page;

            int height = maxItemsPerPage * y_inc + 120;
            int width = x_creditoffset + 250;

            AddBackground(0, 0, width, height, 0xDAC);

            AddHtmlLocalized(40, 20, x_creditoffset, 20, 505751, false, false);//Ricompense dei Punti Kill
            int qcredits = XmlPoints.GetCredits(from);
            AddHtmlLocalized(400, 20, 220, 20, 504732, qcredits.ToString(), 0, false, false);//Crediti Disponibili: ~1_val~
                                                                                             //AddLabel( 400, 20, 0, String.Format("Crediti Disponibili: {0}", qcredits ));
            int qpoints = XmlPoints.GetPoints(from) - XmlPoints.STARTING_POINTS;

            //AddButton( 30, height - 35, 0xFB7, 0xFB9, 0, GumpButtonType.Reply, 0 );
            //AddLabel( 70, height - 35, 0, "Close" );

            // put the page buttons in the lower right corner
            if (Rewards != null && Rewards.Count > 0)
            {
                AddHtmlLocalized(width - 165, height - 35, 160, 20, 504733, string.Format("{0}\t{1}", ViewPage + 1, (int)Math.Ceiling(Rewards.Count / (double)maxItemsPerPage)), 0, false, false);//Pagina: ~1_val~/~2_val~
                                                                                                                                                                                                  //AddLabel( width - 165, height - 35, 0, String.Format("Pagina: {0}/{1}", viewpage+1, (int)Math.Ceiling(Rewards.Count/(double)maxItemsPerPage)));

                // page up and down buttons
                AddButton(width - 55, height - 35, 0x15E0, 0x15E4, 13, GumpButtonType.Reply, 0);
                AddButton(width - 35, height - 35, 0x15E2, 0x15E6, 12, GumpButtonType.Reply, 0);
            }

            AddHtmlLocalized(70, 50, 100, 20, 504734, 0x4000, false, false);//Ricompensa
                                                                            //AddLabel( 70, 50, 40, "Ricompensa" );
            AddHtmlLocalized(x_creditoffset - 10, 50, 100, 20, 504735, 0x4000, false, false);//Crediti
                                                                                             //AddLabel( x_creditoffset - 10, 50, 40, "Crediti" );
            AddHtmlLocalized(x_creditoffset + 102, 50, 100, 20, 505752, false, false);//Punti Kill
                                                                                   //AddLabel( x_creditoffset + 120 - 18, 50, 40, "Punti Quest" );

            // display the items with their selection buttons
            if (Rewards != null)
            {
                int y = 50;
                for (int i = 0; i < Rewards.Count; ++i)
                {
                    if (i / maxItemsPerPage != ViewPage)
                    {
                        continue;
                    }

                    XmlQuestPointsRewards r = Rewards[i];
                    if (r == null)
                    {
                        continue;
                    }

                    y += y_inc;

                    //string stringhue = "<BASEFONT COLOR=BLACK>";
                    int hue = 0;

                    // display the item
                    if ((r.MinPoints > 0 && r.MinPoints > qpoints) || r.Cost > qcredits)
                    {
                        //stringhue = "<BASEFONT COLOR=RED>";
                        hue = 0x7C00;
                    }
                    else
                    {
                        // add the selection button
                        AddButton(30, y, 0xFA5, 0xFA7, 1000 + i, GumpButtonType.Reply, 0);
                    }

                    // display the name
                    AddHtmlLocalized(70, y + 3, s_LargestString, 20, 504740, $"#{r.Name}", hue, false, false);//stringhue+r.Name, false, false );
                                                                                                        //AddLabel( 70, y+3, texthue, r.Name);

                    // display the cost
                    AddHtml(x_creditoffset, y + 3, 60, 20, r.Cost.ToString(), false, false);
                    //AddLabel( x_creditoffset, y+3, texthue, r.Cost.ToString() );

                    // display the item
                    if (r.ItemID > 0)
                    {
                        AddItem(x_creditoffset + 50, y + r.yOffset, r.ItemID, r.ItemHue);
                    }

                    // display the min points requirement
                    AddHtml(x_creditoffset + 130, y + 3, 60, 20, $"{r.MinPoints}", false, false);
                    //AddLabel( x_pointsoffset, y+3, texthue, r.MinPoints.ToString() );
                }
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info == null || state == null || state.Mobile == null || Rewards == null || state.Mobile.Account == null)
            {
                return;
            }

            Mobile from = state.Mobile;

            switch (info.ButtonID)
            {
                case 12:
                    // page up
                    int nitems = 0;
                    if (Rewards != null)
                    {
                        nitems = Rewards.Count;
                    }

                    int page = ViewPage + 1;
                    if (page >= (int)Math.Ceiling(nitems / (double)maxItemsPerPage))
                    {
                        page = ViewPage;
                    }
                    state.Mobile.SendGump(new PointsRewardGump(state.Mobile, page));
                    break;
                case 13:
                    // page down
                    page = ViewPage - 1;
                    if (page < 0)
                    {
                        page = 0;
                    }
                    state.Mobile.SendGump(new PointsRewardGump(state.Mobile, page));
                    break;
                default:
                {
                    if (info.ButtonID >= 1000)
                    {
                        int selection = info.ButtonID - 1000;
                        if (selection < Rewards.Count)
                        {
                            XmlQuestPointsRewards r = Rewards[selection];

                            // check the price
                            if (XmlPoints.HasCredits(from, r.Cost, r.MinPoints))
                            {
                                // create an instance of the reward type
                                object o = null;

                                try
                                {
                                    o = Activator.CreateInstance(r.RewardType, r.RewardArgs);
                                }
                                catch { }

                                bool received = true;

                                    if (o is Item)
                                    {
                                        // and give them the item
                                        from.AddToBackpack((Item)o);
                                    }
                                    else if (o is Mobile)
                                    {
                                        // if it is controllable then set the buyer as master.  Note this does not check for control slot limits.
                                        if (o is BaseCreature)
                                        {
                                            BaseCreature b = o as BaseCreature;
                                            b.Controlled = true;
                                            b.ControlMaster = from;
                                        }

                                        ((Mobile)o).MoveToWorld(from.Location, from.Map);
                                    }
                                    else if (o is XmlAttachment xa)
                                    {
                                        XmlAttachment a;
                                        if (!string.IsNullOrWhiteSpace(xa.Name))
                                        {
                                            a = XmlAttach.FindAttachment(from, null, xa.Name);
                                            if (a != null && !a.Deleted)
                                            {
                                                from.SendLocalizedMessage(502173);//Sei già stato sotto un effetto simile.
                                                received = false;
                                            }
                                            else
                                                XmlAttach.AttachTo(from, xa);
                                        }
                                        else if ((a = XmlAttach.FindAttachment(from, xa.GetType())) != null && !a.Deleted)
                                        {
                                            from.SendLocalizedMessage(502173);//Sei già stato sotto un effetto simile.
                                            received = false;
                                        }
                                        else
                                            XmlAttach.AttachTo(from, xa);
                                    }

                                    else
                                    {
                                        from.SendLocalizedMessage(504737, r.RewardType.Name, 33);//, "impossibile creare {0}.", r.RewardType.Name);
                                        received = false;
                                    }

                                // complete the transaction
                                if (received)
                                {
                                    // charge them
                                    if (XmlPoints.TakeCredits(from, r.Cost))
                                    {
                                        from.SendLocalizedMessage(504738, string.Format("#{0}\t{1}", r.Name, r.Cost));//"Hai acquisito {0} per {1} crediti.", name, r.Cost);
                                        try
                                        {
                                            if (!Directory.Exists("Logs/Vari"))
                                            {
                                                Directory.CreateDirectory("Logs/Vari");
                                            }

                                            using (StreamWriter op = new StreamWriter("Logs/Vari/PremiPuntiKill.log", true))
                                            {
                                                op.WriteLine(string.Format("{0} *** Nome PG: {1} - Account: {2} - Premio Preso: {3}", Core.MistedDateTime, from, from.Account.Username, o));
                                            }
                                        }
                                        catch
                                        {
                                        }
                                    }
                                    else
                                    {
                                        if (o is XmlAttachment)
                                        {
                                            ((XmlAttachment)o).Delete();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                from.SendLocalizedMessage(504739, $"#{r.Name}");//"Crediti Insufficienti per {0}.", name);
                            }
                            from.SendGump(new PointsRewardGump(from, ViewPage));
                        }
                    }
                    break;
                }
            }
        }
    }
}