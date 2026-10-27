namespace Pricely.Modules.Pricing.Application.WebResearch;

/// <summary>
/// The pages a web search returned recently, per research (a conversation, or a user outside an agent). Only those pages,
/// and the pages of the followed merchants, may be read by <c>read_web_page</c> (ADR-027): a page cannot make the model
/// read an address of its choosing, which could carry data of the conversation to a third party. Implemented in
/// Infrastructure.
/// </summary>
public interface ISearchedPages
{
    void Remember(string research, IEnumerable<Uri> addresses);

    bool WasFound(string research, Uri address);
}
