using UnityEngine;

namespace NewPlusDecorations.Components
{
	public class NoCollisionOnStart : MonoBehaviour
	{
		void Start() =>
			Destroy(toDestroy);

		[SerializeField]
		internal Collider toDestroy;
	}
}
