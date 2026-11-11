namespace DWIMS.Service.Analysis;

public interface IFormHeuristicsEngine
{
    PageProposals Propose(PageContent page);
}