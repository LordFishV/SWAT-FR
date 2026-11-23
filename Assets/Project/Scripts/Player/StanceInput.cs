using UnityEngine;

// How a stance button behaves:
//   Toggle: press once to enter the stance, press again to leave it.
//   Hold:   you are in the stance only while the button is down.
public enum StanceInputType { Toggle, Hold }

// What a button did this frame. Filled in by Player.cs.
public struct ButtonState {
    public bool Pressed;    // went down this frame
    public bool Held;       // is down right now
    public bool Released;   // went up this frame
}

public static class StanceInput
{
    // Works out which stance the player is asking for from this frame's buttons.
    // PlayerCharacter stores the result in _requestedStance.
    public static Stance Resolve(Stance requested, PlayerInput input,
        StanceInputType runMode, StanceInputType crouchMode) {

        // The most recent press wins if two buttons are pressed together
        requested = Apply(requested, Stance.Run, input.Run, runMode);
        requested = Apply(requested, Stance.Crouch, input.Crouch, crouchMode);

        // If we ended up standing but a hold button is still down (for example after
        // releasing crouch while still holding run, or after a jump), go back to it.
        if (requested == Stance.Stand) {
            if (runMode == StanceInputType.Hold && input.Run.Held)
                requested = Stance.Run;
            else if (crouchMode == StanceInputType.Hold && input.Crouch.Held)
                requested = Stance.Crouch;
        }

        return requested;
    }

    private static Stance Apply(Stance requested, Stance stance, ButtonState button, StanceInputType mode) {
        if (mode == StanceInputType.Toggle) {
            // Press to enter, press again to go back to standing
            if (button.Pressed)
                return requested == stance ? Stance.Stand : stance;
        } else {
            // Enter on press. Leave on release, but only if this button's stance is still the
            // requested one (another button may have taken over in the meantime)
            if (button.Pressed)
                return stance;
            if (button.Released && requested == stance)
                return Stance.Stand;
        }

        return requested;
    }
}