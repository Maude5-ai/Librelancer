// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics.CodeAnalysis;
using LibreLancer.Interface;

namespace LibreLancer.Input
{
	public class InputManager : IDisposable
	{
		public event Action<InputAction>? ActionDown;
		public event Action<InputAction>? ActionUp;

        private Game game;

        private InputMap map;
        private bool[] _isActionDown;
        // Bindings that have fired ActionDown and not yet been released.
        // ActionUp must be matched by base key alone: the modifier half of a
        // chord can be released first, or chat can grab keyboard focus while
        // the key is held, and the action still has to come back up (#411).
        private UserInput[] _eventDown;

        public KeyCaptureContext KeyCapture = null!;

		public InputManager(Game game, InputMap map)
        {
            this.map = map;
            _isActionDown = new bool[(int) InputAction.COUNT];
            _eventDown = new UserInput[(int) InputAction.COUNT];
            game.Keyboard.KeyDown += Keyboard_KeyDown;
			game.Keyboard.KeyUp += Keyboard_KeyUp;
            game.Mouse.MouseDown += Mouse_MouseDown;
            game.Mouse.MouseUp += Mouse_MouseUp;
			this.game = game;
        }

        private bool IsDown(UserInput check)
        {
            if (!check.NonEmpty) return false; // Empty = nothing to check
            if (game.TextInputEnabled && !AllowedWhenTextInput(check))
                return false;
            if (check.IsMouseButton) {
                return game.Mouse.IsButtonDown(check.Mouse);
            }
            else {
                if (!game.Keyboard.IsKeyDown(check.Key)) return false;
                // Chord bindings are distinct: Shift+W must not activate a
                // plain W binding and vice versa (#411). A modifier bound as
                // the base key is exempt from its own chord family.
                const KeyModifiers chord = KeyModifiers.Shift | KeyModifiers.Control | KeyModifiers.Alt;
                var mask = chord & ~OwnChordFamily(check.Key);
                return (HeldChordModifiers() & mask) == (check.Modifiers & mask);
            }
        }

        private KeyModifiers HeldChordModifiers()
        {
            var kb = game.Keyboard;
            var mod = KeyModifiers.None;
            if (kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift))
                mod |= KeyModifiers.Shift;
            if (kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl))
                mod |= KeyModifiers.Control;
            if (kb.IsKeyDown(Keys.LeftAlt) || kb.IsKeyDown(Keys.RightAlt))
                mod |= KeyModifiers.Alt;
            return mod;
        }

        private static KeyModifiers OwnChordFamily(Keys key)
        {
            switch (key)
            {
                case Keys.LeftShift:
                case Keys.RightShift:
                    return KeyModifiers.Shift;
                case Keys.LeftControl:
                case Keys.RightControl:
                    return KeyModifiers.Control;
                case Keys.LeftAlt:
                case Keys.RightAlt:
                    return KeyModifiers.Alt;
                default:
                    return KeyModifiers.None;
            }
        }

		public void Update()
		{
            if (KeyCaptureContext.Capturing(KeyCapture))
            {
                for (int i = 0; i < map.Actions.Length; i++)
                {
                    _isActionDown[i] = false;
                }
            }
            else
            {
                for (int i = 0; i < map.Actions.Length; i++)
                {
                    _isActionDown[i] = IsDown(map.Actions[i].Primary) ||
                                       IsDown(map.Actions[i].Secondary);
                }
            }
        }

		public bool IsActionDown(InputAction action)
        {
            return _isActionDown[(int)action];
        }

        private bool TryGetAction(UserInput input, out InputAction act)
        {
            for (int i = 0; i < map.Actions.Length; i++)
            {
                if (input == map.Actions[i].Primary ||
                    input == map.Actions[i].Secondary)
                {
                    act = (InputAction) i;
                    return true;
                }
            }
            act = InputAction.COUNT;
            return false;
        }

        private void Keyboard_KeyDown(KeyEventArgs e)
        {
            if (KeyCaptureContext.Capturing(KeyCapture)) return;
            var input = UserInput.FromKey(e.Modifiers, e.Key);
			if (e.IsRepeat) return;
            if (game.TextInputEnabled && !AllowedWhenTextInput(input))
                return;
            if (TryGetAction(input, out var act))
            {
                _eventDown[(int) act] = input;
                ActionDown?.Invoke(act);
            }
        }

        private static bool AllowedWhenTextInput(UserInput input)
        {
            if (input.IsMouseButton) return true;
            if ((input.Modifiers & KeyModifiers.Control) != 0 ||
                (input.Modifiers & KeyModifiers.Alt) != 0)
                return true;
            switch (input.Key)
            {
                case Keys.LeftControl:
                case Keys.RightControl:
                case Keys.LeftAlt:
                case Keys.RightAlt:
                case Keys.F1:
                case Keys.F2:
                case Keys.F3:
                case Keys.F4:
                case Keys.F5:
                case Keys.F6:
                case Keys.F7:
                case Keys.F8:
                case Keys.F9:
                case Keys.F10:
                case Keys.F11:
                case Keys.F12:
                    return true;
            }
            return false;
        }

        private void Keyboard_KeyUp(KeyEventArgs e)
        {
            if (KeyCaptureContext.Capturing(KeyCapture))
            {
                if(e.Key == Keys.Escape || e.Key == Keys.F1)
                    KeyCapture.Cancel();
                else
                {
                    KeyCapture.Set(UserInput.FromKey(e.Modifiers, e.Key));
                }
            }
            else
            {
                // Releases are matched against armed bindings by base key
                // only, with no text-input gate: modifiers may already be up
                // and chat may have grabbed focus since the press. Only armed
                // actions release, so no spurious ActionUp reaches the
                // toggle-on-release handlers.
                for (int i = 0; i < map.Actions.Length; i++)
                {
                    var armed = _eventDown[i];
                    if (armed.NonEmpty && !armed.IsMouseButton && armed.Key == e.Key)
                    {
                        _eventDown[i] = default;
                        ActionUp?.Invoke((InputAction) i);
                    }
                }
            }
        }

        private void Mouse_MouseUp(MouseEventArgs e)
        {
            if (KeyCaptureContext.Capturing(KeyCapture))
            {
                if(e.Buttons != MouseButtons.Left)
                    KeyCapture.Set(UserInput.FromMouse(e.Buttons));
            }
            else
            {
                var input = UserInput.FromMouse(e.Buttons);
                for (int i = 0; i < map.Actions.Length; i++)
                {
                    if (_eventDown[i] == input)
                    {
                        _eventDown[i] = default;
                        ActionUp?.Invoke((InputAction) i);
                    }
                }
            }
        }

        private void Mouse_MouseDown(MouseEventArgs e)
        {
            if (KeyCaptureContext.Capturing(KeyCapture)) return;
            var input = UserInput.FromMouse(e.Buttons);
            if (TryGetAction(input, out var act))
            {
                _eventDown[(int) act] = input;
                ActionDown?.Invoke(act);
            }
        }

		public void Dispose()
		{
			game.Keyboard.KeyDown -= Keyboard_KeyDown;
			game.Keyboard.KeyUp -= Keyboard_KeyUp;
            game.Mouse.MouseDown -= Mouse_MouseDown;
            game.Mouse.MouseUp -= Mouse_MouseUp;
        }
	}
}
