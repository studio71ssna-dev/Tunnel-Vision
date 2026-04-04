// EnemyAnimator.cs  (v3)
//
// Parameter layout — simple and unambiguous:
//
//   Speed       Float   0 = idle, 1 = walking  (drives Idle <-> Walk blend)
//   IsAttacking Bool    true while in AttackState
//   IsHit       Bool    one-frame pulse
//   IsDead      Bool    latches true, never reset
//
// Why float for movement instead of bool:
//   - Idle <-> Walk is a blend, not a switch. A float threshold transition
//     (Speed > 0.1 → Walk, Speed < 0.1 → Idle) is what Unity's blendtrees
//     are designed for. No "all-four-bools-false" race condition.
//
// Animator Controller recipe (bottom of this file as a comment block).

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour, IAnimatable
{
    // ── Parameter name constants ─────────────────────────────────
    public const string PARAM_SPEED = "Speed";
    public const string PARAM_IS_ATTACKING = "IsAttacking";
    public const string PARAM_IS_HIT = "IsHit";
    public const string PARAM_IS_DEAD = "IsDead";

    private Animator _animator;
    private readonly Dictionary<string, int> _hashes = new();

    // ── Init ─────────────────────────────────────────────────────
    public void Initialise(Animator animator, EnemyData data)
    {
        _animator = animator;
        _hashes.Clear();

        RegisterFloat(PARAM_SPEED);
        RegisterBool(PARAM_IS_ATTACKING);
        RegisterBool(PARAM_IS_HIT);
        RegisterBool(PARAM_IS_DEAD);

        InitialiseExtra(data);
        ResetAll();
    }

    protected virtual void InitialiseExtra(EnemyData data) { }

    private void RegisterFloat(string p) => _hashes[p] = Animator.StringToHash(p);
    private void RegisterBool(string p) => _hashes[p] = Animator.StringToHash(p);
    protected void RegisterExtraParam(string p) => _hashes[p] = Animator.StringToHash(p);

    // ── Low-level setters ─────────────────────────────────────────
    public void SetFloat(string p, float v)
    {
        if (_hashes.TryGetValue(p, out int h)) _animator.SetFloat(h, v);
        else Debug.LogWarning($"[EnemyAnimator] Float param '{p}' not registered on {name}");
    }

    public void SetBool(string p, bool v)
    {
        if (_hashes.TryGetValue(p, out int h)) _animator.SetBool(h, v);
        else Debug.LogWarning($"[EnemyAnimator] Bool param '{p}' not registered on {name}");
    }

    public bool GetBool(string p)
    {
        if (_hashes.TryGetValue(p, out int h)) return _animator.GetBool(h);
        return false;
    }

    // ── Named state helpers (called by states) ────────────────────
    public void SetIdle()
    {
        SetFloat(PARAM_SPEED, 0f);
        SetBool(PARAM_IS_ATTACKING, false);
    }

    public void SetWalking()
    {
        SetFloat(PARAM_SPEED, 1f);
        SetBool(PARAM_IS_ATTACKING, false);
    }

    public void SetAttacking()
    {
        SetFloat(PARAM_SPEED, 0f);
        SetBool(PARAM_IS_ATTACKING, true);
    }

    public void SetDead()
    {
        SetFloat(PARAM_SPEED, 0f);
        SetBool(PARAM_IS_ATTACKING, false);
        SetBool(PARAM_IS_DEAD, true);
    }

    /// <summary>One-frame pulse — Animator transition handles the blend back.</summary>
    public void SetHit()
    {
        SetBool(PARAM_IS_HIT, true);
        if (gameObject.activeInHierarchy)
            StartCoroutine(ResetHitNextFrame());
    }

    private IEnumerator ResetHitNextFrame()
    {
        yield return null;
        SetBool(PARAM_IS_HIT, false);
    }

    // ── IAnimatable (string key API — for backward compat) ────────
    public void PlayAnimation(string key) => MapKey(key);
    public void CrossFadeAnimation(string key, float _ = 0.15f) => MapKey(key);

    private void MapKey(string key)
    {
        switch (key)
        {
            case "Walk": SetWalking(); break;
            case "Attack": SetAttacking(); break;
            case "Hit": SetHit(); break;
            case "Dead": SetDead(); break;
            default: SetIdle(); break;
        }
    }

    // ── Utilities ─────────────────────────────────────────────────
    private void ResetAll()
    {
        SetFloat(PARAM_SPEED, 0f);
        SetBool(PARAM_IS_ATTACKING, false);
        SetBool(PARAM_IS_HIT, false);
        SetBool(PARAM_IS_DEAD, false);
    }

    public bool IsCurrentStateName(string stateName) =>
        _animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);

    public float CurrentStateNormalizedTime =>
        _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
}

/*
 ═══════════════════════════════════════════════════════════════════
  ANIMATOR CONTROLLER SETUP — read this, it takes 3 minutes
 ═══════════════════════════════════════════════════════════════════

 PARAMETERS TAB (left panel):
   Speed         Float
   IsAttacking   Bool
   IsHit         Bool
   IsDead        Bool

 STATES (create these in the graph):
   Idle      ← set as Default (orange)
   Walk
   Attack
   Hit
   Dead

 TRANSITIONS — wire exactly these, no others:

   Idle   → Walk     condition: Speed > 0.1        Has Exit Time: OFF  Duration: 0.15
   Walk   → Idle     condition: Speed < 0.1        Has Exit Time: OFF  Duration: 0.15
   Walk   → Attack   condition: IsAttacking = true Has Exit Time: OFF  Duration: 0.1
   Idle   → Attack   condition: IsAttacking = true Has Exit Time: OFF  Duration: 0.1
   Attack → Idle     condition: IsAttacking = false Has Exit Time: OFF Duration: 0.1

   Any State → Hit   condition: IsHit = true       Has Exit Time: OFF  Duration: 0.05
   Hit → (previous)  No condition. Has Exit Time: ON, Exit Time: 0.9   Duration: 0.05
   
   Any State → Dead  condition: IsDead = true      Has Exit Time: OFF  Duration: 0.1

 CLIP SETTINGS:
   Idle, Walk, Attack → Loop Time: ON
   Hit, Dead          → Loop Time: OFF

 That's it. No other transitions. No parameters. Nothing else.
 ═══════════════════════════════════════════════════════════════════
*/