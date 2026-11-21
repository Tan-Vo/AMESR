using UnityEngine;

public sealed class Weapon_Harpoon : WeaponBase
{
    [SerializeField] private HarpoonSystem harpoon;
    [SerializeField] private SO_WeaponData[] tips;
    private int tip;
    private bool selected;
    public override SO_WeaponData Data => tips != null && tips.Length > tip && tips[tip] != null ? tips[tip] : base.Data;
    private void Update()
    {
        if (!selected || harpoon == null || harpoon.IsBusy || tips == null) return;
        for (int i = 0; i < Mathf.Min(5, tips.Length); i++)
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) { tip = i; harpoon.Equip(Data); }
    }
    /// <inheritdoc />
    public override void SetSelected(bool selected)
    {
        this.selected = selected;
        if (harpoon == null) return;
        if (selected) harpoon.Equip(Data);
        harpoon.SetSelected(selected);
    }
    protected override bool Shoot(Vector2 direction)
    {
        if (harpoon == null || harpoon.IsBusy) return false;
        harpoon.RequestFire(direction);
        return true;
    }
}
