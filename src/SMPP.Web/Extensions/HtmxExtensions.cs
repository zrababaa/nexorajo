using Microsoft.AspNetCore.Mvc;

namespace SMPP.Web.Extensions;

/// <summary>
/// Small helpers for the htmx-boosted front end. Most navigation needs nothing beyond hx-boost
/// (it follows the existing 302 redirects transparently), so these only cover the two cases where
/// a controller needs to branch: rendering a partial for an htmx request, and forcing a full
/// client-side redirect from an action that isn't a normal boosted form/link submit.
/// </summary>
public static class HtmxExtensions
{
    public static bool IsHtmx(this HttpRequest request) => request.Headers["HX-Request"] == "true";

    /// <summary>
    /// True only for an htmx request explicitly targeting the given fragment container id - e.g. a
    /// filter form's hx-get, or a boosted link whose ancestor sets hx-target to that id.
    /// False for a boosted top-level navigation (sidebar link, typed URL), whose target defaults to
    /// &lt;body&gt; and therefore needs the full layout, not just the fragment. IsHtmx() alone can't
    /// tell these apart: htmx sends "HX-Request: true" for both.
    /// </summary>
    public static bool IsHtmxFragment(this HttpRequest request, string targetId) =>
        request.IsHtmx() && request.Headers["HX-Target"] == targetId;

    /// <summary>
    /// Redirects via the HX-Redirect response header when the request came from htmx (so the
    /// client does a full navigation instead of swapping the redirect response into the current
    /// target), otherwise falls back to a normal RedirectToAction.
    /// </summary>
    public static IActionResult HtmxRedirectToAction(this Controller controller, string actionName)
    {
        if (controller.Request.IsHtmx())
        {
            controller.Response.Headers["HX-Redirect"] = controller.Url.Action(actionName);
            return new EmptyResult();
        }

        return controller.RedirectToAction(actionName);
    }
}
