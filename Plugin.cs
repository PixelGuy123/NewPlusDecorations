using System.Collections;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OBJImporter;
using MTM101BaldAPI.Registers;
using NewPlusDecorations.Components;
using NewPlusDecorations.Patches;
using PixelInternalAPI.Classes;
using PixelInternalAPI.Extensions;
using PlusStudioLevelLoader;
using UnityEngine;

namespace NewPlusDecorations
{
	[BepInPlugin(guid_DecorationsPlus, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInDependency(guid_PixelIntAPI, BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency(guid_Mtm101API, BepInDependency.DependencyFlags.HardDependency)] // let's not forget this
	[BepInDependency(guid_LevelLoader, BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency(guid_LevelStudio, BepInDependency.DependencyFlags.SoftDependency)]

	public class DecorsPlugin : BaseUnityPlugin
	{
		public const string
		guid_LevelStudio = "mtm101.rulerp.baldiplus.levelstudio",
		guid_LevelLoader = "mtm101.rulerp.baldiplus.levelstudioloader",
		guid_Mtm101API = "mtm101.rulerp.bbplus.baldidevapi",
		guid_PixelIntAPI = "pixelguy.pixelmodding.baldiplus.pixelinternalapi",
		guid_DecorationsPlus = "pixelguy.pixelmodding.baldiplus.newdecors",
		newDecor_PrefabPrefix = "NewDecorationsPlus_";
		private void Awake()
		{
			path = AssetLoader.GetModPath(this);
			var h = new Harmony(guid_DecorationsPlus);
			h.PatchAllConditionals();

			LoadingEvents.RegisterOnAssetsLoaded(Info, Load(), LoadingEventOrder.Pre);
			LoadingEvents.RegisterOnAssetsLoaded(Info, PostLoad, LoadingEventOrder.Post);

			AssetLoader.LoadLocalizationFolder(Path.Combine(path, "Language", "English"), Language.English);

#if KOFI
			MTM101BaldiDevAPI.AddWarningScreen(
				"<color=#c900d4>Ko-fi Exclusive Build!</color>\nKo-fi members helped make this possible. This Lots O\' Items build was made exclusively for supporters. Please, don't share it publicly. If you'd like to support future content, visit my Ko-fi page!",
				false
			);
#endif
		}

		void PostLoad()
		{
			var allShoppingItems = MiscExtensions.GetAllShoppingItems();
			CardboardBox.itemLootBox.AddRange(allShoppingItems);
		}

		IEnumerator Load()
		{
			bool hasEditor = Chainloader.PluginInfos.ContainsKey(guid_LevelStudio);
			yield return loadSteps + (hasEditor ? 1 : 0);

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


				dirs.ForEach(dir => CreatePlane("PlaneDir_" + dir, whiteTex, column.transform, dir.ToVector3() * size.x, new(size.x * 0.2f, size.y * 0.1f, 1f), dir.GetOpposite().ToRotation().eulerAngles).transform.GetChild(0).gameObject.RemoveHitbox());

				column.AddContainer(renderers);
				size.x *= 2f;
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

			var closet = new GameObject("Closet") { layer = LayerStorage.ignoreRaycast };
			closet.AddBoxCollider(Vector3.up * 5f, new(7.2f, 10f, 7.2f), true);
			closet.AddNavObstacle(new(5f, 10f, 5f));
			AddObjectToEditor(closet);
			closet.AddComponent<EnvironmentObjectDistributor>();

			CreateCube("ClosetBase", closetTexture, false, closet.transform, Vector3.down, new(5f, 1f, 5f));
			CreateCube("ClosetRightSide", closetTexture, false, closet.transform, (Vector3.right * 3f) + (Vector3.up * 2.5f), new(1f, 8f, 5f));
			CreateCube("ClosetLeftSide", closetTexture, false, closet.transform, (Vector3.left * 3f) + (Vector3.up * 2.5f), new(1f, 8f, 5f));
			CreateCube("ClosetTop", closetTexture, false, closet.transform, Vector3.up * 7f, new(7f, 1f, 5f)).GetComponent<BoxCollider>().size = new(1f, 3f, 1f);
			CreateCube("ClosetBack", closetTexture, false, closet.transform, (Vector3.back + Vector3.up) * 3f, new(7f, 9f, 1f));

			var spriteObj = ObjectCreationExtensions.CreateSpriteBillboard(closetDoorTexture[0], false).AddSpriteHolder(out var sprite, 0f, LayerStorage.iClickableLayer);
			sprite.name = "ClosetDoorTex";
			renderers[5] = sprite;

			var door = spriteObj;
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

			yield return "Adding couches...";

			MakingCouch("couch.png", "couchBack.png", "Couch", true);
			MakingCouch("redCouch.png", "redCouchBack.png", "RedCouch", false);


			void MakingCouch(string couchTextureName, string couchTextureBackName, string couchName, bool includesCouchComponent)
			{
				renderers = new Renderer[4];
				rendIdx = 0;

				var couch = new GameObject(couchName)
				{
					layer = LayerStorage.iClickableLayer
				};

				couch.AddBoxCollider(Vector3.zero, new(4f, 10f, 4f), true);
				couch.AddNavObstacle(new(4.5f, 10f, 4.5f));

				if (includesCouchComponent)
				{
					var couchComp = couch.AddComponent<Couch>();
					couchComp.camTarget = new GameObject("CouchCam").transform;
					couchComp.camTarget.transform.SetParent(couch.transform);
				}

				AddObjectToEditor(couch);

				var couchTexture = AssetLoader.TextureFromFile(Path.Combine(path, couchTextureName)); //AssetLoader.TextureFromFile(Path.Combine(path, "couch.png"));
				var couchTextBack = AssetLoader.TextureFromFile(Path.Combine(path, couchTextureBackName));
				var sitCollider = CreateCube("CouchSit", couchTexture, false, couch.transform, Vector3.down * 4.2f, new(4.2f, 2f, 4.2f)).SetBoxHitbox(y: 2f).IgnoreRaycast();
				CreateCubeWithRot("CouchBack", couchTextBack, true, couch.transform, new(-2.5f, -3.4f, -2.5f), new(5f, 4.65f, 1f), Vector3.right * 345f).IgnoreRaycast().GetComponent<BoxCollider>().center = Vector3.right * 0.5f;
				CreateCube("CouchSideRight", couchTexture, false, couch.transform, new(2.5f, -3.5f, -0.1f), new(1f, 3.7f, 4.5f)).SetBoxHitbox(y: 10f).IgnoreRaycast();
				CreateCube("CouchSideLeft", couchTexture, false, couch.transform, new(-2.5f, -3.5f, -0.1f), new(1f, 3.7f, 4.5f)).SetBoxHitbox(y: 10f).IgnoreRaycast();

				couch.AddContainer(renderers);
			}

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

			grandFatherClock.AddContainer(renderers);

			yield return "Adding wall shelves...";
			renderers = new Renderer[3];
			rendIdx = 0;
			darkWood = Instantiate(man.Get<Texture2D>("woodTexture")).ApplyLightLevel(-15f);
			darkWood.name = "Times_lessDarkWood";

			shelf = new GameObject("WallShelf")
			{
				layer = LayerStorage.ignoreRaycast
			};
			shelf.AddNavObstacle(new(9.5f, 2.5f, 4.5f));
			shelf.AddBoxCollider(new(0f, 3f, -2.25f), new(9f, 0.8f, 4f), true);
			AddObjectToEditor(shelf);

			CreateCube("ShelfBody", darkWood, false, shelf.transform, new(0f, 3f, -2.25f), new(9, 0.7f, 4f));
			CreateCubeWithRot("ShelfLeftConnection", blackTexture, false, shelf.transform, new(-3f, 1.49f, -3.4f), new(0.5f, 4f, 0.5f), Vector3.right * 45f).RemoveHitbox();
			CreateCubeWithRot("ShelfRightConnection", blackTexture, false, shelf.transform, new(3f, 1.49f, -3.4f), new(0.5f, 4f, 0.5f), Vector3.right * 45f).RemoveHitbox();

			shelf.AddContainer(renderers);

			yield return "Adding long office table...";

			renderers = new Renderer[3];
			rendIdx = 0;
			shelf = new GameObject("LongOfficeTable")
			{
				layer = LayerStorage.ignoreRaycast
			};
			shelf.AddNavObstacle(new(22f, 10f, 6.5f));
			shelf.AddBoxCollider(Vector3.zero, new(21f, 2f, 6f), false);
			AddObjectToEditor(shelf);

			CreateCube("TableBody", closetTexture, false, shelf.transform, Vector3.zero, new(21f, 1f, 6f)).RemoveHitbox();
			CreateCube("TableLongRightLeg", closetTexture, false, shelf.transform, new(10f, -2f, 0f), new(1f, 3f, 6f)).RemoveHitbox();
			CreateCube("TableLongLeftLeg", closetTexture, false, shelf.transform, new(-10f, -2f, 0f), new(1f, 3f, 6f)).RemoveHitbox();

			shelf.AddContainer(renderers);

			yield return "Loading the Slide obj...";
			var slide = SetupObjCollisionAndScale(LoadObjFile("Slide"), new(15f, 10f, 4.5f), 0.25f, addMeshCollider: false);
			slide.layer = LayerStorage.ignoreRaycast;
			slide.AddBoxCollider(Vector3.up * 5f, new(14f, 5f, 5.55f), false);
			slide.name = "Slide";
			AddObjectToEditor(slide);

			yield return "Loading the MonkeyBars obj...";
			slide = SetupObjCollisionAndScale(LoadObjFile("monkeyBars"), default, 0.25f, addMeshCollider: false);

			AddAdditionalCollider(slide.transform, LayerStorage.ignoreRaycast, new(1f, 5f, 1.5f), new(4.5f, 5f, 0f), new(3.85f, 10f, 2.5f), Vector3.right * 4.5f);
			AddAdditionalCollider(slide.transform, LayerStorage.ignoreRaycast, new(1f, 5f, 1.5f), new(-4.25f, 5f, 0f), new(3.85f, 10f, 2.5f), Vector3.left * 4.5f);

			slide.name = "Monkeybars";
			AddObjectToEditor(slide);

			yield return "Loading the Seesaw obj...";
			slide = SetupObjCollisionAndScale(LoadObjFile("Seesaw"), new(15.85f, 10f, 4.5f), 0.35f, addMeshCollider: false);
			slide.layer = LayerStorage.ignoreRaycast;
			slide.AddBoxCollider(Vector3.up * 5f, new(14f, 5f, 3f), false);
			slide.name = "Seesaw";
			AddObjectToEditor(slide);

			yield return "Loading the DinnerTable obj";
			slide = SetupObjCollisionAndScale(LoadObjFile("DinnerTable"), default, 0.15f, addMeshCollider: false);
			slide.layer = LayerStorage.ignoreRaycast;
			slide.AddBoxCollider(Vector3.up * 5f, new(10f, 10f, 6f), false);
			slide.AddNavObstacle(Vector3.up * 5f, new(10.5f, 10f, 6.5f));
			slide.name = "DinnerTable";
			AddObjectToEditor(slide);

			yield return "Loading the DinnerSeat obj";
			slide = SetupObjCollisionAndScale(LoadObjFile("DinnerSeat"), default, 0.25f, addMeshCollider: false);
			slide.layer = LayerStorage.ignoreRaycast;
			slide.AddBoxCollider(Vector3.up * 5f, new(10f, 10f, 6f), false);
			slide.AddNavObstacle(Vector3.up * 5f, new(10.5f, 10f, 6.5f));
			slide.name = "DinnerSeat";
			AddObjectToEditor(slide);

			yield return "Loading the DinnerMenu obj";
			slide = SetupObjCollisionAndScale(LoadObjFile("DinnerMenu"), default, 0.65f, addMeshCollider: false);

			slide.layer = LayerStorage.ignoreRaycast;
			slide.AddBoxCollider(Vector3.up * 2.5f, new(6.5f, 5f, 2f), false);
			slide.AddNavObstacle(Vector3.up * 2.5f, new(7f, 5f, 2f));
			slide.name = "DinnerMenu";
			AddObjectToEditor(slide);

			yield return "Loading the CardboardBox obj";
			slide = SetupObjCollisionAndScale(LoadObjFile("CardboardBox"), default, 0.99f, addMeshCollider: false);

			slide.AddNavObstacle(Vector3.up * 5f, new(5.5f, 10f, 5.5f));
			slide.name = "CardboardBox";

			var cardboardBox = slide.AddComponent<CardboardBox>();
			cardboardBox.audNope = ((ITM_PortalPoster)ItemMetaStorage.Instance.FindByEnum(Items.PortalPoster).value.item).audNo;
			cardboardBox.audSlide = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(path, "CardboardBox_Slide.wav")), string.Empty, SoundType.Effect, Color.white);
			cardboardBox.audSlide.subtitle = false;
			cardboardBox.audMan = slide.CreatePropagatedAudioManager(45f, 85f);

			cardboardBox.collider = slide.AddBoxCollider(Vector3.up * 5f, new(5f, 10f, 5f), false);

			AddObjectToEditor(slide);

			yield return "Loading the Swingset obj...";
			slide = SetupObjCollisionAndScale(LoadObjFile("Swingset"), new(15f, 10f, 4.5f), 0.3f, addMeshCollider: false);

			slide.layer = LayerStorage.ignoreRaycast;
			slide.AddBoxCollider(Vector3.up * 5f, new(8.85f, 5f, 5f), false);

			slide.name = "Swingset";
			AddObjectToEditor(slide);

			yield return "Loading the Metal Chair obj...";
			slide = SetupObjCollisionAndScale(LoadObjFile("MetalChair"), new(2f, 10f, 2f), 1f, addMeshCollider: false);

			slide.layer = LayerStorage.ignoreRaycast;
			slide.AddBoxCollider(Vector3.up * 5f, new(1.75f, 5f, 1.75f), false);

			slide.name = "MetalChair";
			AddObjectToEditor(slide);

			yield return "Loading the Metal Desk obj...";
			slide = SetupObjCollisionAndScale(LoadObjFile("MetalDesk"), new(12f, 10f, 4.5f), 1f, addMeshCollider: false);

			slide.layer = LayerStorage.ignoreRaycast;
			slide.AddBoxCollider(Vector3.up * 5f, new(11f, 5f, 4f), false);

			slide.name = "MetalDesk";
			AddObjectToEditor(slide);

			yield return "Creating the pavement variants...";

			CreatePavement("pavementCover", "lineStraight.png");
			CreatePavement("pavementCorner", "lineStraight_corner.png");
			CreatePavement("pavementOutCorner", "lineStraight_outCorner.png");
			CreatePavement("pavementLcover", "lineStraight_Lcover.png");
			CreatePavement("pavementRcover", "lineStraight_Rcover.png");

			yield return "Creating the picnic sheet...";

			CreatePavement("OutsidePicnicSheet", "picnic_texture.png", 13.5f);

			void CreatePavement(string name, string fileName, float pixelsPerUnit = 12.8f)
			{
				var pavement = ObjectCreationExtensions.CreateSpriteBillboard(AssetLoader.SpriteFromFile(Path.Combine(path, fileName), Vector2.one * 0.5f, pixelsPerUnit), false)
				.AddSpriteHolder(out var pavementRenderer, 0.01f, 0);

				pavementRenderer.name = name + "_Renderer";
				pavementRenderer.gameObject.layer = 0;
				pavementRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

				pavement.name = name;
				AddObjectToEditor(pavement.gameObject);
			}

			yield return "Creating the Bush...";

			var bush = AddDecorationWithHolder("PlaygroundBush", "bush.png", 20f, Vector3.up * 3.5f);
			bush.gameObject.AddBoxCollider(Vector3.up * 5f, new(3f, 5f, 3f), true);
			bush.gameObject.layer = LayerStorage.iClickableLayer;

			var bushObj = bush.gameObject.AddComponent<Bush>();
			bushObj.audMan = bush.gameObject.CreatePropagatedAudioManager(45f, 70f);

			bushObj.audEnter = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(path, "bushEnter.mp3")), string.Empty, SoundType.Effect, Color.white);
			bushObj.audEnter.subtitle = false;

			bushObj.audLeave = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(path, "bushLeave.mp3")), string.Empty, SoundType.Effect, Color.white);
			bushObj.audLeave.subtitle = false;

			bushObj.cameraTransform = new GameObject("CameraTransform").transform;
			bushObj.cameraTransform.SetParent(bushObj.transform);
			bushObj.cameraTransform.localPosition = Vector3.up * 4f;

			bushObj.hud = ObjectCreationExtensions.CreateCanvas();
			bushObj.hud.name = "BushCanvas";
			bushObj.hud.transform.SetParent(bushObj.transform);
			bushObj.hud.gameObject.SetActive(false);

			ObjectCreationExtensions.CreateImage(bushObj.hud, AssetLoader.SpriteFromFile(Path.Combine(path, "bushInside.png"), Vector2.one * 0.5f));

			yield return "Creating birds...";

			const float birdPixsUnit = 10f;
			var birdWingSound = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(path, "birdFlapWing.wav")), string.Empty, SoundType.Effect, Color.white);
			CreateBird(TextureExtensions.LoadSpriteSheet(8, 1, birdPixsUnit, path, "greenBird.png"), "GreenBird");
			CreateBird(TextureExtensions.LoadSpriteSheet(8, 1, birdPixsUnit, path, "orangeBird.png"), "OrangeBird");
			CreateBird(TextureExtensions.LoadSpriteSheet(8, 1, birdPixsUnit, path, "purpleBird.png"), "PurpleBird");

			void CreateBird(Sprite[] sprs, string name)
			{
				var birdObj = ObjectCreationExtensions.CreateSpriteBillboard(sprs[0]).AddSpriteHolder(out var birdRenderer, 2.35f);
				birdObj.name = name;
				birdRenderer.name = name + "_Renderer";

				var bird = birdObj.gameObject.AddComponent<Bird>();
				bird.renderer = birdRenderer;

				var birdTrigger = new GameObject(name + "_Trigger")
				{
					layer = LayerStorage.ignoreRaycast
				};

				birdTrigger.transform.SetParent(birdObj.transform);
				birdTrigger.transform.localPosition = Vector3.zero;

				bird.collider = birdTrigger.gameObject.AddComponent<CapsuleCollider>();
				bird.collider.isTrigger = true;
				bird.collider.height = 1f;
				bird.collider.radius = 35f;

				birdTrigger.gameObject.AddComponent<BirdTrigger>().bird = bird;

				AddObjectToEditor(birdObj.gameObject);

				bird.audMan = bird.gameObject.CreatePropagatedAudioManager(55f, 125f);
				bird.audFlyAway = birdWingSound;
				bird.audFlyAway.subtitle = false;
				bird.sprIdle = sprs.Take(2);
				bird.sprFloorEat = sprs.Skip(2).Take(2);
				bird.sprFlyingAway = [sprs[4], sprs[5]];
				bird.rotator = birdRenderer.CreateAnimatedSpriteRotator(
					GenericExtensions.CreateRotationMap(2, sprs[4], sprs[6]),
					GenericExtensions.CreateRotationMap(2, sprs[5], sprs[7])
					);
			}


			yield return "Creating misc decorations...";
			// Misc Decorations
			AddDecoration("SmallPottedPlant", "plant.png", 25f);
			AddDecoration("TableLightLamp", "tablelamp.png", 25f);
			AddDecoration("BaldiPlush", "baldiPlush.png", 35f);
			AddDecoration("FancyOfficeLamp", "veryLikeOfficeLamp.png", 29f);
			AddDecoration("SaltAndHot", "saltObjects.png", 26f);
			AddDecoration("TheRulesBook", "TheRulesBook.png", 25f);
			AddDecoration("PencilHolder", "PencilHolder.png", 25f);

			SpriteRenderer AddDecoration(string name, string fileName, float pixelsPerUnit)
			{
				var bred = ObjectCreationExtensions.CreateSpriteBillboard(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(path, fileName)), pixelsPerUnit));
				bred.name = name;
				AddObjectToEditor(bred.gameObject);
				//"editorPrefab_"
				return bred;
			}

			RendererContainer AddDecorationWithHolder(string name, string fileName, float pixelsPerUnit, Vector3 offset)
			{
				var bred = ObjectCreationExtensions.CreateSpriteBillboard(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(path, fileName)), pixelsPerUnit))
				.AddSpriteHolder(out _, offset);
				bred.name = name;
				AddObjectToEditor(bred.gameObject);
				return bred;
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
				var planeHolder = new GameObject(planeName);
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

			GameObject LoadObjFile(string objName) =>
				new OBJLoader().Load(
					Path.Combine(path, "objModels", objName, $"{objName}.obj"),
					Path.Combine(path, "objModels", objName, $"{objName}.mtl"),
					ObjectCreationExtension.defaultMaterial
					);



			if (hasEditor)
			{
				yield return "Triggering editor integration...";
				EditorIntegration.Initialize(man);
			}
			yield return "Triggering post setup...";


			PostSetup(man);
		}

		const int loadSteps = 21;

		GameObject SetupObjCollisionAndScale(GameObject obj, Vector3 navMeshSize, float newScale, bool automaticallyContainer = true, bool addMeshCollider = true)
		{
			obj.transform.localScale = Vector3.one;
			if (navMeshSize != default)
				obj.gameObject.AddNavObstacle(navMeshSize);

			var childRef = new GameObject(obj.name + "_Renderer");
			childRef.transform.SetParent(obj.transform);
			childRef.transform.localPosition = Vector3.zero;

			var childs = obj.transform.AllChilds();
			childs.ForEach(c =>
			{
				if (c == childRef.transform)
					return;
				c.SetParent(childRef.transform);
				c.transform.localPosition = Vector3.zero;
				c.transform.localScale = Vector3.one * newScale;
				if (addMeshCollider)
					c.gameObject.AddComponent<MeshCollider>();
			});

			if (automaticallyContainer)
				obj.AddContainer(obj.GetComponentsInChildren<MeshRenderer>());


			return obj;
		}

		void AddAdditionalCollider(Transform owner, LayerMask layer, Vector3 size, Vector3 center, Vector3 navMeshSize, Vector3 navMeshCenter)
		{
			var collider = new GameObject("Collider")
			{
				layer = layer
			};

			collider.transform.SetParent(owner);
			collider.transform.localPosition = Vector3.zero;

			collider.gameObject.AddBoxCollider(center, size, false);
			collider.gameObject.AddNavObstacle(navMeshCenter, navMeshSize);
		}

		void AddObjectToEditor(GameObject obj)
		{
			LevelLoaderPlugin.Instance.basicObjects.Add(newDecor_PrefabPrefix + obj.name, obj);
			man.Add($"editorPrefab_{obj.name}", obj);
			obj.ConvertToPrefab(true);

			// Debug.Log($"\"{obj.name}\": \"{newDecor_PrefabPrefix + obj.name}\""); // Constructing the filter for the ConverterTool
		}

		static void PostSetup(AssetManager man) { }

		internal static string path;
		internal static AssetManager man = new();
		public static T Get<T>(string name) =>
			man.Get<T>(name);
	}

	static class ArrayExtensions
	{
		public static T[] Skip<T>(this T[] ar, int count)
		{
			var newAr = new T[ar.Length - count];
			int index = count;
			for (int z = 0; z < newAr.Length; z++)
				newAr[z] = ar[index++];
			return newAr;
		}
		public static T[] Take<T>(this T[] ar, int count) =>
			ar.Take(0, count);
		public static T[] Take<T>(this T[] ar, int index, int count)
		{
			var newAr = new T[count];
			for (int z = 0; z < count; z++)
				newAr[z] = ar[index++];
			return newAr;
		}
	}
}
