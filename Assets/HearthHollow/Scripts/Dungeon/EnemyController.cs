// Hearth & Hollow — a monster in the dungeon. HP and damage come from GameSession (difficulty-scaled);
// how it moves and attacks comes from its EnemyProfile (Dungeon Settings asset).
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class EnemyController : MonoBehaviour
    {
        enum St { Spawning, Moving, Windup, Striking, Charging, Recover, Stagger }

        DungeonController _dc;
        RoomInstance _room;
        public EnemyStats Stats { get; private set; }
        public EnemyProfile Profile { get; private set; }
        public int Hp { get; private set; }
        public bool Dead { get; private set; }
        public bool Boss { get { return Stats != null && Stats.boss; } }
        public string DisplayName { get; private set; }

        Rigidbody2D _rb;
        CharacterAnimator _anim;
        SpriteRenderer _hpBg, _hpFill;
        St _st = St.Spawning;
        float _stT;            // time left in the current state
        float _cd;             // attack cooldown
        float _ultChargeAt;    // when this enemy can next charge the player's ultimate
        float _speedMult = 1f;
        Vector2 _lockDir;      // charge / dart direction
        int _hitsLeft;
        bool _chargeHit;
        float _wobble;
        float _healCd = 6f;
        bool _breathNext;
        float _flashT;
        Facing _facing = Facing.Down;
        // statuses from player moves
        float _stunT, _rootT, _slowT, _slowMult = 1f, _markT, _markBonus;
        public bool Stunned { get { return _stunT > 0f; } }
        public bool Spawning { get { return _st == St.Spawning; } }
        // light plan step 6
        public bool Shadow { get; private set; }   // a shadow creature: near-invisible and tough in the dark, weak and flinching once lit
        public float Lit01 { get; private set; }   // 0 = in the dark … 1 = well lit (DungeonLight)
        bool _wasLit; float _wispT;
        float _nerveT, _nerveNeed = -1f, _orbit = 1f; bool _inSrc;   // light-shy: circle round a light, then push through
        bool Brave { get { return _nerveNeed > 0f && _nerveT >= _nerveNeed; } }
        /// Bosses shrug off most of a stun, elites some of it.
        float StatusResist { get { return Stats.boss ? 0.3f : Stats.elite ? 0.6f : 1f; } }

        TuningData T { get { return _dc.Session.Content.tuning; } }
        PlayerController Player { get { return _dc.Player; } }
        public Vector2 Body { get { return (Vector2)transform.position + new Vector2(0, _anim != null ? _anim.FigureHeight * 0.45f : 0.4f); } }

        public void Setup(DungeonController dc, RoomInstance room, EnemyStats stats, EnemyProfile profile, HearthHollow.Data.ArtLibrary.Anim art, float speedMult)
        {
            _dc = dc; _room = room; Stats = stats; Profile = profile; _speedMult = speedMult;
            Hp = stats.maxHp;
            var md = dc.Session.Content.Monster(stats.monsterId);
            DisplayName = md != null ? md.name : stats.monsterId;
            float size = profile.scale * (stats.boss ? 1.7f : stats.elite ? 1.3f : 1f);

            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f; _rb.freezeRotation = true;
            _rb.mass = stats.boss ? 20f : stats.elite ? 4f : 2f;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.3f * size; col.offset = new Vector2(0, 0.22f * size);
            // enemies never body-push the player (stops them shoving you through door bars); hits are range checks
            if (dc.Player != null) foreach (var pc in dc.Player.GetComponents<Collider2D>()) Physics2D.IgnoreCollision(col, pc);

            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            body.AddComponent<SpriteRenderer>();
            _anim = body.AddComponent<CharacterAnimator>();
            _anim.Setup(art, dc.Settings.characterPixelsPerUnit, size);
            // a little hit-box on the body too, so swings at the sprite (not just the feet) connect
            var hurt = body.AddComponent<CircleCollider2D>();
            hurt.isTrigger = true;
            hurt.radius = Mathf.Max(0.3f, _anim.FigureHeight * 0.4f) / Mathf.Max(0.01f, body.transform.lossyScale.x);
            hurt.offset = (Vector2)body.transform.InverseTransformPoint(Body);

            if (stats.elite && !stats.boss) _anim.Renderer.color = new Color(1f, 0.88f, 0.6f);
            gameObject.AddComponent<EnemyGlow>().Setup(this, _anim, profile, stats, size, dc.Settings);   // silhouette, rim, eyes, carried lights
            MakeHpBar();
            SetupElements(profile);
            _stT = 0.7f;                 // spawn telegraph
            _cd = Random.Range(0.5f, 1.2f);
            _wobble = Random.value * 10f;
            SetAlpha(0f);
        }

        void MakeHpBar()
        {
            if (Stats.boss) return;   // the HUD shows a boss bar
            var bg = new GameObject("HpBg"); bg.transform.SetParent(transform, false);
            _hpBg = bg.AddComponent<SpriteRenderer>(); _hpBg.sprite = DungeonFx.BarPixel; _hpBg.color = new Color(0, 0, 0, 0.6f);
            var fill = new GameObject("HpFill"); fill.transform.SetParent(transform, false);
            _hpFill = fill.AddComponent<SpriteRenderer>(); _hpFill.sprite = DungeonFx.BarPixel;
            _hpFill.color = Stats.elite ? new Color(1f, 0.8f, 0.25f) : new Color(0.9f, 0.3f, 0.25f);
            float y = _anim.FigureHeight + 0.15f;
            bg.transform.localPosition = new Vector3(-0.4f, y, 0);
            bg.transform.localScale = new Vector3(0.8f * DungeonFx.PPU, 0.1f * DungeonFx.PPU, 1);
            fill.transform.localPosition = new Vector3(-0.4f, y, 0); fill.transform.localScale = new Vector3(0.8f * DungeonFx.PPU, 0.1f * DungeonFx.PPU, 1);
            _hpBg.enabled = _hpFill.enabled = false;
        }

        void SetAlpha(float a)
        {
            if (_anim == null || _anim.Renderer == null) return;
            var c = _anim.Renderer.color; c.a = a; _anim.Renderer.color = c;
        }

        // ───────────────────────── AI ─────────────────────────
        void Update()
        {
            if (Dead || _dc == null || !_dc.GameplayActive) return;
            float dt = Time.deltaTime;
            _cd -= dt; _stT -= dt; _flashT -= dt; _wobble += dt;

            if (_st == St.Spawning)
            {
                SetAlpha(Mathf.Clamp01(1f - _stT / 0.7f));
                if (_stT <= 0f) { _st = St.Moving; SetAlpha(1f); }
                return;
            }
            if (Player == null) return;
            UpdateLight(dt);
            if (_slowT > 0f) { _slowT -= dt; if (_slowT <= 0f) _slowMult = 1f; }
            if (_rootT > 0f) _rootT -= dt;
            if (_markT > 0f) _markT -= dt;
            if (_stunT > 0f)
            {
                _stunT -= dt;
                if (_st == St.Windup || _st == St.Charging || _st == St.Striking) { _st = St.Recover; _stT = 0.1f; }
                _anim.SetMoving(false);
                UpdateTint(); UpdateHpBar();
                return;
            }

            Vector2 to = Player.Feet - (Vector2)transform.position;
            float dist = to.magnitude;
            Vector2 dir = dist > 0.001f ? to / dist : Vector2.down;

            switch (_st)
            {
                case St.Moving: Think(dist, dir); break;
                case St.Windup:
                    if (_stT <= 0f) Release(dist, dir);
                    break;
                case St.Striking:
                    if (_stT <= 0f) { _st = St.Recover; _stT = 0.35f; }
                    break;
                case St.Charging:
                    if (!_chargeHit && Player.CanBeHit && Vector2.Distance(Player.Body, Body) < 0.75f * Mathf.Max(1f, Profile.scale))
                    { _chargeHit = true; Player.TakeHit(Stats.damage, transform.position, OwnElement); }
                    if (_stT <= 0f) { _st = St.Recover; _stT = 0.5f; }
                    break;
                case St.Recover:
                case St.Stagger:
                    if (_stT <= 0f) _st = St.Moving;
                    break;
            }

            if (Profile.healsAllies && _st == St.Moving)
            {
                _healCd -= dt;
                if (_healCd <= 0f) { _healCd = 6f; _room.HealWeakestAlly(this, T.shamanHealPct); }
            }

            // facing + anim
            Vector2 look = (_st == St.Charging || _st == St.Striking) ? _lockDir : dir;
            _facing = CharacterAnimator.FacingFrom(look, _facing);
            _anim.Dir = _facing;
            _anim.SetMoving(_st == St.Moving || _st == St.Charging);
            UpdateTint();
            UpdateHpBar();
        }

        void Think(float dist, Vector2 dir)
        {
            if (Profile.lightReaction == LightReaction.Shy)
            {
                Vector2 src; float s;
                if (!Brave && DungeonLight.StrongestSource(Body, 0.8f, out src, out s)) return;   // too bright to strike from here (until it loses patience)
            }
            switch (Profile.archetype)
            {
                case EnemyArchetype.Caster:
                    if (_cd <= 0f && dist < 9f) { BeginWindup(dir, Profile.windup); return; }
                    break;
                case EnemyArchetype.Charger:
                    if (_cd <= 0f && dist < 6.5f) { BeginWindup(dir, Profile.windup); return; }
                    if (_cd <= 0f && dist <= Profile.attackRange) { BeginWindup(dir, Profile.windup); return; }
                    break;
                case EnemyArchetype.Brute:
                    if (_cd <= 0f && dist < 7f) { BeginWindup(dir, Profile.windup); return; }
                    break;
                default:
                    if (_cd <= 0f && dist <= Profile.attackRange) { BeginWindup(dir, Profile.windup); return; }
                    break;
            }
        }

        void BeginWindup(Vector2 dir, float seconds)
        {
            _st = St.Windup; _stT = seconds; _lockDir = dir;
            _hitsLeft = Mathf.Max(1, Profile.hits);
            _anim.PlayAttack(seconds + 0.2f);
        }

        void Release(float dist, Vector2 dir)
        {
            _cd = T.monsterAttackCooldown * Profile.cooldownMult * Random.Range(0.85f, 1.2f);
            if (Profile.lightReaction == LightReaction.Shy && Lit01 < 0.15f) _cd /= Mathf.Max(1f, _dc.Settings.shyBoldness);   // bolder in the dark
            switch (Profile.archetype)
            {
                case EnemyArchetype.Charger:
                    _lockDir = dir; _chargeHit = false; _st = St.Charging; _stT = 0.55f;
                    break;
                case EnemyArchetype.Flyer:
                    _lockDir = dir; _chargeHit = false; _st = St.Charging; _stT = 0.22f;
                    break;
                case EnemyArchetype.Caster:
                    Shoot(dir, 0f);
                    _st = St.Striking; _stT = 0.2f;
                    break;
                case EnemyArchetype.Brute:
                    _breathNext = !_breathNext;
                    if (_breathNext) { for (int i = -2; i <= 2; i++) Shoot(dir, i * 14f); _st = St.Striking; _stT = 0.4f; }
                    else { _lockDir = dir; _chargeHit = false; _st = St.Charging; _stT = 0.7f; }
                    break;
                default:   // Chaser: a lunge + bite(s)
                    _lockDir = dir; _st = St.Striking; _stT = 0.18f;
                    Bite();
                    if (Profile.hits > 1) Invoke("Bite", 0.2f);
                    break;
            }
        }

        void Bite()
        {
            if (Dead || Player == null || _dc == null || !_dc.GameplayActive || _stunT > 0f) return;
            float reach = Profile.attackRange + 0.35f;
            if (Vector2.Distance(Player.Body, Body + _lockDir * 0.3f) <= reach) Player.TakeHit(Stats.damage, transform.position, OwnElement);
            DungeonFx.SlashAt(Body + _lockDir * 0.3f, _lockDir, new Color(1f, 0.5f, 0.45f, 0.75f), 0.8f);
        }

        void Shoot(Vector2 dir, float angle)
        {
            Vector2 d = Quaternion.Euler(0, 0, angle) * dir;
            Color c = Profile.archetype == EnemyArchetype.Brute ? new Color(1f, 0.55f, 0.2f) : new Color(0.75f, 0.45f, 1f);
            // hip height (same plane as player arrows), so a tall monster's level shots don't sail over the player
            var shot = Projectile.Spawn(_dc, false, (Vector2)transform.position + new Vector2(0, 0.5f) + d * 0.5f, d, Profile.projectileSpeed, 0f, Stats.damage, DungeonFx.Orb, c, false, Stats.boss ? 1.6f : 1.1f);
            if (shot != null) shot.SetHit(OwnElement);   // the monster's element: a guard meal halves it
        }

        void FixedUpdate()
        {
            if (_rb == null) return;
            if (Dead || _dc == null || !_dc.GameplayActive || Player == null || _st == St.Spawning) { _rb.linearVelocity = Vector2.zero; return; }
            if (_stunT > 0f || (_rootT > 0f && _st != St.Stagger)) { _rb.linearVelocity = _st == St.Stagger ? _rb.linearVelocity * 0.85f : Vector2.zero; return; }
            float speed = Profile.moveSpeed * _speedMult * _slowMult;
            Vector2 to = Player.Feet - (Vector2)transform.position;
            float dist = to.magnitude;
            Vector2 dir = dist > 0.001f ? to / dist : Vector2.zero;
            Vector2 v = Vector2.zero;
            switch (_st)
            {
                case St.Moving:
                    switch (Profile.archetype)
                    {
                        case EnemyArchetype.Caster:
                            if (dist < 3.5f) v = -dir * speed;
                            else if (dist > 6f) v = dir * speed;
                            else v = new Vector2(-dir.y, dir.x) * speed * 0.6f * Mathf.Sign(Mathf.Sin(_wobble * 0.7f));
                            break;
                        case EnemyArchetype.Flyer:
                            v = (dir + new Vector2(-dir.y, dir.x) * Mathf.Sin(_wobble * 4f) * 0.9f).normalized * speed;
                            if (dist < Profile.attackRange * 0.7f) v *= 0.3f;
                            break;
                        case EnemyArchetype.Charger:
                            v = dir * speed * 0.6f;
                            break;
                        default:
                            v = dist > Profile.attackRange * 0.8f ? dir * speed : Vector2.zero;
                            break;
                    }
                    var steer = LightSteer(v, speed, dist);
                    if (steer.HasValue) v = steer.Value;
                    v = Avoid(v);
                    break;
                case St.Windup: v = Vector2.zero; break;
                case St.Striking: v = Profile.archetype == EnemyArchetype.Chaser ? _lockDir * 2.5f : Vector2.zero; break;
                case St.Charging:
                    float cs = Profile.archetype == EnemyArchetype.Flyer ? 9f : Profile.archetype == EnemyArchetype.Brute ? 8f : 10f;
                    v = _lockDir * cs * Mathf.Max(0.8f, _speedMult);
                    if (HitsWall(_lockDir, 0.45f)) { _st = St.Stagger; _stT = 0.5f; v = Vector2.zero; _dc.Shake(0.08f, 0.08f); }
                    break;
                case St.Stagger: v = _rb.linearVelocity * 0.85f; break;
            }
            _rb.linearVelocity = v;
        }

        void LateUpdate() { if (_anim != null && _anim.Renderer != null) _anim.Renderer.sortingOrder = DungeonController.SortOrder(transform.position.y); }

        static readonly System.Collections.Generic.List<RaycastHit2D> _hits = new System.Collections.Generic.List<RaycastHit2D>(8);

        bool HitsWall(Vector2 dir, float dist)
        {
            if (dir.sqrMagnitude < 0.0001f) return false;
            var filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };   // same hits as the old CircleCastNonAlloc
            int n = Physics2D.CircleCast((Vector2)transform.position + new Vector2(0, 0.22f), 0.25f, dir, filter, _hits, dist);
            for (int i = 0; i < n; i++)
            {
                var c = _hits[i].collider;
                if (c != null && !c.isTrigger && c.attachedRigidbody == null) return true;
            }
            return false;
        }

        /// Simple steering: if the way ahead is blocked, try slipping round at ±50° / ±90°.
        Vector2 Avoid(Vector2 v)
        {
            if (v.sqrMagnitude < 0.0001f) return v;
            Vector2 d = v.normalized; float m = v.magnitude;
            if (!HitsWall(d, 0.6f)) return v;
            float[] tries = { 50f, -50f, 90f, -90f };
            foreach (float a in tries)
            {
                Vector2 t = Quaternion.Euler(0, 0, a) * d;
                if (!HitsWall(t, 0.6f)) return t * m;
            }
            return Vector2.zero;
        }

        // ───────────────────────── damage ─────────────────────────
        public void TakeHit(float dmg, bool crit, Vector2 knockDir, float knock = 6f, Element element = Element.None)
        {
            if (Dead || _st == St.Spawning) return;
            float em = Elements.Multiplier(element, _weak, _resist);   // elements: weakness ×1.5, resistance ×0.5
            dmg *= em;
            if (_dc != null && _dc.Player != null && Time.time >= _ultChargeAt) { _ultChargeAt = Time.time + 0.15f; _dc.Player.ChargeUltimate(); }   // hits charge the ultimate
            if (_markT > 0f) dmg *= 1f + _markBonus;   // Hunter's Mark
            // light: shadow creatures shrug off hits in the dark (the old "lit = weak spot" bonus was cut in Design v2)
            var set = _dc.Settings;
            if (Shadow) dmg *= Mathf.Lerp(set.shadowDarkDamage, 1f + set.shadowLitBonus, Lit01);
            int d = Mathf.Max(1, Mathf.RoundToInt(dmg));
            Hp -= d;
            _flashT = 0.12f;
            DungeonHud.Number(Body + Vector2.up * (_anim.FigureHeight * 0.6f), (crit ? d + "!" : d.ToString()), crit ? "dnum--crit" : "dnum--dmg");
            DungeonFx.Spark(Body, crit ? new Color(1f, 0.85f, 0.3f) : Color.white);
            bool learned = ElementContact(element);   // a first discovery shows its own "found" message instead
            if (!learned && em > 1f) DungeonHud.Number(Body + Vector2.up * (_anim.FigureHeight * 0.9f), Elements.Emoji(element) + " Weak!", "dnum--crit");
            else if (!learned && em < 1f) DungeonHud.Number(Body + Vector2.up * (_anim.FigureHeight * 0.9f), Elements.Emoji(element) + " Resists", "dnum--info");
            // impact: sparks + a flash of light, a white flash on the body, a few frames' freeze (more for crits)
            Color hitCol = crit ? new Color(1f, 0.9f, 0.45f) : new Color(1f, 0.62f, 0.25f);
            DungeonFx.Sparks(Body, knockDir, hitCol, crit ? 18 : 10);
            DungeonFx.Throw(Body, knockDir, hitCol, crit ? 12 : 6, 2f, 6f);
            DungeonFx.FloorEmbers(transform.position, hitCol, crit ? 7 : 4);   // fire left burning on the floor (light only)
            DungeonFx.Flash(Body, hitCol, crit ? 4.5f : 3f, crit ? 2.4f : 1.7f, crit ? 0.35f : 0.22f);
            DungeonFx.Ghost(_anim.Renderer, new Color(1f, 1f, 1f, 0.8f), 0.1f, true, true);
            GameFeel.HitStop(crit ? 0.075f : 0.035f);
            if (crit) { if (_dc != null) _dc.Shake(0.08f, 0.07f); GameLook.Punch(0.35f); }
            if (Hp <= 0) { Die(); return; }
            if (!Stats.boss && !Stats.elite && _st != St.Charging)
            {
                _st = St.Stagger; _stT = 0.16f;
                if (_rb != null && knock > 0f) _rb.linearVelocity = knockDir.normalized * knock;
            }
        }

        // ───────────────────────── light (step 6) ─────────────────────────
        /// Turns this monster into a shadow creature (DungeonController picks them by floor depth).
        public void MakeShadow()
        {
            Shadow = true;
            DisplayName = "Shadow " + DisplayName;
            if (!_weak.Contains(Element.Light)) _weak.Add(Element.Light);   // shadows are always weak to Light
            _elShown = -1;   // redraw the icons with the Light diamond
            name = "Shadow " + name;
        }

        void UpdateLight(float dt)
        {
            var set = _dc.Settings;
            Lit01 = Mathf.Clamp01(Mathf.InverseLerp(set.silhouetteBelow, set.silhouetteAbove, DungeonLight.At(Body)));
            if (!Shadow) return;
            // caught in the light: a shadow creature recoils
            if (!_wasLit && Lit01 > 0.6f)
            {
                _wasLit = true;
                if (_st == St.Moving || _st == St.Windup) { _st = St.Stagger; _stT = 0.35f; }
                DungeonFx.Puff(Body, new Color(0.25f, 0.1f, 0.4f, 0.8f), 8, 0.8f);
                DungeonHud.Number(Body + Vector2.up * (_anim.FigureHeight * 0.7f), "Revealed!", "dnum--info");
            }
            else if (_wasLit && Lit01 < 0.3f) _wasLit = false;
            // a wispy trail of shadow while it moves
            _wispT -= dt;
            if (_wispT <= 0f)
            {
                _wispT = 0.07f;
                var fx = DungeonFx.Spawn(DungeonFx.Circle, (Vector3)Body + (Vector3)(Random.insideUnitCircle * 0.2f), new Color(0.12f, 0.04f, 0.2f, 0.55f * (1f - Lit01 * 0.6f)), 0.7f, 0.5f, 0.15f, 0f, DungeonController.SortOrder(transform.position.y) - 1);
                fx.velocity = new Vector2(Random.Range(-0.2f, 0.2f), 0.5f); fx.drag = 1f;
            }
        }

        /// Light-shy / drawn-to-light steering on top of the normal chase (null = no change).
        Vector2? LightSteer(Vector2 v, float speed, float distToPlayer)
        {
            var reaction = Profile.lightReaction;
            if (reaction == LightReaction.None) return null;
            Vector2 src; float strength;
            bool has = DungeonLight.StrongestSource(Body, 0.5f, out src, out strength);
            if (reaction == LightReaction.Shy)
            {
                var set = _dc.Settings; float fdt = Time.fixedDeltaTime;
                if (_nerveNeed < 0f) _nerveNeed = set.shyNerveSeconds * Random.Range(0.7f, 1.3f);
                if (has)
                {
                    Vector2 away = (Body - src); away = away.sqrMagnitude > 0.001f ? away.normalized : Random.insideUnitCircle.normalized;
                    Vector2 toP = Player.Feet - (Vector2)transform.position;
                    Vector2 tan = new Vector2(-away.y, away.x);
                    // pick which way to circle when it first meets the light (towards your side), and stick with it
                    if (!_inSrc) { _inSrc = true; float d = Vector2.Dot(tan, toP); _orbit = Mathf.Abs(d) > 0.1f ? Mathf.Sign(d) : (Random.value < 0.5f ? -1f : 1f); }
                    _nerveT += fdt;
                    if (Brave) return v.sqrMagnitude > 0.0001f ? v.normalized * speed * set.shyBraveSpeed : Vector2.zero;   // lost patience: comes through, slowly
                    // circle round the edge of the light (goes round a torch in the way, or orbits you if you stand on one)
                    float outW = Mathf.Clamp01((strength - 0.5f) * 1.2f) * 0.7f + 0.15f;
                    return (tan * _orbit + away * outW).normalized * speed * 0.9f;
                }
                _inSrc = false;
                if (_nerveT > 0f) _nerveT = Mathf.Max(0f, _nerveT - fdt * 0.35f);   // calms down slowly once out of the light
                if (_nerveT <= 0f) _nerveNeed = -1f;
                if (Lit01 < 0.15f) return v * set.shyBoldness;   // bolder in the dark
                return null;
            }
            // Drawn: off to the light unless you're right in its face
            if (has && distToPlayer > 2.5f && Vector2.Distance(Body, src) > 1.1f) return (src - Body).normalized * speed;
            return null;
        }

        // ───────────────────────── elements (Design v2, ticket 06) ─────────────────────────
        readonly List<Element> _weak = new List<Element>(), _resist = new List<Element>();
        public IList<Element> WeakTo { get { return _weak; } }
        /// The monster's own element (its attacks carry it).
        public Element OwnElement { get { return Profile != null ? Profile.element : Element.None; } }
        public IList<Element> Resists { get { return _resist; } }

        void SetupElements(EnemyProfile p)
        {
            _weak.Clear(); _resist.Clear();
            if (p == null) return;
            if (p.weakTo != null) foreach (var e in p.weakTo) if (e != Element.None && !_weak.Contains(e)) _weak.Add(e);
            if (p.resists != null) foreach (var e in p.resists) if (e != Element.None && !_resist.Contains(e)) _resist.Add(e);
            if (_weak.Count == 0 && _resist.Count == 0) Elements.Defaults(p.element, _weak, _resist);
        }

        /// This monster was touched by an element (a hit, Flare's Light, a brazier's fire), damage or not.
        /// Discovery (ticket 07): you now know how that element works on every monster of this kind.
        /// Returns true when this taught you something new.
        public bool ElementContact(Element element)
        {
            if (element == Element.None || Dead || _dc == null || Stats == null) return false;
            if (Shadow && element == Element.Light) return false;   // shadows are always weak to Light: a rule, not a fact about this kind
            if (!_dc.Session.DiscoverElement(Stats.monsterId, element)) return false;
            DiscoveryVersion++;   // every monster of this kind redraws its icons
            bool weak = _weak.Contains(element), res = _resist.Contains(element);
            DungeonHud.Number(Body + Vector2.up * (_anim.FigureHeight * 1.1f),
                Elements.Emoji(element) + (weak ? " Weakness found!" : res ? " Resistance found" : " No effect"), weak ? "dnum--crit" : "dnum--info");
            var md = _dc.Session.Content.Monster(Stats.monsterId);
            _dc.Session.Log(Elements.Emoji(element) + " " + (md != null ? md.name : Stats.monsterId) + (weak ? " is weak to " : res ? " resists " : " shrugs off ") + Elements.Name(element));   // run log (ticket 20)
            return true;
        }

        // ───── element icons over the head (what you've learned about this kind of monster) ─────
        //   weakness: a big diamond in the element's colour · resistance: a faded square · no effect: a small outlined square
        public static int DiscoveryVersion;
        readonly List<SpriteRenderer> _elIcons = new List<SpriteRenderer>();
        readonly List<float> _elAlpha = new List<float>();
        readonly List<int> _elOrder = new List<int>();
        int _elShown = -1;
        const float IconWeak = 0.22f, IconResist = 0.17f, IconNeutral = 0.15f, IconGap = 0.09f;

        public static Color ElementColour(Element e)
        {
            switch (e)
            {
                case Element.Fire: return new Color(1f, 0.45f, 0.12f);
                case Element.Frost: return new Color(0.6f, 0.9f, 1f);
                case Element.Nature: return new Color(0.35f, 0.9f, 0.3f);
                case Element.Water: return new Color(0.25f, 0.5f, 1f);
                case Element.Light: return new Color(1f, 0.95f, 0.55f);
                default: return Color.white;
            }
        }

        void BuildElementIcons()
        {
            foreach (var sr in _elIcons) if (sr != null) Destroy(sr.gameObject);
            _elIcons.Clear(); _elAlpha.Clear(); _elOrder.Clear();
            _elShown = DiscoveryVersion;
            if (_anim == null) return;
            var p = _dc.Session.Profile;
            var weak = Discovery.KnownOf(p, Stats.monsterId, _weak);
            if (Shadow && !weak.Contains(Element.Light)) weak.Add(Element.Light);
            var res = Discovery.KnownOf(p, Stats.monsterId, _resist);
            var none = new List<Element>();
            foreach (Element e in System.Enum.GetValues(typeof(Element)))
                if (e != Element.None && !weak.Contains(e) && !res.Contains(e) && Discovery.Knows(p, Stats.monsterId, e)) none.Add(e);
            int n = weak.Count + res.Count + none.Count;
            if (n == 0) return;
            float w = weak.Count * IconWeak + res.Count * IconResist + none.Count * IconNeutral + (n - 1) * IconGap;
            float x = -w * 0.5f, y = _anim.FigureHeight + 0.42f;
            var dark = new Color(0.08f, 0.07f, 0.06f);   // a dark backing so the icons read on bright floors and in firelight
            foreach (var e in weak)
            {
                Icon(x + IconWeak * 0.5f, y, IconWeak * 0.75f + 0.05f, 45f, dark, 0.7f);
                Icon(x + IconWeak * 0.5f, y, IconWeak * 0.75f, 45f, ElementColour(e), 1f, 1);
                x += IconWeak + IconGap;
            }
            foreach (var e in res)
            {
                Icon(x + IconResist * 0.5f, y, IconResist + 0.05f, 0f, dark, 0.7f);
                Icon(x + IconResist * 0.5f, y, IconResist, 0f, ElementColour(e), 0.5f, 1);
                x += IconResist + IconGap;
            }
            foreach (var e in none)
            {
                Icon(x + IconNeutral * 0.5f, y, IconNeutral + 0.05f, 0f, dark, 0.7f);
                Icon(x + IconNeutral * 0.5f, y, IconNeutral, 0f, ElementColour(e), 0.9f, 1);            // outline…
                Icon(x + IconNeutral * 0.5f, y, IconNeutral * 0.55f, 0f, dark, 1f, 2);                    // …dark middle
                x += IconNeutral + IconGap;
            }
        }

        void Icon(float x, float y, float size, float rot, Color c, float alpha, int orderOffset = 0)
        {
            var g = new GameObject("Element"); g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(x, y, 0f); g.transform.localRotation = Quaternion.Euler(0, 0, rot);
            var b = DungeonFx.Pixel.bounds.size;
            g.transform.localScale = new Vector3(size / b.x, size / b.y, 1f);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = DungeonFx.Pixel; c.a = alpha; sr.color = c;
            _elIcons.Add(sr); _elAlpha.Add(alpha); _elOrder.Add(orderOffset);
        }

        void UpdateElementIcons()
        {
            if (_elShown != DiscoveryVersion) BuildElementIcons();
            if (_elIcons.Count == 0) return;
            float a = Shadow ? Mathf.Lerp(0.15f, 1f, Lit01) : 1f;
            int order = DungeonController.SortOrder(transform.position.y) + 3;
            for (int i = 0; i < _elIcons.Count; i++)
            {
                var sr = _elIcons[i]; if (sr == null) continue;
                var c = sr.color; c.a = _elAlpha[i] * a; sr.color = c; sr.sortingOrder = order + _elOrder[i];
            }
        }

        // ───────────────────────── statuses (player moves) ─────────────────────────
        public void Stun(float seconds)
        {
            if (Dead || _st == St.Spawning || seconds <= 0f) return;
            _stunT = Mathf.Max(_stunT, seconds * StatusResist);
            CancelInvoke("Bite");
            DungeonHud.Number(Body + Vector2.up * (_anim.FigureHeight * 0.7f), "Stunned", "dnum--info");
        }
        public void Root(float seconds)
        {
            if (Dead || _st == St.Spawning || seconds <= 0f) return;
            _rootT = Mathf.Max(_rootT, seconds * StatusResist);
        }
        /// Moves at <paramref name="speedMult"/> of normal speed (0.5 = half) for a while.
        public void Slow(float speedMult, float seconds)
        {
            if (Dead || seconds <= 0f) return;
            _slowMult = Mathf.Min(_slowMult, Mathf.Lerp(1f, Mathf.Clamp01(speedMult), StatusResist));
            _slowT = Mathf.Max(_slowT, seconds);
        }
        /// Takes +<paramref name="bonus"/> damage (0.5 = +50%) for a while.
        public void Mark(float bonus, float seconds)
        {
            if (Dead) return;
            _markBonus = Mathf.Max(_markBonus, bonus); _markT = Mathf.Max(_markT, seconds);
            DungeonFx.RingAt(Body, new Color(1f, 0.35f, 0.3f, 0.9f), 0.9f, 0.5f);
        }

        public void Heal(int amount)
        {
            if (Dead) return;
            Hp = Mathf.Min(Stats.maxHp, Hp + amount);
            DungeonHud.Number(Body + Vector2.up * 0.5f, "+" + amount, "dnum--heal");
            DungeonFx.RingAt(Body, new Color(0.5f, 1f, 0.6f, 0.7f), 0.8f);
        }

        void Die()
        {
            Dead = true;
            CancelInvoke();
            DungeonFx.Puff(Body, Stats.boss ? new Color(1f, 0.6f, 0.3f) : new Color(0.85f, 0.85f, 0.85f), Stats.boss ? 18 : 8, Stats.boss ? 1.4f : 0.7f);
            if (Stats.boss) _dc.Shake(0.3f, 0.25f);
            // death: a burst of light and embers; a boss going down slows time for a moment
            Color deathCol = Stats.boss ? new Color(1f, 0.6f, 0.3f) : new Color(1f, 0.88f, 0.7f);
            DungeonFx.Flash(Body, deathCol, Stats.boss ? 10f : 4.5f, Stats.boss ? 3f : 2f, Stats.boss ? 1.4f : 0.5f);
            DungeonFx.Embers(Body, deathCol, Stats.boss ? 60 : (Stats.elite ? 26 : 14), Stats.boss ? 1f : 0.4f);
            DungeonFx.Sparks(Body, Vector2.up, deathCol, Stats.boss ? 30 : 12);
            for (int i = 0; i < (Stats.boss ? 40 : 14); i++) { Vector2 o = Random.insideUnitCircle.normalized; DungeonFx.Throw(Body, o, deathCol, 1, 3f, Stats.boss ? 10f : 7f); }
            if (Stats.boss) { GameFeel.SlowMo(0.25f, 1f); GameLook.Punch(1f); }
            else GameFeel.HitStop(Stats.elite ? 0.09f : 0.06f);
            var room = _room; var dc = _dc; var stats = Stats;
            Destroy(gameObject);
            if (dc != null) { dc.Session.DungeonEnemyKilled(stats); dc.OnEnemyKilled(); }   // rewards, loot (refreshes the HUD); trinkets (ticket 16)
            if (room != null) room.OnEnemyDied(this);
        }

        void UpdateTint()
        {
            var sr = _anim.Renderer;
            if (sr == null) return;
            Color c = Stats.elite && !Stats.boss ? new Color(1f, 0.88f, 0.6f) : Color.white;
            if (_flashT > 0f) c = new Color(1f, 0.45f, 0.45f);
            else if (_stunT > 0f) c = new Color(0.75f, 0.8f, 1f);
            else if (_rootT > 0f) c = new Color(0.7f, 1f, 0.7f);
            else if (_markT > 0f && Mathf.FloorToInt(Time.time * 6f) % 2 == 0) c = new Color(1f, 0.6f, 0.55f);
            else if (_slowT > 0f) c = new Color(0.85f, 0.85f, 0.7f);
            else if (_st == St.Windup && Mathf.FloorToInt(Time.time * 16f) % 2 == 0) c = new Color(1f, 0.75f, 0.55f);
            sr.color = c;
        }

        void UpdateHpBar()
        {
            UpdateElementIcons();
            if (_hpFill == null) return;
            bool show = Hp < Stats.maxHp;
            _hpBg.enabled = _hpFill.enabled = show;
            if (!show) return;
            float k = Mathf.Clamp01(Hp / (float)Stats.maxHp);
            _hpFill.transform.localScale = new Vector3(0.8f * DungeonFx.PPU * k, 0.1f * DungeonFx.PPU, 1);
            int order = DungeonController.SortOrder(transform.position.y) + 1;
            _hpBg.sortingOrder = order; _hpFill.sortingOrder = order + 1;
        }
    }
}
