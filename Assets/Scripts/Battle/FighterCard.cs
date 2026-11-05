using System.Collections;
using UnityEngine;

/// <summary>
/// Modular 2.5D fighter card slab with Inspector-driven idle pose, bob, attacks, and recoil.
/// </summary>
[DisallowMultipleComponent]
public class FighterCard : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer cardSprite;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color telegraphColor = new Color(1f, 0.55f, 0.2f, 1f);

    [Header("Transform Tuning")]
    [SerializeField] private Vector3 idlePosition = Vector3.zero;
    [SerializeField] private Vector3 idleRotation = Vector3.zero;
    [SerializeField] private Vector3 idleScale = new Vector3(1.8f, 1.8f, 1.8f);

    [Header("Facing")]
    [Tooltip("World-space direction this fighter lunges toward (Left: +X, Right: -X).")]
    [SerializeField] private Vector3 attackDirection = Vector3.right;

    [Header("Bob / Breathing Motion")]
    [SerializeField] private float bobSpeed = 2.0f;
    [SerializeField] private float bobHeight = 0.08f;

    [Header("Attack Settings")]
    [SerializeField] private float lungeDistance = 1.2f;
    [SerializeField] private float lungeDuration = 0.12f;
    [SerializeField] private float recoveryDuration = 0.25f;
    [SerializeField] private Vector3 attackTilt = new Vector3(20f, 0f, 0f);

    [Header("Overhead Cleave")]
    [SerializeField] private float cleaveWindupDuration = 0.18f;
    [SerializeField] private float cleaveStrikeDuration = 0.12f;
    [SerializeField] private Vector3 cleaveWindupTilt = new Vector3(-25f, 0f, 0f);
    [SerializeField] private Vector3 cleaveStrikeTilt = new Vector3(35f, 0f, 0f);
    [SerializeField] private float cleaveStrikeDistance = 0.6f;

    [Header("Parry / Spin")]
    [SerializeField] private float parryDuration = 0.45f;
    [SerializeField] private float parrySpinDegrees = 360f;
    [SerializeField] private Vector3 parryHopOffset = new Vector3(0f, 0.25f, 0f);

    [Header("Hurt / Recoil Settings")]
    [SerializeField] private float recoilDistance = 0.4f;
    [SerializeField] private float recoilDuration = 0.15f;
    [SerializeField] private float wobbleShakeStrength = 10f;
    [SerializeField] private float recoilRecoverDuration = 0.2f;
    [SerializeField] private int wobbleOscillations = 4;

    [Header("Clash Settings")]
    [SerializeField] private float clashApproachDuration = 0.14f;
    [SerializeField] private float clashBounceDuration = 0.22f;
    [SerializeField] private float clashOvershoot = 0.15f;

    private bool _busy;
    private Coroutine _actionRoutine;
    private Vector3 _bobOffset;

    public bool IsBusy => _busy;
    public Vector3 IdlePosition => idlePosition;
    public Vector3 AttackDirection => attackDirection.sqrMagnitude > 0.0001f
        ? attackDirection.normalized
        : Vector3.right;

    private void Reset()
    {
        cardSprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (cardSprite == null)
        {
            cardSprite = GetComponentInChildren<SpriteRenderer>();
        }

        ApplyIdlePose(includeBob: false);
        ApplyDefaultColor();
    }

    private void Update()
    {
        if (_busy)
        {
            return;
        }

        float bobY = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        _bobOffset = new Vector3(0f, bobY, 0f);
        ApplyIdlePose(includeBob: true);
    }

    public void ApplyIdlePose(bool includeBob)
    {
        Vector3 position = idlePosition;
        if (includeBob)
        {
            position += _bobOffset;
        }

        transform.localPosition = position;
        transform.localRotation = Quaternion.Euler(idleRotation);
        transform.localScale = idleScale;
    }

    public void PlayLightThrust()
    {
        StartAction(LightThrustRoutine());
    }

    public void PlayOverheadCleave()
    {
        StartAction(OverheadCleaveRoutine());
    }

    public void PlayParrySpin()
    {
        StartAction(ParrySpinRoutine());
    }

    public void PlayTakeHit()
    {
        StartAction(TakeHitRoutine());
    }

    public void PlayClashToward(Vector3 worldClashPoint)
    {
        StartAction(ClashRoutine(worldClashPoint));
    }

    public void StopActionsAndReset()
    {
        if (_actionRoutine != null)
        {
            StopCoroutine(_actionRoutine);
            _actionRoutine = null;
        }

        _busy = false;
        ApplyDefaultColor();
        ApplyIdlePose(includeBob: false);
    }

    private void StartAction(IEnumerator routine)
    {
        if (_actionRoutine != null)
        {
            StopCoroutine(_actionRoutine);
        }

        _actionRoutine = StartCoroutine(RunAction(routine));
    }

    private IEnumerator RunAction(IEnumerator routine)
    {
        _busy = true;
        yield return routine;
        ApplyDefaultColor();
        ApplyIdlePose(includeBob: false);
        _busy = false;
        _actionRoutine = null;
    }

    private IEnumerator LightThrustRoutine()
    {
        ApplyTelegraphColor();

        Vector3 startPos = idlePosition;
        Quaternion startRot = Quaternion.Euler(idleRotation);
        Vector3 endPos = idlePosition + AttackDirection * lungeDistance;
        Quaternion endRot = Quaternion.Euler(idleRotation + attackTilt);

        yield return TweenTransform(startPos, endPos, startRot, endRot, idleScale, idleScale, lungeDuration, EaseOutCubic);
        yield return TweenTransform(endPos, startPos, endRot, startRot, idleScale, idleScale, recoveryDuration, EaseInOutCubic);
    }

    private IEnumerator OverheadCleaveRoutine()
    {
        ApplyTelegraphColor();

        Vector3 startPos = idlePosition;
        Quaternion startRot = Quaternion.Euler(idleRotation);
        Quaternion windupRot = Quaternion.Euler(idleRotation + cleaveWindupTilt);
        Vector3 strikePos = idlePosition + AttackDirection * cleaveStrikeDistance;
        Quaternion strikeRot = Quaternion.Euler(idleRotation + cleaveStrikeTilt);

        yield return TweenTransform(startPos, startPos, startRot, windupRot, idleScale, idleScale, cleaveWindupDuration, EaseOutCubic);
        yield return TweenTransform(startPos, strikePos, windupRot, strikeRot, idleScale, idleScale, cleaveStrikeDuration, EaseInCubic);
        yield return TweenTransform(strikePos, startPos, strikeRot, startRot, idleScale, idleScale, recoveryDuration, EaseInOutCubic);
    }

    private IEnumerator ParrySpinRoutine()
    {
        ApplyTelegraphColor();

        Vector3 startPos = idlePosition;
        Vector3 hopPos = idlePosition + parryHopOffset;
        Quaternion startRot = Quaternion.Euler(idleRotation);
        Vector3 spinAxis = Vector3.up;

        float elapsed = 0f;
        while (elapsed < parryDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, parryDuration));
            float eased = EaseInOutCubic(t);

            transform.localPosition = Vector3.Lerp(startPos, hopPos, Mathf.Sin(t * Mathf.PI));
            transform.localRotation = startRot * Quaternion.AngleAxis(parrySpinDegrees * eased, spinAxis);
            transform.localScale = idleScale;
            yield return null;
        }

        transform.localPosition = startPos;
        transform.localRotation = startRot;
        transform.localScale = idleScale;
    }

    private IEnumerator TakeHitRoutine()
    {
        Vector3 startPos = idlePosition;
        Quaternion startRot = Quaternion.Euler(idleRotation);
        Vector3 recoilPos = idlePosition - AttackDirection * recoilDistance;

        float elapsed = 0f;
        while (elapsed < recoilDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, recoilDuration));
            float shake = Mathf.Sin(t * Mathf.PI * wobbleOscillations) * wobbleShakeStrength * (1f - t);

            transform.localPosition = Vector3.Lerp(startPos, recoilPos, EaseOutCubic(t));
            transform.localRotation = startRot * Quaternion.Euler(0f, 0f, shake);
            transform.localScale = idleScale;
            yield return null;
        }

        yield return TweenTransform(
            recoilPos,
            startPos,
            transform.localRotation,
            startRot,
            idleScale,
            idleScale,
            recoilRecoverDuration,
            EaseInOutCubic);
    }

    private IEnumerator ClashRoutine(Vector3 worldClashPoint)
    {
        ApplyTelegraphColor();

        Vector3 startPos = idlePosition;
        Quaternion startRot = Quaternion.Euler(idleRotation);
        Vector3 localClash = transform.parent != null
            ? transform.parent.InverseTransformPoint(worldClashPoint)
            : worldClashPoint;

        Vector3 approachPos = localClash + AttackDirection * clashOvershoot;
        Quaternion clashRot = Quaternion.Euler(idleRotation + attackTilt);
        Vector3 bouncePos = idlePosition - AttackDirection * recoilDistance;

        yield return TweenTransform(startPos, approachPos, startRot, clashRot, idleScale, idleScale, clashApproachDuration, EaseOutCubic);
        yield return TweenTransform(approachPos, bouncePos, clashRot, startRot, idleScale, idleScale, clashBounceDuration * 0.5f, EaseOutCubic);
        yield return TweenTransform(bouncePos, startPos, startRot, startRot, idleScale, idleScale, clashBounceDuration * 0.5f, EaseInOutCubic);
    }

    private IEnumerator TweenTransform(
        Vector3 fromPos,
        Vector3 toPos,
        Quaternion fromRot,
        Quaternion toRot,
        Vector3 fromScale,
        Vector3 toScale,
        float duration,
        System.Func<float, float> ease)
    {
        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float e = ease(t);

            transform.localPosition = Vector3.LerpUnclamped(fromPos, toPos, e);
            transform.localRotation = Quaternion.SlerpUnclamped(fromRot, toRot, e);
            transform.localScale = Vector3.LerpUnclamped(fromScale, toScale, e);
            yield return null;
        }

        transform.localPosition = toPos;
        transform.localRotation = toRot;
        transform.localScale = toScale;
    }

    private void ApplyDefaultColor()
    {
        if (cardSprite != null)
        {
            cardSprite.color = defaultColor;
        }
    }

    private void ApplyTelegraphColor()
    {
        if (cardSprite != null)
        {
            cardSprite.color = telegraphColor;
        }
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    private static float EaseInCubic(float t)
    {
        return t * t * t;
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && _busy)
        {
            return;
        }

        if (cardSprite == null)
        {
            cardSprite = GetComponentInChildren<SpriteRenderer>();
        }

        ApplyIdlePose(includeBob: false);
        ApplyDefaultColor();
    }
#endif
}
