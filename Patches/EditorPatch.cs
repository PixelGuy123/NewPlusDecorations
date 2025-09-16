using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using MTM101BaldAPI.AssetTools;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using UnityEngine;

namespace NewPlusDecorations.Patches
{
	internal static class EditorIntegration
	{
		private static AssetManager _editorAssetMan;

		internal static void Initialize(AssetManager man)
		{
			LoadEditorAssets();
			InitializeVisuals(man);
			EditorInterfaceModes.AddModeCallback(InitializeTools);
		}

		private static void LoadEditorAssets()
		{
			_editorAssetMan = new AssetManager();
			string editorUIPath = Path.Combine(DecorsPlugin.path, "EditorUI");
			if (!Directory.Exists(editorUIPath))
			{
				Debug.LogWarning("NewPlusDecorations: EditorUI folder not found!");
				return;
			}

			string[] files = Directory.GetFiles(editorUIPath);
			foreach (string file in files)
			{
				string name = Path.GetFileNameWithoutExtension(file);

				_editorAssetMan.Add("UI/" + DecorsPlugin.newDecor_PrefabPrefix + name, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(file), 1f));
			}
		}

		private static void InitializeVisuals(AssetManager man)
		{
			// Helper to reduce redudant code
			void AddVisual(string key, bool useRegularCollider = true) =>
				EditorInterface.AddObjectVisual(DecorsPlugin.newDecor_PrefabPrefix + key, man.Get<GameObject>("editorPrefab_" + key), useRegularCollider);
			void AddVisualWithCustomSphereCollider(string key, Vector3 center, float radius = 1f) =>
				EditorInterface.AddObjectVisualWithCustomSphereCollider(DecorsPlugin.newDecor_PrefabPrefix + key, man.Get<GameObject>("editorPrefab_" + key), radius, center);
			void AddVisualWithCustomBoxCollider(string key, Vector3 size, Vector3 center) =>
				EditorInterface.AddObjectVisualWithCustomBoxCollider(DecorsPlugin.newDecor_PrefabPrefix + key, man.Get<GameObject>("editorPrefab_" + key), size, center);


			// Single Objects
			AddVisual("ClosetShelf");
			AddVisual("Closet");
			AddVisual("Couch");
			AddVisual("RedCouch");
			AddVisual("GrandFatherClock");
			AddVisual("WallShelf");
			AddVisual("LongOfficeTable");
			AddVisual("Slide");
			AddVisual("Monkeybars");
			AddVisual("Seesaw");
			AddVisual("DinnerTable");
			AddVisual("DinnerSeat");
			AddVisual("DinnerMenu");
			AddVisual("Swingset");
			AddVisualWithCustomBoxCollider("OutsidePicnicSheet", new(6f, 1f, 6f), Vector3.zero);
			AddVisualWithCustomBoxCollider("pavementCover", new(5f, 1f, 5f), Vector3.zero);
			AddVisualWithCustomBoxCollider("pavementCorner", new(5f, 1f, 5f), Vector3.zero);
			AddVisualWithCustomBoxCollider("pavementOutCorner", new(5f, 1f, 5f), Vector3.zero);
			AddVisualWithCustomBoxCollider("pavementLcover", new(5f, 1f, 5f), Vector3.zero);
			AddVisualWithCustomBoxCollider("pavementRcover", new(5f, 1f, 5f), Vector3.zero);
			AddVisual("PlaygroundBush");
			AddVisual("GreenBird");
			AddVisual("OrangeBird");
			AddVisual("PurpleBird");
			AddVisual("MetalChair");
			AddVisual("MetalDesk");
			AddVisualWithCustomSphereCollider("SmallPottedPlant", Vector3.zero);
			AddVisualWithCustomSphereCollider("TableLightLamp", Vector3.zero);
			AddVisualWithCustomSphereCollider("BaldiPlush", Vector3.zero);
			AddVisualWithCustomSphereCollider("FancyOfficeLamp", Vector3.zero);
			AddVisualWithCustomSphereCollider("SaltAndHot", Vector3.zero);
			AddVisualWithCustomSphereCollider("TheRulesBook", Vector3.zero);
			AddVisualWithCustomSphereCollider("PencilHolder", Vector3.zero);

			// Columns
			AddVisual("BigColumn");
			AddVisual("MediumColumn");
			AddVisual("SmallColumn");
			AddVisual("ThinColumn");
		}

		private static void InitializeTools(EditorMode mode, bool isVanillaCompliant)
		{
			// Rotatable objects
			var rotatableObjects = new ObjectWithOffset[]
				{
				new("Closet", 1.5f), new("Couch", 5.2f), new("RedCouch", 5.2f), new("GrandFatherClock", 3.5f), new("WallShelf", 1f), new("LongOfficeTable", 3.5f), "Slide",
				"Monkeybars", "Seesaw", "Swingset", "pavementCorner", "pavementOutCorner",
				"pavementLcover", "pavementRcover", "MetalChair", "MetalDesk", "DinnerTable", "DinnerSeat", new("DinnerMenu", 5f),
				};
			foreach (var obj in rotatableObjects)
			{
				Debug.Log("{\"key\":\"Ed_Tool_object_" + DecorsPlugin.newDecor_PrefabPrefix + obj.key + "_Title\",\"value\":\"" + ToReadableName(obj.key) + "\"},");
				Debug.Log("{\"key\":\"Ed_Tool_object_" + DecorsPlugin.newDecor_PrefabPrefix + obj.key + "_Desc\",\"value\":\"[DESCRIPTION]\"},");
				EditorInterfaceModes.AddToolToCategory(mode, "objects", new ObjectTool(DecorsPlugin.newDecor_PrefabPrefix + obj.key, GetSprite(obj.key), obj.offset));
			}

			// Non-Rotatable objects
			var nonRotatableObjects = new ObjectWithOffset[]
			{
				"OutsidePicnicSheet", "pavementCover", "PlaygroundBush", "GreenBird", "OrangeBird", "PurpleBird",
				new("SmallPottedPlant", 5f), new("TableLightLamp", 5f), new("BaldiPlush", 5.7f), new("FancyOfficeLamp", 5f), new("SaltAndHot", 4f),
				new("TheRulesBook", 5f), new("PencilHolder", 5f), new("BigColumn", 5f), new("MediumColumn", 5f), new("SmallColumn", 5f), new("ThinColumn", 5f)
			};
			foreach (var obj in nonRotatableObjects)
			{
				Debug.Log("{\"key\":\"Ed_Tool_object_" + DecorsPlugin.newDecor_PrefabPrefix + obj.key + "_Title\",\"value\":\"" + ToReadableName(obj.key) + "\"},");
				Debug.Log("{\"key\":\"Ed_Tool_object_" + DecorsPlugin.newDecor_PrefabPrefix + obj.key + "_Desc\",\"value\":\"[DESCRIPTION]\"},");
				EditorInterfaceModes.AddToolToCategory(mode, "objects", new ObjectToolNoRotation(DecorsPlugin.newDecor_PrefabPrefix + obj.key, GetSprite(obj.key), obj.offset));
			}

			// Bulk / Prebuilt Tools
			EditorInterfaceModes.AddToolToCategory(mode, "objects", new BulkObjectTool("highShelf", GetSprite("highShelf"),
			[
				new("ClosetShelf", new Vector3(0f, 0f, 0f)),
				new("ClosetShelf", new Vector3(0f, 4.5f, 0f)),
				new("ClosetShelf", new Vector3(0f, 9f, 0f))
			]).CorrectlyAssignKeys());

			EditorInterfaceModes.AddToolToCategory(mode, "objects", new BulkObjectTool("oneMetalDeskSit", GetSprite("oneMetalDeskSit"),
			[
				new("MetalDesk", Vector3.zero),
				new("MetalChair", new Vector3(0f, 0f, -3.5f))
			]).CorrectlyAssignKeys());

			EditorInterfaceModes.AddToolToCategory(mode, "objects", new BulkObjectTool("twoMetalDeskSit", GetSprite("twoMetalDeskSit"),
			[
				new("MetalDesk", Vector3.zero),
				new("MetalChair", new Vector3(3.5f, 0f, -3.5f)),
				new("MetalChair", new Vector3(-3.5f, 0f, -3.5f))
			]).CorrectlyAssignKeys());

			EditorInterfaceModes.AddToolToCategory(mode, "objects", new BulkObjectTool("dinnerRow", GetSprite("dinnerRow"),
			[
				new("DinnerSeat", new(0f, 0f, 5.25f), new(0f, 180f, 0f)),
				new("DinnerTable", Vector3.zero),
				new("DinnerSeat", new(0f, 0f, -5.25f))
			]).CorrectlyAssignKeys());
		}

		private static Sprite GetSprite(string name)
		{
			string key = "UI/" + DecorsPlugin.newDecor_PrefabPrefix + name;
			if (_editorAssetMan.ContainsKey(key))
				return _editorAssetMan.Get<Sprite>(key);

			Debug.LogWarning($"NewPlusDecorations: Missing Editor UI sprite for key: {key}");
			return null; // The editor should handle null sprites gracefully
		}

		static BulkObjectTool CorrectlyAssignKeys(this BulkObjectTool objTool)
		{
			string oldName = objTool.type;
			objTool.type = DecorsPlugin.newDecor_PrefabPrefix + objTool.type;
			Debug.Log("{\"key\":\"Ed_Tool_" + objTool.id + "_Title\",\"value\":\"" + ToReadableName(oldName) + "\"},");
			Debug.Log("{\"key\":\"Ed_Tool_" + objTool.id + "_Desc\",\"value\":\"[DESCRIPTION]\"},");
			for (int i = 0; i < objTool.data.Length; i++)
			{
				objTool.data[i].prefab = DecorsPlugin.newDecor_PrefabPrefix + objTool.data[i].prefab;
			}
			return objTool;
		}

		readonly struct ObjectWithOffset(string key)
		{
			public static implicit operator ObjectWithOffset(string key) => new(key);
			public ObjectWithOffset(string key, float verticalOffset) : this(key) => offset = verticalOffset;

			readonly public string key = key;
			readonly public float offset = 0f;
			public override string ToString() => $"{key} => Y: {offset}";
		}
		static string ToReadableName(string key)
		{
			if (string.IsNullOrEmpty(key)) return key;
			// Replace underscores/dashes with spaces
			string s = key.Replace('_', ' ').Replace('-', ' ');
			// Insert space between lower-case or digits and upper-case letters: "bookShelf" -> "book Shelf"
			s = Regex.Replace(s, "([a-z0-9])([A-Z])", "$1 $2");
			// Also insert space between consecutive upper-case followed by lower-case (e.g., "XMLHttp" -> "XML Http")
			s = Regex.Replace(s, "([A-Z]+)([A-Z][a-z])", "$1 $2");
			// Normalize case and then title-case each word
			s = s.ToLowerInvariant();
			s = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
			return s;
		}
	}
}