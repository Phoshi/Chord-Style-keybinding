using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Forms;

namespace KeyBindings {

    /// <summary>
    /// A class to handle key bindings in a vim-like fashion, supporting key sequences of arbitrary size as well as key sequences which are exact subsets of longer 
    /// sequences, executable by hitting "return" after typing them.
    /// </summary>
    public class KeyBindings {
        /// <summary>
        /// Stores the list of keybindings for this instance
        /// </summary>
        List<KeyBinding> keyList = new List<KeyBinding>();
        /// <summary>
        /// Stores the current list of pressed keys
        /// </summary>
        List<Keys> currentInput = new List<Keys>();
        /// <summary>
        /// A list of keys to not add to the list of pressed keys - this is mostly the actual modifier keys.
        /// </summary>
        readonly List<Keys> toIgnore = new List<Keys> {
                                                   Keys.Shift | Keys.ShiftKey,
                                                   Keys.Control | Keys.ControlKey,
                                                   Keys.Alt | Keys.Menu, 
                                                   Keys.Return
                                              };

        public KeyBindings() {
        }

        public KeyBindings(Control parent) {
            parent.KeyDown += new KeyEventHandler(handleKeyDown);
        }

        public KeyBindings(Form parent) {
            parent.KeyPreview = true;
            parent.KeyDown += new KeyEventHandler(handleKeyDown);
        }

        void handleKeyDown(object sender, KeyEventArgs e) {
            if (ProcessKey(e.KeyData)) {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Main method, processes the passed `Keys` and decides what to do. Will execute a command if the keysequence is complete.
        /// </summary>
        /// <param name="keyData">A Keys instance containing the pressed keys</param>
        /// <returns>A bool containing whether the keypresses were handled by this instance.</returns>
        public bool ProcessKey(Keys keyData) {
            if (!toIgnore.Contains(keyData)) {
                currentInput.Add(keyData);
            }
            bool isStillValidInput = false;
            List<KeyBinding> potentialBinds = new List<KeyBinding>();

            foreach (KeyBinding bind in keyList) {
                if (bind.binding.SequenceEqual(currentInput)) {
                    potentialBinds.Insert(0, bind);
                }
                else if (bind.binding.StartsWith(currentInput)) {
                    potentialBinds.Add(bind);
                    isStillValidInput = true;
                }
            }
            if ((potentialBinds.Count == 1 && !isStillValidInput) || keyData==Keys.Return) {
                potentialBinds[0].Execute();
                currentInput.Clear();
                isStillValidInput = true;
            }
            if (!isStillValidInput) {
                currentInput.Clear();
            }
            OnKeyUpdate(this, new KeyUpdateEventArgs(currentInput));
            return isStillValidInput;
        }

        /// <summary>
        /// Returns a formatted string of current input, only if input forms part of a key sequence.
        /// </summary>
        /// <returns>String</returns>
        public string GetUnfinishedInput() {
            return currentInput.KeysToString();
        }

        /// <summary>
        /// Binds an Action to a sequence of keys passed as a Keys enumerable 
        /// </summary>
        /// <param name="binding">An enumerable of keys to bind</param>
        /// <param name="toRun">The action to run</param>
        /// <returns>A boolean containing whether the operation was successful.</returns>
        public bool bind(IEnumerable<Keys> binding, Action toRun) {
            KeyBinding newBinding = new KeyBinding(binding, toRun);
            keyList.Add(newBinding);
            return true;
        }

        /// <summary>
        /// Binds an action to a vim-like key sequence (For example, <C-s>f for Control-s, f)
        /// Case is handled - uppercase letters automatically add the Shift modifier
        /// </summary>
        /// <param name="binding">A key sequence to bind to</param>
        /// <param name="toRun">The action to run</param>
        /// <returns>A boolean containing whether the operation was successful.</returns>
        public bool bind(string binding, Action toRun) {
            List<Keys> keys = parseKeysFromString(binding);
            return bind(keys, toRun);
        }

        /// <summary>
        /// An internal method to convert a vim-like key sequence string into a list of Keys instances
        /// </summary>
        /// <param name="binding">The string to convert</param>
        /// <returns>The converted list</returns>
        private List<Keys> parseKeysFromString(string binding) {
            List<Keys> newList = new List<Keys>();
            string currentString = "";
            bool inCombo = false;
            for (int i = 0; i < binding.Length; i++) {
                char currentCharacter = binding[i];
                if (currentCharacter == '<') {
                    inCombo = true;
                }
                if (!inCombo) {
                    currentString = "";
                }
                currentString += currentCharacter;
                if (currentCharacter == '>') {
                    inCombo = false;
                }
                if (!inCombo) {
                    if (currentString.Length == 1) {
                        Keys key;
                        if (Keys.TryParse(currentString, true, out key)) {
                            if (currentString == currentString.ToUpper()) {
                                key = key | Keys.Shift;
                            }
                            newList.Add(key);
                        }
                        else {
                            throw new KeyBindingException("Unrecognised Letter: " + currentString);
                        }
                    }
                    else if (currentString.Length == 5) {
                        if (currentString.StartsWith("<") && currentString.EndsWith(">")) {
                            Keys modifier;
                            if (currentString.StartsWith("<C-")) {
                                modifier = Keys.Control;
                            }
                            else if (currentString.StartsWith("<S-")) {
                                modifier = Keys.Shift;
                            }
                            else if (currentString.StartsWith("<A-")) {
                                modifier = Keys.Alt;
                            }
                            else {
                                throw new KeyBindingException("Unrecognised Modifier");
                            }
                            Keys key;
                            if (Keys.TryParse(currentString[3].ToString(), true, out key)) {
                                if (currentString == currentString.ToUpper()) {
                                    key = key | Keys.Shift;
                                }
                                newList.Add(modifier | key);
                                currentString = "";
                            }
                            else {
                                throw new KeyBindingException("Unrecognised Letter: "+currentString[3]);
                            }
                        }
                    }
                }
            }
            return newList;
        }

        public delegate void KeyUpdateHandler(object sender, KeyUpdateEventArgs data);

        public event KeyUpdateHandler KeyUpdate;

        protected void OnKeyUpdate(object sender, KeyUpdateEventArgs data) {
            if (KeyUpdate != null) {
                KeyUpdate(this, data);
            }
        }
    }

    /// <summary>
    /// A class to hold a specific key binding and the action it should fire. Does not perform any logic itself.
    /// </summary>
    class KeyBinding {
        private IEnumerable<Keys> keyList;
        private Action onFire;

        /// <summary>
        /// Gets the list of keys the action is bound to
        /// Read-Only
        /// </summary>
        public IEnumerable<Keys> binding { get { return keyList; } }

        /// <summary>
        /// Creates a new KeyBinding instance
        /// </summary>
        /// <param name="keys">List of keys to bind to</param>
        /// <param name="action">Action to fire</param>
        public KeyBinding(IEnumerable<Keys> keys, Action action) {
            keyList = keys;
            onFire = action;
        }

        /// <summary>
        /// Executes the action
        /// </summary>
        public void Execute() {
            onFire();
        }
    }

    public class KeyUpdateEventArgs : EventArgs {
        public List<Keys> newKeyList { get; internal set; }
        public KeyUpdateEventArgs(List<Keys> newList) {
            newKeyList = newList;
        }
    }


    [Serializable]
    public class KeyBindingException : Exception {
        public KeyBindingException() {
        }

        public KeyBindingException(string message) : base(message) {
        }

        public KeyBindingException(string message, Exception inner) : base(message, inner) {
        }

        protected KeyBindingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {
        }
    }

    static class VimExtensionMethods {
        /// <summary>
        /// Converts an Enumerable of Keys to a string representation
        /// Control is ^
        /// Shift is represented by capital letters
        /// Alt is !
        /// </summary>
        /// <param name="list">The list to convert</param>
        /// <returns>The string representing that sequence</returns>
        static public string KeysToString(this IEnumerable<Keys> list) {
            StringBuilder input = new StringBuilder();
            foreach (Keys key in list) {
                if ((key & Keys.Control) == Keys.Control) {
                    input.Append("^");
                    input.Append((key ^ Keys.Control).ToString().ToLower());
                }
                else if ((key & Keys.Shift) == Keys.Shift) {
                    input.Append((key ^ Keys.Shift).ToString());
                }
                else if ((key & Keys.Alt) == Keys.Alt) {
                    input.Append("!");
                    input.Append((key ^ Keys.Alt).ToString().ToLower());
                }
                else {
                    input.Append(key.ToString().ToLower());
                }
            }
            return input.ToString();
        }
    }
}
