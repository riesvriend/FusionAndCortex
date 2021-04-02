Playground project combining [Stl.Fusion](https://github.com/servicetitan/Stl.Fusion) and [Cortex.Net](https://github.com/jspuij/Cortex.Net).

See the project [Blazorise](https://github.com/riesvriend/FusionAndCortex/tree/master/samples/Blazorise). It is an adaptation of Fusion's Blazorise todo list sample.

Key Benefits
* End-to-end Real Time, from database to multiple hosts and multiple Blazor clients; thanks to Fusion.
* The Cortex Store architecture enables:
  * Isolation of UI state and update logic into an observable model. 
  * Efficient rendering without the need to implement Blazor's [ShouldComponentRender](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/rendering?view=aspnetcore-5.0) in the components.
  * Testing the stores is a good substitute for testing the actual Blazor components and much easier.
  * Good scalability of complex user interfaces. All components can be simple, 'Pure'; free of complex code and side-effects or inter-component rendering dependencies. All code is testable and delegated away into the Actions in the stores.

Done:
* created observable stores: [AppStore](https://github.com/riesvriend/FusionAndCortex/blob/master/samples/Blazorise/UI/Stores/AppStore.cs) and [TodoPageStore](https://github.com/riesvriend/FusionAndCortex/blob/master/samples/Blazorise/UI/Stores/TodoPageStore.cs) 
* AppStore has a simple timer and shows as a [clock](https://github.com/riesvriend/FusionAndCortex/blob/master/samples/Blazorise/UI/Shared/LeftBarClock.razor) in the [main layout](https://github.com/riesvriend/FusionAndCortex/blob/master/samples/Blazorise/UI/Shared/MainLayout.razor)
* TodoPageStore contains the client-side state for the todo page, such as current page size and page marker. It 
  also contains a Fusion LiveState reference that is subscribed to changes impacting the [TodoPageResponse](https://github.com/riesvriend/FusionAndCortex/blob/b6f31480bfb6b856e8423feba6cd61bf3dc1fa80/samples/Blazorise/Abstractions/ITodoService.cs#L17). This DTO contains
  the currently visible todo-items and it is auto-requeried by Fusion when update command
  commands are executed that affect related data in the database.
* Extracted all logic from the [todopage.razor](https://github.com/riesvriend/FusionAndCortex/blob/master/samples/Blazorise/UI/Pages/TodoPage.razor) and [TodoItem.razor](https://github.com/riesvriend/FusionAndCortex/blob/master/samples/Blazorise/UI/Shared/TodoItem.razor), making it a render-only component
*  Testers still to be implemented.

 Issues and Todos:
 1. When more than 5 (the default page size) todo's exist, The MORE button appears. It invalidates the LiveState
    which should trigger a recompute/fetch of the TodoPageResponse. However, this somehow is broken. 
    Adding or updating an item will subsequently retrigger and show the increased page size. Needs analysis.
 2. WebAssembly mode fails on DI injecting ISessionProvider into the TodoPageStore. Needs analysis.
    Debug output: Cannot resolve scoped service 'Stl.Fusion.Authentication.ISessionProvider' from root provider.
 3. The Fusion Auth state is not yet live-plugged to the TodoPageStore, hence after login, the Todolist does not 
    immediately appear yet.
 4. Fusion query and command errors are not tested and probably not working yet in the TodoPageStore  
 
