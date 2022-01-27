# ExtEvents
A better replacement for UnityEvents

ExtEvents is a package that should replace UnityEvents in all your projects and relieve you from all the limitations you felt with UnityEvents.

### Feature Comparison

|                            | UnityEvent                                          | UltEvent                         | ExtEvent                                                     |
| -------------------------- | --------------------------------------------------- | -------------------------------- | ------------------------------------------------------------ |
| Serialized parameter types | A small number of types: Object, bool, string, etc. | More types: Enum, Vector2, Rect  | Any serializable parameter shows up in the inspector with the correct UI |
| Number of parameters       | 0 or 1                                              | Up to 4                          | Up to 4                                                      |
| Static methods             | No                                                  | Yes                              | Yes                                                          |
| Non-void methods           | No                                                  | Yes                              | Yes (+ smart filtration system)                              |
| Non-public methods         | No                                                  | Yes                              | Yes (+ flexible options to show/hide such methods)           |
| Performance                | Fast                                                | Very Slow                        | Fast                                                         |
| Method Dropdown            | All methods in one GenericMenu list                 | GenericMenu with a few sub-menus | Scrollable list with a search field and folders              |

### Performance

