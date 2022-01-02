namespace ExtEvents.Editor
{
    using System.Collections.Generic;
    using SolidUtilities.Editor.Extensions;
    using SolidUtilities.UnityEditorInternals;
    using UnityEditor;
    using UnityEngine;

    public static class BuiltResponsesCreator
    {
        public static IEnumerable<(SerializedProperty extEventProperty, ExtEvent extEvent)> FindExtEvents(Object obj)
        {
            var serializedObject = new SerializedObject(obj);

            var prop = serializedObject.GetIterator();

            if (prop.Next(true)) // TODO this probably doesn't iterate over array properties, but there may be an ExtEvent[] field, so check that.
            {
                do
                {
                    // TODO is getfieldinfoandtype faster?
                    if (prop.name == nameof(ExtEvent._responses) && prop.GetObjectType() == typeof(SerializedResponse[]))
                    {
                        // get parent property
                        // get the extEvent object
                        var resultProperty = prop.GetParent();
                        (var fieldInfo, var _) = resultProperty.GetFieldInfoAndType();
                        var extEvent = (ExtEvent) fieldInfo.GetValue(resultProperty.serializedObject.targetObject); // TODO improve so that it works with nested hierarchies.
                        yield return (resultProperty, extEvent);
                    }
                } while (prop.NextVisible(true));
            }
        }
    }
}