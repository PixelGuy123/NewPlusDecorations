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

			// Shelf creation
			var darkWood = Instantiate(man.Get<Texture2D>("woodTexture"));
			darkWood.name = "Times_darkWood";
			var shelf = new GameObject("ClosetShelf");
			shelf.gameObject.AddBoxCollider(Vector3.zero, new(4f, 10f, 15f), false);
			shelf.gameObject.AddNavObstacle(new(4.2f, 10f, 16.3f));
			shelf.layer = LayerStorage.ignoreRaycast;

			var shelfBody = ObjectCreationExtension.CreateCube(darkWood.ApplyLightLevel(-25f), false);
			shelfBody.transform.SetParent(shelf.transform);
			shelfBody.transform.localPosition = Vector3.up * 4f;
			shelfBody.transform.localScale = new(4f, 0.7f, 15f);
			Destroy(shelfBody.GetComponent<Collider>());

			var rendererCont = shelf.AddContainer(shelfBody.GetComponent<MeshRenderer>());

			ShelfLegCreator(new(-1.5f, 2.3f, 6.5f));
			ShelfLegCreator(new(1.5f, 2.3f, -6.5f));
			ShelfLegCreator(new(-1.5f, 2.3f, -6.5f));
			ShelfLegCreator(new(1.5f, 2.3f, 6.5f));

			void ShelfLegCreator(Vector3 pos)
			{
				var shelfLeg = ObjectCreationExtension.CreatePrimitiveObject(PrimitiveType.Cylinder, blackTexture);
				shelfLeg.transform.SetParent(shelf.transform);
				shelfLeg.transform.localPosition = pos;
				shelfLeg.transform.localScale = new(0.8f, 2.3f, 0.8f);
				Destroy(shelfLeg.GetComponent<Collider>());
				rendererCont.renderers = rendererCont.renderers.AddToArray(shelfLeg.GetComponent<MeshRenderer>());
			}

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

				Directions.All().ForEach(columnSide);

				void columnSide(Direction dir)
				{
					var planeHolder = new GameObject("PlaneDir_" + dir);
					planeHolder.transform.SetParent(column.transform);
					planeHolder.transform.localPosition = dir.ToVector3() * size.x;
					planeHolder.transform.rotation = dir.GetOpposite().ToRotation();
					planeHolder.transform.localScale = new(size.x * 0.2f, size.y * 0.1f, 1f);

					var plane = Instantiate(man.Get<GameObject>("PlaneTemplate"), planeHolder.transform);
					plane.transform.localPosition = Vector3.zero;

					plane.GetComponent<MeshRenderer>().material.SetMainTexture(whiteTex);
					plane.name = "PlaneRendererDir_" + dir;
				}

				var colrender = column.GetComponentsInChildren<MeshRenderer>();
				column.AddContainer(colrender);
				column.AddBoxCollider(Vector3.zero, new(size.x, size.y, size.x), false);
				column.AddNavObstacle(new(size.x, size.y, size.x));

				AddObjectToEditor(column);
				var actualColumn = column.AddComponent<Column>();
				actualColumn.renderer = colrender;
				return actualColumn;


			}

			yield return "Adding closet...";
			// Closets

			var closetTexture = AssetLoader.TextureFromFile(Path.Combine(path, "closetwood.png"));
			var closetDoorTexture = TextureExtensions.LoadSpriteSheet(2, 1, 25f, path, "closetdoors.png");

			Renderer[] renderers = new Renderer[6];

			var closet = new GameObject("Closet")
			{
				layer = LayerStorage.ignoreRaycast
			};
			closet.AddBoxCollider(Vector3.zero, new(5f, 10f, 5f), true);
			closet.AddNavObstacle(new(5f, 10f, 5f));
			closet.AddComponent<EnvironmentObjectDistributor>();
			AddObjectToEditor(closet);

			var closetBase = ObjectCreationExtension.CreateCube(closetTexture, false);
			closetBase.name = "ClosetBase";
			closetBase.transform.SetParent(closet.transform);
			closetBase.transform.localPosition = Vector3.down;
			closetBase.transform.localScale = new(5f, 1f, 5f);

			renderers[0] = closetBase.GetComponent<MeshRenderer>();

			renderers[1] = ClosetSide(Vector3.right * 3f);
			renderers[2] = ClosetSide(Vector3.left * 3f);

			MeshRenderer ClosetSide(Vector3 offset)
			{
				var closetSide = ObjectCreationExtension.CreateCube(closetTexture, false);
				closetSide.name = "ClosetSide";
				closetSide.transform.SetParent(closet.transform);
				closetSide.transform.localPosition = offset + Vector3.up * 2.5f;
				closetSide.transform.localScale = new(1f, 8f, 5f);
				return closetSide.GetComponent<MeshRenderer>();
			}

			closetBase = ObjectCreationExtension.CreateCube(closetTexture, false); // Top
			closetBase.name = "ClosetTop";
			closetBase.transform.SetParent(closet.transform);
			closetBase.transform.localPosition = Vector3.up * 7f;
			closetBase.transform.localScale = new(7f, 1f, 5f);
			closetBase.GetComponent<BoxCollider>().size = new(1f, 3f, 1f);
			renderers[3] = closetBase.GetComponent<MeshRenderer>();

			closetBase = ObjectCreationExtension.CreateCube(closetTexture, false); // Back
			closetBase.name = "ClosetBack";
			closetBase.transform.SetParent(closet.transform);
			closetBase.transform.localPosition = (Vector3.back + Vector3.up) * 3f;
			closetBase.transform.localScale = new(7f, 9f, 1f);
			renderers[4] = closetBase.GetComponent<MeshRenderer>();

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



			yield return "Creating misc decorations...";
			// Misc Decorations
			AddDecoration("SmallPottedPlant", "plant.png", 25f, Vector3.up);

			void AddDecoration(string name, string fileName, float pixelsPerUnit, Vector3 offset)
			{
				var bred = ObjectCreationExtensions.CreateSpriteBillboard(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(path, fileName)), pixelsPerUnit)).AddSpriteHolder(offset);
				bred.transform.parent.name = name;
				bred.name = name;
				AddObjectToEditor(bred.transform.parent.gameObject);
				//"editorPrefab_"
			}

			yield return "Triggering post setup...";


			PostSetup(man);
		}

		const int loadSteps = 5;

		void AddObjectToEditor(GameObject obj)
		{
			PlusLevelLoaderPlugin.Instance.prefabAliases.Add(obj.name, obj);
			man.Add($"editorPrefab_{obj.name}", obj);
			obj.ConvertToPrefab(true);
		}

		static void PostSetup(AssetManager man) { }

		internal static string path;
		public static AssetManager man = new();
    }
}
