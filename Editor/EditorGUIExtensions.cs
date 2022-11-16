using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

public sealed class EditorGUIExtensions {

	private static List<string> AutoCompleteCache = new List<string> ();
	private static Rect AutoCompletePopupRect;
	private static string AutoCompletedString;
	private static AutoCompletePopupWindow AutoCompleteWindow;

	/// <summary>A textField to popup a matching popup, based on developers input values.</summary>
	/// <param name="input">string input.</param>
	/// <param name="stringOptions">the data of all possible values (string).</param>
	/// <param name="maxShownCount">the amount to display result.</param>
	/// <param name="levenshteinDistance">
	/// value between 0f ~ 1f,
	/// - more then 0f will enable the fuzzy matching
	/// - 1f = anything thing is okay.
	/// - 0f = require full match to the reference
	/// - recommend 0.4f ~ 0.7f
	/// </param>
	/// <returns>output string.</returns>
	public static string TextFieldAutoComplete (Rect position, string input, string [] stringOptions, params GUILayoutOption [] options) {
		EditorGUI.BeginChangeCheck ();
		if (!string.IsNullOrEmpty (AutoCompletedString)) {
			input = AutoCompletedString;
			AutoCompletedString = string.Empty;
		}
		var textFieldName = "AutoComplete" + GUIUtility.GetControlID (FocusType.Keyboard);
		GUI.SetNextControlName (textFieldName);
		var editedString = EditorGUILayout.TextField (input, options);
		if (EditorGUI.EndChangeCheck ()) {
			AutoCompleteCache.Clear ();
			if (editedString.Length > 1) {
				AutoCompleteCache.Clear ();
				AutoCompleteCache.Capacity = Mathf.Max (AutoCompleteCache.Capacity, stringOptions.Length);
				for (int i = 0; i < stringOptions.Length; i++) {
					if (stringOptions [i].Contains (editedString)) {
						AutoCompleteCache.Add (stringOptions [i]);
					}
				}
				AutoCompletedString = string.Empty;

				if (AutoCompleteWindow == null) {
					/*AutoCompleteWindow = ScriptableObject.CreateInstance<AutoCompletePopupWindow> ();
					AutoCompleteWindow.position = GUILayoutUtility.GetLastRect ();
					var showWithMode = typeof (EditorWindow).GetMethod ("ShowPopupWithMode", BindingFlags.Instance | BindingFlags.NonPublic);
					var popupMenu = 1;//ShowMode.PopupMenu;
					showWithMode.Invoke (AutoCompleteWindow, new object [] { popupMenu, false });*/
					AutoCompleteWindow = EditorWindow.GetWindowWithRect<AutoCompletePopupWindow> (GUILayoutUtility.GetLastRect (), false, string.Empty, false);
				}
			} else {
				if (AutoCompleteWindow != null) {
					AutoCompleteWindow.Close ();
					AutoCompleteWindow = null;
				}
			}
		}
		if (Event.current.type == EventType.Repaint) AutoCompletePopupRect = GUILayoutUtility.GetLastRect ();
		return editedString;
	}

	public static string TextFieldAutoComplete (string input, string [] stringOptions, params GUILayoutOption [] options) {
		return TextFieldAutoComplete (EditorGUILayout.GetControlRect (), input, stringOptions, options);
	}

	public class AutoCompletePopupWindow : EditorWindow {

		public Vector2 GetWindowSize () {
			return new Vector2 (AutoCompletePopupRect.width, GetNumLines () * EditorGUIUtility.singleLineHeight);
		}

		private int GetNumLines () {
			var cnt = AutoCompleteCache.Count;
			return cnt;
		}

		void OnGUI () {
			var cnt = GetNumLines ();
			var line = new Rect (0, 0, AutoCompletePopupRect.width, EditorGUIUtility.singleLineHeight);
			var optionWasChosen = false;

			for (int i = 0; i < cnt; i++) {
				if (GUI.Button (line, AutoCompleteCache [i], EditorStyles.label)) {
					Event.current.Use ();
					AutoCompletedString = AutoCompleteCache [i];
					GUI.changed = true;
					GUI.FocusControl (string.Empty); // force update
					optionWasChosen = true;
				}
				line.y += line.height;
			}

			if (optionWasChosen) {
				Close ();
				AutoCompleteCache.Clear ();
			}
		}
	}

	/// <summary>Computes the Levenshtein Edit Distance between two strings.</summary>
	/// <returns>The edit distance.</returns>
	/// <see cref="https://en.wikipedia.org/wiki/Levenshtein_distance"/>
	public static int LevenshteinDistance (string s, string t, bool caseSensitive = true) {
		if (!caseSensitive) {
			s = s.ToLower ();
			t = t.ToLower ();
		}

		// Get the length of both.  If either is 0, return
		// the length of the other, since that number of insertions
		// would be required.
		int m = s.Length, n = t.Length;
		if (n == 0) return m;
		if (m == 0) return n;

		// Rather than maintain an entire matrix (which would require O(n*m) space),
		// just store the current row and the next row, each of which has a length m+1,
		// so just O(m) space. Initialize the current row.
		int v0 = 0, v1 = 1;

		var rows = new int [] [] { new int [n + 1], new int [n + 1] };
		for (int i = 0; i <= n; i++)
			rows [v0] [i] = i;

		// For each virtual row (since we only have physical storage for two)
		for (int i = 0; i < m - 1; i++) {
			// Fill in the values in the row
			rows [v1] [0] = i + 1;

			for (int j = 0; j < n - 1; j++) {
				int dist1 = rows [v0] [j + 1] + 1;
				int dist2 = rows [v1] [j] + 1;
				int dist3 = rows [v0] [j] + (s [i].Equals (t [j]) ? 0 : 1);

				rows [v1] [j + 1] = Mathf.Min (dist1, Mathf.Min (dist2, dist3));
			}

			// Swap the current and next rows
			v0 ^= 1;
			v1 ^= 1;
		}

		// Return the computed edit distance
		return rows [v0] [n];
	}
}
