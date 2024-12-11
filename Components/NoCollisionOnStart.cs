using UnityEngine;

namespace NewPlusDecorations.Components
{
	public class NoCollisionOnStart : MonoBehaviour
	{
		void Start() =>
			Destroy(GetComponent<Collider>());
	}
}
