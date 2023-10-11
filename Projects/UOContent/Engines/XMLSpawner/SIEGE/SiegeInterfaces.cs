using System.Collections.Generic;

namespace Server.Items
{
    public interface ISiegeWeapon : IEntity
    {
        int Facing { get; set; }
        bool FixedFacing { get; set; }
        BaseSiegeProjectile Projectile { get; set; }
        void LoadWeapon(Mobile from, BaseSiegeProjectile projectile);
        void PlaceWeapon(Mobile from, Point3D location, Map map);
        void StoreWeapon(Mobile from);
        bool IsPackable { get; set; }
        bool IsDraggable { get; set; }
        double WeaponDamageFactor { get; }
        int MinStorageRange { get; }
        List<AddonComponent> Components { get; }
    }

    public interface ISiegeProjectile
    {
        int Range { get; set; }
        int FiringSpeed { get; set; }
        int AccuracyBonus { get; set; }
        int Area { get; set; }
        int FireDamage { get; set; }
        int PhysicalDamage { get; set; }
        int AnimationID { get; }
        int AnimationHue { get; }
        BaseSiegeWeapon LoadedWeapon { get; set; }
        void OnHit(Mobile from, ISiegeWeapon weapon, IEntity target, Point3D targetloc);
    }
}
