namespace ExtEvents.Editor
{
    using System;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.Assertions;

    internal class FoldoutList
    {
        private readonly ReorderableList _list;
        private readonly SerializedProperty _elementsProperty;
        private readonly string _title;

        public Action<Rect, int> DrawElementCallback;
        public Func<int, float> ElementHeightCallback;
        public Action OnAddDropdownCallback;
        public Action<Rect, FoldoutList> DrawFooterCallback;

        private static Action<ReorderableList> _clearCache;
        private static Action<ReorderableList> ClearCache
        {
            get
            {
                if (_clearCache == null)
                {
                    var clearCacheMethod =
                        typeof(ReorderableList).GetMethod("ClearCache", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? typeof(ReorderableList).GetMethod("InvalidateCache", BindingFlags.Instance | BindingFlags.NonPublic); // the name of the method in newer Unity versions.

                    Assert.IsNotNull(clearCacheMethod);
                    // ReSharper disable once AssignNullToNotNullAttribute
                    _clearCache = (Action<ReorderableList>) Delegate.CreateDelegate(typeof(Action<ReorderableList>), clearCacheMethod);
                }

                return _clearCache;
            }
        }

        private static Action<ReorderableList> _cacheIfNeeded;
        private static Action<ReorderableList> CacheIfNeeded
        {
            get
            {
                if (_cacheIfNeeded == null)
                {
                    var cachedIfNeededMethod = typeof(ReorderableList).GetMethod("CacheIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.IsNotNull(cachedIfNeededMethod);
                    // ReSharper disable once AssignNullToNotNullAttribute
                    _cacheIfNeeded = (Action<ReorderableList>) Delegate.CreateDelegate(typeof(Action<ReorderableList>), cachedIfNeededMethod);
                }

                return _cacheIfNeeded;
            }
        }

        private static Action<ReorderableList> _clearCacheRecursive;
        private void ClearCacheRecursive()
        {
            if (_clearCacheRecursive == null)
            {
                var clearCacheRecursive = typeof(ReorderableList).GetMethod("ClearCacheRecursive", BindingFlags.Instance | BindingFlags.NonPublic)
                                          ?? typeof(ReorderableList).GetMethod("InvalidateCacheRecursive", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(clearCacheRecursive);
                // ReSharper disable once AssignNullToNotNullAttribute
                _clearCacheRecursive = (Action<ReorderableList>) Delegate.CreateDelegate(typeof(Action<ReorderableList>), clearCacheRecursive);
            }

            _clearCacheRecursive.Invoke(_list);
        }

        private static bool _triedGetScheduleRemoveField;
        private static FieldInfo _scheduleRemove;
        private bool ScheduleRemove
        {
            get
            {
                if (_scheduleRemove == null && !_triedGetScheduleRemoveField)
                {
                    _scheduleRemove = typeof(ReorderableList).GetField("scheduleRemove", BindingFlags.NonPublic | BindingFlags.Instance);
                    _triedGetScheduleRemoveField = true;
                }

                if (_scheduleRemove == null)
                    return true;

                return (bool) _scheduleRemove.GetValue(_list);
            }
            set
            {
                if (_scheduleRemove == null && !_triedGetScheduleRemoveField)
                {
                    _scheduleRemove = typeof(ReorderableList).GetField("scheduleRemove", BindingFlags.NonPublic | BindingFlags.Instance);
                    _triedGetScheduleRemoveField = true;
                }

                _scheduleRemove?.SetValue(_list, value);
            }
        }

        private static Func<ReorderableList, bool> _isOverMaxMultiEditLimit;
        private static bool IsOverMaxMultiEditLimit(ReorderableList list)
        {
            if (_isOverMaxMultiEditLimit == null)
            {
                var method = typeof(ReorderableList).GetMethod("get_isOverMaxMultiEditLimit", BindingFlags.NonPublic | BindingFlags.Instance);
                _isOverMaxMultiEditLimit = (Func<ReorderableList, bool>) Delegate.CreateDelegate(typeof(Func<ReorderableList, bool>), method);
            }

            return _isOverMaxMultiEditLimit(list);
        }

        /// <summary>
        /// Creates a new instance of foldout reorderable list.
        /// </summary>
        /// <param name="elementsProperty">The property that represents a list of elements that will be drawn.</param>
        /// <param name="title">A title of the list.</param>
        /// <param name="expandedProperty">The bool serialized property that keeps a value representing the expanded state of the list.</param>
        /// <remarks>
        /// We use a bool SerializedProperty instead of _elementsProperty.isExpanded because we can't control which
        /// value will be set by default to isExpanded. It's always false by default until it is set otherwise.
        /// </remarks>
        public FoldoutList(SerializedProperty elementsProperty, string title, SerializedProperty expandedProperty)
        {
            _elementsProperty = elementsProperty;
            _title = title;
            _list = CreateReorderableList(expandedProperty);
        }

        private ReorderableList CreateReorderableList(SerializedProperty expandedProperty)
        {
            return new ReorderableList(_elementsProperty.serializedObject, _elementsProperty)
            {
                drawHeaderCallback = rect =>
                {
                    const float leftMargin = 10f;
                    var shiftedRight = new Rect(rect.x + leftMargin, rect.y, rect.width - leftMargin, rect.height);

                    bool newValue = EditorGUI.Foldout(shiftedRight, expandedProperty.boolValue, _title, true);

                    if (expandedProperty.boolValue == newValue)
                        return;

                    expandedProperty.boolValue = newValue;
                    _list.draggable = newValue; // When the list is folded, draggable should be set to false. Otherwise, its icon will be drawn.
                    ClearCache(_list);
                },
                drawElementCallback = (rect, index, _, __) =>
                {
                    if ( ! expandedProperty.boolValue)
                        return;

                    DrawElementCallback(rect, index);
                },
                elementHeightCallback = index => expandedProperty.boolValue ? ElementHeightCallback(index) : 0f,
                onAddDropdownCallback = (_, __) => OnAddDropdownCallback(),
                drawFooterCallback = rect =>
                {
                    // This will prevent add and remove buttons from being drawn when the list is folded.
                    if ( ! expandedProperty.boolValue)
                        return;

                    if (DrawFooterCallback != null)
                    {
                        DrawFooterCallback.Invoke(rect, this);
                    }
                    else
                    {
                        ReorderableList.defaultBehaviours.DrawFooter(rect, _list);
                    }
                },
                draggable = expandedProperty.boolValue // When the list is folded, draggable should be set to false. Otherwise, its icon will be drawn.
            };
        }

        public void DoList(Rect rect) => _list.DoList(rect);

        public float GetHeight() => _list.GetHeight();

        public void ResetCache()
        {
            ClearCache(_list);
            CacheIfNeeded(_list);
        }

        private static readonly GUIStyle _footerBackground = "RL Footer";
        private static readonly GUIStyle _preButton = (GUIStyle) "RL FooterButton";

        public static void DrawFooter(Rect buttonsRect, FoldoutList list, params ButtonData[] buttons)
        {
            float rightBorder = buttonsRect.xMax - 10f;
            float leftBorder = rightBorder - 8f - buttons.Sum(button => button.Size.x);
            buttonsRect = new Rect(leftBorder, buttonsRect.y, rightBorder - leftBorder, buttonsRect.height);

            if (Event.current.type == EventType.Repaint)
                _footerBackground.Draw(buttonsRect, false, false, false, false);

            leftBorder += 4f;

            foreach (var button in buttons)
            {
                if (button.IsAddButton)
                {
                    using (new EditorGUI.DisabledScope(list._list.onCanAddCallback != null && !list._list.onCanAddCallback(list._list) || IsOverMaxMultiEditLimit(list._list)))
                    {
                        var buttonRect = new Rect(new Vector2(leftBorder, buttonsRect.y), button.Size);
                        if (GUI.Button(buttonRect, button.Content, _preButton))
                        {
                            button.Action?.Invoke(buttonRect, list);
                        }
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledScope(list._list.index < 0 || list._list.index >= list._list.count ||
                                                       list._list.onCanRemoveCallback != null &&
                                                       !list._list.onCanRemoveCallback(list._list) || IsOverMaxMultiEditLimit(list._list)))
                    {
                        var buttonRect = new Rect(new Vector2(leftBorder, buttonsRect.y), button.Size);
                        if (GUI.Button(buttonRect, button.Content, _preButton) || GUI.enabled && list.ScheduleRemove)
                        {
                            button.Action?.Invoke(buttonRect, list);
                        }
                    }
                }

                leftBorder += button.Size.x;
            }

            list.ScheduleRemove = false;
        }

        private static ButtonData _defaultAddButton;
        public static ButtonData DefaultAddButton
        {
            get
            {
                if (_defaultAddButton == null)
                {
                    _defaultAddButton = new ButtonData(new Vector2(25f, 16f),
                        EditorGUIUtility.TrIconContent("Toolbar Plus", "Add to the list"),
                        true,
                        (rect, list) =>
                        {
                            if (list._list.onAddDropdownCallback != null)
                            {
                                list._list.onAddDropdownCallback(rect, list._list);
                            }
                            else if (list._list.onAddCallback != null)
                            {
                                list._list.onAddCallback(list._list);
                            }
                            else
                            {
                                ReorderableList.defaultBehaviours.DoAddButton(list._list);
                            }

                            ReorderableList.ChangedCallbackDelegate onChangedCallback = list._list.onChangedCallback;

                            onChangedCallback?.Invoke(list._list);

                            list.ClearCacheRecursive();
                        });
                }

                return _defaultAddButton;
            }
        }

        private static ButtonData _defaultRemoveButton;
        public static ButtonData DefaultRemoveButton
        {
            get
            {
                if (_defaultRemoveButton == null)
                {
                    _defaultRemoveButton = new ButtonData(new Vector2(25f, 16f),
                        EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove selection from the list"),
                        false,
                        (rect, list) =>
                        {
                            if (list._list.onRemoveCallback == null)
                            {
                                ReorderableList.defaultBehaviours.DoRemoveButton(list._list);
                            }
                            else
                            {
                                list._list.onRemoveCallback(list._list);
                            }

                            ReorderableList.ChangedCallbackDelegate onChangedCallback = list._list.onChangedCallback;
                            onChangedCallback?.Invoke(list._list);
                            list.ClearCacheRecursive();
                            GUI.changed = true;
                        });
                }

                return _defaultRemoveButton;
            }
        }

        public class ButtonData
        {
            public readonly Vector2 Size;
            public readonly GUIContent Content;
            public readonly Action<Rect, FoldoutList> Action;
            public readonly bool IsAddButton;

            public ButtonData(Vector2 size, GUIContent content, bool isAddButton, Action<Rect, FoldoutList> action)
            {
                Size = size;
                Content = content;
                Action = action;
                IsAddButton = isAddButton;
            }
        }
    }
}