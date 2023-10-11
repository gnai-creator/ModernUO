using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.XmlSpawner2
{
    public class ChallengeGameRegion : GuardedRegion
    {
        private BaseChallengeGame m_ChallengeGame;

        public BaseChallengeGame ChallengeGame { get { return m_ChallengeGame; } set { m_ChallengeGame = value; } }

        public ChallengeGameRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
        {
        }

        public override bool AllowHarmful(Mobile from, Mobile target)
        {
            if (from == null || target == null) return false;

            // during a challenge games or 1-on-1 duels, restrict harmful acts to opponents
            return XmlPoints.AreChallengers(from, target);
        }


        public override bool AllowBeneficial(Mobile from, Mobile target)
        {
            if (from != null && target != null && XmlAttach.FindAttachment(from, typeof(XmlPoints)) is XmlPoints afrom && XmlAttach.FindAttachment(target, typeof(XmlPoints)) is XmlPoints atarg)
            {

                // during a challenge game, beneficial acts on participants is restricted to between team members
                if (XmlPoints.AreInAnyGame(atarg))
                    return XmlPoints.AreTeamMembers(afrom, atarg);
            }
            // restrict everyone else
            return false;
        }


        public override bool OnDoubleClick(Mobile m, object o)
        {

            if (o is Corpse c)
            {
                // dont allow other players to loot corpses while a challenge game is present
                if ((ChallengeGame != null) && !ChallengeGame.Deleted && m != null && !(c.Owner is BaseCreature) &&
                (c.Owner != m) && (m.AccessLevel == AccessLevel.Player))
                {
                    m.SendLocalizedMessage(505706);// "You are not allowed to open that here."
                    return false;
                }
            }

            return base.OnDoubleClick(m, o);
        }



        public override void OnEnter(Mobile m)
        {
            if (m != null)
                m.SendLocalizedMessage(505707, Name);// "You have entered the Challenge Game region '{0}'"

            base.OnEnter(m);

        }

        public override void OnExit(Mobile m)
        {
            if (m != null)
                m.SendLocalizedMessage(505708, Name);// "You have left the Challenge Game region '{0}'"

            base.OnExit(m);
        }
    }
}
