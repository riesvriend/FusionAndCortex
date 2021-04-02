using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Cortex.Net.Blazor
{
    /// <summary>
    /// Code from https://github.com/jspuij/Cortex.Net/pull/38#pullrequestreview-625493762
    /// Sometimes it is hard to apply Observer to a part of the rendering,
    /// for example because you are rendering inside a RenderFragment,
    /// and you don't want to extract a new component to be able to mark it as observer.
    /// In those cases <Observer /> comes in handy. It takes a child content that
    /// is automatically re-rendered if any referenced observables change.
    /// </summary>
    [Observer]
    public class Observer : ComponentBase
    {
        /// <summary>
        /// Gets or sets the wrapped child content of the component.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <inheritdoc/>
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            this.ChildContent?.Invoke(builder);
        }
    }
}