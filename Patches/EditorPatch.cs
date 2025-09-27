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

			foreach (var studentSprite in prefabs_students_icons)
				_editorAssetMan.Add("UI/" + DecorsPlugin.newDecor_PrefabPrefix + studentSprite.Key, studentSprite.Value);
		}

		private static void InitializeVisuals(AssetManager man)
		{
			// Helper to reduce redudant code
			EditorBasicObject AddVisual(string key, bool useRegularCollider = true) =>
				EditorInterface.AddObjectVisual(DecorsPlugin.newDecor_PrefabPrefix + key, man.Get<GameObject>("editorPrefab_" + key), useRegularCollider);
			EditorBasicObject AddVisualWithCustomSphereCollider(string key, Vector3 center, float radius = 1f) =>
				EditorInterface.AddObjectVisualWithCustomSphereCollider(DecorsPlugin.newDecor_PrefabPrefix + key, man.Get<GameObject>("editorPrefab_" + key), radius, center);
			EditorBasicObject AddVisualWithCustomBoxCollider(string key, Vector3 size, Vector3 center) =>
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
			AddVisual("CardboardBox");
			AddVisual("Cubby");
			AddVisual("PrisonBar");
			AddVisual("DinnerWoodenTable");
			AddVisual("Planter");
			AddVisual("Pallet");
			AddVisual("HalfShelf");
			AddVisual("Swingset");
			foreach (var studentName in prefabs_students_names)
				AddVisual(studentName).gameObject.ReplaceAnimatedRotators();
			AddVisualWithCustomBoxCollider("OutsidePicnicSheet", new(6f, 1f, 6f), Vector3.zero);
			AddVisualWithCustomBoxCollider("pavementCover", new(5f, 1f, 5f), Vector3.zero);
			AddVisualWithCustomBoxCollider("pavementCorner", new(5f, 1f, 5f), Vector3.zero);
			AddVisualWithCustomBoxCollider("pavementOutCorner", new(5f, 1f, 5f), Vector3.zero);
			AddVisualWithCustomBoxCollider("pavementLcover", new(5f, 1f, 5f), Vector3.zero);
			AddVisualWithCustomBoxCollider("pavementRcover", new(5f, 1f, 5f), Vector3.zero);
			AddVisual("PlaygroundBush");
			AddVisualWithCustomSphereCollider("GreenBird", Vector3.up * 2f, 2f).gameObject.DestroySpriteRotators();
			AddVisualWithCustomSphereCollider("OrangeBird", Vector3.up * 2f, 2f).gameObject.DestroySpriteRotators();
			AddVisualWithCustomSphereCollider("PurpleBird", Vector3.up * 2f, 2f).gameObject.DestroySpriteRotators();
			AddVisual("MetalChair");
			AddVisual("MetalDesk");
			AddVisualWithCustomSphereCollider("SmallPottedPlant", Vector3.zero);
			AddVisualWithCustomSphereCollider("TableLightLamp", Vector3.zero);
			AddVisualWithCustomSphereCollider("BaldiPlush", Vector3.zero);
			AddVisualWithCustomSphereCollider("FancyOfficeLamp", Vector3.zero);
			AddVisualWithCustomSphereCollider("SaltAndHot", Vector3.zero);
			AddVisualWithCustomSphereCollider("TheRulesBook", Vector3.zero);
			AddVisualWithCustomSphereCollider("PencilHolder", Vector3.zero);
			AddVisualWithCustomSphereCollider("DinnerMenu", Vector3.zero);

			// Columns
			AddVisual("BigColumn");
			AddVisual("MediumColumn");
			AddVisual("SmallColumn");
			AddVisual("ThinColumn");
		}

		private static void InitializeTools(EditorMode mode, bool isVanillaCompliant)
		{
			// Rotatable objects
			var rotatableObjects = new List<ObjectWithOffset>
			{
				new("Closet", 1.5f), new("Couch", 5.2f), new("RedCouch", 5.2f), new("GrandFatherClock", 3.5f), new("WallShelf", 1f), new("LongOfficeTable", 3.5f), "Slide",
				"Monkeybars", "Seesaw", "Swingset", "pavementCorner", "pavementOutCorner",
				"pavementLcover", "pavementRcover", "MetalChair", "MetalDesk", "DinnerTable", "DinnerSeat", "Cubby", "PrisonBar", "Pallet", "HalfShelf"
			};
			foreach (var studentName in prefabs_students_names)
			{
				rotatableObjects.Add(new(studentName, 5f));
			}

			// Register
			foreach (var obj in rotatableObjects)
			{
				// Debug.Log("{\"key\":\"Ed_Tool_object_" + DecorsPlugin.newDecor_PrefabPrefix + obj.key + "_Title\",\"value\":\"" + ToReadableName(obj.key) + "\"},");
				// Debug.Log("{\"key\":\"Ed_Tool_object_" + DecorsPlugin.newDecor_PrefabPrefix + obj.key + "_Desc\",\"value\":\"[DESCRIPTION]\"},");
				EditorInterfaceModes.AddToolToCategory(mode, "objects", new ObjectTool(DecorsPlugin.newDecor_PrefabPrefix + obj.key, GetSprite(obj.key), obj.offset));
			}

			// Non-Rotatable objects
			var nonRotatableObjects = new List<ObjectWithOffset>
			{
				"OutsidePicnicSheet", "pavementCover", "PlaygroundBush", "GreenBird", "OrangeBird", "PurpleBird",
				new("SmallPottedPlant", 5f), new("TableLightLamp", 5f), new("BaldiPlush", 5.7f), new("FancyOfficeLamp", 5f), new("SaltAndHot", 4f),
				new("TheRulesBook", 5f), new("PencilHolder", 5f), new("BigColumn", 5f), new("MediumColumn", 5f), new("SmallColumn", 5f), new("ThinColumn", 5f), "CardboardBox",
				new("DinnerMenu", 6.25f), "DinnerWoodenTable", "Planter"
			};

			// Register
			foreach (var obj in nonRotatableObjects)
			{
				// Debug.Log("{\"key\":\"Ed_Tool_object_" + DecorsPlugin.newDecor_PrefabPrefix + obj.key + "_Title\",\"value\":\"" + ToReadableName(obj.key) + "\"},");
				// Debug.Log("{\"key\":\"Ed_Tool_object_" + DecorsPlugin.newDecor_PrefabPrefix + obj.key + "_Desc\",\"value\":\"[DESCRIPTION]\"},");
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

			EditorInterfaceModes.AddToolToCategory(mode, "objects", new BulkObjectTool("cardboardBoxTile", GetSprite("cardboardBoxTile"),
			[
				new("CardboardBox", new(2.5f, 0f, 2.5f)),
				new("CardboardBox", new(-2.5f, 0f, 2.5f)),
				new("CardboardBox", new(2.5f, 0f, -2.5f)),
				new("CardboardBox", new(-2.5f, 0f, -2.5f))
			]).CorrectlyAssignKeys());

			EditorInterfaceModes.AddToolToCategory(mode, "objects", new BulkObjectTool("CubbyWall", GetSprite("CubbyWall"),
			[
				new("Cubby", new(-3.99f, 0f, 3.99f)),
				new("Cubby", new(-2f, 0f, 3.99f)),
				new("Cubby", new(0f, 0f, 3.99f)),
				new("Cubby", new(2f, 0f, 3.99f)),
				new("Cubby", new(3.99f, 0f, 3.99f))
			]).CorrectlyAssignKeys());

			EditorInterfaceModes.AddToolToCategory(mode, "objects", new BulkObjectTool("PrisonBarCorner", GetSprite("PrisonBarCorner"),
			[
				new("PrisonBar", new(5f, 0f, 0f), new(0f, 90f, 0f)),
				new("PrisonBar", new(0f, 0f, 5f), new(0f, 0f, 0f))
			]).CorrectlyAssignKeys());

			EditorInterfaceModes.AddToolToCategory(mode, "objects", new BulkObjectTool("DinnerWoodenTable_FourCardinal", GetSprite("DinnerWoodenTable_FourCardinal"),
			[
				new("DinnerWoodenTable", new(0f, 0f, 0f)),
				new("_chair", new(0f, 0f, -5f), new(0f, 0f, 0f)),
				new("_chair", new(-5f, 0f, 0f), new(0f, 90f, 0f)),
				new("_chair", new(0f, 0f, 5f), new(0f, 180f, 0f)),
				new("_chair", new(5f, 0f, 0f), new(0f, 270f, 0f))
			]).CorrectlyAssignKeys());

			EditorInterfaceModes.AddToolToCategory(mode, "objects", new BulkObjectTool("DinnerWoodenTable_FourDiagonal", GetSprite("DinnerWoodenTable_FourDiagonal"),
			[
				new("DinnerWoodenTable", new(0f, 0f, 0f)),
				new("_chair", new(4f, 0f, -4f), new(0f, 315f, 0f)), // Bottom right
				new("_chair", new(-4f, 0f, 4f), new(0f, 135f, 0f)), // Top left
				new("_chair", new(-4f, 0f, -4f), new(0f, 45f, 0f)), // Bottom left
				new("_chair", new(4f, 0f, 4f), new(0f, 225f, 0f)) // Top right
			]).CorrectlyAssignKeys());

			List<BulkObjectData> dynamicBulkData = [new("Planter", new(0f, 0f, 0f))];
			for (float x = -3.25f; x <= 3.25f; x++) // small area for plants
				for (float z = -3.25f; z <= 3.25f; z++)
					dynamicBulkData.Add(new("_plant", new(x, 1.5f, z)));

			EditorInterfaceModes.AddToolToCategory(mode, "objects", new BulkObjectTool("PlanterFillment", GetSprite("PlanterFillment"), [.. dynamicBulkData]).CorrectlyAssignKeys());
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
			// Debug.Log("{\"key\":\"Ed_Tool_" + objTool.id + "_Title\",\"value\":\"" + ToReadableName(oldName) + "\"},");
			// Debug.Log("{\"key\":\"Ed_Tool_" + objTool.id + "_Desc\",\"value\":\"[DESCRIPTION]\"},");
			for (int i = 0; i < objTool.data.Length; i++)
			{
				if (objTool.data[i].prefab[0] != '_') // _ will indicate whether it should be kept as it is or be changed
					objTool.data[i].prefab = DecorsPlugin.newDecor_PrefabPrefix + objTool.data[i].prefab;
				else
					objTool.data[i].prefab = objTool.data[i].prefab.Remove(0, 1); // Removes the first '_'
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

		// Specific lists for objects that have their names dynamically added
		public static List<string> prefabs_students_names = [];
		public static List<KeyValuePair<string, Sprite>> prefabs_students_icons = [];
	}
}