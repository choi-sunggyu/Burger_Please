
using System;
using System.Collections.Generic;
using UnityEngine;

public class EmotionIconManager : MonoBehaviour
{
    [Serializable]
    private class EmotionSpriteEntry
    {
        public EmotionType emotion;
        public Sprite sprite;
    }

    [Header("UI")]
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private EmotionIconUI iconPrefab;
    [SerializeField] private Camera worldCamera;

    [Header("Sprites")]
    [SerializeField]
    private List<EmotionSpriteEntry> sprites = new();

    [Header("Settings")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private int maxVisibleIcons = 30;

    private readonly Dictionary<EmotionType, Sprite>
        spriteLookup = new();

    private readonly Dictionary<WorkerEmotion, EmotionIconUI>
        activeIcons = new();

    private readonly Dictionary<EmotionIconUI, WorkerEmotion>
        owners = new();

    private readonly Stack<EmotionIconUI>
        iconPool = new();

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        foreach (var entry in sprites)
        {
            if (entry.sprite != null)
                spriteLookup[entry.emotion] = entry.sprite;
        }
    }

    private void OnEnable()
    {
        WorkerEmotion.EmotionRequested += OnEmotionRequested;
        WorkerEmotion.WorkerDisabled += OnWorkerDisabled;
    }

    private void OnEmotionRequested(
        WorkerEmotion worker,
        EmotionType emotion)
    {
        if (worker == null ||
            canvasRoot == null ||
            iconPrefab == null ||
            worldCamera == null)
            return;

        if (!spriteLookup.TryGetValue(emotion, out Sprite sprite))
            return;

        EmotionIconUI icon;

        if (!activeIcons.TryGetValue(worker, out icon) ||
            icon == null)
        {
            if (activeIcons.Count >= maxVisibleIcons)
                return;

            if (iconPool.Count > 0)
                icon = iconPool.Pop();
            else
                icon = Instantiate(iconPrefab, canvasRoot);

            if (icon == null)
                return;

            activeIcons[worker] = icon;
            owners[icon] = worker;
        }

        icon.Play(
            worker.FollowTarget,
            worker.Offset,
            sprite,
            duration,
            canvasRoot,
            worldCamera,
            OnIconFinished
        );
    }

    private void OnIconFinished(EmotionIconUI icon)
    {
        if (!owners.TryGetValue(icon, out var worker))
            return;

        Release(worker, icon);
    }

    private void OnWorkerDisabled(WorkerEmotion worker)
    {
        if (activeIcons.TryGetValue(worker, out var icon))
            Release(worker, icon);
    }

    private void Release(
        WorkerEmotion worker,
        EmotionIconUI icon)
    {
        activeIcons.Remove(worker);
        owners.Remove(icon);

        if (icon == null)
            return;

        icon.HideImmediately();
        iconPool.Push(icon);
    }

    private void OnDisable()
    {
        WorkerEmotion.EmotionRequested -= OnEmotionRequested;
        WorkerEmotion.WorkerDisabled -= OnWorkerDisabled;

        foreach (var icon in activeIcons.Values)
        {
            if (icon != null)
            {
                icon.HideImmediately();
                iconPool.Push(icon);
            }
        }

        activeIcons.Clear();
        owners.Clear();
    }
}
