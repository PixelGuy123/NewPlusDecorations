﻿using BepInEx;
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
			int rendIdx = 0;

			var closet = new GameObject("Closet")
			{
				layer = LayerStorage.ignoreRaycast
			};
			closet.AddBoxCollider(Vector3.zero, new(5f, 10f, 5f), true);
			closet.AddNavObstacle(new(5f, 10f, 5f));
			closet.AddComponent<EnvironmentObjectDistributor>();
			AddObjectToEditor(closet);

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

			renderers = new MeshRenderer[4];
			rendIdx = 0;

			var couch = new GameObject("Couch")
			{
				layer = LayerStorage.iClickableLayer
			};

			couch.AddBoxCollider(Vector3.zero, new(2f, 10f, 2f), false);
			couch.AddNavObstacle(new(2f, 10f, 2f));
			AddObjectToEditor(couch);

			var couchTexture = AssetLoader.TextureFromFile(Path.Combine(path, "couchtexture.png"));
			CreateCube("CouchSit", couchTexture, false, couch.transform, Vector3.down * 4.2f, new(4.2f, 2f, 4.2f));
			CreateCubeWithRot("CouchBack", couchTexture, false, couch.transform, Vector3.down * 1.6f + Vector3.back * 2.35f, new(4f, 3.5f, 1f), Vector3.right * 345f);
			CreateCube("CouchFeet", closetTexture, false, couch.transform, Vector3.down * 3f, new(1f, 1f, 1f)); // Finish this <<



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
				var cube = ObjectCreationExtension.CreateCube(texture, false);
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

			yield return "Triggering post setup...";


			PostSetup(man);
		}

		const int loadSteps = 6;

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
