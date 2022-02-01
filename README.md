# ExtEvents
A better replacement for UnityEvents

ExtEvents is a package that should replace UnityEvents in all your projects and relieve you from all the limitations you felt with UnityEvents.

### Feature Comparison

|                            | UnityEvent                                                   | UltEvent                                                     | ExtEvent                                                     |
| -------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| Serialized parameter types | <span style="color:red">A small number of types: Object, bool, string, etc.</span> | <span style="color:orange">More types: Enum, Vector2, Rect</span> | <span style="color:#0bce00">**Any serializable parameter shows up in the inspector with the correct UI**</span> |
| Number of parameters       | <span style="color:red">0 or 1</span>                        | <span style="color:#0bce00">Up to 4</span>                   | <span style="color:#0bce00">**Up to 4**</span>               |
| Static methods             | <span style="color:red">No</span>                            | <span style="color:#0bce00">Yes</span>                       | <span style="color:#0bce00">**Yes**</span>                   |
| Non-void methods           | <span style="color:red">No</span>                            | <span style="color:orange">Yes</span>                        | <span style="color:#0bce00">**Yes (+ smart filtration system)**</span> |
| Non-public methods         | <span style="color:red">No</span>                            | <span style="color:orange">Yes</span>                        | <span style="color:#0bce00">**Yes (+ flexible options to show/hide such methods)**</span> |
| Performance                | <span style="color:#0bce00">Fast</span>                      | <span style="color:red">Very Slow</span>                     | <span style="color:#0bce00">**Fast**</span>                  |
| Method Dropdown            | <span style="color:red">All methods in one GenericMenu list</span> | <span style="color:orange">GenericMenu with a few sub-menus</span> | <span style="color:#0bce00">**Scrollable list with a search field and folders**</span> |

## Installation

## Quick Start

Declare an event like this:

```csharp
public class TestBehaviour : Monobehaviour
{
    [SerializeField] private ExtEvent _testEvent;
}
```

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\no-elements.png" alt="no-elements" style="zoom:80%;" />

Press "+i" to add an instance listener:

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\one-empty-instance-listener.png" alt="one-empty-instance-listener" style="zoom:80%;" />

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

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\add-instance-listener.gif" alt="add-instance-listener" style="zoom:80%;" />

Try choosing EventWithOneArg(). A field for the argument will appear where you will be able to fill in the data:

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\string-argument.png" alt="string-argument" style="zoom:80%;" />

Let's add a static listener by pressing "+s":

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\static-listener.png" alt="static-listener" style="zoom:80%;" />

The process is basically the same, except that instead of dragging a target you choose a type from the dropdown:

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\type-dropdown.png" alt="type-dropdown" style="zoom: 67%;" />

But let's say you have a string event and want to pass the value dynamically instead of having the same serialized value:

```csharp
public class TestBehaviour : Monobehaviour
{
    [SerializeField] private ExtEvent<string> _stringEvent;
}
```

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\one-dynamic-arg.png" alt="one-dynamic-arg" style="zoom:80%;" />

The argument is shown as dynamic now, so every time you pass a value to _stringEvent, it will be sent to EventWithOneArg(). Should you need it preset in editor, you can switch the argument to serialized:

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\dynamic-serialized-dropdown.png" alt="dynamic-serialized-dropdown" style="zoom:80%;" />

The "Arg1" dropdown is disabled now because there is no other argument that can be passed to 'arg', only the first argument of _stringEvent. But what if we have a choice? Let's imagine we have a player creation menu and want to add the player username to the database:

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

The _onPlayerCreated has three arguments but we want only one passed to the AddToDatabase method:

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\dynamic-argument-dropdown.png" alt="dynamic-argument-dropdown" style="zoom:80%;" />

Well, we can do that, and moreover we can choose which specific argument we want to pass. However, it is not clear which arguments we are choosing from. Is username the first argument passed to the event, or second? We can clear out the confusion by declaring the argument names:

```csharp
public class TestBehaviour : Monobehaviour
{
    [EventArguments("Username", "Password", "Region")]
    [SerializeField] private ExtEvent<string, string, string> _onPlayerCreated;
}
```

And voila, the argument name has been replaced:

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\new-arg-name.png" alt="new-arg-name" style="zoom:80%;" />

And if we want to pass something else to the method, we can choose another argument:

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\arg-name-dropdown.png" alt="arg-name-dropdown" style="zoom:80%;" />

## Performance

The performance of events depends on the number of arguments you pass through them. When invoking a method with 0 arguments, ExtEvent is even faster than UnityEvent! The performance of ExtEvent is less than 2 times lower when building a project with the [faster build time](https://docs.unity3d.com/2021.2/Documentation/ScriptReference/EditorUserBuildSettings-il2CppCodeGeneration.html) setting. The performance of UnityEvent is better in some cases because of how limited its feature set is. You can see that ExtEvents are faster than UltEvents in all cases while providing even more features! Still, performance of ExtEvents should not be your concern until you are performing hundreds of thousands of calls per frame.

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\performance-graph.png" alt="performance-graph" style="zoom: 67%;" />

When an event is invoked for the first time, it needs to initialize itself and it takes slightly more time than in the subsequent calls. ExtEvent is initialized much more quickly than UltEvent and slightly slower than UnityEvent. Moreover, initialization of other ExtEvents with similar methods will take much less time. The initialization will not cause stutter unless there are hundreds of events being called for the first time in the same frame. However, unlike UnityEvent and UltEvent, ExtEvent exposes the `Initialize()` method, so should you notice any impact of initialization on the performance, you can call it in Awake() or Start() when a level is loading.

<img src="D:\UnityProjects\Packages Test\Packages\ExtEvents\.images\invocation-graph.png" alt="invocation-graph" style="zoom:80%;" />
