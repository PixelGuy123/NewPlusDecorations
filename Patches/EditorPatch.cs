using BaldiLevelEditor;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using PlusLevelFormat;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NewPlusDecorations.Patches
{
	[HarmonyPatch]
	[ConditionalPatchMod("mtm101.rulerp.baldiplus.leveleditor")]
	internal class EditorPatch
	{
		[HarmonyPatch(typeof(DecorsPlugin), "PostSetup")]
		[HarmonyPostfix]
		private static void MakeEditorSeeAssets(AssetManager man)
		{
			GameObject[] array = [man.Get<GameObject>("editorPrefab_ClosetShelf")];
			MarkRotatingObject(array[0], Vector3.zero);
			MarkObjectRow("highShelf",
			[
				new ObjectData(array[0], Vector3.zero, default),
				new ObjectData(array[0], Vector3.up * 4.5f, default),
				new ObjectData(array[0], Vector3.up * 9f, default)
			]);

			MarkRotatingObject(man.Get<GameObject>("editorPrefab_Closet"), Vector3.up * 1.5f);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_Couch"), Vector3.up * 5.2f);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_GrandFatherClock"), Vector3.up * 3.5f);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_WallShelf"), Vector3.up * 2f);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_LongOfficeTable"), Vector3.up * 3.5f);

			// Decorations
			MarkObject(man.Get<GameObject>("editorPrefab_SmallPottedPlant"), Vector3.up * 5f);
			MarkObject(man.Get<GameObject>("editorPrefab_TableLightLamp"), Vector3.up * 5f);

			// Columns
			MarkObject(man.Get<GameObject>("editorPrefab_BigColumn"), Vector3.up * 5f);
			MarkObject(man.Get<GameObject>("editorPrefab_MediumColumn"), Vector3.up * 5f);
			MarkObject(man.Get<GameObject>("editorPrefab_SmallColumn"), Vector3.up * 5f);
			MarkObject(man.Get<GameObject>("editorPrefab_ThinColumn"), Vector3.up * 5f);
		}

		static void MarkRotatingObject(GameObject obj, Vector3 offset, bool useActual = false)
		{
			markersToAdd.Add(new(obj.name, new(false, null)));
			BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(obj.name, obj, offset, useActual));
		}

		static void MarkObject(GameObject obj, Vector3 offset, bool useActual = false)
		{
			markersToAdd.Add(new(obj.name, new(true, null)));
			BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(obj.name, obj, offset, useActual));
		}

		static void MarkObjectRow(string prebuiltToolName, params ObjectData[] objs) =>
			markersToAdd.Add(new(prebuiltToolName, new(false, objs)));

		struct ObjectData(GameObject obj, Vector3 vec, Quaternion rot)
		{
			public GameObject Item1 = obj;

			public Vector3 Item2 = vec;

			public Quaternion Item3 = rot;
		}

		static readonly List<KeyValuePair<string, KeyValuePair<bool, ObjectData[]>>> markersToAdd = [];


		[HarmonyPatch(typeof(PlusLevelEditor), "Initialize")]
		[HarmonyPostfix]
		static void InitializeStuff(PlusLevelEditor __instance)
		{
			string[] files = Directory.GetFiles(Path.Combine(DecorsPlugin.path, "EditorUI"));
			for (int i = 0; i < files.Length; i++)
				BaldiLevelEditorPlugin.Instance.assetMan.Add("UI/" + Path.GetFileNameWithoutExtension(files[i]), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(files[i]), 40f));

			var objectCats = __instance.toolCats.Find(x => x.name == "objects").tools;

			foreach (var objMark in markersToAdd)
			{
				if (objMark.Value.Value == null)
				{
					objectCats.Add(objMark.Value.Key ? new ObjectTool(objMark.Key) : new RotateAndPlacePrefab(objMark.Key));
					continue;
				}
				PrefabLocation[] array = new PrefabLocation[objMark.Value.Value.Length];

				for (int i = 0; i < objMark.Value.Value.Length; i++)
					array[i] = new PrefabLocation(objMark.Value.Value[i].Item1.name, PlusLevelLoader.Extensions.ToData(objMark.Value.Value[i].Item2), PlusLevelLoader.Extensions.ToData(objMark.Value.Value[i].Item3));

				objectCats.Add(new PrebuiltStructureTool(objMark.Key, new EditorPrebuiltStucture(array)));

			}
		}
	}
}
