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

        /// <summary>
        /// Description of the KeyBindings object
        /// </summary>
        public string description { get; internal set; }

        /// <summary>
        /// Initiliases a new keybindings instance and doesn't hook into the keypress event of any control
        /// You will have to call ProcessKey yourself
        /// </summary>
        /// <param name="ndescription">Description of the keybinding object</param>
        public KeyBindings(string ndescription = null) {
            description = ndescription;
        }

        /// <summary>
        /// Initialises a new keybindings instance and hooks into the keypress event of the given control
        /// </summary>
        /// <param name="parent">The control to hook into</param>
        /// <param name="ndescription">Description of the keybinding object</param>
        public KeyBindings(Control parent, string ndescription = null) {
            description = ndescription;
            hook(parent);
        }

        /// <summary>
        /// Initialises a new keybindings instance and hooks into the keypress event of the given form.
        /// NOTE: Toggles the form's KeyPreview parameter on
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="ndescription">Description of the keybinding object</param>
        public KeyBindings(Form parent, string ndescription = null) {
            description = ndescription;
            hook(parent);
        }

        /// <summary>
        /// Hooks into the KeyDown event of this control
        /// </summary>
        /// <param name="control">The control to hook into</param>
        /// <param name="ndescription">Description of the keybinding object</param>
        public void hook(Control control, string ndescription = null) {
            control.KeyDown += handleKeyDown;
        }

        /// <summary>
        /// Hooks into the KeyDown event of this form, and toggles KeyPreview on
        /// </summary>
        /// <param name="form">The form to hook into</param>
        public void hook(Form form) {
            form.KeyPreview = true;
            form.KeyDown += handleKeyDown;
        }

        /// <summary>
        /// Handles the keyDown event for hooked objects
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void handleKeyDown(object sender, KeyEventArgs e) {
            if (ProcessKey(e.KeyData)) {
                e.Handled = true;
                e.SuppressKeyPress = true;
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
        /// <param name="description">A description of the command</param>
        /// <returns>A boolean containing whether the operation was successful.</returns>
        public bool bind(IEnumerable<Keys> binding, Action toRun, string description = null) {
            KeyBinding newBinding = new KeyBinding(binding, toRun, description);
            keyList.Add(newBinding);
            return true;
        }

        /// <summary>
        /// Binds an action to a vim-like key sequence (For example, <C-s>f for Control-s, f)
        /// Case is handled - uppercase letters automatically add the Shift modifier
        /// </summary>
        /// <param name="binding">A key sequence to bind to</param>
        /// <param name="toRun">The action to run</param>
        /// <param name="description">A description of the command</param>
        /// <returns>A boolean containing whether the operation was successful.</returns>
        public bool bind(string binding, Action toRun, string description = null) {
            List<Keys> keys = parseKeysFromString(binding);
            return bind(keys, toRun, description);
        }

        /// <summary>
        /// Returns the Key Binding object for that binding.
        /// </summary>
        /// <param name="keySequence">The string to get the command for</param>
        /// <returns>The KeyBinding object</returns>
        public KeyBinding GetBinding(string keySequence) {
            List<Keys> keysList = parseKeysFromString(keySequence);
            return GetBinding(keysList);
        }

        /// <summary>
        /// Returns the Key Binding object for that binding
        /// </summary>
        /// <param name="keySequence">The Enumerable of Keys to get the command for</param>
        /// <returns>The KeyBinding object</returns>
        public KeyBinding GetBinding(IEnumerable<Keys> keySequence) {
            IEnumerable<KeyBinding> bindings = (from key in keyList where key.binding.SequenceEqual(keySequence) select key);
            if (bindings.Count() == 0) {
                throw new KeyBindingException("No command is bound to sequence {0}".With(keySequence.KeysToString()));
            }
            return bindings.ElementAt(0);
        }

        public IEnumerable<IEnumerable<Keys>> GetAllKeyBindings() {
            return from key in keyList select key.binding;
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

        /// <summary>
        /// An event that fires every time the instance runs a key update. KeyUpdateEventArgs contains the current input.
        /// </summary>
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
    public class KeyBinding {
        private IEnumerable<Keys> keyList;
        private Action onFire;
        public string description { get; internal set; }

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
        public KeyBinding(IEnumerable<Keys> keys, Action action, string newDescription = null) {
            keyList = keys;
            onFire = action;
            description = newDescription;
        }

        /// <summary>
        /// Executes the action
        /// </summary>
        public void Execute() {
            onFire();
        }
    }

    /// <summary>
    /// EventArgs object that holds the current input of the KeyBindings instance
    /// </summary>
    public class KeyUpdateEventArgs : EventArgs {
        public List<Keys> newKeyList { get; internal set; }
        public KeyUpdateEventArgs(List<Keys> newList) {
            newKeyList = newList;
        }
    }

    /// <summary>
    /// Exception to throw if anything goes wrong at bind-time
    /// </summary>
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

    /// <summary>
    /// Extension methods that would only be useful in this project
    /// </summary>
    public static class KeyBindingExtensionMethods {
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
