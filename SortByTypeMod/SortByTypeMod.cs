using System;

using ScrollsModLoader.Interfaces;
using UnityEngine;
using Mono.Cecil;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

namespace SortByTypeMod
{
	public class SortByTypeMod : ScrollsModLoader.Interfaces.BaseMod
	{
		int myFilterNumber = -1;
		FieldInfo collectionSorterField;
		FieldInfo currentSortModeField;
		FieldInfo buttonGroupItemsField;
		Dictionary<string, MethodInfo> deckSorterMethods = new Dictionary<string, MethodInfo> ();
		private List<string> requiredMethods = new List<string>(new string[]{"clear", "byName", "byLevel", "byType", "byResourceCount", "byColor"});


		object decksorter;
		public SortByTypeMod ()
		{
			collectionSorterField = typeof(DeckBuilder2).GetField ("collectionSorter", BindingFlags.Instance | BindingFlags.NonPublic);
			currentSortModeField = typeof(DeckBuilder2).GetField ("currentSortMode", BindingFlags.Instance | BindingFlags.NonPublic);
			buttonGroupItemsField = typeof(ButtonGroup).GetField ("items", BindingFlags.Instance | BindingFlags.NonPublic);

			Type t = typeof(DeckBuilder2).Assembly.GetType ("DeckSorter");
			//Just get all the Methods.
			MethodInfo[] methods = t.GetMethods (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (MethodInfo m in methods) {
				if (!deckSorterMethods.ContainsKey (m.Name)) {
					deckSorterMethods.Add (m.Name, m);
				} else {
					Console.WriteLine ("Ignoring " + m + "in favor of " + deckSorterMethods [m.Name]);
				}
			}
			//This fails because == does not exist/fails for FieldInfo - WTF?!
			/*if (collectionSorterField == null || currentSortModeField == null || buttonGroupItemsField == null) {
				Console.WriteLine ("Could not get all required fields");
				throw new Exception ("Could not find fields - propably an version-conflict");
			}*/
			foreach (string s in requiredMethods) {
				if (!deckSorterMethods.ContainsKey (s)) {
					Console.WriteLine("Can not find method:" + s);
					throw new Exception("Cound not find all required methods");
				}
			}

			decksorter = Activator.CreateInstance(t);
			deckSorterMethods["byType"].Invoke (decksorter, new object[] { });
			deckSorterMethods["byResourceCount"].Invoke (decksorter, new object[] { });
			deckSorterMethods["byColor"].Invoke (decksorter, new object[] { });
			deckSorterMethods["byName"].Invoke (decksorter, new object[] { });
			deckSorterMethods["byLevel"].Invoke (decksorter, new object[] { });
		}

		public static string GetName ()
		{
			return "SortByType";
		}

		public static int GetVersion ()
		{
			return 1;
		}

		public static MethodDefinition[] GetHooks (TypeDefinitionCollection scrollsTypes, int version)
		{
			MethodDefinition[] method;
			method = scrollsTypes ["DeckBuilder2"].Methods.GetMethod ("Start");
			MethodDefinition dbStart, dbSetSortMode, dbOnTableGUI;
			if (method.Length == 1) {
				dbStart = method [0];
			} else {
				return new MethodDefinition[] { };
			}

			method = scrollsTypes ["DeckBuilder2"].Methods.GetMethod ("setSortMode");
			if (method.Length == 1) {
				dbSetSortMode = method [0];
			} else {
				return new MethodDefinition[] { };
			}

			method = scrollsTypes ["DeckBuilder2"].Methods.GetMethod ("OnGUI_drawTableGUI");
			if (method.Length == 1) {
				dbOnTableGUI = method [0];
			} else {
				Console.WriteLine("Can not find OnGUI_drawTableGUI");
				return new MethodDefinition[] { };
			}

			return new MethodDefinition[] {dbStart, dbSetSortMode, dbOnTableGUI};
		}

		public override void BeforeInvoke (InvocationInfo info)
		{
			return;
		}

		public override void AfterInvoke (InvocationInfo info, ref object returnValue)
		{
			if (info.targetMethod == "Start") {
				if (info.target is DeckBuilder2) {
					ButtonGroup sortGroup = (ButtonGroup)typeof(DeckBuilder2).GetField ("sortGroup", BindingFlags.Instance | BindingFlags.NonPublic).GetValue (info.target);
					IList items = (IList)buttonGroupItemsField.GetValue (sortGroup);
					myFilterNumber = items.Count;
					sortGroup.addItem ("Type", false);
				}
			} else if (info.targetMethod == "OnGUI_drawTableGUI") {
			    float num2 = (float) Screen.height * 0.08f;
				Rect rectRight = (Rect)typeof(DeckBuilder2).GetField ("rectRight", BindingFlags.Instance | BindingFlags.NonPublic).GetValue (info.target);;
			    Rect position = new Rect(rectRight.x - num2, (float) Screen.height * 0.925f, num2, num2);
				position.x -= 2 * position.width; //TODO: Detect how far we have to shift that button to the left.
				position.height = (2.0f * position.height/3.0f);
				GUISkin guiSkin = (GUISkin)Resources.Load ("_GUISkins/PlaqueTitle");
				GUIStyle style = new GUIStyle(guiSkin.button);
				if (GUI.Button (position, "Type", style)) {
					typeof(DeckBuilder2).GetMethod ("alignTableCards", BindingFlags.Instance | BindingFlags.NonPublic).Invoke (info.target, new object[] { 0, decksorter});
					//this.alignTableCards (0, new DeckSorter ().byColor ().byResourceCount ().byName ().byLevelAscending ());
				}
			}
			return;
		}

		public override void ReplaceMethod (InvocationInfo info, out object returnValue)
		{
			if ((int)currentSortModeField.GetValue (info.target) == myFilterNumber) {
				returnValue = null;
				return;
			}
			currentSortModeField.SetValue (info.target, myFilterNumber);
			object collectionSorter = collectionSorterField.GetValue (info.target);
			deckSorterMethods["clear"].Invoke (collectionSorter, new object[] { });
			deckSorterMethods["byType"].Invoke (collectionSorter, new object[] { });
			deckSorterMethods["byResourceCount"].Invoke (collectionSorter, new object[] { });
			deckSorterMethods["byColor"].Invoke (collectionSorter, new object[] { });
			deckSorterMethods["byName"].Invoke (collectionSorter, new object[] { });
			deckSorterMethods["byLevel"].Invoke (collectionSorter, new object[] { });
			returnValue = null;
		}

		public override bool WantsToReplace (InvocationInfo info)
		{
			return info.targetMethod == "setSortMode" && ((int)info.arguments [0]).Equals (myFilterNumber);
		}
	}
}

