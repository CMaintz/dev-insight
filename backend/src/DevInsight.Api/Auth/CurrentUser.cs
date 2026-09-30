namespace DevInsight.Api.Auth;

/// <summary>
/// The signed-in user, bound straight into endpoint handlers (<c>(CurrentUser me, …) =&gt; …</c>) so no handler
/// digs the id out of the claims itself. Only valid on endpoints that require authorization.
/// </summary>
public readonly record struct CurrentUser(Guid Id)
{
    public static ValueTask<CurrentUser> BindAsync(HttpContext context) =>
        ValueTask.FromResult(new CurrentUser(context.User.GetUserId()));
}
