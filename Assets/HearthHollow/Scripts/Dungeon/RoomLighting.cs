// Hearth & Hollow — a dungeon floor's light: the room light (dark while there's something to fight, lighting up
// after a pause in calm or cleared rooms, darker in boss rooms and the deeper you go) and the light you carry
// (fed by your meals: Sandwich = wider, BBQ = hotter, Pot = steadier). DungeonController makes one per floor.
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public sealed class RoomLighting
    {
        readonly DungeonController _dc;
        DungeonSettings Settings { get { return _dc.Settings; } }
        GameSession Session { get { return _dc.Session; } }

        Light2D _global; float _darkI, _litI, _lightLevel, _lightTarget, _lightUpAt = -1f;
        float _roomMult = 1f, _roomMultTarget = 1f;   // boss rooms are darker until the boss is down
        float _glowAdd, _glowDecay;                    // big moves flood the room with light for a moment
        Light2D _playerLight;
        float _lightBaseI, _lightBaseR;
        /// How much your Pot meals steady your light (and shorten Flare's cooldown).
        public float MealSteady { get; private set; }

        public RoomLighting(DungeonController dc) { _dc = dc; }

        /// How dark this room gets (1 = as the floor; boss rooms and the fall's later rooms go lower).
        public float RoomMultTarget { set { _roomMultTarget = value; } }

        /// You walked in: a calm or cleared room lights up after a pause; a fight goes back to the dark.
        public void Enter(bool calm, float delay)
        {
            if (calm) { if (_lightTarget < 1f && _lightUpAt < 0f) _lightUpAt = Time.time + delay; }
            else { _lightTarget = 0f; _lightUpAt = -1f; }
        }

        /// The room was cleared: back to full light (after the pause, if you're in it).
        public void Cleared(bool here)
        {
            _roomMultTarget = 1f;
            if (here) _lightUpAt = Time.time + Settings.lightUpDelay;
        }

        public void MakeRoomLight(Transform root, DungeonTheme theme)
        {
            if (theme == null) return;
            var lgo = new GameObject("Global Light");
            lgo.transform.SetParent(root, false);
            var light = lgo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = theme.lightColor;
            light.intensity = theme.lightIntensity * Settings.roomLight;   // dark rooms: light comes from you, your moves and the fire
            // darker the deeper you go (step 5): floor 1 as set, floor 2 dusky, floor 3 properly dark; caves start darker
            int floorNo = Session.Run != null ? Mathf.Max(1, Session.Run.floor) : 1;
            var fl = Settings.floorLight;
            float depth = fl != null && fl.Length > 0 ? fl[Mathf.Min(floorNo - 1, fl.Length - 1)] : 1f;
            bool cave = theme.ambient == RoomGlow.Mushrooms || theme.ambient == RoomGlow.Crystals || theme.ambient == RoomGlow.LavaCracks;
            light.intensity *= depth * (cave ? Settings.caveLight : 1f);
            LightSetup.ApplyToAllLayers(light);
            _global = light; _darkI = light.intensity;
            _roomMult = _roomMultTarget = 1f;
            _litI = _darkI * Settings.litRoomBoost;
            _lightLevel = 0f; _lightTarget = 0f; _lightUpAt = -1f;
        }

        public void MakePlayerLight(PlayerController player, DungeonTheme theme)
        {
            var lgo = new GameObject("Player Light");
            lgo.transform.SetParent(player.transform, false);
            lgo.transform.localPosition = new Vector3(0, 0.2f, 0);   // the centre of your collision circle: it can't be pushed inside a wall or rock
            _playerLight = lgo.AddComponent<Light2D>();
            _playerLight.lightType = Light2D.LightType.Point;
            _playerLight.color = Color.Lerp(new Color(1f, 0.76f, 0.48f), PlayerController.ClassColour(player.ClassId), Settings.carriedLightClassTint);   // firelight, tinted by your class colour
            _playerLight.intensity = Mathf.Max(theme.playerLight, Settings.carriedLight);
            _playerLight.pointLightOuterRadius = Settings.carriedLightRadius * (1f + Gear.LightReach(Session.Content, Session.Profile));   // the lantern (ticket 16) + the Lamp-maker (ticket 17)
            // packed meals feed your light (step 5): Sandwich = wider, BBQ = hotter + redder, Pot = steadier; finer meals count more
            float reach = 0f, heat = 0f, steady = 0f;
            if (Session.Run != null)
                foreach (var m in Session.Run.eaten)
                {
                    var r = m != null ? Session.Content.Recipe(m.recipeId) : null;
                    if (r == null) continue;
                    float g = (m.grade >= 2 ? 1.5f : m.grade == 1 ? 1.25f : 1f) * (m.potency > 0f ? Mathf.Clamp(m.potency, 0.3f, 1.5f) : 1f);
                    if (r.station == Station.Pot) steady += g; else if (r.station == Station.BBQ) heat += g; else reach += g;
                }
            MealSteady = steady;
            _playerLight.pointLightOuterRadius *= 1f + Settings.mealReach * reach;
            _playerLight.intensity *= 1f + Settings.mealHeat * heat;
            _playerLight.color = Color.Lerp(_playerLight.color, new Color(1f, 0.5f, 0.25f), Mathf.Clamp01(heat * 0.12f));
            if (reach + heat + steady > 0.01f && player != null)
            {
                var parts = new List<string>();
                if (reach > 0.01f) parts.Add("wider"); if (heat > 0.01f) parts.Add("hotter"); if (steady > 0.01f) parts.Add("steadier");
                DungeonHud.Number(player.Body + Vector2.up * 1f, "🔥 Your meals: light " + string.Join(" · ", parts.ToArray()), "dnum--info");
                Session.Log("🔥 Your meals make your light " + string.Join(", ", parts.ToArray()));
            }
            _playerLight.pointLightInnerRadius = 0.3f;
            _playerLight.falloffIntensity = Settings.carriedLightFalloff;   // reaches far, fades gradually
            LightSetup.CastShadows(_playerLight);   // pillars throw shadows away from you
            lgo.AddComponent<ShadowSafeLight>();   // never goes dark if its centre ends up inside a wall
            // walls pick the light up from further away than the ground
            var wgo = new GameObject("Wall Glow");
            wgo.transform.SetParent(lgo.transform, false);
            var wl = wgo.AddComponent<Light2D>();
            wl.shadowsEnabled = false;   // walls glow, they aren't shadowed
            wl.lightType = Light2D.LightType.Point;
            wl.color = _playerLight.color;
            wl.intensity = Settings.wallGlow;
            wl.pointLightOuterRadius = Settings.carriedLightRadius * Settings.wallGlowReach;
            wl.pointLightInnerRadius = 0.5f;
            wl.falloffIntensity = Settings.wallGlowFalloff;
            LightSetup.ApplyToLayers(wl, "Walls");
            LightSetup.ApplyToAllLayers(_playerLight);
            _lightBaseI = _playerLight.intensity;   // the flicker in Update works round this
            _lightBaseR = _playerLight.pointLightOuterRadius;
        }

        /// A big move floods the room with light for a moment (class rework).
        public void FloodLight(float add, float seconds) { if (add > _glowAdd) { _glowAdd = add; _glowDecay = add / Mathf.Max(0.1f, seconds); } }

        /// Ramps the room light towards its target; fires the light-up when the pause is over.
        public void Tick(RoomInstance current)
        {
            if (_global == null) return;
            if (_lightUpAt >= 0f && Time.time >= _lightUpAt)
            {
                _lightUpAt = -1f; _lightTarget = 1f;
                if (current != null)
                {
                    // a soft whoomph of warm light from the middle of the room, and embers rising
                    Vector3 c = (Vector3)(current.Origin + RoomTemplate.Centre);
                    DungeonFx.Flash(c, new Color(1f, 0.8f, 0.5f), 14f, 1.2f, Settings.lightUpSeconds * 1.2f);
                    DungeonFx.Embers(c, new Color(1f, 0.65f, 0.3f), 24, 4f);
                    if (current.Lights != null) current.Lights.IgniteWave(c);
                }
            }
            bool up = _lightTarget > _lightLevel;
            _lightLevel = Mathf.MoveTowards(_lightLevel, _lightTarget, Time.deltaTime / Mathf.Max(0.05f, up ? Settings.lightUpSeconds : 0.35f));
            _roomMult = Mathf.MoveTowards(_roomMult, _roomMultTarget, Time.deltaTime * 0.8f);
            _global.intensity = Mathf.Lerp(_darkI, _litI, Mathf.SmoothStep(0f, 1f, _lightLevel)) * _roomMult;
            _glowAdd = Mathf.MoveTowards(_glowAdd, 0f, _glowDecay * Time.deltaTime);
            _global.intensity += _glowAdd;
        }

        /// Your light flickers like a flame (Pot meals steady it).
        public void Flicker()
        {
            if (_playerLight == null || _lightBaseI <= 0f) return;
            float amp = 0.08f / (1f + MealSteady);
            float tt = Time.time;
            _playerLight.intensity = _lightBaseI * (1f + amp * (Mathf.Sin(tt * 7.3f) * 0.6f + Mathf.Sin(tt * 17.1f) * 0.4f));
        }
    }
}
