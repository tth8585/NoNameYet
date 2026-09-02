using System;
using System.Collections.Generic;
using TMPro;
using TTH.Combat.Status;
using UnityEngine;
using UnityEngine.UI;

public class StatusSmokeTestUI : MonoBehaviour
{
    [Header("Status System")]
    [SerializeField] private bool _useUnscaledTime = false;

    [Header("Buttons (Apply/Refresh)")]
    [SerializeField] private Button _btnBerserk;
    [SerializeField] private Button _btnSlow;
    [SerializeField] private Button _btnParalyze;
    [SerializeField] private Button _btnSpeedy;
    [SerializeField] private Button _btnDazed;

    [Header("Durations (seconds)")]
    [SerializeField] private float _durBerserk = 5f;
    [SerializeField] private float _durSlow = 5f;
    [SerializeField] private float _durParalyze = 2f;
    [SerializeField] private float _durSpeedy = 5f;
    [SerializeField] private float _durDazed = 3f;

    [Header("UI Rows (Icon + Text)")]
    [SerializeField] private StatusRow _rowBerserk;
    [SerializeField] private StatusRow _rowSlow;
    [SerializeField] private StatusRow _rowParalyze;
    [SerializeField] private StatusRow _rowSpeedy;
    [SerializeField] private StatusRow _rowDazed;

    private StatusSystem _status;

    private readonly Dictionary<StatusId, StatusRow> _rows = new();

    [Serializable]
    public class StatusRow
    {
        public GameObject Root;      // container row (set active/inactive)
        public Image Icon;           // optional
        public TMP_Text Label;       // "Berserk (2.3s)"
    }

    private void Awake()
    {
        _status = new StatusSystem();

        // map rows
        _rows[StatusId.Berserk] = _rowBerserk;
        _rows[StatusId.Slow] = _rowSlow;
        _rows[StatusId.Paralyze] = _rowParalyze;
        _rows[StatusId.Speedy] = _rowSpeedy;
        _rows[StatusId.Dazed] = _rowDazed;

        // init UI off
        foreach (var kv in _rows)
            SetRowActive(kv.Key, false);

        // wire buttons
        if (_btnBerserk) _btnBerserk.onClick.AddListener(() => Apply(StatusId.Berserk, _durBerserk));
        if (_btnSlow) _btnSlow.onClick.AddListener(() => Apply(StatusId.Slow, _durSlow));
        if (_btnParalyze) _btnParalyze.onClick.AddListener(() => Apply(StatusId.Paralyze, _durParalyze));
        if (_btnSpeedy) _btnSpeedy.onClick.AddListener(() => Apply(StatusId.Speedy, _durSpeedy));
        if (_btnDazed) _btnDazed.onClick.AddListener(() => Apply(StatusId.Dazed, _durDazed));

        // listen status changes
        _status.OnChanged += OnStatusChanged;
    }

    private void OnDestroy()
    {
        if (_status != null)
            _status.OnChanged -= OnStatusChanged;
    }

    private void Update()
    {
        float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _status.Tick(dt);

        // update remaining timers in UI (cheap, 5 statuses)
        UpdateRow(StatusId.Berserk);
        UpdateRow(StatusId.Slow);
        UpdateRow(StatusId.Paralyze);
        UpdateRow(StatusId.Speedy);
        UpdateRow(StatusId.Dazed);
    }

    private void Apply(StatusId id, float duration)
    {
        _status.AddOrRefresh(id, duration);
        // UI will be updated via OnStatusChanged + Update()
    }

    private void OnStatusChanged(StatusId id)
    {
        // ClearAll may invoke default(StatusId). We'll just refresh all.
        RefreshAllRows();
    }

    private void RefreshAllRows()
    {
        foreach (var kv in _rows)
        {
            bool active = _status.Has(kv.Key);
            SetRowActive(kv.Key, active);
            if (active) UpdateRow(kv.Key);
        }
    }

    private void UpdateRow(StatusId id)
    {
        if (!_rows.TryGetValue(id, out var row) || row.Root == null)
            return;

        bool active = _status.Has(id);
        if (!active)
        {
            if (row.Root.activeSelf) row.Root.SetActive(false);
            return;
        }

        if (!row.Root.activeSelf) row.Root.SetActive(true);

        float remain = _status.GetRemainingSeconds(id);

        if (row.Label != null)
        {
            // duration <=0 means infinite in StatusSystem → remaining will be 0; display ∞
            string timeStr = remain > 0f ? $"{remain:0.0}s" : "∞";
            row.Label.text = $"{timeStr}";
        }
    }

    private void SetRowActive(StatusId id, bool active)
    {
        if (!_rows.TryGetValue(id, out var row) || row.Root == null)
            return;
        row.Root.SetActive(active);
    }
}
