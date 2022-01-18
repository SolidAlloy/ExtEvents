namespace ExtEvents.Editor
{
    using System.Collections.Generic;
    using SolidUtilities.Editor;
    using UnityEditor;
    using UnityEngine;

    public static class ExtEventHelper
    {
        public static IEnumerable<(SerializedProperty extEventProperty, BaseExtEvent extEvent)> FindExtEvents(Object obj)
        {
            var serializedObject = new SerializedObject(obj);
            var prop = serializedObject.GetIterator();

            if (!prop.Next(true))
                yield break;

            do
            {
                if (prop.name != nameof(BaseExtEvent._responses) || prop.GetObjectType() != typeof(SerializedResponse[]))
                    continue;

                var extEventProperty = prop.GetParent();
                var extEvent = extEventProperty.GetObject<BaseExtEvent>();
                yield return (extEventProperty, extEvent);
            }
            while (prop.NextVisible(true));
        }
    }
}