# Defence meter — FB3 Planned

Reserved for an **actual depletable** player/enemy shield, barrier or guard resource with current/max state and changed/depleted events. The resource itself must live with its owning actor or shared stats system, **not in FEEDBACK**.

The existing `HealthSettings.Armour` and per-type resistances only mitigate damage. They do not decrease when hit, so they must **not** be displayed as a consumable bar. An optional armour icon/percentage can be designed later without implying a shield pool.

No defence mechanism, widget, scripts or resources are implemented now. Read [FB plan](../../IMPLEMENTATION_PLAN.md) before adding UI.
