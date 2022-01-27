using UnityEngine;

[CreateAssetMenu(fileName = " ProjectileWeapon", menuName = "Scriptable Objects/Weapon", order = 1)]
public class ProjectileWeapon : ScriptableObject
{
    public string weaponName;
    public int weaponSlot;
    public bool automatic;
    public AmmoType ammoType;
    public int damage;
    public float spread;
    public float reloadTime;
    public int magazineSize;
    public int pelletPerShot;
    public float primaryCdr;
}