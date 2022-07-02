# ExtEvents
A better replacement for UnityEvents

[![openupm](https://img.shields.io/npm/v/com.solidalloy.extevents?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.solidalloy.extevents/)

ExtEvents is a package that should replace UnityEvents in all your projects and free you from all the limitations you had with UnityEvents.

### Feature Comparison

|                              | UnityEvent                                              | UltEvent                                  | ExtEvent                                                     |
| ---------------------------- | ------------------------------------------------------- | ----------------------------------------- | ------------------------------------------------------------ |
| Serialized parameter types   | :x: A small number of types: Object, bool, string, etc. | :warning:More types: Enum, Vector2, Rect  | :white_check_mark: **Any serializable parameter shows up in the inspector with the correct UI** |
| Number of parameters         | :x: 0 or 1                                              | :white_check_mark: Up to 4                | :white_check_mark: **Up to 4**                               |
| Static methods               | :x: No                                                  | :white_check_mark: Yes                    | :white_check_mark: **Yes**                                   |
| Non-void methods             | :x: No                                                  | :warning:Yes                              | :white_check_mark: **Yes (+ smart filtration system)**       |
| Non-public methods           | :x: No                                                  | :warning:Yes                              | :white_check_mark: **Yes (+ flexible options to show/hide such methods)** |
| Performance                  | :white_check_mark: Fast                                 | :x: Very Slow                             | :white_check_mark: **Very Fast**                             |
| Method Dropdown              | :x: All methods in one GenericMenu list                 | :warning:GenericMenu with a few sub-menus | :white_check_mark: **Scrollable list with a search field and folders** |
| Finding renamed types        | :x: No                                                  | :x: No                                    | :white_check_mark: **Yes**                                   |
| Implicit conversions support | :x: No                                                  | :x: No                                    | :white_check_mark: **Yes**                                   |

## Installation

:heavy_exclamation_mark: Before installing the package, please disable the **Assembly Version Validation** option in **Player Settings**.
:heavy_exclamation_mark: If you see compilation errors in Unity 2021.1 and below, switch from **NET Standard 2.0** to **NET 4.x**.

### Install with OpenUPM

Once you have the [OpenUPM cli](https://github.com/openupm/openupm-cli#installation), run the following command:

```openupm install com.solidalloy.extevents```

Or if you don't have it, add the scoped registry to manifest.json with the desired dependency semantic version: 

```json
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.solidalloy",
        "com.openupm",
        "org.nuget"
      ]
    }
  ],
  "dependencies": {
    "com.solidalloy.extevents": "1.5.1"
  },

```

### Install via Package Manager

Project supports Unity Package Manager. To install the project as a Git package do the following:

1. In Unity, open **Project Settings** -> **Package Manager**.
2. Add a new scoped registry with the following details:
   - **Name**: package.openupm.com
   - **URL**: https://package.openupm.com
   - Scope(s):
     - com.openupm
     - com.solidalloy
     - org.nuget
3. Hit **Apply**.
4. Go to **Window** -> **Package Manager**.
5. Press the **+** button, *Add package from git URL*.
6. Enter **com.solidalloy.extevents**, press **Add**.

## Quick Start

Declare an event like this:

```csharp
public class TestBehaviour : Monobehaviour
{
    [SerializeField] private ExtEvent _testEvent;
}
```

<img src="/.images/no-elements.png" alt="no-elements"  />

Press "+i" to add an instance listener:

<img src="/.images/one-empty-instance-listener.png" alt="one-empty-instance-listener"  />

Let's declare a couple of methods in TestBehaviour:

```csharp
public class TestBehaviour : Monobehaviour
{
    [SerializeField] private ExtEvent _testEvent;
    
    public void EventWithNoArgs() { }
    
    public void EventWithOneArg(string arg) { }
    
    public static void StaticMethod() { }
}
```

Drag and drop TestBehaviour into the target field. A dropdown with methods will be shown immediately, where you can choose a method. Let's choose `EventWithNoArgs()` for now. You can already see how many unnecessary clicks the plugin saves compared to UnityEvent.

<img src="/.images/add-instance-listener.gif" alt="add-instance-listener" style="zoom:80%;" />

Try choosing `EventWithOneArg()`. A field for the argument will appear where you will be able to fill in the data:

<img src="/.images/string-argument.png" alt="string-argument"  />

Let's add a static listener by pressing "**+s**":

<img src="/.images/static-listener.png" alt="static-listener"  />

The process is basically the same, except that instead of dragging a target you choose a type from the dropdown:

<img src="/.images/type-dropdown.png" alt="type-dropdown"  />

But let's say you have a string event and want to pass the value dynamically instead of having the same serialized value:

```csharp
public class TestBehaviour : Monobehaviour
{
    [SerializeField] private ExtEvent<string> _stringEvent;
}
```

<img src="/.images/one-dynamic-arg.png" alt="one-dynamic-arg"  />

The argument is shown as dynamic now, so every time you pass a value to `_stringEvent`, it will be sent to `EventWithOneArg()`. Should you need it preset in editor, you can switch the argument to serialized by pressing on the **d** button:

<img src="/.images/dynamic-serialized-dropdown.png" alt="dynamic-serialized-dropdown"  />

The "*Arg1*" dropdown is disabled now because there is no other argument that can be passed to `arg`, only the first argument of `_stringEvent`. But what if we have a choice? Let's imagine we have a player creation menu and want to add the player username to the database:

```csharp
public class TestBehaviour : Monobehaviour
{
    [SerializeField] private ExtEvent<string, string, string> _onPlayerCreated;

    public void AddPlayer(string username, string password, string region)
    {
        _onPlayerCreated.Invoke(username, password, region);
    }

    public void AddToDatabase(string username) { }
}
```

The `_onPlayerCreated` has three arguments but we want only one passed to the `AddToDatabase` method:

<img src="/.images/dynamic-argument-dropdown.png" alt="dynamic-argument-dropdown"  />

Well, we can do that, and moreover we can choose which specific argument we want to pass. However, it is not clear which arguments we are choosing from. Is username the first argument passed to the event, or second? We can clear out the confusion by declaring the argument names:

```csharp
public class TestBehaviour : Monobehaviour
{
    [EventArguments("Username", "Password", "Region")]
    [SerializeField] private ExtEvent<string, string, string> _onPlayerCreated;
}
```

And voila, the argument name has been replaced:

<img src="/.images/new-arg-name.png" alt="new-arg-name"  />

And if we want to pass something else to the method, we can choose another argument:

<img src="/.images/arg-name-dropdown.png" alt="arg-name-dropdown"  />

Finally, I'll show you that we can serialize any serializable argument, even the custom ones. Let's create it:

```csharp
public class TestBehaviour : Monobehaviour
{
    [SerializeField] private ExtEvent _testEvent;
    
    public void MethodWithCustomArg(CustomSerializableClass customArg) { }
}

[Serializable]
public class CustomSerializableClass
{
    public string StringField;
    public string IntField;
}
```

The  serializable class showed up correctly. And if you create a custom drawer for it, it will work too.

<img src="/.images/custom-serializable-arg.png" alt="custom-serializable-arg"  />

## Implicit Conversions

The package supports implicit conversion of arguments that are passed dynamically. For example, when you have `ExtEvent<int>` but want to answer to it with a method that accepts float, you can do that. This works for all numerical conversions of built-in types (e.g. int => float, float => double, etc.) and types with defined [implicit operators](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators). If you don't want to define an implicit operator but still want to convert the type of the argument on the fly, or you simply don't have access to the internal structure of the class, you can declare a custom converter. Just inherit from `Converter<TFrom, TTo>` and define your conversion:

```csharp
public class FloatToIntConverter : Converter<float, int>
{
    protected override int Convert(float from)
    {
        return Mathf.RoundToInt(from);
    }
}
```

The declared type will be automatically used by ExtEvents, no need to register it anywhere.

## Project Settings and Preferences

### Project Settings

**Show invocation warning** (*true* by default) - Whether a warning should be logged when an event is invoked but the listener property or method is missing.

**Include internal methods** (*false* by default) - Whether to include internal methods and properties in the methods dropdown when choosing a listener in ExtEvent.

**Include private methods** (*false* by default) - Whether to include private and protected methods and properties in the methods dropdown when choosing a listener in ExtEvent.

**Build preprocessor callback order** (*0* by default) - When a build is initiated with IL2CPP and '*Faster runtime*' chosen, ExtEvents needs to generate some code for events to work properly. You can change the callback order of the code generation here if it conflicts with other preprocessors.

Note that if you want to include a particular internal or private method into the dropdown, I recommend you use the `[ExtEventListener]` attribute for the method instead, so that the dropdown is not crammed with the methods you wouldn't use in events.

### Preferences

**Nicify arguments names** (*true* by default) - Replace the original argument names (e.g. "currentPlayer") with more readable labels - "Current Player".

## Attributes

**EventArguments** - use this attribute over an ExtEvent field to specify the names of the arguments this event sends out to listeners. This is just for the UI to be more understandable and doesn't affect runtime.

**ExtEventListener** - place it over an internal or private method so that it appears in the method dropdown and users can choose it as a response to an event. This is a better solution than allowing all internal and private methods to show up in the dropdown.

## Method Filtering

Methods and properties of the type you chose are shown in the dropdown, and are divided into folders, where instance and static methods are two separate folders. Fields are not included because invoking them is a big performance hit.

Instance listeners show both their instance and static methods. It is done for convenience, so that you don't need to replace instance listener with a static one if you just need to change the method inside the same type. Static listeners, on the other hand, show only static methods.

When a list of methods is composed, methods that have at least one argument that can't be serialized AND can't be passed from the event (be dynamic) are filtered out of the list.

Only the public methods and properties are shown by default. If you need an internal or private method to show up in the list, mark it with the `ExtEventListener` attribute. Should you want all internal or private methods to be included in the method dropdown, you can enable it in the Project Settings.

Methods that return a value (not void) are also included in the list. But if your method doesn't change the state of the class and just operates on the passed arguments, mark it with the [Pure](https://www.jetbrains.com/help/resharper/Reference__Code_Annotation_Attributes.html#PureAttribute) attribute and it will be excluded from the list. Moreover, it's just a nice way to annotate your code so that the code editor warns you of the incorrect usage of methods.

Finally, a method is not allowed to have more than 4 arguments. This is mainly done to limit the amount of work needed to generate the IL2CPP code, but also in order not to clutter the method lists. It is a general rule of good code design to have no more than 3-4 arguments.

## Warnings

You may encounter a few warnings while working with the package that will help you find the broken events.

> Tried to invoke a listener to an event but the declaring type is missing: {typeName}

Appears when a static method is invoked by the type that contained that method, but the type was removed. By the way, the serialization of target and argument types is backed by [TypeReferences](https://github.com/SolidAlloy/ClassTypeReference-for-Unity). So as long as the file name is the same as the type name, you can rename the type without worrying that a reference to it will be lost.

> Tried to invoke a listener to an event but the target is missing

Appears when an instance method is invoked but the MonoBehaviour or ScriptableObject target went missing.

> Tried to invoke a listener to an event but the method {typeName}.{methodName} is missing.

Means the method signature was not found. It may appear when the method name was removed or its arguments were changed, so the signature is no longer same. If the issue is because of one of the argument types went missing, you will receive the following warning above this one:

> Tried to invoke a listener to an event but some of the argument types are missing: {argTypeName1, argTypeName2, ...}.

> Tried to invoke a method {method} but there was no code generated for it ahead of time.

Means that when the code necessary to invoke listeners was generated before a build, it didn't find this listener, or that you added a new listener to an addressable after the build was made, so the build doesn't have this method signature generated. If it's the first case, you can report it as a bug. You can get more info on this in the [IL2CPP Code Generation](#il2cpp-code-generation) section.

> Tried to invoke a method with a serialized argument of type {valueType} but there was no code generated for it ahead of time.

It's a similar issue but in this case the code for the serialized argument type was not generated because a listener it was in was not found by the code generation algorithm. The reasons for this are the same as the previous case.

## IL2CPP Code Generation

ExtEvents needs to construct generic types/methods through reflection at runtime, that's why it is faster than UltEvents. However, it introduces a few issues for IL2CPP. IL2CPP needs to generate concrete implementation for all generic types/methods that contain value types. That's because IL2CPP needs to know the size of structures that are passed to methods as arguments. For reference types the size is irrelevant as their pointers are passed instead, but different code needs to be generated for each value type. ExtEvents generates the necessary C# code so that it is translated properly by IL2CPP, so you don't need to worry about it. The generation process occurs automatically before an IL2CPP build with "Faster runtime" option enabled. For "Smaller build" option, IL2CPP doesn't generate concrete implementations of generic methods, so there's no generation from the ExtEvents side too.

The above is just implementation details, but it leads to a warning you may receive at runtime: "Tried to invoke a method {method} but there was no code generated for it ahead of time." That means the ExtEvents didn't find your ExtEvent for some reason and didn't include it when generating code for the build. Unless there's a bug in ExtEvents, this warning may only occur in a situation when built the project first, then created a new addressable asset, and used a new listener there that had a new set of arguments that was never used before. The code generation ran before the build, and the build doesn't know of this new set of arguments that was added to an addressable later. So should you stumble upon such a warning, firstly check if your addressables used new listeners.

## Code Stripping

I tested how the code stripping affects the events and I was not able to make Unity strip the methods that were used by ExtEvents, even though they weren't used anywhere else in the code. It might be that IL2CPP finds the method names in serialized assets and doesn't strip them. Nevertheless, if a method you used as a response to an ExtEvent was stripped in build, let me know as I have code to force IL2CPP to not strip those methods, it's just it hasn't been needed yet.

## Rider Integration

You may know that Rider has a cool feature of marking the methods used by UnityEvents and finding which game objects use the method as a response to events. I haven't tried to create a similar plugin because I've never worked with Rider plugins before but I suppose we can take a look at the source code of the Rider plugin [here](https://github.com/JetBrains/resharper-unity) and copy specific parts of it responsible for the UnityEvent features, adapting it for ExtEvents. If anyone can contribute to the project by implementing such a plugin, it would be super cool!

## Working with ExtEvents from code

### Working with persistent listeners

```csharp
// You can manage persistent listeners of ExtEvent easily. This is how you can add a new persistent listener:
_testEvent.AddPersistentListener(PersistentListener.FromInstance((Action) EventWithNoArgs, this, UnityEventCallState.RuntimeOnly));

// When creating a persistent listener for a method with arguments, you need to specify whether you want to create dynamic or serialized arguments.
// In this case, we create a dynamic argument and make it accept a value from the first argument of _testEvent (hence index 0).
_testEvent.AddPersistentListener(PersistentListener.FromInstance((Action<string>) EventWithOneArg, this, UnityEventCallState.RuntimeOnly, PersistentArgument.CreateDynamic<string>(0)));

// Or we can pass a serialized argument with its predetermined value. Note that callState is not a required argument and defaults to RuntimeOnly.
_testEvent.AddPersistentListener(PersistentListener.FromInstance((Action<string>) EventWithOneArg, this, arguments: PersistentArgument.CreateSerialized("test")));

// When creating a persistent argument for a static method, pass null into the target parameter.
var newListener = PersistentListener.FromStatic((Action) EventWithNoArgs);
_testEvent.AddPersistentListener(newListener);

// PersistentListener exposes a bunch of read-only properties. If you need to change the listener, remove it from the event and add a new one.
Debug.Log($"target: {newListener.Target}, method: {newListener.MethodName}");

// You can access other persistent listeners easily.
var firstListener = _testEvent.PersistentListeners[0];

// Remove listeners like that.
_testEvent.RemovePersistentListener(newListener);
_testEvent.RemovePersistentListenerAt(0);

// Also, persistent arguments are exposed, so you can check their values, for example.
Debug.Log(firstListener.PersistentArguments[0].SerializedValue);

// If you need the changes to persistent listeners to be saved, don't forget to mark the object that contains the event as dirty.
#if UNITY_EDITOR
EditorUtility.SetDirty(this);
#endif
```

### Working with dynamic listeners

Of course, you can work with ExtEvents with an interface identical to default C# events:
```csharp
[SerializeField] private ExtEvent _testEvent;

private void Start()
{
	_testEvent += TestCallback;
}

private void OnDestroy()
{
    _testEvent -= TestCallback;
}

private void TestCallback()
{
    Debug.Log("test");
}
```

You cannot shuffle around arguments though, like you can do with persistent listeners.

## Performance

ExtEvent is twice faster than UnityEvent in most of the use cases, and is at least on-par in the worst case scenario, leaving UltEvent far behind.
*Faster Runtime* and *Faster Build Time* is related to the option you can choose in the Build Settings window when building with IL2CPP in Unity 2021+. Those options use different approach to code generation which results in the difference in performance.

<img src="/.images/performance-graph.png" alt="performance-graph"  />

When an event is invoked for the first time, it needs to initialize itself and it takes slightly more time than in the subsequent calls. ExtEvent is initialized much more quickly than UltEvent and slightly slower than UnityEvent. Moreover, initialization of other ExtEvents with similar method signatures will take much less time. The initialization will not cause stutter unless there are hundreds of events being called for the first time in the same frame. However, unlike UnityEvent and UltEvent, ExtEvent exposes the `Initialize()` method, so should you notice any impact of initialization on the performance, you can call it in Awake() or Start() when a level is loading.

<img src="/.images/invocation-graph.png" alt="invocation-graph"  />
