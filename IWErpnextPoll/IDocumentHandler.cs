namespace IWErpnextPoll
{
    interface IDocumentHandler
    {
        IDocumentHandler SetNext(IDocumentHandler handler);

        object Handle(object request);
    }
}
