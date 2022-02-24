using UnityEngine;

[CreateAssetMenu(fileName = " ProjectileWeapon", menuName = "Scriptable Objects/Weapon", order = 1)]
public class ProjectileWeapon : ScriptableObject
{
    [Header("General")]
    public string weaponName;
    public int weaponSlot;
    public bool automatic = false;
    public int damage = 1;
    public float range = 10;
    public float spreadArc = 2;
    public int pelletPerShot = 1;
    public float pelletCaliber = 0.1f;
    public AmmoType ammoType;

    [Header("Reload")]
    public bool shotgunStyleReload;
    public float reloadTime;
    public int magazineSize;

    [Header("Firerate")]
    public float primaryCdr;
    public float secondaryCdr;

    [Header("Audio")]
    public AudioClip[] selectSfxs;
    public AudioClip[] drySfxs;
    public AudioClip[] primarySfxs;
    public AudioClip[] secondarySfxs;
    public AudioClip[] reloadSfxs;
}