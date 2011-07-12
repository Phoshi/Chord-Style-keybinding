using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KeyBindings {
    public static class ExtensionMethods {

        /// <summary>
        /// Scrolls a list view up or down 
        /// </summary>
        /// <param name="listView">The list view to scroll</param>
        /// <param name="howMuch">How much to scroll. Use negative values to go up.</param>
        public static void scroll(this ListView listView, int howMuch) {
            int selectedIndex;
            if (listView.SelectedIndices.Count > 0) {
                selectedIndex = listView.SelectedIndices[listView.SelectedIndices.Count - 1];
                listView.Items[selectedIndex].Selected = false;
            }
            else {
                selectedIndex = howMuch > 0 ? -1 : listView.Items.Count;
            }
            if (selectedIndex + howMuch < listView.Items.Count && selectedIndex + howMuch >= 0) {
                listView.Items[selectedIndex + howMuch].Selected = true;
                listView.Items[selectedIndex + howMuch].EnsureVisible();
            }
        }

        /// <summary>
        /// Scrolls a list box up or down 
        /// </summary>
        /// <param name="listBox">The list view to scroll</param>
        /// <param name="howMuch">How much to scroll. Use negative values to go up.</param>
        public static void scroll(this ListBox listBox, int howMuch) {
            int selectedIndex;
            if (listBox.SelectedIndices.Count > 0) {
                selectedIndex = listBox.SelectedIndices[listBox.SelectedIndices.Count - 1];
            }
            else {
                selectedIndex = howMuch > 0 ? -1 : listBox.Items.Count;
            }
            if (selectedIndex + howMuch < listBox.Items.Count && selectedIndex + howMuch >= 0) {
                listBox.SelectedIndex = selectedIndex + howMuch;
            }
        }

        /// <summary>
        /// Returns if the list Starts With the contents of another list
        /// Both lists must be the same type, and the type must implement .Equals
        /// </summary>
        /// <typeparam name="T">The Type of List to compare</typeparam>
        /// <param name="list">The list to compare</param>
        /// <param name="otherList">The list to check if the other list starts with this.</param>
        /// <returns></returns>
        static public bool StartsWith<T>(this IEnumerable<T> list, IEnumerable<T> otherList) {
            if (otherList.Count() > list.Count()) {
                return false;
            }
            for (int i = 0; i < otherList.Count(); i++) {
                if (!list.ElementAt(i).Equals(otherList.ElementAt(i))) {
                    return false;
                }
            }
            return true;
        }

        static public string With(this string toFormat, params string[] formatWith) {
            return String.Format(toFormat, formatWith);
        }
    }
}
