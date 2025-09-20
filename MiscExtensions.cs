
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
