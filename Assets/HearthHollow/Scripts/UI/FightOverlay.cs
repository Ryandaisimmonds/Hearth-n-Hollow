// Hearth & Hollow — fight overlay: monster card, HP bars, big action buttons, combat juice.
// Juice: floating damage numbers, screen shake on big hits, crit hit-pause (input freeze + scale pop),
// low-HP pulse on the player bar, recent combat log line, short victory/defeat outro.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class FightOverlay : UIScreen
    {
        readonly VisualElement _monsterCard, _playerPanel;
        readonly SpriteAnimView _mEmoji, _pPortrait;
        readonly VisualElement _mEliteBadge, _skillIconHolder, _outroImage;
        readonly Label _mName, _mHint, _mWindUp, _turn;
        readonly Bar _mHp, _pHp;
        readonly Label _pName;
        readonly Label _log, _outro;
        readonly Button _strike, _guard, _lucky, _skill;
        readonly Label _strikeSub, _guardSub, _luckySub, _skillTitle, _skillSub;
        readonly VisualElement _actions;
        readonly ConfirmButton _flee;

        FightState _f;               // cached so the outro can still show the final state
        readonly List<string> _logLines = new List<string>();
        float _inputBlockedUntil;
        float _lastEventAt; int _eventBurst;

        public bool InOutro { get; private set; }

        public FightOverlay(GameUI ui) : base(ui, "overlay", "fight")
        {
            var box = UIKit.E("overlay__box", "fight__box");
            Root.Add(box);

            _turn = UIKit.L("", "muted", "fight__turn");
            box.Add(_turn);

            // ───── monster card ─────
            _monsterCard = UIKit.E("card", "monster-card", "float-host");
            _mEmoji = new SpriteAnimView(null, "", "monster-card__emoji", "monster-card__anim");
            _mEliteBadge = Art.Icon("elite", "👑", "ico", "monster-card__crown");
            _mName = UIKit.L("", "monster-card__name");
            _mHp = new Bar("bar--monster");
            _mWindUp = UIKit.L("⚠️ Winding up — next blow hits ×2! Guard!", "monster-card__windup");
            _mHint = UIKit.L("", "muted", "monster-card__hint");
            var stage = UIKit.E("monster-card__stage");
            stage.Add(_mEmoji);
            stage.Add(_mEliteBadge);
            _monsterCard.Add(stage); _monsterCard.Add(_mName); _monsterCard.Add(_mHp);
            _monsterCard.Add(_mWindUp); _monsterCard.Add(_mHint);
            box.Add(_monsterCard);

            _log = UIKit.L("", "fight__log");
            box.Add(_log);

            // ───── player ─────
            _playerPanel = UIKit.E("fight__player", "float-host");
            _pPortrait = new SpriteAnimView(null, "", "fight__player-anim");
            var pInfo = UIKit.E("fight__player-info");
            _pName = UIKit.L("", "fight__player-name");
            _pHp = new Bar("bar--hp");
            pInfo.Add(_pName);
            pInfo.Add(PlayerStatRow());
            pInfo.Add(_pHp);
            _playerPanel.Add(Art.Framed(_pPortrait, "fight__player-frame"));
            _playerPanel.Add(pInfo);
            box.Add(_playerPanel);

            // ───── actions ─────
            _actions = UIKit.E("actions");
            _strike = UIKit.BigButton(() => Act(FightAction.Strike), "action--strike", out var st, out _strikeSub);
            st.text = "1 · Strike";
            _strike.Insert(0, Art.Icon("action_strike", "🗡️", "ico", "btn-big__icon"));
            _guard = UIKit.BigButton(() => Act(FightAction.Guard), "action--guard", out var gt, out _guardSub);
            gt.text = "2 · Guard";
            _guard.Insert(0, Art.Icon("action_guard", "🛡️", "ico", "btn-big__icon"));
            _lucky = UIKit.BigButton(() => Act(FightAction.Lucky), "action--lucky", out var lt, out _luckySub);
            lt.text = "3 · Lucky";
            _lucky.Insert(0, Art.Icon("action_lucky", "🍀", "ico", "btn-big__icon"));
            _skill = UIKit.BigButton(() => Act(FightAction.Skill), "action--skill", out _skillTitle, out _skillSub);
            _skillIconHolder = UIKit.E("btn-big__icon-holder");
            _skillIconHolder.pickingMode = PickingMode.Ignore;
            _skill.Insert(0, _skillIconHolder);
            _actions.Add(_strike); _actions.Add(_guard); _actions.Add(_lucky); _actions.Add(_skill);
            box.Add(_actions);

            _flee = new ConfirmButton("Flee home", "Really flee? (keeps goods, no clear bonus)", () => S.FleeHome(), "btn--small", "btn--danger", "btn--flee");
            _flee.SetIcon(Art.Icon("action_flee", "🏃", "ico", "btn__icon"));
            box.Add(_flee.Button);

            _outroImage = UIKit.E("fight__outro-image");
            _outroImage.pickingMode = PickingMode.Ignore;
            UIKit.Show(_outroImage, false);
            box.Add(_outroImage);
            _outro = UIKit.L("", "fight__outro");
            UIKit.Show(_outro, false);
            box.Add(_outro);
        }

        Label _pDmg, _pArmour, _pCrit;
        string _skillIconKey;

        VisualElement PlayerStatRow()
        {
            var row = UIKit.E("fight__player-stats");
            row.Add(Art.IconText("damage", "🗡️", "-", out _pDmg, "hud__stat"));
            row.Add(Art.IconText("armour", "🛡️", "-", out _pArmour, "hud__stat"));
            row.Add(Art.IconText("crit", "💥", "-", out _pCrit, "hud__stat"));
            return row;
        }

        protected override void OnShow()
        {
            _logLines.Clear();
            _log.text = "";
            UIKit.Show(_outro, false);
            UIKit.Show(_outroImage, false);
            UIKit.Show(_actions, true);
            UIKit.Show(_flee.Button, true);
            _flee.Reset();
            _monsterCard.RemoveFromClassList("monster-card--dead");
            _f = S.Run != null ? S.Run.fight : null;
        }

        protected override void OnHide() { InOutro = false; }

        // ───────────────────────── refresh ─────────────────────────
        public override void Refresh()
        {
            var run = S.Run;
            if (run == null) return;
            if (run.fight != null) _f = run.fight;
            var f = _f;
            if (f == null) return;
            var t = C.tuning;
            var m = C.Monster(f.monsterId);
            var cls = C.Class(run.classId);
            var st = S.Stats;

            _turn.text = "Turn " + (f.turn + 1) + (f.elite ? "  ·  👑 Elite" : "") + "  ·  difficulty " + f.difficulty;
            _mEmoji.SetAnim(ArtLibrary.GetMonster(f.monsterId), m != null && !string.IsNullOrEmpty(m.emoji) ? m.emoji : "👾");
            UIKit.Show(_mEliteBadge, f.elite);
            _mName.text = (f.elite ? "Elite " : "") + (m != null ? m.name : "Monster");
            _monsterCard.EnableInClassList("monster-card--elite", f.elite);
            _mHp.Set(f.monsterHp, f.monsterMaxHp, f.monsterHp + " / " + f.monsterMaxHp);
            UIKit.Show(_mWindUp, f.monsterWindingUp && !f.over);
            _monsterCard.EnableInClassList("monster-card--windup", f.monsterWindingUp && !f.over);
            _mHint.text = (m != null ? UIKit.BehaviourHint(m.behaviour) : "") + "  ·  hits ~" + Mathf.RoundToInt(Combat.ApplyArmour(f.monsterAtk, st != null ? st.armourPct : 0f));

            int max = st != null ? st.maxHp : run.hp;
            _pPortrait.SetAnim(Art.ClassAnim(run.classId), cls != null ? cls.emoji : "🧑‍🍳");
            _pName.text = cls != null ? cls.name : "You";
            _pDmg.text = st != null ? UIKit.Num(st.damage) : "-";
            _pArmour.text = st != null ? UIKit.Pct(st.armourPct) : "-";
            _pCrit.text = st != null ? UIKit.Pct(st.critChance) : "-";
            _pHp.Set(run.hp, max, run.hp + " / " + max);
            _pHp.SetLow(max > 0 && run.hp < max * 0.3f);

            bool live = !f.over && S.Phase == Phase.Fight;
            float hit = f.lastStrikeMissed ? t.strikeHitAfterMiss : t.strikeHit;
            _strikeSub.text = f.riposteReady ? "↩ riposte ×" + UIKit.Num(t.riposteMult) + " · sure hit"
                                             : UIKit.Pct(hit) + " to hit" + (f.lastStrikeMissed ? " (steadied)" : "");
            _guardSub.text = "heal " + t.guardHeal + ", take ×" + UIKit.Num(t.guardDamageTaken) + " · next hit ×" + UIKit.Num(t.riposteMult);
            _luckySub.text = "always hits ×" + UIKit.Num(t.luckyDamageMult) + ", crit ×" + UIKit.Num(t.luckyCritMult);
            _strike.SetEnabled(live); _guard.SetEnabled(live); _lucky.SetEnabled(live);
            _strike.EnableInClassList("action--hot", f.lastStrikeMissed || f.riposteReady);
            _guard.EnableInClassList("action--hot", f.monsterWindingUp);

            var cp = S.Profile.Progress(run.classId);
            var skill = cls != null ? cls.skill : null;
            _skillTitle.text = "4 · " + (skill != null ? skill.name : "Skill");
            string sKey = Art.SkillIconId(skill) ?? "";
            if (_skillIconKey != sKey)
            {
                _skillIconKey = sKey;
                Art.Set(_skillIconHolder, Art.Icon(Art.SkillIconId(skill), skill != null ? skill.emoji : "✨", "ico", "btn-big__icon"));
            }
            bool unlocked = cp.level >= t.skillUnlockLevel;
            if (!unlocked) _skillSub.text = "unlocks at level " + t.skillUnlockLevel;
            else if (f.skillCooldown > 0) _skillSub.text = "⏳ ready in " + f.skillCooldown + " turn" + (f.skillCooldown == 1 ? "" : "s");
            else _skillSub.text = skill != null ? skill.description : "Ready!";
            bool skillReady = live && S.CanUseSkill();
            _skill.SetEnabled(skillReady);
            _skill.EnableInClassList("action--ready", skillReady);

        }

        void Act(FightAction a)
        {
            if (UIKit.Now < _inputBlockedUntil || InOutro) return;
            if (S.Phase != Phase.Fight) return;
            if (a == FightAction.Skill && !S.CanUseSkill()) { Juice.Flash(_skill, "btn--nope", 200); return; }
            S.Fight(a);
        }

        // ───────────────────────── outro ─────────────────────────
        /// Keeps the overlay up briefly after the fight ends, then calls `done` to re-layout.
        public void PlayOutro(RunOutcome endedRun, Action done)
        {
            InOutro = true;
            bool won = endedRun == RunOutcome.None || endedRun == RunOutcome.Cleared;
            bool died = endedRun == RunOutcome.Died;
            Refresh();
            UIKit.Show(_actions, false);
            UIKit.Show(_flee.Button, false);
            UIKit.Show(_outro, true);
            _outro.text = won ? "🎉 Victory!" : died ? "💀 You collapse… and limp home." : "🏃 You slip away into the fog.";
            _outro.EnableInClassList("fight__outro--lose", died);
            // painted word-art headline when the pack has it; the text stays as a subtitle
            Sprite head = won ? ArtLibrary.GetIcon("text_win") : died ? ArtLibrary.GetIcon("text_defeat") : null;
            _outroImage.style.backgroundImage = head != null ? new StyleBackground(head) : new StyleBackground(StyleKeyword.None);
            UIKit.Show(_outroImage, head != null);
            _outro.EnableInClassList("fight__outro--sub", head != null);
            if (head != null)
                _outro.text = won ? "Victory!" : "You collapse… and limp home.";
            if (won) _monsterCard.AddToClassList("monster-card--dead");
            Juice.Pop(head != null ? _outroImage : (VisualElement)_outro, 1.3f, 0.05f, 0.25f);
            Root.schedule.Execute(() =>
            {
                InOutro = false;
                done();
            }).StartingIn(died ? 1300 : 750);
        }

        // ───────────────────────── juice ─────────────────────────
        public override void OnGameEvent(GameEvent e)
        {
            // stagger numbers that arrive in the same turn so double hits read as two hits
            float now = UIKit.Now;
            if (now - _lastEventAt > 0.05f) _eventBurst = 0;
            _lastEventAt = now;
            float delay = _eventBurst * 0.18f;

            int amt = Mathf.RoundToInt(e.amount);
            int maxHp = S.Stats != null ? S.Stats.maxHp : 20;
            switch (e.type)
            {
                case GameEventType.PlayerHit:
                    _pPortrait.PlayAttack();
                    Juice.Float(_monsterCard, "-" + amt, "float-text--dmg", delay);
                    Juice.Flash(_monsterCard, "flash-hit");
                    Log("You hit for " + amt + ".");
                    _eventBurst++;
                    break;
                case GameEventType.PlayerCrit:
                    _pPortrait.PlayAttack();
                    Juice.Float(_monsterCard, "CRIT -" + amt, "float-text--crit", delay);
                    Juice.Pop(_mEmoji, 1.45f, 0.14f, 0.22f);       // hit-pause: hold the pop
                    _inputBlockedUntil = now + 0.25f;              // brief freeze of input
                    Ui.Juice.Shake(6f, 0.18f);
                    Log("💥 Critical hit for " + amt + "!");
                    _eventBurst++;
                    break;
                case GameEventType.PlayerMiss:
                    Juice.Float(_monsterCard, "miss", "float-text--miss", delay);
                    Log("You miss — steady your aim (next Strike more likely).");
                    _eventBurst++;
                    break;
                case GameEventType.Riposte:
                    _pPortrait.PlayAttack();
                    Juice.Float(_monsterCard, "↩ Riposte!", "float-text--crit", delay);
                    Log("↩ Riposte!");
                    _eventBurst++;
                    break;
                case GameEventType.PlayerGuard:
                    Juice.Float(_playerPanel, "🛡️ " + (string.IsNullOrEmpty(e.text) ? "Guard" : e.text), "float-text--guard", delay);
                    Log("You brace yourself — your next attack is a riposte.");
                    _eventBurst++;
                    break;
                case GameEventType.MonsterHit:
                    _mEmoji.PlayAttack();
                    Juice.Float(_playerPanel, "-" + amt, "float-text--hurt", delay);
                    Juice.Flash(_playerPanel, "flash-hurt");
                    bool big = e.text == "Big hit!" || amt >= Mathf.Max(4, maxHp / 5);
                    Ui.Juice.Shake(big ? 12f : 4f, big ? 0.25f : 0.12f);
                    Log((e.text == "Big hit!" ? "💢 Crushing blow: " : "It hits you for ") + amt + ".");
                    _eventBurst++;
                    break;
                case GameEventType.MonsterHeal:
                    Juice.Float(_monsterCard, "+" + amt, "float-text--heal", delay);
                    Log("It patches itself up (+" + amt + ").");
                    _eventBurst++;
                    break;
                case GameEventType.MonsterWindUp:
                    Juice.Pop(_mEmoji, 1.2f, 0.1f, 0.3f);
                    Log("⚠️ It's winding up a huge blow…");
                    break;
                case GameEventType.PlayerHeal:
                    if (amt > 0) Juice.Float(_playerPanel, "+" + amt, "float-text--heal", delay);
                    break;
                case GameEventType.MonsterDied:
                    Juice.Pop(_mEmoji, 1.5f, 0.12f, 0.3f);
                    Log("It's down!");
                    break;
                case GameEventType.PlayerDied:
                    Ui.Juice.Shake(14f, 0.35f);
                    Log("You fall…");
                    break;
                case GameEventType.Info:
                    if (!string.IsNullOrEmpty(e.text)) Log(e.text);
                    break;
            }
        }

        void Log(string line)
        {
            _logLines.Add(line);
            while (_logLines.Count > 2) _logLines.RemoveAt(0);
            _log.text = string.Join("\n", _logLines.ToArray());
        }

        public override bool HandleKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Alpha1: case KeyCode.Keypad1: Act(FightAction.Strike); return true;
                case KeyCode.Alpha2: case KeyCode.Keypad2: Act(FightAction.Guard); return true;
                case KeyCode.Alpha3: case KeyCode.Keypad3: Act(FightAction.Lucky); return true;
                case KeyCode.Alpha4: case KeyCode.Keypad4: Act(FightAction.Skill); return true;
            }
            return false;
        }
    }
}
