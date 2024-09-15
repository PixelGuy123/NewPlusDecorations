
using UnityEngine.AI;
using UnityEngine;

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
	}
}
