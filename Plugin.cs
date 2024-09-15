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

			yield return "Creating shelf...";

			// Shelf creation
			var blackTexture = TextureExtensions.CreateSolidTexture(1, 1, Color.black);
			var darkWood = Object.Instantiate(man.Get<Texture2D>("woodTexture"));
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
