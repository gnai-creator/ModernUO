using Server.Engines.XmlSpawner2;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;

/*
** QuestRewardGump
** ArteGordon
** updated 9/18/05
**
** Gives out rewards based on the XmlQuestReward reward list entries and the players Credits that are accumulated through quests with the XmlQuestPoints attachment.
** The Gump supports Item, Mobile, and Attachment type rewards.
*/

namespace Server.Gumps
{
    public class QuestRewardGump : Gump
    {
        private const int s_LargestString = 450;
        private const int y_inc = 35;
        private const int x_creditoffset = s_LargestString + 110;
        private const int maxItemsPerPage = 9;

        private List<XmlQuestPointsRewards> Rewards;
        private int viewpage;

        public QuestRewardGump(Mobile from, int page) : base(20, 30)
        {
            from.CloseGump(typeof(QuestRewardGump));

            // determine the gump size based on the number of rewards
            Rewards = XmlQuestPointsRewards.RewardsList;

            viewpage = page;

            int height = maxItemsPerPage * y_inc + 120;
            int width = x_creditoffset + 250;

            AddBackground(0, 0, width, height, 0xDAC);

            AddHtmlLocalized(40, 20, x_creditoffset, 20, 504731, false, false);//"Ricompense dei Punti Quest (<A HREF=\"http://www.uoitalia.net/Guide-UOI-Reborn/i-premi-dei-punti-quest.html\">Info QUI</a>)"
            int qcredits = XmlQuestPoints.GetCredits(from);
            AddHtmlLocalized(400, 20, 220, 20, 504732, qcredits.ToString(), 0, false, false);//Crediti Disponibili: ~1_val~
                                                                                             //AddLabel( 400, 20, 0, String.Format("Crediti Disponibili: {0}", qcredits ));
            int qpoints = XmlQuestPoints.GetPoints(from);

            //AddButton( 30, height - 35, 0xFB7, 0xFB9, 0, GumpButtonType.Reply, 0 );
            //AddLabel( 70, height - 35, 0, "Close" );

            // put the page buttons in the lower right corner
            if (Rewards != null && Rewards.Count > 0)
            {
                AddHtmlLocalized(width - 165, height - 35, 160, 20, 504733, string.Format("{0}\t{1}", viewpage + 1, (int)Math.Ceiling(Rewards.Count / (double)maxItemsPerPage)), 0, false, false);//Pagina: ~1_val~/~2_val~
                                                                                                                                                                                                  //AddLabel( width - 165, height - 35, 0, String.Format("Pagina: {0}/{1}", viewpage+1, (int)Math.Ceiling(Rewards.Count/(double)maxItemsPerPage)));

                // page up and down buttons
                AddButton(width - 55, height - 35, 0x15E0, 0x15E4, 13, GumpButtonType.Reply, 0);
                AddButton(width - 35, height - 35, 0x15E2, 0x15E6, 12, GumpButtonType.Reply, 0);
            }

            AddHtmlLocalized(70, 50, 100, 20, 504734, 0x4000, false, false);//Ricompensa
                                                                            //AddLabel( 70, 50, 40, "Ricompensa" );
            AddHtmlLocalized(x_creditoffset - 10, 50, 100, 20, 504735, 0x4000, false, false);//Crediti
                                                                                             //AddLabel( x_creditoffset - 10, 50, 40, "Crediti" );
            AddHtmlLocalized(x_creditoffset + 102, 50, 100, 20, 504736, 0x4000, false, false);//Punti Quest
                                                                                              //AddLabel( x_creditoffset + 120 - 18, 50, 40, "Punti Quest" );

            // display the items with their selection buttons
            if (Rewards != null)
            {
                int y = 50;
                for (int i = 0; i < Rewards.Count; ++i)
                {
                    if (i / maxItemsPerPage != viewpage)
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
                    if (r.MinPoints > qpoints || r.Cost > qcredits)
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
                    if(string.IsNullOrEmpty(r.NameArgs))
                    {
                        AddHtmlLocalized(70, y + 3, s_LargestString, 20, r.Name, hue, false, false);
                    }
                    else
                    {
                        AddHtmlLocalized(70, y + 3, s_LargestString, 20, r.Name, r.NameArgs, hue, false, false);
                    }

                    // display the cost
                    AddHtml(x_creditoffset, y + 3, 60, 20, r.Cost.ToString(), false, false);
                    //AddLabel( x_creditoffset, y+3, texthue, r.Cost.ToString() );

                    // display the item
                    if (r.ItemID > 0)
                    {
                        AddItem(x_creditoffset + 50, y + r.yOffset, r.ItemID, r.ItemHue);
                    }

                    // display the min points requirement
                    AddHtml(x_creditoffset + 130, y + 3, 60, 20, r.MinPoints.ToString(), false, false);
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

                    int page = viewpage + 1;
                    if (page >= (int)Math.Ceiling(nitems / (double)maxItemsPerPage))
                    {
                        page = viewpage;
                    }
                    state.Mobile.SendGump(new QuestRewardGump(state.Mobile, page));
                    break;
                case 13:
                    // page down
                    page = viewpage - 1;
                    if (page < 0)
                    {
                        page = 0;
                    }
                    state.Mobile.SendGump(new QuestRewardGump(state.Mobile, page));
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
                            if (XmlQuestPoints.HasCredits(from, r.Cost, r.MinPoints))
                            {
                                // create an instance of the reward type
                                object o = null;

                                try
                                {
                                    o = Activator.CreateInstance(r.RewardType, r.RewardArgs);
                                }
                                catch { }

                                bool received = true;

                                if (o is Item it)
                                {
                                    // and give them the item
                                    from.AddToBackpack(it);
                                }
                                else if (o is Mobile mob)
                                {
                                    // if it is controllable then set the buyer as master.  Note this does not check for control slot limits.
                                    if (mob is BaseCreature b)
                                    {
                                        b.Controlled = true;
                                        b.ControlMaster = from;
                                    }

                                    mob.MoveToWorld(from.Location, from.Map);
                                }
                                else if (o is XmlAttachment a)
                                {
                                    XmlAttach.AttachTo(from, a);
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
                                    if (XmlQuestPoints.TakeCredits(from, r.Cost))
                                    {
                                        from.SendLocalizedMessage(504738, string.Format("#{0}\t{1}", r.Name, r.Cost));//"Hai acquisito {0} per {1} crediti.", name, r.Cost);
                                        try
                                        {
                                            if (!Directory.Exists("Logs/Vari"))
                                            {
                                                Directory.CreateDirectory("Logs/Vari");
                                            }

                                            using (StreamWriter op = new StreamWriter("Logs/Vari/PremiQuest.log", true))
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
                                        if (o is Item item)
                                        {
                                            item.Delete();
                                        }
                                        else if (o is Mobile mobile)
                                        {
                                            mobile.Delete();
                                        }
                                        else if (o is XmlAttachment xa)
                                        {
                                            xa.Delete();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if(string.IsNullOrEmpty(r.NameArgs))
                                {
                                    from.SendLocalizedMessage(504739, $"#{r.Name}");//"Crediti Insufficienti per {0}.", name);
                                }
                                else
                                {
                                    from.SendLocalizedMessage(505965, $"#{r.Name}\t{r.NameArgs}");//Crediti insufficienti per ~1_val~ ~2_val~.
                                }
                            }
                            from.SendGump(new QuestRewardGump(from, viewpage));
                        }
                    }
                    break;
                }
            }
        }
    }
}