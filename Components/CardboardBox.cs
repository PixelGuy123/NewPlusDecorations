using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewPlusDecorations.Components;

public class CardboardBox : EnvironmentObject, IItemAcceptor, IClickable<int>
{
    [SerializeField]
    internal float moveOffset = 5f;
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
        float slideDuration = audSlide.subDuration;
        audMan.PlaySingle(audSlide);

        while (t < slideDuration)
        {
            t += ec.EnvironmentTimeScale * Time.deltaTime;
            transform.position = Vector3.Lerp(start, newPos, Mathf.Clamp01(t / slideDuration));
            yield return null;
        }

        transform.position = newPos;
        currentCell = ec.CellFromPosition(transform.position);
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

    public override void LoadingFinished()
    {
        base.LoadingFinished();
        currentCell = ec.CellFromPosition(transform.position);
    }
    public void Clicked(int player)
    {
        if (sliding) return;
        var pm = Singleton<CoreGameManager>.Instance.GetPlayer(player);

        // Get dir from forward, then get the targetPos
        Vector3 targetPos = transform.position + Directions.DirFromVector3(pm.transform.forward, 45f).ToVector3() * moveOffset;

        // Collision check with the boxcollider bounds
        var checkBounds = collider.bounds;
        checkBounds.center = targetPos;

        var overlapping = Physics.OverlapBox(checkBounds.center, checkBounds.extents * 0.9f, transform.rotation, -1, QueryTriggerInteraction.Ignore);
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

    Cell currentCell;
    Coroutine goToCoroutine;
    bool sliding = false;
    internal static List<ItemObject> itemLootBox = [];
    internal static HashSet<Items> acceptableCuttingItems = [Items.Scissors];
    public static void AddCuttingItem(Items item) => acceptableCuttingItems.Add(item);
}