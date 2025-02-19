﻿using BaldiLevelEditor;
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
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_RedCouch"), Vector3.up * 5.2f);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_GrandFatherClock"), Vector3.up * 3.5f);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_WallShelf"), Vector3.up);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_LongOfficeTable"), Vector3.up * 3.5f);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_Slide"), Vector3.zero);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_Monkeybars"), Vector3.zero);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_Seesaw"), Vector3.zero);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_Swingset"), Vector3.zero);
			MarkObject(man.Get<GameObject>("editorPrefab_OutsidePicnicSheet"), Vector3.zero);
			MarkObject(man.Get<GameObject>("editorPrefab_pavementCover"), Vector3.zero);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_pavementCorner"), Vector3.zero);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_pavementOutCorner"), Vector3.zero);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_pavementLcover"), Vector3.zero);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_pavementRcover"), Vector3.zero);
			MarkObject(man.Get<GameObject>("editorPrefab_PlaygroundBush"), Vector3.zero);
			MarkObject(man.Get<GameObject>("editorPrefab_GreenBird"), Vector3.zero);
			MarkObject(man.Get<GameObject>("editorPrefab_OrangeBird"), Vector3.zero);
			MarkObject(man.Get<GameObject>("editorPrefab_PurpleBird"), Vector3.zero);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_MetalChair"), Vector3.zero);
			MarkRotatingObject(man.Get<GameObject>("editorPrefab_MetalDesk"), Vector3.zero);
			MarkObjectRow("oneMetalDeskSit", 
				new() 
				{
				Item1 = man.Get<GameObject>("editorPrefab_MetalDesk"),
				Item2 = Vector3.zero,
				Item3 = Quaternion.identity
				},
				new()
				{
					Item1 = man.Get<GameObject>("editorPrefab_MetalChair"),
					Item2 = Vector3.back * 3.5f,
					Item3 = Quaternion.identity
				}
				);
			MarkObjectRow("twoMetalDeskSit",
				new()
				{
					Item1 = man.Get<GameObject>("editorPrefab_MetalDesk"),
					Item2 = Vector3.zero,
					Item3 = Quaternion.identity
				},
				new()
				{
					Item1 = man.Get<GameObject>("editorPrefab_MetalChair"),
					Item2 = Vector3.back * 3.5f + Vector3.right * 3.5f,
					Item3 = Quaternion.identity
				},
				new()
				{
					Item1 = man.Get<GameObject>("editorPrefab_MetalChair"),
					Item2 = Vector3.back * 3.5f + Vector3.left * 3.5f,
					Item3 = Quaternion.identity
				}
				);


			// Decorations
			MarkObject(man.Get<GameObject>("editorPrefab_SmallPottedPlant"), Vector3.up * 5f);
			MarkObject(man.Get<GameObject>("editorPrefab_TableLightLamp"), Vector3.up * 5f);
			MarkObject(man.Get<GameObject>("editorPrefab_BaldiPlush"), Vector3.up * 5f);
			MarkObject(man.Get<GameObject>("editorPrefab_FancyOfficeLamp"), Vector3.up * 5f);
			MarkObject(man.Get<GameObject>("editorPrefab_SaltAndHot"), Vector3.up * 4f);
			MarkObject(man.Get<GameObject>("editorPrefab_TheRulesBook"), Vector3.up * 5f);

			// Columns
			MarkObject(man.Get<GameObject>("editorPrefab_BigColumn"), Vector3.up * 5f);
			MarkObject(man.Get<GameObject>("editorPrefab_MediumColumn"), Vector3.up * 5f);
			MarkObject(man.Get<GameObject>("editorPrefab_SmallColumn"), Vector3.up * 5f);
			MarkObject(man.Get<GameObject>("editorPrefab_ThinColumn"), Vector3.up * 5f);

			string[] files = Directory.GetFiles(Path.Combine(DecorsPlugin.path, "EditorUI"));
			for (int i = 0; i < files.Length; i++)
				BaldiLevelEditorPlugin.Instance.assetMan.Add("UI/" + Path.GetFileNameWithoutExtension(files[i]), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(files[i]), 40f));
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
