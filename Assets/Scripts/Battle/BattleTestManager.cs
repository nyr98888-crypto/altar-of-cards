using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard-driven sandbox harness for testing fighter card motion feel.
/// </summary>
public class BattleTestManager : MonoBehaviour
{
    [Header("Fighters")]
    [SerializeField] private FighterCard leftFighter;
    [SerializeField] private FighterCard rightFighter;

    [Header("Clash")]
    [SerializeField] private Vector3 clashPoint = Vector3.zero;
    [SerializeField] private bool blockInputWhileBusy = true;

    [Header("Key Bindings — Left Fighter")]
    [SerializeField] private Key leftThrustKey = Key.A;
    [SerializeField] private Key leftCleaveKey = Key.S;
    [SerializeField] private Key leftParryKey = Key.D;
    [SerializeField] private Key leftHitKey = Key.F;

    [Header("Key Bindings — Right Fighter")]
    [SerializeField] private Key rightThrustKey = Key.J;
    [SerializeField] private Key rightCleaveKey = Key.K;
    [SerializeField] private Key rightParryKey = Key.L;
    [SerializeField] private Key rightHitKey = Key.Semicolon;

    [Header("Key Bindings — Global")]
    [SerializeField] private Key clashKey = Key.Space;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (WasPressed(keyboard, leftThrustKey) && CanAct(leftFighter))
        {
            leftFighter.PlayLightThrust();
        }

        if (WasPressed(keyboard, leftCleaveKey) && CanAct(leftFighter))
        {
            leftFighter.PlayOverheadCleave();
        }

        if (WasPressed(keyboard, leftParryKey) && CanAct(leftFighter))
        {
            leftFighter.PlayParrySpin();
        }

        if (WasPressed(keyboard, leftHitKey) && CanAct(leftFighter))
        {
            leftFighter.PlayTakeHit();
        }

        if (WasPressed(keyboard, rightThrustKey) && CanAct(rightFighter))
        {
            rightFighter.PlayLightThrust();
        }

        if (WasPressed(keyboard, rightCleaveKey) && CanAct(rightFighter))
        {
            rightFighter.PlayOverheadCleave();
        }

        if (WasPressed(keyboard, rightParryKey) && CanAct(rightFighter))
        {
            rightFighter.PlayParrySpin();
        }

        if (WasPressed(keyboard, rightHitKey) && CanAct(rightFighter))
        {
            rightFighter.PlayTakeHit();
        }

        if (WasPressed(keyboard, clashKey) && CanClash())
        {
            PlayClash();
        }
    }

    private bool CanAct(FighterCard fighter)
    {
        if (fighter == null)
        {
            return false;
        }

        return !blockInputWhileBusy || !fighter.IsBusy;
    }

    private bool CanClash()
    {
        if (leftFighter == null || rightFighter == null)
        {
            return false;
        }

        if (!blockInputWhileBusy)
        {
            return true;
        }

        return !leftFighter.IsBusy && !rightFighter.IsBusy;
    }

    private void PlayClash()
    {
        leftFighter.PlayClashToward(clashPoint);
        rightFighter.PlayClashToward(clashPoint);
    }

    private static bool WasPressed(Keyboard keyboard, Key key)
    {
        return keyboard[key].wasPressedThisFrame;
    }
}
