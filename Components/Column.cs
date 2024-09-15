using UnityEngine;

namespace NewPlusDecorations.Components
{
	public class Column : EnvironmentObject
	{
		public override void LoadingFinished()
		{
			base.LoadingFinished();
			for (int i = 0; i < renderer.Length; i++)
				renderer[i].material.mainTexture = ec.CellFromPosition(transform.position).room.wallTex;
		}

		[SerializeField]
		internal MeshRenderer[] renderer;
	}
}
