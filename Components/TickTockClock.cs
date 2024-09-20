using UnityEngine;

namespace NewPlusDecorations.Components
{
    internal class TickTockClock : EnvironmentObject
    {
		void Update()
		{
			delay -= ec.EnvironmentTimeScale * Time.deltaTime;
			if (delay < 0f)
			{
				delay += tickTockDelay;
				audMan.PlaySingle(tickTock ? audTick : audTock);
				renderer.sprite = tickTock ? sprTick : sprTock;
				tickTock = !tickTock;
			}
		}

		float delay = 1f;
		bool tickTock = false;

		[SerializeField]
		internal AudioManager audMan;

		[SerializeField]
		internal SoundObject audTick, audTock;

		[SerializeField]
		internal SpriteRenderer renderer;

		[SerializeField]
		internal Sprite sprTick, sprTock;

		[SerializeField]
		internal float tickTockDelay = 1.5f;
	}
}
