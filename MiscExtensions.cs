using System.Collections.Generic;
using HarmonyLib;
using MTM101BaldAPI.Registers;
using PixelInternalAPI.Classes;
using PixelInternalAPI.Extensions;
using UnityEngine;
using UnityEngine.AI;

namespace NewPlusDecorations
{
	internal static class MiscExtensions
	{
		// Create the new sprite like how the mtm101 api does
		public static Sprite Cut(this Sprite ogSprite, int sizeX, int sizeY, int offsetX, int offsetY, float pixelsPerUnit)
		{
			var newSpr = Sprite.Create(
				ogSprite.texture,
				new(ogSprite.rect.x + offsetX, ogSprite.rect.y + offsetY, sizeX, sizeY), // Add the offset and new size for the Rect
				Vector2.one * 0.5f, // Center pivot
				pixelsPerUnit,
				0u,
				SpriteMeshType.FullRect // Use same mesh type as original
			);
			newSpr.name = ogSprite.name + "_Cut";
			return newSpr;
		}

		public static void ReplaceAnimatedRotators(this GameObject rootObject)
		{
			AnimatedSpriteRotator[] animatedRotators = rootObject.GetComponentsInChildren<AnimatedSpriteRotator>(); // Get all rotators

			if (animatedRotators.Length == 0)
			{
				return;
			}

			foreach (var animatedRotator in animatedRotators)
			{
				GameObject targetGameObject = animatedRotator.gameObject;
				Sprite targetSprite = animatedRotator.targetSprite ?? animatedRotator.renderer.sprite; // A failsafe for this case

				// Replicate what AnimatedSpriteRotator.LateUpdate does to find the correct animation frame and sprite map based on the targetSprite
				int foundMapId = -1;
				int foundSpriteId = -1;
				bool wasFound = false;

				for (int i = 0; i < animatedRotator.spriteMap.Length; i++)
				{
					var map = animatedRotator.spriteMap[i];
					// The original logic iterates through the start of each animation sequence.
					for (int j = 0; j < map.SpriteCount; j += map.angleCount)
					{
						// Check for normal sprite and override, bruh!
						if (map.Sprite(j) == targetSprite || (map.HasOverride && map.OverriddenSprite(i) == targetSprite))
						{
							foundMapId = i;
							foundSpriteId = j;
							wasFound = true;
							break;
						}
					}
					if (wasFound)
						break;
				}

				if (!wasFound)
				{
					Debug.LogWarning($"Could not find targetSprite {targetSprite?.name} in the sprite maps for the rotator on {targetGameObject.name}.", targetGameObject);
					continue;
				}

				//New spriterotator data here

				SpriteRotationMap activeMap = animatedRotator.spriteMap[foundMapId];
				int angleCount = activeMap.angleCount;

				// Create flat array of sprites for the new rotator
				Sprite[] newSprites = new Sprite[angleCount];
				Sprite[] sourceSheet = activeMap.HasOverride ? activeMap.overrideSpriteSheet : activeMap.spriteSheet;

				if (sourceSheet.Length < foundSpriteId + angleCount) // Shouldn't happen much
				{
					Debug.LogWarning($"Failed to convert the sprite array because sourceSheet length ({sourceSheet.Length}) is less than the angleCount from id ({foundSpriteId + angleCount})", targetGameObject);
					continue;
				}

				int shift = Mathf.RoundToInt(angleCount * 0.25f); // quarter rotation step
				for (int i = 0; i < angleCount; i++)
				{
					newSprites[(i + shift) % angleCount] = sourceSheet[foundSpriteId + i];
				}

				// Add the SpriteRotator at the end
				SpriteRotator newRotator = targetGameObject.AddComponent<SpriteRotator>();

				newRotator.spriteRenderer = animatedRotator.renderer;
				newRotator.sprites = newSprites;

				// Destroy old comp
				Object.DestroyImmediate(animatedRotator);
			}
		}
		public static BoxCollider AddBoxCollider(this GameObject g, Vector3 center, Vector3 size, bool isTrigger)
		{
			var c = g.AddComponent<BoxCollider>();
			c.center = center;
			c.size = size;
			c.isTrigger = isTrigger;
			return c;
		}
		public static NavMeshObstacle AddNavObstacle(this GameObject g, Vector3 size) =>
			g.AddNavObstacle(Vector3.zero, size);
		public static NavMeshObstacle AddNavObstacle(this GameObject g, Vector3 center, Vector3 size)
		{
			var nav = g.AddComponent<NavMeshObstacle>();
			nav.center = center;
			nav.size = size;
			nav.carving = true;
			return nav;
		}
		public static RendererContainer AddContainer(this GameObject obj, params Renderer[] renderers)
		{
			var r = obj.AddComponent<RendererContainer>();
			r.renderers = renderers;
			return r;
		}
		internal static GameObject RemoveHitbox(this GameObject obj)
		{
			Object.Destroy(obj.GetComponent<Collider>());
			return obj;
		}

		internal static GameObject SetBoxHitbox(this GameObject obj, float x = -1, float y = -1, float z = -1)
		{
			var coll = obj.GetComponent<BoxCollider>();
			coll.size = new(x == -1 ? coll.size.x : x,
				y == -1 ? coll.size.y : y,
				z == -1 ? coll.size.z : z
				);
			return obj;
		}

		internal static GameObject IgnoreRaycast(this GameObject obj)
		{
			obj.layer = LayerStorage.ignoreRaycast;
			return obj;
		}

		internal static List<ItemObject> GetAllShoppingItems()
		{
			List<ItemObject> itmObjs = [];
			foreach (var s in GenericExtensions.FindResourceObjects<SceneObject>())
			{
				s.shopItems.Do(x =>
				{
					var meta = x.selection.GetMeta();
					if (meta != null && !itmObjs.Contains(meta.value))
						itmObjs.Add(meta.value);
				});
			}
			return itmObjs;
		}
	}
}
