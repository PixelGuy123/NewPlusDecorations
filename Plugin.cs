using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using PlusLevelLoader;
using System.Collections;
using UnityEngine;
using PixelInternalAPI.Extensions;
using PixelInternalAPI.Classes;
using System.IO;
using NewPlusDecorations.Components;

namespace NewPlusDecorations
{
    [BepInPlugin("pixelguy.pixelmodding.baldiplus.newdecors", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInDependency("pixelguy.pixelmodding.baldiplus.pixelinternalapi", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)] // let's not forget this
	[BepInDependency("mtm101.rulerp.baldiplus.levelloader", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("mtm101.rulerp.baldiplus.leveleditor", BepInDependency.DependencyFlags.SoftDependency)]

	public class DecorsPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
			path = AssetLoader.GetModPath(this);
			var h = new Harmony("pixelguy.pixelmodding.baldiplus.newdecors");
			h.PatchAllConditionals();

			LoadingEvents.RegisterOnAssetsLoaded(Info, Load(), false);
        }

		IEnumerator Load()
		{
			yield return loadSteps;

			yield return "Loading planes...";
			var basePlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
			var renderer = basePlane.GetComponent<MeshRenderer>();
			renderer.material = GenericExtensions.FindResourceObjectByName<Material>("TileBase");
			basePlane.transform.localScale = Vector3.one * LayerStorage.TileBaseOffset; // Gives the tile size
			basePlane.name = "PlaneTemplate";
			renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
			man.Add("PlaneTemplate", basePlane);

			basePlane.gameObject.ConvertToPrefab(true);

			basePlane = basePlane.DuplicatePrefab();
			basePlane.GetComponent<MeshRenderer>().material = GenericExtensions.FindResourceObjectByName<Material>("TileBase_Alpha");
			man.Add("TransparentPlaneTemplate", basePlane);

			yield return "Loading misc resources...";
			ObjectCreationExtension.defaultMaterial = GenericExtensions.FindResourceObjectByName<Material>("Locker_Red"); // Actually a good material, has even lightmap
			var blackTexture = TextureExtensions.CreateSolidTexture(1, 1, Color.black);
			var whiteTex = TextureExtensions.CreateSolidTexture(1, 1, Color.white);
			man.Add("woodTexture", GenericExtensions.FindResourceObjectByName<Texture2D>("wood 1").MakeReadableTexture()); // Wood from the tables
			man.Add("plasticTexture", GenericExtensions.FindResourceObjectByName<Texture2D>("PlasticTable").MakeReadableTexture());

			yield return "Creating shelf...";

			var renderers = new Renderer[5];
			int rendIdx = 0;

			// Shelf creation
			var darkWood = Instantiate(man.Get<Texture2D>("woodTexture")).ApplyLightLevel(-25f);
			darkWood.name = "Times_darkWood";
			var shelf = new GameObject("ClosetShelf")
			{
				layer = LayerStorage.ignoreRaycast
			};
			shelf.AddBoxCollider(Vector3.zero, new(4f, 10f, 15f), false);
			shelf.AddNavObstacle(new(4.2f, 10f, 16.3f));

			CreateCube("ShelfBody", darkWood, false, shelf.transform, Vector3.up * 4f, new(4f, 0.7f, 15f)).RemoveHitbox();

			ShelfLegCreator(new(-1.5f, 2.3f, 6.5f));
			ShelfLegCreator(new(1.5f, 2.3f, -6.5f));
			ShelfLegCreator(new(-1.5f, 2.3f, -6.5f));
			ShelfLegCreator(new(1.5f, 2.3f, 6.5f));

			void ShelfLegCreator(Vector3 pos)
			{
				var shelfLeg = ObjectCreationExtension.CreatePrimitiveObject(PrimitiveType.Cylinder, blackTexture).RemoveHitbox();
				shelfLeg.transform.SetParent(shelf.transform);
				shelfLeg.transform.localPosition = pos;
				shelfLeg.transform.localScale = new(0.8f, 2.3f, 0.8f);
				renderers[rendIdx++] = shelfLeg.GetComponent<MeshRenderer>();
			}

			shelf.AddContainer(renderers);
			AddObjectToEditor(shelf.gameObject);

			yield return "Adding columns...";
			// Some cool decorations

			AddColumn("BigColumn", new(3f, LayerStorage.TileBaseOffset));
			AddColumn("MediumColumn", new(2f, LayerStorage.TileBaseOffset));
			AddColumn("SmallColumn", new(1f, LayerStorage.TileBaseOffset));
			AddColumn("ThinColumn", new(0.35f, LayerStorage.TileBaseOffset));


			Column AddColumn(string name, Vector2 size)
			{
				var column = new GameObject(name);
				var dirs = Directions.All();

				renderers = new Renderer[dirs.Count];
				rendIdx = 0;


				dirs.ForEach(dir => CreatePlane("PlaneDir_" + dir, whiteTex, column.transform, dir.ToVector3() * size.x, new(size.x * 0.2f, size.y * 0.1f, 1f), dir.GetOpposite().ToRotation().eulerAngles));
				
				column.AddContainer(renderers);
				column.AddBoxCollider(Vector3.zero, new(size.x, size.y, size.x), false);
				column.AddNavObstacle(new(size.x, size.y, size.x));

				AddObjectToEditor(column);
				var actualColumn = column.AddComponent<Column>();
				actualColumn.renderer = renderers;
				return actualColumn;


			}

			yield return "Adding closet...";
			// Closets

			var closetTexture = AssetLoader.TextureFromFile(Path.Combine(path, "closetwood.png"));
			var closetDoorTexture = TextureExtensions.LoadSpriteSheet(2, 1, 25f, path, "closetdoors.png");

			renderers = new Renderer[6];
			rendIdx = 0;

			var closet = new GameObject("Closet")
			{
				layer = LayerStorage.ignoreRaycast
			};
			closet.AddBoxCollider(Vector3.zero, new(5f, 10f, 5f), true);
			closet.AddNavObstacle(new(5f, 10f, 5f));
			AddObjectToEditor(closet);
			closet.AddComponent<EnvironmentObjectDistributor>();

			CreateCube("ClosetBase", closetTexture, false, closet.transform, Vector3.down, new(5f, 1f, 5f));
			CreateCube("ClosetRightSide", closetTexture, false, closet.transform, Vector3.right * 3f + Vector3.up * 2.5f, new(1f, 8f, 5f));
			CreateCube("ClosetLeftSide", closetTexture, false, closet.transform, Vector3.left * 3f + Vector3.up * 2.5f, new(1f, 8f, 5f));
			CreateCube("ClosetTop", closetTexture, false, closet.transform, Vector3.up * 7f, new(7f, 1f, 5f)).GetComponent<BoxCollider>().size = new(1f, 3f, 1f);
			CreateCube("ClosetBack", closetTexture, false, closet.transform, (Vector3.back + Vector3.up) * 3f, new(7f, 9f, 1f));

			var sprite = ObjectCreationExtensions.CreateSpriteBillboard(closetDoorTexture[0], false).AddSpriteHolder(0f, LayerStorage.iClickableLayer);
			sprite.name = "ClosetDoorTex";
			renderers[5] = sprite;

			var door = sprite.transform.parent;
			door.transform.SetParent(closet.transform);
			door.name = "ClosetDoor";
			door.transform.localScale = new(1f, 1.5f, 1f);
			door.transform.localPosition = new(0f, 3f, 2.45f);

			var closetDoor = door.gameObject.AddComponent<ClosetDoor>();
			closetDoor.renderer = sprite;
			closetDoor.closed = closetDoorTexture[0];
			closetDoor.open = closetDoorTexture[1];
			closetDoor.collider = closetDoor.gameObject.AddBoxCollider(Vector3.zero, new(4f, 10f, 1f), true);
			closetDoor.audMan = closetDoor.gameObject.CreatePropagatedAudioManager(35f, 50f);
			closetDoor.audClose = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(path, "closetCloseNoise.wav")), "Sfx_Doors_StandardShut", SoundType.Voice, Color.white);
			closetDoor.audOpen = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(path, "closetOpenNoise.wav")), "Sfx_Doors_StandardOpen", SoundType.Voice, Color.white);

			closet.AddContainer(renderers);

			yield return "Adding couch...";

			renderers = new Renderer[4];
			rendIdx = 0;

			var couch = new GameObject("Couch")
			{
				layer = LayerStorage.iClickableLayer
			};

			couch.AddBoxCollider(Vector3.zero, new(4f, 10f, 4f), true);
			couch.AddNavObstacle(new(4.5f, 10f, 4.5f));

			var couchComp = couch.AddComponent<Couch>();
			couchComp.camTarget = new GameObject("CouchCam").transform;
			couchComp.camTarget.transform.SetParent(couch.transform);

			AddObjectToEditor(couch);

			var couchTexture = AssetLoader.TextureFromFile(Path.Combine(path, "couch.png")); //AssetLoader.TextureFromFile(Path.Combine(path, "couch.png"));
			var couchTextBack = AssetLoader.TextureFromFile(Path.Combine(path, "couchBack.png"));
			var sitCollider = CreateCube("CouchSit", couchTexture, false, couch.transform, Vector3.down * 4.2f, new(4.2f, 2f, 4.2f)).SetBoxHitbox(y:2f);
			CreateCubeWithRot("CouchBack", couchTextBack, true, couch.transform, new(-2.5f, -3.4f, -2.5f), new(5f, 4.65f, 1f), Vector3.right * 345f).GetComponent<BoxCollider>().center = Vector3.right * 0.5f;
			CreateCube("CouchSideRight", couchTexture, false, couch.transform, new(2.5f, -3.5f, -0.1f), new(1f, 3.7f, 4.5f)).SetBoxHitbox(y: 10f);
			CreateCube("CouchSideLeft", couchTexture, false, couch.transform, new(-2.5f, -3.5f, -0.1f), new(1f, 3.7f, 4.5f)).SetBoxHitbox(y: 10f);

			yield return "Adding Grand Father Clock...";

			renderers = new Renderer[6];
			rendIdx = 0;
			var grandFatherTextures = TextureExtensions.LoadSpriteSheet(2, 1, 25f, path, "grandFatherClock_Body.png");
			var grandFatherClockTexture = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(path, "grandFatherClock_head.png")), 25f);
			var grandFatherClock = new GameObject("GrandFatherClock");

			grandFatherClock.AddBoxCollider(Vector3.zero, new(2f, 10f, 2.5f), false);
			grandFatherClock.AddNavObstacle(new(3f, 10f, 3f));

			AddObjectToEditor(grandFatherClock);
			var clock = grandFatherClock.AddComponent<TickTockClock>();
			clock.audMan = grandFatherClock.CreatePropagatedAudioManager(34f, 55f);
			clock.audTick = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(path, "grandFatherClock_tick.wav")), string.Empty, SoundType.Voice, Color.white);
			clock.audTick.subtitle = false;
			clock.audTock = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(path, "grandFatherClock_tock.wav")), string.Empty, SoundType.Voice, Color.white);
			clock.audTock.subtitle = false;
			clock.sprTick = grandFatherTextures[0];
			clock.sprTock = grandFatherTextures[1];

			CreateCube("GrandFatherClockBody", closetTexture, false, grandFatherClock.transform, new(), new(2.6f, 5.2f, 1.2f)).RemoveHitbox();
			CreateCube("GrandFatherClockHead", closetTexture, false, grandFatherClock.transform, Vector3.up * 4f, new(3f, 3f, 2f)).RemoveHitbox();
			CreateCube("GrandFatherClockFirstBase", closetTexture, false, grandFatherClock.transform, Vector3.down * 2.85f, new(3f, 0.5f, 2f)).RemoveHitbox();
			CreateCube("GrandFatherClockSecondBase", closetTexture, false, grandFatherClock.transform, Vector3.down * 3.35f, new(3.5f, 0.5f, 2.5f)).RemoveHitbox();
			clock.renderer = CreateBillboard("GrandFather_TickTock", grandFatherTextures[0], false, grandFatherClock.transform, Vector3.forward * 0.601f, Vector3.one * 0.9f, Vector3.zero);
			CreateBillboard("GrandFather_Clock", grandFatherClockTexture, false, grandFatherClock.transform, new(0f, 4f, 1.01f), new(1f, 1f, 1f), Vector3.zero);

			yield return "Adding wall shelves...";
			renderers = new Renderer[3];
			rendIdx = 0;
			darkWood = Instantiate(man.Get<Texture2D>("woodTexture")).ApplyLightLevel(-15f);
			darkWood.name = "Times_lessDarkWood";

			shelf = new GameObject("WallShelf");
			shelf.AddNavObstacle(new(9.5f, 2.5f, 4.5f));
			shelf.AddBoxCollider(new(0f, 3f, -2.25f), Vector3.one * 6f, true);
			AddObjectToEditor(shelf);

			CreateCube("ShelfBody", darkWood, false, shelf.transform, new(0f, 3f, -2.25f), new(9, 0.7f, 4f)).SetBoxHitbox(y:2.5f);
			CreateCubeWithRot("ShelfLeftConnection", blackTexture, false, shelf.transform, new(-3f, 1.49f, -3.89f), new(0.5f, 4f, 0.5f), Vector3.right * 45f).RemoveHitbox();
			CreateCubeWithRot("ShelfRightConnection", blackTexture, false, shelf.transform, new(3f, 1.49f, -3.89f), new(0.5f, 4f, 0.5f), Vector3.right * 45f).RemoveHitbox();

			yield return "Adding long office table...";

			renderers = new Renderer[3];
			rendIdx = 0;
			shelf = new GameObject("LongOfficeTable");
			shelf.AddNavObstacle(new(22f, 10f, 6.5f));
			shelf.AddBoxCollider(Vector3.zero, new(21f, 10f, 6f), false);
			AddObjectToEditor(shelf);

			CreateCube("TableBody", closetTexture, false, shelf.transform, Vector3.zero, new(21f, 1f, 6f)).RemoveHitbox();
			CreateCube("TableLongRightLeg", closetTexture, false, shelf.transform, new(10f, -2f, 0f), new(1f, 3f, 6f)).RemoveHitbox();
			CreateCube("TableLongLeftLeg", closetTexture, false, shelf.transform, new(-10f, -2f, 0f), new(1f, 3f, 6f)).RemoveHitbox();


			yield return "Creating misc decorations...";
			// Misc Decorations
			AddDecoration("SmallPottedPlant", "plant.png", 25f, Vector3.up);
			AddDecoration("TableLightLamp", "tablelamp.png", 25f, Vector3.up * 0.7f);

			void AddDecoration(string name, string fileName, float pixelsPerUnit, Vector3 offset)
			{
				var bred = ObjectCreationExtensions.CreateSpriteBillboard(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(path, fileName)), pixelsPerUnit)).AddSpriteHolder(offset);
				bred.transform.parent.name = name;
				bred.name = name;
				AddObjectToEditor(bred.transform.parent.gameObject);
				//"editorPrefab_"
			}

			GameObject CreateCube(string cubeName, Texture2D texture, bool useUV, Transform parent, Vector3 offset, Vector3 scale)
			{
				var cube = ObjectCreationExtension.CreateCube(texture, useUV);
				cube.name = cubeName;
				cube.transform.SetParent(parent);
				cube.transform.localPosition = offset;
				cube.transform.localScale = scale;
				renderers[rendIdx++] = cube.GetComponent<MeshRenderer>();
				return cube;
			}

			GameObject CreateCubeWithRot(string cubeName, Texture2D texture, bool useUV, Transform parent, Vector3 offset, Vector3 scale, Vector3 rot)
			{
				var co = CreateCube(cubeName, texture, useUV, parent, offset, scale);
				co.transform.rotation = Quaternion.Euler(rot);
				return co;
			}

			GameObject CreatePlane(string planeName, Texture2D spr, Transform parent, Vector3 offset, Vector3 scale, Vector3 rot)
			{
				var planeHolder = new GameObject(planeName );
				planeHolder.transform.SetParent(parent);
				planeHolder.transform.localPosition = offset;
				planeHolder.transform.eulerAngles = rot;
				planeHolder.transform.localScale = scale;

				var plane = Instantiate(man.Get<GameObject>("PlaneTemplate"), planeHolder.transform);
				plane.transform.localPosition = Vector3.zero;

				plane.GetComponent<MeshRenderer>().material.SetMainTexture(spr);
				plane.name = planeName + "_Renderer";
				renderers[rendIdx++] = plane.GetComponent<MeshRenderer>();

				return planeHolder;
			}

			SpriteRenderer CreateBillboard(string planeName, Sprite spr, bool billboard, Transform parent, Vector3 offset, Vector3 scale, Vector3 rot)
			{
				var planeHolder = ObjectCreationExtensions.CreateSpriteBillboard(spr, billboard);
				planeHolder.name = planeName;
				planeHolder.transform.SetParent(parent);
				planeHolder.transform.localPosition = offset;
				planeHolder.transform.eulerAngles = rot;
				planeHolder.transform.localScale = scale;
				renderers[rendIdx++] = planeHolder;

				return planeHolder;
			}

			yield return "Triggering post setup...";


			PostSetup(man);
		}

		const int loadSteps = 8;

		void AddObjectToEditor(GameObject obj)
		{
			PlusLevelLoaderPlugin.Instance.prefabAliases.Add(obj.name, obj);
			man.Add($"editorPrefab_{obj.name}", obj);
			obj.ConvertToPrefab(true);
		}

		static void PostSetup(AssetManager man) { }

		internal static string path;
		internal static AssetManager man = new();
		public static T Get<T>(string name) =>
			man.Get<T>(name);
    }
}
