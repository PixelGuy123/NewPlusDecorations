using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewPlusDecorations.Components;

public class CardboardBox : EnvironmentObject, IItemAcceptor, IClickable<int>
{
    [SerializeField]
    internal float moveOffset = 5f, boundExtentFactor = 0.98f, slideTime = 0.85f;
    [SerializeField]
    [Range(0f, 1f)]
    internal float itemRewardChance = 0.25f;
    [SerializeField]
    internal SoundObject audSlide, audNope;
    [SerializeField]
    internal AudioManager audMan;
    [SerializeField]
    internal BoxCollider collider;
    IEnumerator GoTo(Vector3 newPos)
    {
        sliding = true;

        Vector3 start = transform.position;
        float t = 0f;

        // Duration goes by the sound
        audMan.PlaySingle(audSlide);
        bool interruptLoop = false;
        while (t < slideTime)
        {
            t += ec.EnvironmentTimeScale * Time.deltaTime;
            Vector3 nextPos = Vector3.Lerp(start, newPos, Mathf.Clamp01(t / slideTime));

            // Check entity overlapping
            var checkBounds = collider.bounds;
            checkBounds.center = nextPos;
            var overlapping = Physics.OverlapBox(checkBounds.center, checkBounds.extents * boundExtentFactor, transform.rotation, -1, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlapping.Length; i++)
            {
                var o = overlapping[i];
                if (o.transform != transform) // If there's anything interrupting, stop the box
                {
                    interruptLoop = true;
                    break;
                }
            }
            if (interruptLoop) break;

            transform.position = nextPos;

            yield return null;
        }

        if (!interruptLoop) transform.position = newPos;
        else audMan.FlushQueue(true);

        sliding = false;
        yield break;
    }

    public void InsertItem(PlayerManager player, EnvironmentController ec)
    {
        Destroy(gameObject); // Destroys itself
        if (itemRewardChance < Random.value)
            return;
        ec.CreateItem(ec.CellFromPosition(transform.position).room, itemLootBox[Random.Range(0, itemLootBox.Count)], new(transform.position.x, transform.position.z));
        ec.items.RemoveAt(ec.items.Count - 1); // Removes the itemObject since it is not naturally generated
    }

    public bool ItemFits(Items item) => acceptableCuttingItems.Contains(item);

    public void Clicked(int player)
    {
        if (sliding) return;

        // Get dir from forward, then get the targetPos
        Vector3 dir = Directions.DirFromVector3(Singleton<CoreGameManager>.Instance.GetCamera(player).transform.forward, 45f).ToVector3();
        Vector3 targetPos = transform.position + dir * moveOffset;
        Vector3 nextFramePos = transform.position + dir * 0.2f;

        // Collision check with the boxcollider bounds for the next frame
        var checkBounds = collider.bounds;
        checkBounds.center = nextFramePos;

        var overlapping = Physics.OverlapBox(checkBounds.center, checkBounds.extents * boundExtentFactor, transform.rotation, -1, QueryTriggerInteraction.Ignore);
        bool blocked = false;
        foreach (var o in overlapping)
        {
            if (o.transform == transform) continue; // allow overlapping with self
            blocked = true;
            break;
        }

        if (blocked)
        {
            Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audNope);
            return; // Shouldn't go if there's any block
        }

        // Start movement coroutine
        if (goToCoroutine != null)
            StopCoroutine(goToCoroutine);

        goToCoroutine = StartCoroutine(GoTo(targetPos));
    }

    public void ClickableSighted(int player) { }

    public void ClickableUnsighted(int player) { }

    public bool ClickableHidden() => sliding;

    public bool ClickableRequiresNormalHeight() => true;

    Coroutine goToCoroutine;
    bool sliding = false;
    internal static List<ItemObject> itemLootBox = [];
    internal static HashSet<Items> acceptableCuttingItems = [Items.Scissors];
    public static void AddCuttingItem(Items item) => acceptableCuttingItems.Add(item);
}