using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerStateMachine))]
public sealed class WeaponController : MonoBehaviour
{
    [SerializeField] private WeaponBase[] weapons;
    [SerializeField] private SpriteRenderer weaponSprite;
    [SerializeField] private Text weaponLabel;
    private PlayerStateMachine states;
    private int index;
    private bool fire;
    private bool reload;
    private Vector2 direction = Vector2.right;
    private float actionRemaining;
    private SO_WeaponData displayedWeapon;
    private int displayedAmmo = -1;
    private bool displayedReload;
    public WeaponBase Current => weapons != null && weapons.Length > 0 ? weapons[index] : null;
    private void Awake() { states = GetComponent<PlayerStateMachine>(); }
    private void Start() { Select(0); }
    private void Select(int next)
    {
        if (weapons == null || weapons.Length == 0) return;
        if (Current != null) Current.SetSelected(false);
        index = next % weapons.Length;
        if (Current != null) Current.SetSelected(true);
        fire = reload = false;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !states.MovementLocked) Select(index + 1);
        if (Current == null || Current.Data == null) return;
        reload |= Input.GetKeyDown(KeyCode.R);
        if (Current.Data.WeaponType != WeaponType.Harpoon)
            fire |= Current.Data.IsAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");
        var camera = Camera.main;
        if (camera != null) direction = ((Vector2)camera.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized;
    }
    private void FixedUpdate()
    {
        actionRemaining -= Time.fixedDeltaTime;
        if (actionRemaining <= 0f && (states.CurrentState == PlayerState.Combat_Melee || states.CurrentState == PlayerState.Combat_Ranged))
            states.ChangeState(states.InWater ? PlayerState.Swimming : PlayerState.Idle);
        if (Current == null || states.MovementLocked || states.CurrentState == PlayerState.GrappleSwinging) { fire = reload = false; return; }
        if (reload) Current.Reload();
        if (fire && Current.Fire(direction))
        {
            states.ChangeState(Current.Data.IsMelee ? PlayerState.Combat_Melee : PlayerState.Combat_Ranged);
            actionRemaining = Current.Data.IsMelee ? 0.3f : 0.18f;
        }
        fire = reload = false;
    }
    private void LateUpdate()
    {
        if (Current == null || Current.Data == null) return;
        if (weaponSprite != null)
        {
            weaponSprite.sprite = Current.Data.Icon;
            weaponSprite.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            weaponSprite.flipY = direction.x < 0f;
        }
        if (weaponLabel != null && (displayedWeapon != Current.Data || displayedAmmo != Current.CurrentAmmo || displayedReload != Current.IsReloading))
        {
            displayedWeapon = Current.Data;
            displayedAmmo = Current.CurrentAmmo;
            displayedReload = Current.IsReloading;
            weaponLabel.text = Current.Data.WeaponName + (Current.Data.IsMelee || Current.Data.WeaponType == WeaponType.Harpoon ? " ∞" : "  " + Current.CurrentAmmo + "/" + Current.Data.MaxAmmo) +
                (Current.IsReloading ? " RELOADING" : "") + "\nQ: Switch weapon   R: Reload   1–5: Harpoon tip";
        }
    }
}
