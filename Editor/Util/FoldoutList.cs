namespace ExtEvents.Editor
{
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.Assertions;

    internal class FoldoutList
    {
        private readonly SerializedProperty _elementsProperty;
        private readonly string _title;
        private readonly ReorderableList _list;

        public Action<Rect, int> DrawElementCallback;
        public Func<int, float> ElementHeightCallback;
        public Action OnAddDropdownCallback;
        
        private static Action<ReorderableList> _clearCache;
        private static Action<ReorderableList> ClearCache
        {
            get
            {
                if (_clearCache == null)
                {
                    var clearCacheMethod = typeof(ReorderableList).GetMethod("ClearCache", BindingFlags.Instance | BindingFlags.NonPublic);
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
                    if (expandedProperty.boolValue)
                        ReorderableList.defaultBehaviours.DrawFooter(rect, _list); // This will prevent add and remove buttons from being drawn when the list is folded.
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
    }
}