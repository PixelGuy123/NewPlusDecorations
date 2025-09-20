using System.Collections;
using UnityEngine;

namespace NewPlusDecorations.Components
{
	public class ClosetDoor : EnvironmentObject, IClickable<int>
	{
		public void Clicked(int player)
		{
			openTimer = 3f;
			audMan.PlaySingle(audOpen);
			if (openTimerEn != null)
				StopCoroutine(openTimerEn);

			openTimerEn = StartCoroutine(OpenTimer());
		}
		public void ClickableSighted(int player) { }
		public void ClickableUnsighted(int player) { }
		public bool ClickableHidden() => !IsClosed;
		public bool ClickableRequiresNormalHeight() => true;
		IEnumerator OpenTimer()
		{
			collider.enabled = false;
			renderer.sprite = open;
			while (openTimer > 0f)
			{
				openTimer -= ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}
			renderer.sprite = closed;
			collider.enabled = true;
			audMan.PlaySingle(audClose);

			yield break;
		}

		Coroutine openTimerEn;
		float openTimer = 0f;
		bool IsClosed => renderer.sprite = closed;
		[SerializeField]
		internal Sprite closed, open;

		[SerializeField]
		internal SpriteRenderer renderer;

		[SerializeField]
		internal PropagatedAudioManager audMan;

		[SerializeField]
		internal SoundObject audOpen, audClose;

		[SerializeField]
		internal Collider collider;
	}
}